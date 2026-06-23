/*
 * Copyright 2026 Philterd, LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections;
using System.Reflection;
using System.Text.Json;
using Phileas.Policy;
using Phileas.Policy.Filters;
using PhilterData;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// Edits redaction policies using the Phileas policy model. Filters are discovered by
    /// reflection and laid out in category groups that flow into columns; actions live on
    /// a toolbar, with a search box and an enabled-count indicator.
    /// </summary>
    public sealed class PolicyEditorForm : Form
    {
        private sealed class FilterRow
        {
            public required string Name;
            public required Control Panel;
            public required CheckBox CheckBox;
            public required GroupBox Group;
        }

        private readonly PolicyRepository _repo;
        private PhileasPolicy? _policy;
        private bool _loading;
        private bool _dirty;
        private string? _currentPolicyName;
        private bool _suppressComboChange;

        private readonly ToolStrip _toolStrip = new() { GripStyle = ToolStripGripStyle.Hidden };
        private readonly ToolStripComboBox _policyCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, AutoSize = false, Size = new Size(190, 23) };
        private readonly ToolStripButton _new = new() { Text = "New" };
        private readonly ToolStripButton _save = new() { Text = "Save", Enabled = false };
        private readonly ToolStripButton _saveAs = new() { Text = "Save As", Enabled = false };
        private readonly ToolStripButton _delete = new() { Text = "Delete", Enabled = false };
        private readonly ToolStripButton _viewJson = new() { Text = "View JSON", Enabled = false };
        private readonly ToolStripTextBox _search = new() { AutoSize = false, Size = new Size(170, 23) };

        private readonly StatusStrip _statusStrip = new();
        private readonly ToolStripStatusLabel _countLabel = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };

        private readonly FlowLayoutPanel _filters = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = true,
            AutoScroll = true,
            Padding = new Padding(10)
        };

        private readonly List<Action> _loaders = new();
        private readonly List<FilterRow> _rows = new();
        private readonly List<GroupBox> _groups = new();

        public PolicyEditorForm(PolicyRepository repo)
        {
            _repo = repo;

            Text = "Policy Editor";
            StartPosition = FormStartPosition.CenterScreen;
            // Fixed size large enough to show every filter group (~3 columns) without scrolling.
            ClientSize = new Size(960, 780);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            BuildLayout();
            RegisterFilters();
            UpdateCount();

            _policyCombo.SelectedIndexChanged += (_, _) => OnPolicySelectionChanged();
            _new.Click += OnNew;
            _save.Click += OnSave;
            _saveAs.Click += OnSaveAs;
            _delete.Click += OnDelete;
            _viewJson.Click += OnViewJson;
            _search.TextChanged += (_, _) => ApplySearch(_search.Text);

            ModernTheme.Apply(this);

            Load += (_, _) => ReloadPolicyList("default");
        }

        private void BuildLayout()
        {
            _new.Image = ModernTheme.CreateGlyphImage("\uE710", 16, ModernTheme.Text);     // Add
            _save.Image = ModernTheme.CreateGlyphImage("\uE74E", 16, ModernTheme.Accent);  // Save
            _saveAs.Image = ModernTheme.CreateGlyphImage("\uE792", 16, ModernTheme.Text);  // SaveAs
            _delete.Image = ModernTheme.CreateGlyphImage("\uE74D", 16, ModernTheme.Text);  // Delete
            _viewJson.Image = ModernTheme.CreateGlyphImage("\uE943", 16, ModernTheme.Text); // Code

            _toolStrip.Items.Add(new ToolStripLabel("Policy:"));
            _toolStrip.Items.Add(_policyCombo);
            _toolStrip.Items.Add(new ToolStripSeparator());
            _toolStrip.Items.AddRange(new ToolStripItem[] { _new, _save, _saveAs, _delete, new ToolStripSeparator(), _viewJson });

            // Search box, right-aligned (add the box first so it sits rightmost).
            _search.Alignment = ToolStripItemAlignment.Right;
            _toolStrip.Items.Add(_search);
            _toolStrip.Items.Add(new ToolStripLabel("Search:") { Alignment = ToolStripItemAlignment.Right });

            _statusStrip.Items.Add(_countLabel);

            Controls.Add(_filters);
            Controls.Add(_statusStrip);
            Controls.Add(_toolStrip);
            SetEditingEnabled(false);
        }

        // --- Filter registration (reflection + category grouping) ------------

        private void RegisterFilters()
        {
            (string Category, string[] Props)[] categories =
            {
                ("Personal", new[] { "FirstName", "Surname", "Age" }),
                ("Contact", new[] { "EmailAddress", "PhoneNumber", "PhoneNumberExtension" }),
                ("Location", new[] { "City", "County", "State", "StateAbbreviation", "ZipCode", "StreetAddress" }),
                ("Financial", new[] { "CreditCard", "BankRoutingNumber", "IbanCode", "BitcoinAddress", "Currency" }),
                ("Identifiers", new[] { "Ssn", "DriversLicense", "PassportNumber", "Vin", "TrackingNumber" }),
                ("Technical", new[] { "IpAddress", "MacAddress", "Url" }),
                ("Medical", new[] { "Hospital" }),
                ("Other", new[] { "Date" }),
            };

            Dictionary<string, PropertyInfo> discovered = typeof(Identifiers)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => typeof(AbstractPolicyFilter).IsAssignableFrom(p.PropertyType))
                .ToDictionary(p => p.Name);

            var placed = new HashSet<string>();
            foreach ((string category, string[] props) in categories)
            {
                List<PropertyInfo> members = props
                    .Where(discovered.ContainsKey)
                    .Select(n => discovered[n])
                    .ToList();
                if (members.Count == 0)
                {
                    continue;
                }
                FlowLayoutPanel inner = NewGroup(category);
                foreach (PropertyInfo prop in members)
                {
                    AddFilterRow(inner, prop);
                    placed.Add(prop.Name);
                }
            }

            // Any filter not assigned to a category goes under "Other".
            List<PropertyInfo> leftovers = discovered.Values
                .Where(p => !placed.Contains(p.Name))
                .OrderBy(p => FilterLabel.Humanize(p.Name))
                .ToList();
            if (leftovers.Count > 0)
            {
                FlowLayoutPanel inner = NewGroup("Other");
                foreach (PropertyInfo prop in leftovers)
                {
                    AddFilterRow(inner, prop);
                }
            }

            AddCustomIdentifiersRow(NewGroup("Custom"));
        }

        private FlowLayoutPanel NewGroup(string title)
        {
            var inner = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Location = new Point(10, 24),
                Margin = Padding.Empty
            };
            var group = new GroupBox
            {
                Text = title,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(290, 0),
                Margin = new Padding(8),
                Padding = new Padding(0, 0, 10, 10)
            };
            group.Controls.Add(inner);
            _groups.Add(group);
            _filters.Controls.Add(group);
            return inner;
        }

        private static (Panel row, CheckBox checkBox, Button configure) NewRow(string name)
        {
            var checkBox = new CheckBox { Text = name, AutoSize = true, Location = new Point(6, 10) };
            var configure = new Button { Text = "Configure…", Size = new Size(105, 30), Location = new Point(165, 5), Enabled = false, Visible = false, Font = new Font(ModernTheme.UiFont.FontFamily, 8.25f) };
            var row = new Panel { Width = 275, Height = 40, Margin = Padding.Empty };
            row.Controls.Add(checkBox);
            row.Controls.Add(configure);
            return (row, checkBox, configure);
        }

        private void AddFilterRow(FlowLayoutPanel inner, PropertyInfo filterProp)
        {
            string name = FilterLabel.Humanize(filterProp.Name);
            PropertyInfo? strategiesProp = filterProp.PropertyType.GetProperty("Strategies");
            (Panel row, CheckBox checkBox, Button configure) = NewRow(name);
            inner.Controls.Add(row);
            _rows.Add(new FilterRow { Name = name, Panel = row, CheckBox = checkBox, Group = (GroupBox)inner.Parent! });

            checkBox.CheckedChanged += (_, _) =>
            {
                if (!_loading && _policy is not null)
                {
                    if (checkBox.Checked)
                    {
                        if (filterProp.GetValue(_policy.Identifiers) is null)
                        {
                            filterProp.SetValue(_policy.Identifiers, CreateFilter(filterProp.PropertyType));
                        }
                    }
                    else
                    {
                        filterProp.SetValue(_policy.Identifiers, null);
                    }
                    _dirty = true;
                }
                configure.Visible = checkBox.Checked && strategiesProp is not null;
                configure.Enabled = configure.Visible;
                UpdateCount();
            };

            configure.Click += (_, _) =>
            {
                if (_policy is null || strategiesProp is null)
                {
                    return;
                }
                object? filter = filterProp.GetValue(_policy.Identifiers);
                if (filter is null)
                {
                    filter = CreateFilter(filterProp.PropertyType);
                    filterProp.SetValue(_policy.Identifiers, filter);
                    checkBox.Checked = true;
                }

                Type strategyType = strategiesProp.PropertyType.GetGenericArguments()[0];
                IEnumerable existing = (IEnumerable?)strategiesProp.GetValue(filter) ?? Array.Empty<object>();
                using var dlg = new FilterStrategiesForm(name, existing, strategyType);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    IList result = dlg.BuildResultList();
                    strategiesProp.SetValue(filter, result.Count > 0 ? result : null);
                    _dirty = true;
                }
            };

            _loaders.Add(() =>
            {
                bool enabled = _policy is not null && filterProp.GetValue(_policy.Identifiers) is not null;
                checkBox.Checked = enabled;
                configure.Visible = enabled && strategiesProp is not null;
                configure.Enabled = configure.Visible;
            });
        }

        private void AddCustomIdentifiersRow(FlowLayoutPanel inner)
        {
            const string name = "Custom Identifiers";
            (Panel row, CheckBox checkBox, Button configure) = NewRow(name);
            inner.Controls.Add(row);
            _rows.Add(new FilterRow { Name = name, Panel = row, CheckBox = checkBox, Group = (GroupBox)inner.Parent! });

            checkBox.CheckedChanged += (_, _) =>
            {
                if (!_loading && _policy is not null)
                {
                    _policy.Identifiers.CustomIdentifiers = checkBox.Checked
                        ? _policy.Identifiers.CustomIdentifiers ?? new List<Identifier>()
                        : null;
                    _dirty = true;
                }
                configure.Visible = checkBox.Checked;
                configure.Enabled = configure.Visible;
                UpdateCount();
            };

            configure.Click += (_, _) =>
            {
                if (_policy is null)
                {
                    return;
                }
                _policy.Identifiers.CustomIdentifiers ??= new List<Identifier>();
                using var dlg = new CustomIdentifiersForm(_policy.Identifiers.CustomIdentifiers);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _policy.Identifiers.CustomIdentifiers = dlg.Identifiers.Count > 0 ? dlg.Identifiers : null;
                    checkBox.Checked = _policy.Identifiers.CustomIdentifiers is not null;
                    _dirty = true;
                }
            };

            _loaders.Add(() =>
            {
                bool enabled = _policy?.Identifiers.CustomIdentifiers is { Count: > 0 };
                checkBox.Checked = enabled;
                configure.Visible = enabled;
                configure.Enabled = configure.Visible;
            });
        }

        private static AbstractPolicyFilter CreateFilter(Type filterType)
        {
            var filter = (AbstractPolicyFilter)Activator.CreateInstance(filterType)!;
            filter.Enabled = true;
            return filter;
        }

        // --- Search + count --------------------------------------------------

        private void ApplySearch(string query)
        {
            query = query.Trim();
            _filters.SuspendLayout();
            var groupsWithMatch = new HashSet<GroupBox>();
            foreach (FilterRow r in _rows)
            {
                bool match = query.Length == 0 || r.Name.Contains(query, StringComparison.OrdinalIgnoreCase);
                r.Panel.Visible = match;
                if (match)
                {
                    groupsWithMatch.Add(r.Group);
                }
            }
            foreach (GroupBox g in _groups)
            {
                g.Visible = groupsWithMatch.Contains(g);
            }
            _filters.ResumeLayout();
        }

        private void UpdateCount()
        {
            int enabled = _rows.Count(r => r.CheckBox.Checked);
            _countLabel.Text = $"{enabled} of {_rows.Count} filters enabled";
        }

        // --- Policy list / load / save ---------------------------------------

        private void ReloadPolicyList(string? selectName)
        {
            // Repopulating the list changes the selection programmatically; suppress the
            // unsaved-changes prompt and load the chosen policy explicitly afterwards.
            _suppressComboChange = true;
            _policyCombo.ComboBox.BeginUpdate();
            _policyCombo.Items.Clear();
            foreach (PolicyEntity entity in _repo.GetAll().OrderBy(p => p.Name))
            {
                _policyCombo.Items.Add(entity.Name);
            }
            _policyCombo.ComboBox.EndUpdate();

            int index = selectName is null ? -1 : _policyCombo.Items.IndexOf(selectName);
            if (index < 0 && _policyCombo.Items.Count > 0)
            {
                index = 0;
            }
            if (index >= 0)
            {
                _policyCombo.SelectedIndex = index;
            }
            _suppressComboChange = false;

            if (index >= 0)
            {
                LoadSelectedPolicy();
            }
            else
            {
                _policy = null;
                _currentPolicyName = null;
                SetEditingEnabled(false);
            }
        }

        private void OnPolicySelectionChanged()
        {
            if (_suppressComboChange || _policyCombo.SelectedItem is not string newName)
            {
                return;
            }
            if (newName == _currentPolicyName)
            {
                return;
            }

            if (_dirty && _policy is not null && _currentPolicyName is not null)
            {
                DialogResult choice = MessageBox.Show(
                    $"Save changes to policy '{_currentPolicyName}'?",
                    "Policy Editor",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);
                if (choice == DialogResult.Cancel)
                {
                    _suppressComboChange = true;
                    _policyCombo.SelectedItem = _currentPolicyName;
                    _suppressComboChange = false;
                    return;
                }
                if (choice == DialogResult.Yes)
                {
                    SaveCurrent();
                }
            }

            LoadSelectedPolicy();
        }

        private void LoadSelectedPolicy()
        {
            if (_policyCombo.SelectedItem is not string name)
            {
                return;
            }
            PolicyEntity? entity = _repo.FindByName(name);
            if (entity is null)
            {
                return;
            }

            _policy = PolicySerializer.DeserializeFromJson(string.IsNullOrWhiteSpace(entity.Json) ? "{}" : entity.Json);

            _loading = true;
            foreach (Action loader in _loaders)
            {
                loader();
            }
            _loading = false;

            SetEditingEnabled(true);
            UpdateCount();
            _currentPolicyName = name;
            _dirty = false;
        }

        private void OnSave(object? sender, EventArgs e)
        {
            if (SaveCurrent())
            {
                MessageBox.Show($"Policy '{_currentPolicyName}' saved.", "Policy Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private bool SaveCurrent()
        {
            if (_policy is null || _currentPolicyName is null)
            {
                return false;
            }
            PolicyEntity? entity = _repo.FindByName(_currentPolicyName);
            if (entity is null)
            {
                return false;
            }
            entity.Json = PolicySerializer.SerializeToJson(_policy);
            _repo.Update(entity);
            _dirty = false;
            return true;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_dirty && _policy is not null)
            {
                DialogResult choice = MessageBox.Show(
                    $"Save changes to policy '{_currentPolicyName}'?",
                    "Policy Editor",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);
                if (choice == DialogResult.Cancel)
                {
                    e.Cancel = true;
                }
                else if (choice == DialogResult.Yes)
                {
                    SaveCurrent();
                }
            }
            base.OnFormClosing(e);
        }

        private void OnNew(object? sender, EventArgs e)
        {
            string? name = Prompt("Enter a name for the new policy:", "New Policy");
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }
            if (_repo.FindByName(name) is not null)
            {
                MessageBox.Show($"A policy named '{name}' already exists.", "Duplicate Policy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _repo.Insert(new PolicyEntity { Name = name, Json = PolicySerializer.SerializeToJson(new PhileasPolicy { Name = name }) });
            ReloadPolicyList(name);
        }

        private void OnSaveAs(object? sender, EventArgs e)
        {
            if (_policy is null)
            {
                return;
            }
            string? name = Prompt("Enter a name for the new policy:", "Save As");
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }
            if (_repo.FindByName(name) is not null)
            {
                MessageBox.Show($"A policy named '{name}' already exists.", "Duplicate Policy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _policy.Name = name;
            _repo.Insert(new PolicyEntity { Name = name, Json = PolicySerializer.SerializeToJson(_policy) });
            ReloadPolicyList(name);
        }

        private void OnDelete(object? sender, EventArgs e)
        {
            if (_policyCombo.SelectedItem is not string name)
            {
                return;
            }
            if (name.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("The 'default' policy cannot be deleted.", "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (MessageBox.Show($"Delete the policy '{name}'?", "Delete Policy", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }
            PolicyEntity? entity = _repo.FindByName(name);
            if (entity is not null)
            {
                _repo.Delete(entity.Id);
            }
            ReloadPolicyList("default");
        }

        private void OnViewJson(object? sender, EventArgs e)
        {
            if (_policy is null)
            {
                return;
            }
            using var dlg = new Form
            {
                Text = $"\"{_policyCombo.SelectedItem}\" Policy JSON",
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(560, 520)
            };
            var box = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill, Text = PrettyJson(PolicySerializer.SerializeToJson(_policy)) };

            var copy = new Button { Text = "Copy to Clipboard", Size = new Size(160, 34) };
            var close = new Button { Text = "Close", DialogResult = DialogResult.OK, Size = new Size(90, 34) };
            copy.Click += (_, _) =>
            {
                if (!string.IsNullOrEmpty(box.Text))
                {
                    Clipboard.SetText(box.Text);
                    copy.Text = "Copied!";
                }
            };

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 52, Padding = new Padding(8) };
            bottom.Controls.AddRange(new Control[] { close, copy });

            dlg.Controls.Add(box);
            dlg.Controls.Add(bottom);
            dlg.AcceptButton = close;
            ModernTheme.Apply(dlg);
            ModernTheme.MakePrimary(copy);
            dlg.ShowDialog(this);
        }

        private void SetEditingEnabled(bool enabled)
        {
            _filters.Enabled = enabled;
            _save.Enabled = enabled;
            _saveAs.Enabled = enabled;
            _delete.Enabled = enabled;
            _viewJson.Enabled = enabled;
        }

        private static string PrettyJson(string json)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                string pretty = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
                return pretty.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
            }
            catch
            {
                return json;
            }
        }

        private static string? Prompt(string message, string title)
        {
            using var form = new Form
            {
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(380, 135)
            };
            var label = new Label { Text = message, AutoSize = true, Location = new Point(12, 15) };
            var textBox = new TextBox { Location = new Point(15, 40), Width = 350 };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(182, 84), Size = new Size(90, 34) };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(278, 84), Size = new Size(90, 34) };
            form.Controls.AddRange(new Control[] { label, textBox, ok, cancel });
            form.AcceptButton = ok;
            form.CancelButton = cancel;
            ModernTheme.Apply(form);
            return form.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : null;
        }
    }
}
