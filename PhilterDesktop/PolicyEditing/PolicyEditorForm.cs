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
            public required Control Group; // the containing tab page
        }

        private readonly PolicyRepository _repo;
        private PhileasPolicy? _policy;
        private bool _loading;
        private bool _dirty;
        private string? _currentPolicyName;
        private bool _suppressComboChange;

        private readonly ToolStrip _toolStrip = new() { GripStyle = ToolStripGripStyle.Hidden };
        private readonly ToolStripComboBox _policyCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, AutoSize = false, Size = new Size(190, 23) };
        private readonly ToolStripDropDownButton _new = new() { Text = "New" };
        private readonly ToolStripMenuItem _newBlank = new() { Text = "Blank Policy" };
        private readonly ToolStripMenuItem _newFromTemplate = new() { Text = "From Template…" };
        private readonly ToolStripButton _save = new() { Text = "Save", Enabled = false };
        private readonly ToolStripButton _saveAs = new() { Text = "Save As", Enabled = false };
        private readonly ToolStripButton _delete = new() { Text = "Delete", Enabled = false };

        // Ignore List / Always Redact edit policy-wide term lists. They live as regular buttons in a
        // panel directly below the tabs, not as toolbar actions.
        private readonly FlowLayoutPanel _actions = new()
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.LeftToRight,
            Height = 48,
            Padding = new Padding(10, 8, 10, 8)
        };
        private readonly Button _ignoreList = new()
        {
            Text = "Ignore List…",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Enabled = false,
            Margin = new Padding(0, 0, 8, 0),
            Padding = new Padding(10, 4, 10, 4)
        };
        private readonly Button _alwaysRedact = new()
        {
            Text = "Always Redact…",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Enabled = false,
            Padding = new Padding(10, 4, 10, 4)
        };

        // Identifies the editor-managed ignored-terms set, so other ignored sets in a policy are left alone.
        private const string IgnoredTermsName = "ignored-terms";

        // Identifies the editor-managed always-redact dictionary, leaving any other dictionaries alone.
        private const string AlwaysRedactName = "always-redact";

        // Each filter category is a tab, so only one group shows at a time and the window stays small.
        private readonly TabControl _tabs = new()
        {
            Dock = DockStyle.Fill
        };

        private readonly List<Action> _loaders = new();
        private readonly List<FilterRow> _rows = new();

        public PolicyEditorForm(PolicyRepository repo)
        {
            _repo = repo;

            Text = "Policy Editor";
            StartPosition = FormStartPosition.CenterScreen;
            // Compact window: filters are split across tabs (one category at a time), and each tab
            // scrolls if its content doesn't fit — so the editor fits comfortably on a small screen.
            ClientSize = new Size(720, 430);
            MinimumSize = new Size(560, 380);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;

            // Don't open larger than the screen on smaller displays (each tab still scrolls).
            Rectangle workingArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1024, 768);
            Size = new Size(Math.Min(Width, workingArea.Width), Math.Min(Height, workingArea.Height));

            BuildLayout();
            RegisterFilters();

            _policyCombo.SelectedIndexChanged += (_, _) => OnPolicySelectionChanged();
            _newBlank.Click += OnNew;
            _newFromTemplate.Click += OnNewFromTemplate;
            _save.Click += OnSave;
            _saveAs.Click += OnSaveAs;
            _delete.Click += OnDelete;
            _ignoreList.Click += OnIgnoredTerms;
            _alwaysRedact.Click += OnAlwaysRedact;

            ModernTheme.Apply(this);

            Load += (_, _) => ReloadPolicyList("default");
        }

        private void BuildLayout()
        {
            _toolStrip.ImageScalingSize = new Size(20, 20);
            _new.Image = ModernTheme.CreateGlyphImage("\uE710", 20, ModernTheme.Text);     // Add
            _save.Image = ModernTheme.CreateGlyphImage("\uE74E", 20, ModernTheme.Accent);  // Save
            _saveAs.Image = ModernTheme.CreateGlyphImage("\uE792", 20, ModernTheme.Text);  // SaveAs
            _delete.Image = ModernTheme.CreateGlyphImage("\uE74D", 20, ModernTheme.Text);  // Delete

            // "New" is a dropdown: Blank Policy or From Template. Menu items use small icons.
            _newBlank.Image = ModernTheme.CreateGlyphImage("\uE7C3", 16, ModernTheme.Text);        // Page (blank)
            _newFromTemplate.Image = ModernTheme.CreateGlyphImage("\uE8A5", 16, ModernTheme.Text); // Document (template)
            _new.DropDownItems.AddRange(new ToolStripItem[] { _newBlank, _newFromTemplate });

            // Toolbar buttons show their icon above the label.
            foreach (ToolStripItem button in new ToolStripItem[] { _new, _save, _saveAs, _delete })
            {
                button.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                button.TextImageRelation = TextImageRelation.ImageAboveText;
            }

            _toolStrip.Items.Add(new ToolStripLabel("Policy:"));
            _toolStrip.Items.Add(_policyCombo);
            _toolStrip.Items.Add(new ToolStripSeparator());
            _toolStrip.Items.AddRange(new ToolStripItem[] { _new, _save, _saveAs, _delete });

            _actions.Controls.Add(_ignoreList);
            _actions.Controls.Add(_alwaysRedact);

            // z-order: tabs fill the middle, the actions panel docks at the bottom (immediately below
            // the tabs), and the toolbar docks at the top.
            Controls.Add(_tabs);
            Controls.Add(_actions);
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

                if (category == "Personal")
                {
                    // On-device AI name detection belongs with the other personal-name filters.
                    AddPhEyeRow(inner);
                    // The Personal group is short; stack Custom Identifiers beneath it to fill the column.
                    AddCustomIdentifiersRow(NewGroup("Custom"));
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
        }

        private FlowLayoutPanel NewGroup(string title)
        {
            // The inner panel fills the tab and scrolls vertically if a category has many filters.
            var inner = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(10)
            };
            var page = new TabPage(title)
            {
                UseVisualStyleBackColor = true,
                Padding = new Padding(3)
            };
            page.Controls.Add(inner);
            _tabs.TabPages.Add(page);
            return inner;
        }

        private static (Panel row, CheckBox checkBox, Button configure) NewRow(string name)
        {
            // A horizontal flow row: the Configure button always sits just right of the checkbox,
            // whatever the label's width or the DPI — so it can never be overlapped or clipped.
            var checkBox = new CheckBox { Text = name, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 0, 4) };
            var configure = new Button
            {
                Text = "Configure…",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(10, 3, 3, 3),
                Enabled = false,
                Visible = false,
                Font = new Font(ModernTheme.UiFont.FontFamily, 8.25f)
            };
            // Fixed-width row: it already reserves room for the checkbox and the Configure button,
            // so showing/hiding the button never changes the row's (or the group's) size.
            var row = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = false,
                WrapContents = false,
                Size = new Size(350, 34),
                Margin = Padding.Empty
            };
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
            _rows.Add(new FilterRow { Name = name, Panel = row, CheckBox = checkBox, Group = inner.Parent! });

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
            _rows.Add(new FilterRow { Name = name, Panel = row, CheckBox = checkBox, Group = inner.Parent! });

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

        private void AddPhEyeRow(FlowLayoutPanel inner)
        {
            const string name = "Names (on-device AI)";
            (Panel row, CheckBox checkBox, Button configure) = NewRow(name);
            inner.Controls.Add(row);
            _rows.Add(new FilterRow { Name = name, Panel = row, CheckBox = checkBox, Group = inner.Parent! });

            var tip = new ToolTip();
            tip.SetToolTip(checkBox,
                "Detects person names using the bundled on-device PhEye model.\n" +
                "Runs locally with no network call; complements the pattern-based filters.");

            checkBox.CheckedChanged += (_, _) =>
            {
                if (!_loading && _policy is not null)
                {
                    _policy.Identifiers.PhEyes = checkBox.Checked
                        ? new List<PhEye> { PhEyeModel.CreateDefaultFilter() }
                        : null;
                    _dirty = true;
                }
                configure.Visible = checkBox.Checked;
                configure.Enabled = configure.Visible;
            };

            configure.Click += (_, _) =>
            {
                if (_policy is null)
                {
                    return;
                }
                _policy.Identifiers.PhEyes ??= new List<PhEye>();
                if (_policy.Identifiers.PhEyes.Count == 0)
                {
                    _policy.Identifiers.PhEyes.Add(PhEyeModel.CreateDefaultFilter());
                }
                checkBox.Checked = true;
                PhEye phEye = _policy.Identifiers.PhEyes[0];

                IEnumerable existing = (IEnumerable?)phEye.Strategies ?? Array.Empty<object>();
                using var dlg = new FilterStrategiesForm(name, existing, typeof(Phileas.Policy.Filters.Strategies.PhEyeFilterStrategy));
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    IList result = dlg.BuildResultList();
                    phEye.Strategies = result.Count > 0
                        ? result.Cast<Phileas.Policy.Filters.Strategies.PhEyeFilterStrategy>().ToList()
                        : null;
                    _dirty = true;
                }
            };

            _loaders.Add(() =>
            {
                bool enabled = _policy?.Identifiers.PhEyes is { Count: > 0 };
                checkBox.Checked = enabled;
                configure.Visible = enabled;
                configure.Enabled = enabled;
            });
        }

        private static AbstractPolicyFilter CreateFilter(Type filterType)
        {
            var filter = (AbstractPolicyFilter)Activator.CreateInstance(filterType)!;
            filter.Enabled = true;
            return filter;
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
            string json = PolicySerializer.SerializeToJson(_policy);
            if (!EnsureValid(json))
            {
                return false;
            }
            entity.Json = json;
            _repo.Update(entity);
            _dirty = false;
            return true;
        }

        // A policy must validate against the engine's PhiSQL policy schema before it can be saved,
        // so we never persist something the redaction engine would reject or silently misread.
        private bool EnsureValid(string policyJson)
        {
            PolicyValidationResult result = PolicyValidator.Validate(policyJson);
            if (result.IsValid)
            {
                return true;
            }

            string details = string.Join(
                Environment.NewLine + "  • ", result.Errors.Take(15));
            MessageBox.Show(
                this,
                "This policy does not match the redaction engine's policy schema (PhiSQL " +
                PolicyValidator.SchemaVersion + "), so it was not saved:" +
                Environment.NewLine + Environment.NewLine + "  • " + details,
                "Invalid policy",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
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

        private void OnNewFromTemplate(object? sender, EventArgs e)
        {
            using var picker = new TemplatePickerForm();
            if (picker.ShowDialog(this) != DialogResult.OK || picker.SelectedTemplate is null)
            {
                return;
            }

            string? name = Prompt("Enter a name for the new policy:", "New Policy from Template");
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }
            if (_repo.FindByName(name) is not null)
            {
                MessageBox.Show($"A policy named '{name}' already exists.", "Duplicate Policy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Templates are built to be schema-valid, but validate anyway so we never persist a policy
            // the engine would reject.
            string json = picker.SelectedTemplate.BuildJson();
            if (!EnsureValid(json))
            {
                return;
            }
            _repo.Insert(new PolicyEntity { Name = name, Json = json });
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
            string json = PolicySerializer.SerializeToJson(_policy);
            if (!EnsureValid(json))
            {
                return;
            }
            _repo.Insert(new PolicyEntity { Name = name, Json = json });
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

        private void OnIgnoredTerms(object? sender, EventArgs e)
        {
            if (_policy is null)
            {
                return;
            }

            Ignored? existing = _policy.Ignored?.FirstOrDefault(i => i.Name == IgnoredTermsName);

            using var dlg = new IgnoredTermsForm(
                existing?.Terms ?? new List<string>(),
                existing?.CaseSensitive ?? false);

            if (dlg.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            // Replace only the editor-managed set; leave any other ignored sets (e.g. file-based) intact.
            _policy.Ignored?.RemoveAll(i => i.Name == IgnoredTermsName);
            if (dlg.Terms.Count > 0)
            {
                _policy.Ignored ??= new List<Ignored>();
                _policy.Ignored.Add(new Ignored
                {
                    Name = IgnoredTermsName,
                    Terms = dlg.Terms.ToList(),
                    Files = new List<string>(),
                    CaseSensitive = dlg.CaseSensitive
                });
            }
            _dirty = true;
        }

        private void OnAlwaysRedact(object? sender, EventArgs e)
        {
            if (_policy is null)
            {
                return;
            }

            Phileas.Policy.Filters.Dictionary? existing =
                _policy.Identifiers.Dictionaries?.FirstOrDefault(d => d.Name == AlwaysRedactName);

            using var dlg = new AlwaysRedactTermsForm(existing?.Terms ?? new List<string>());
            if (dlg.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            // Replace only the editor-managed dictionary; leave any other dictionaries intact.
            _policy.Identifiers.Dictionaries?.RemoveAll(d => d.Name == AlwaysRedactName);
            if (dlg.Terms.Count > 0)
            {
                _policy.Identifiers.Dictionaries ??= new List<Phileas.Policy.Filters.Dictionary>();
                _policy.Identifiers.Dictionaries.Add(new Phileas.Policy.Filters.Dictionary
                {
                    Name = AlwaysRedactName,
                    Terms = dlg.Terms.ToList(),
                    Enabled = true
                });
            }
            _dirty = true;
        }

        private void SetEditingEnabled(bool enabled)
        {
            _tabs.Enabled = enabled;
            _save.Enabled = enabled;
            _saveAs.Enabled = enabled;
            _delete.Enabled = enabled;
            _ignoreList.Enabled = enabled;
            _alwaysRedact.Enabled = enabled;
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
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(139, 84), Size = ModernTheme.StandardButtonSize };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(255, 84), Size = ModernTheme.StandardButtonSize };
            form.Controls.AddRange(new Control[] { label, textBox, ok, cancel });
            form.AcceptButton = ok;
            form.CancelButton = cancel;
            ModernTheme.Apply(form);
            return form.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : null;
        }
    }
}
