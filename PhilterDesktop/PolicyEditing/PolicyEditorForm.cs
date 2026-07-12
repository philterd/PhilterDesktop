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
            public required Control Group; // the containing tab page
        }

        private readonly PolicyRepository _repo;
        private readonly WatchedFolderRepository? _watchedFolders;
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
        private readonly ToolStripMenuItem _newWizard = new() { Text = "From Wizard…" };
        private readonly ToolStripButton _save = new() { Text = "Save", Enabled = false };
        private readonly ToolStripButton _saveAs = new() { Text = "Save As", Enabled = false };
        private readonly ToolStripButton _delete = new() { Text = "Delete", Enabled = false };
        private readonly ToolStripButton _import = new() { Text = "Import" };
        private readonly ToolStripButton _export = new() { Text = "Export", Enabled = false };
        private readonly ToolStripLabel _consultingLink = new("Need help with policies?")
        {
            IsLink = true,
            Alignment = ToolStripItemAlignment.Right,
            ToolTipText = "Philterd offers policy consulting"
        };

        // Ignore List / Always Redact edit policy-wide term lists. They live as regular buttons in a
        // panel directly below the tabs, not as toolbar actions.
        private readonly FlowLayoutPanel _actions = new()
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false, // keep the term/region buttons on a single row
            Height = 48,
            Padding = new Padding(10, 8, 10, 8)
        };
        private readonly Button _ignoreList = new()
        {
            Text = "Always Ignore…",
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
            Margin = new Padding(0, 0, 8, 0),
            Padding = new Padding(10, 4, 10, 4)
        };
        private readonly Button _pdfRegions = new()
        {
            Text = "PDF Regions…",
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Enabled = false,
            Margin = new Padding(0, 0, 8, 0), // match the other action buttons so the row is even
            Padding = new Padding(10, 4, 10, 4)
        };

        // Identifies the editor-managed ignored-terms set, so other ignored sets in a policy are left alone.
        private const string IgnoredTermsName = "ignored-terms";

        // Each filter category is a tab, so only one group shows at a time and the window stays small.
        private readonly TabControl _tabs = new()
        {
            Dock = DockStyle.Fill
        };

        // PhEye tab: user-added local (on-device) models, each a PhEye entry with its own model path.
        private readonly ListView _phEyeModelList = new()
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            HideSelection = false
        };
        private readonly Button _addPhEyeModel = new() { Text = "Add…", AutoSize = true, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(10, 4, 10, 4) };
        private readonly Button _editPhEyeModel = new() { Text = "Edit…", AutoSize = true, Enabled = false, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(10, 4, 10, 4) };
        private readonly Button _removePhEyeModel = new() { Text = "Remove", AutoSize = true, Enabled = false, Margin = new Padding(0, 0, 8, 0), Padding = new Padding(10, 4, 10, 4) };
        // "Add" offers either the built-in person-names model or a user-provided local model.
        private readonly ContextMenuStrip _addPhEyeMenu = new();
        private readonly ToolStripMenuItem _addBundledName = new() { Text = "Person Names (on-device)" };
        private readonly ToolStripMenuItem _addLocalModel = new() { Text = "Custom Local Model…" };
        private const string BundledNameModelLabel = "Person Names (on-device)";

        // Free-text, editor-only description (stored on the policy record, not in the engine JSON).
        private readonly Panel _descPanel = new() { Dock = DockStyle.Top, Height = 30, Padding = new Padding(8, 4, 8, 2) };
        private readonly Label _descLabel = new() { Text = "Description:", AutoSize = true, Dock = DockStyle.Left, Padding = new Padding(0, 4, 6, 0) };
        private readonly TextBox _description = new() { Dock = DockStyle.Fill };

        private readonly List<Action> _loaders = new();
        private readonly List<FilterRow> _rows = new();

        public PolicyEditorForm(PolicyRepository repo, WatchedFolderRepository? watchedFolders = null)
        {
            _repo = repo;
            _watchedFolders = watchedFolders;

            Text = "Policy Editor";
            StartPosition = FormStartPosition.CenterScreen;
            // Resizable window: filters are split across tabs (one category at a time), and each tab
            // scrolls if its content doesn't fit — but the user can grow the window to see more at once.
            ClientSize = new Size(860, 560);
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
            _newWizard.Click += OnNewFromWizard;
            _save.Click += OnSave;
            _saveAs.Click += OnSaveAs;
            _delete.Click += OnDelete;
            _import.Click += OnImport;
            _export.Click += OnExport;
            _description.TextChanged += (_, _) => { if (!_loading) { _dirty = true; } };
            _consultingLink.Click += (_, _) => Links.Open(Links.ConsultingUrl("policy-editor"));
            _ignoreList.Click += OnIgnoredTerms;
            _alwaysRedact.Click += OnAlwaysRedact;
            _pdfRegions.Click += OnPdfRegions;

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
            _import.Image = ModernTheme.CreateGlyphImage("", 20, ModernTheme.Text);  // Download (import from file)
            _export.Image = ModernTheme.CreateGlyphImage("", 20, ModernTheme.Text);  // Upload (export to file)

            // "New" is a dropdown: Blank Policy, From Template, or From Wizard. Menu items use small icons.
            _newBlank.Image = ModernTheme.CreateGlyphImage("\uE7C3", 16, ModernTheme.Text);        // Page (blank)
            _newFromTemplate.Image = ModernTheme.CreateGlyphImage("\uE8A5", 16, ModernTheme.Text); // Document (template)
            _newWizard.Image = ModernTheme.CreateGlyphImage("\uE945", 16, ModernTheme.Text);       // Lightbulb (wizard)
            _new.DropDownItems.AddRange(new ToolStripItem[] { _newBlank, _newFromTemplate, _newWizard });

            // Toolbar buttons show their icon above the label.
            foreach (ToolStripItem button in new ToolStripItem[] { _new, _save, _saveAs, _delete, _import, _export })
            {
                button.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                button.TextImageRelation = TextImageRelation.ImageAboveText;
            }

            _toolStrip.Items.Add(new ToolStripLabel("Policy:"));
            _toolStrip.Items.Add(_policyCombo);
            _toolStrip.Items.Add(new ToolStripSeparator());
            _toolStrip.Items.AddRange(new ToolStripItem[]
            {
                _new, new ToolStripSeparator(), _save, _saveAs, new ToolStripSeparator(), _delete,
                new ToolStripSeparator(), _import, _export
            });
            _toolStrip.Items.Add(_consultingLink); // right-aligned (Alignment = Right)

            _actions.Controls.Add(_ignoreList);
            _actions.Controls.Add(_alwaysRedact);
            _actions.Controls.Add(_pdfRegions);

            // A quiet "score this policy" link pinned to the bottom-right, pointing at Philter Scope.
            // Sits in the empty right side of the bottom actions bar; anchored bottom-right so it stays
            // put if the window is clamped to a smaller screen. Tagged utm_medium=policy-editor.
            LinkLabel scoreLink = Links.CreateLink(
                "Score this policy with Philter Scope →",
                Links.ScopeUrl("policy-editor"));
            scoreLink.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            scoreLink.Location = new Point(
                ClientSize.Width - scoreLink.PreferredWidth - 14,
                ClientSize.Height - scoreLink.PreferredHeight - 16);
            Controls.Add(scoreLink);
            scoreLink.BringToFront();

            _descPanel.Controls.Add(_description); // fill
            _descPanel.Controls.Add(_descLabel);   // left

            // z-order: tabs fill the middle, the actions panel docks at the bottom, the description bar
            // sits just under the toolbar, which is on top.
            Controls.Add(_tabs);
            Controls.Add(_actions);
            Controls.Add(_descPanel);
            Controls.Add(_toolStrip);
            SetEditingEnabled(false);
        }

        // --- Filter registration (reflection + category grouping) ------------

        private void RegisterFilters()
        {
            IReadOnlyList<(string Category, string[] Props)> categories = FilterCatalog.Categories;

            Dictionary<string, PropertyInfo> discovered = FilterCatalog.Discover();

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
                    // On-device AI name detection lives on the dedicated PhEye tab (see BuildPhEyeModelsTab).
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

            // A dedicated tab for user-added local PhEye models (list + Add/Edit/Remove).
            BuildPhEyeModelsTab();
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

        private static (Panel row, CheckBox checkBox, Button configure) NewRow(string name, string? example = null)
        {
            // Filter name (checkbox) with a small grey example beneath it; the Configure button is
            // right-aligned in a fixed-width row so buttons line up cleanly down the column.
            var checkBox = new CheckBox { Text = name, AutoSize = true, Margin = new Padding(0, 0, 0, 0) };

            var leftStack = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = Padding.Empty
            };
            leftStack.Controls.Add(checkBox);
            if (!string.IsNullOrWhiteSpace(example))
            {
                leftStack.Controls.Add(new Label
                {
                    Text = example,
                    AutoSize = true,
                    ForeColor = ModernTheme.SubtleText,
                    Font = new Font(ModernTheme.UiFont.FontFamily, 8f),
                    Margin = new Padding(20, 0, 0, 0) // indent under the checkbox label
                });
            }

            var configure = new Button
            {
                Text = "Configure…",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(10, 0, 0, 0),
                Enabled = false,
                Visible = false,
                Font = new Font(ModernTheme.UiFont.FontFamily, 8.25f)
            };

            // Fixed-size two-column row: label/example fill the left, the button sits at the right edge,
            // so showing/hiding the button never shifts anything.
            var row = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = false,
                Size = new Size(380, 46),
                Margin = new Padding(0, 0, 0, 2)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.Controls.Add(leftStack, 0, 0);
            row.Controls.Add(configure, 1, 0);
            return (row, checkBox, configure);
        }

        private void AddFilterRow(FlowLayoutPanel inner, PropertyInfo filterProp)
        {
            string name = FilterLabel.Humanize(filterProp.Name);
            PropertyInfo? strategiesProp = filterProp.PropertyType.GetProperty("Strategies");
            (Panel row, CheckBox checkBox, Button configure) = NewRow(name, FilterExamples.For(filterProp.Name));
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
                        // Give the just-enabled filter its visible default strategy.
                        FilterStrategyDefaults.MaterializeMissing(_policy);
                    }
                    else
                    {
                        filterProp.SetValue(_policy.Identifiers, null);
                    }
                    _dirty = true;
                }
                // Drive Enabled from the boolean, not configure.Visible — the Visible getter reports
                // effective visibility, which is false while this row's tab is inactive.
                bool showConfigure = checkBox.Checked && strategiesProp is not null;
                configure.Visible = showConfigure;
                configure.Enabled = showConfigure;
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

                // Ensure the default REDACT strategy is present so the list is never empty.
                FilterStrategyDefaults.MaterializeMissing(_policy);

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
                bool showConfigure = enabled && strategiesProp is not null;
                configure.Visible = showConfigure;
                configure.Enabled = showConfigure;
            });
        }

        private void AddCustomIdentifiersRow(FlowLayoutPanel inner)
        {
            const string name = "Custom Identifiers";
            (Panel row, CheckBox checkBox, Button configure) = NewRow(name, "your own patterns, e.g. CASE-2024-00123");
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
                configure.Enabled = checkBox.Checked;
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
                configure.Enabled = enabled;
            });
        }

        // Returns the policy's bundled on-device name entry (empty model path), creating it if absent.
        private PhEye GetOrAddBundledNameEntry()
        {
            _policy!.Identifiers.PhEyes ??= new List<PhEye>();
            PhEye? bundled = _policy.Identifiers.PhEyes.FirstOrDefault(PhEyeModel.IsBundledNameEntry);
            if (bundled is null)
            {
                bundled = PhEyeModel.CreateDefaultFilter();
                _policy.Identifiers.PhEyes.Add(bundled);
            }
            return bundled;
        }

        // --- PhEye tab: on-device models (bundled person-names + user-added local models) ----

        private void BuildPhEyeModelsTab()
        {
            var page = new TabPage("PhEye") { UseVisualStyleBackColor = true, Padding = new Padding(8) };

            var intro = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 48,
                Padding = new Padding(2, 2, 2, 6),
                Text = "PhEye models detect entities on-device (GLiNER), with no network call. Add the built-in " +
                       "person-names model, or your own local models (a model folder on this machine plus the " +
                       "entity types it detects)."
            };

            _phEyeModelList.ShowItemToolTips = true;
            _phEyeModelList.Columns.Add("Model", 200);
            _phEyeModelList.Columns.Add("Entity Types", 240);
            _phEyeModelList.Columns.Add("Threshold", 80);
            _phEyeModelList.SelectedIndexChanged += (_, _) => UpdatePhEyeModelButtons();
            _phEyeModelList.DoubleClick += OnEditPhEyeModel;

            _addBundledName.Click += OnAddBundledNameModel;
            _addLocalModel.Click += OnAddLocalModel;
            _addPhEyeMenu.Items.AddRange(new ToolStripItem[] { _addBundledName, _addLocalModel });

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.LeftToRight,
                Height = 44,
                Padding = new Padding(0, 6, 0, 0)
            };
            buttons.Controls.AddRange(new Control[] { _addPhEyeModel, _editPhEyeModel, _removePhEyeModel });

            var listHost = new Panel { Dock = DockStyle.Fill };
            listHost.Controls.Add(_phEyeModelList);

            // z-order: list fills the remaining space, buttons dock to the bottom, intro to the top.
            page.Controls.Add(listHost);
            page.Controls.Add(buttons);
            page.Controls.Add(intro);
            _tabs.TabPages.Add(page);

            _addPhEyeModel.Click += (_, _) =>
            {
                // The built-in names model is a singleton — offer it only when it isn't already added.
                _addBundledName.Enabled = _policy is not null
                    && _policy.Identifiers.PhEyes?.Any(PhEyeModel.IsBundledNameEntry) != true;
                _addPhEyeMenu.Show(_addPhEyeModel, new Point(0, _addPhEyeModel.Height));
            };
            _editPhEyeModel.Click += OnEditPhEyeModel;
            _removePhEyeModel.Click += OnRemovePhEyeModel;

            _loaders.Add(RefreshPhEyeModelList);
        }

        private void RefreshPhEyeModelList()
        {
            _phEyeModelList.BeginUpdate();
            _phEyeModelList.Items.Clear();
            if (_policy?.Identifiers.PhEyes is { } phEyes)
            {
                foreach (PhEye phEye in phEyes)
                {
                    PhEyeConfiguration config = phEye.PhEyeConfiguration;
                    bool bundled = PhEyeModel.IsBundledNameEntry(phEye);
                    string path = config?.ModelPath ?? string.Empty;
                    var item = new ListViewItem(bundled
                        ? BundledNameModelLabel
                        : Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)))
                    {
                        Tag = phEye,
                        ToolTipText = bundled ? "Built-in on-device person-names model." : path
                    };
                    item.SubItems.Add(string.Join(", ", config?.Labels ?? new List<string>()));
                    item.SubItems.Add((config?.Threshold ?? PhEyeModel.DefaultThreshold).ToString("0.00"));
                    _phEyeModelList.Items.Add(item);
                }
            }
            _phEyeModelList.EndUpdate();
            UpdatePhEyeModelButtons();
        }

        private void UpdatePhEyeModelButtons()
        {
            bool hasSelection = _phEyeModelList.SelectedItems.Count > 0;
            _editPhEyeModel.Enabled = hasSelection;
            _removePhEyeModel.Enabled = hasSelection;
        }

        private void OnAddBundledNameModel(object? sender, EventArgs e)
        {
            if (_policy is null)
            {
                return;
            }
            GetOrAddBundledNameEntry();
            FilterStrategyDefaults.MaterializeMissing(_policy); // give Names its visible default REDACT strategy
            RefreshPhEyeModelList();
            _dirty = true;
        }

        private void OnAddLocalModel(object? sender, EventArgs e)
        {
            if (_policy is null)
            {
                return;
            }
            var phEye = new PhEye
            {
                Enabled = true,
                PhEyeConfiguration = new PhEyeConfiguration
                {
                    Labels = new List<string>(),
                    Threshold = PhEyeModel.DefaultThreshold
                }
            };
            using var dlg = new PhEyeModelForm(phEye);
            if (dlg.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
            _policy.Identifiers.PhEyes ??= new List<PhEye>();
            _policy.Identifiers.PhEyes.Add(phEye);
            FilterStrategyDefaults.MaterializeMissing(_policy); // give the new model a default REDACT strategy
            RefreshPhEyeModelList();
            _dirty = true;
        }

        private void OnEditPhEyeModel(object? sender, EventArgs e)
        {
            if (_policy is null || _phEyeModelList.SelectedItems.Count == 0
                || _phEyeModelList.SelectedItems[0].Tag is not PhEye phEye)
            {
                return;
            }

            // The bundled names model has a fixed model and label ("name"); editing it configures how the
            // detected names are redacted. A user model edits its folder, entity types, and threshold.
            if (PhEyeModel.IsBundledNameEntry(phEye))
            {
                EditPhEyeStrategies(phEye, BundledNameModelLabel);
            }
            else
            {
                using var dlg = new PhEyeModelForm(phEye);
                if (dlg.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                _dirty = true;
            }
            RefreshPhEyeModelList();
        }

        private void OnRemovePhEyeModel(object? sender, EventArgs e)
        {
            if (_policy is null || _phEyeModelList.SelectedItems.Count == 0
                || _phEyeModelList.SelectedItems[0].Tag is not PhEye phEye)
            {
                return;
            }
            bool bundled = PhEyeModel.IsBundledNameEntry(phEye);
            string prompt = bundled
                ? "Turn off the built-in person-names model?"
                : "Remove the selected PhEye model?";
            if (MessageBox.Show(this, prompt, "PhEye", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }
            _policy.Identifiers.PhEyes?.Remove(phEye);
            if (_policy.Identifiers.PhEyes is { Count: 0 })
            {
                _policy.Identifiers.PhEyes = null;
            }
            RefreshPhEyeModelList();
            _dirty = true;
        }

        // Edits the redaction strategies (how detections are replaced) for a PhEye entry.
        private void EditPhEyeStrategies(PhEye phEye, string title)
        {
            FilterStrategyDefaults.MaterializeMissing(_policy); // ensure the default REDACT strategy is present
            IEnumerable existing = (IEnumerable?)phEye.Strategies ?? Array.Empty<object>();
            using var dlg = new FilterStrategiesForm(title, existing, typeof(Phileas.Policy.Filters.Strategies.PhEyeFilterStrategy));
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                IList result = dlg.BuildResultList();
                phEye.Strategies = result.Count > 0
                    ? result.Cast<Phileas.Policy.Filters.Strategies.PhEyeFilterStrategy>().ToList()
                    : null;
                _dirty = true;
            }
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

            // Surface the engine's implicit default: give every enabled filter that has no strategy an
            // explicit REDACT ({{{REDACTED-%t}}}) entry, so it's visible in Configure instead of implied.
            FilterStrategyDefaults.MaterializeMissing(_policy);

            _loading = true;
            _description.Text = entity.Description ?? string.Empty;
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
            entity.Description = _description.Text;
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
            _repo.Insert(new PolicyEntity { Name = name, Json = json, Description = picker.SelectedTemplate.Description });
            ReloadPolicyList(name);
        }

        private void OnNewFromWizard(object? sender, EventArgs e)
        {
            // The wizard collects the name, validates uniqueness and the schema, and builds the JSON.
            using var wizard = new PolicyWizardForm(n => _repo.FindByName(n) is not null);
            if (wizard.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
            _repo.Insert(new PolicyEntity { Name = wizard.ResultName, Json = wizard.ResultJson });
            ReloadPolicyList(wizard.ResultName);
        }

        private void OnExport(object? sender, EventArgs e)
        {
            if (_policy is null)
            {
                return;
            }
            using var dlg = new SaveFileDialog
            {
                Title = "Export Policy",
                Filter = "Policy JSON (*.json)|*.json|All files (*.*)|*.*",
                FileName = (_currentPolicyName ?? "policy") + ".json",
                DefaultExt = "json"
            };
            if (dlg.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
            try
            {
                File.WriteAllText(dlg.FileName, PrettyJson(PolicySerializer.SerializeToJson(_policy)));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not export the policy: {ex.Message}",
                    "Export Policy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnImport(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Import Policy",
                Filter = "Policy JSON (*.json)|*.json|All files (*.*)|*.*",
                CheckFileExists = true
            };
            if (dlg.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            string text;
            try
            {
                text = File.ReadAllText(dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not read the file: {ex.Message}",
                    "Import Policy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Only import a file that's a valid policy for this engine's schema.
            PolicyValidationResult validation = PolicyValidator.Validate(text);
            if (!validation.IsValid)
            {
                string details = string.Join(Environment.NewLine + "  • ", validation.Errors.Take(15));
                MessageBox.Show(
                    this,
                    "This file is not a valid policy for the redaction engine's schema (PhiSQL " +
                    PolicyValidator.SchemaVersion + "), so it was not imported:" +
                    Environment.NewLine + Environment.NewLine + "  • " + details,
                    "Import Policy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PhileasPolicy imported = PolicySerializer.DeserializeFromJson(text);
            string name = string.IsNullOrWhiteSpace(imported.Name)
                ? Path.GetFileNameWithoutExtension(dlg.FileName)
                : imported.Name;

            if (_repo.FindByName(name) is not null)
            {
                string? newName = Prompt($"A policy named '{name}' already exists. Enter a name for the imported policy:", "Import Policy");
                if (string.IsNullOrWhiteSpace(newName))
                {
                    return;
                }
                name = newName.Trim();
                if (_repo.FindByName(name) is not null)
                {
                    MessageBox.Show($"A policy named '{name}' already exists.", "Import Policy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            imported.Name = name;
            _repo.Insert(new PolicyEntity { Name = name, Json = PolicySerializer.SerializeToJson(imported) });
            ReloadPolicyList(name);
        }

        private static string PrettyJson(string json)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                return json; // fall back to the raw text if it can't be reformatted
            }
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
            _repo.Insert(new PolicyEntity { Name = name, Json = json, Description = _description.Text });
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

            // Guard against orphaning a watched folder: if any folder uses this policy, deleting it would
            // leave the folder pointing at an unknown policy (it would then silently fail every file).
            // Offer to delete anyway and switch those folders to the 'default' policy.
            List<WatchedFolderEntity> inUse = _watchedFolders is null
                ? new List<WatchedFolderEntity>()
                : WatchedFolderPolicyGuard.FoldersUsing(_watchedFolders, name);

            if (inUse.Count > 0)
            {
                string folders = string.Join(Environment.NewLine, inUse.Select(f => "• " + f.FolderPath));
                string message =
                    $"The policy '{name}' is used by {inUse.Count} watched folder(s):" + Environment.NewLine + Environment.NewLine +
                    folders + Environment.NewLine + Environment.NewLine +
                    "Delete it anyway? Those folders will be switched to the 'default' policy.";
                if (MessageBox.Show(this, message, "Policy In Use", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }
                WatchedFolderPolicyGuard.ReassignToDefault(_watchedFolders!, inUse);
            }
            else if (MessageBox.Show($"Delete the policy '{name}'?", "Delete Policy", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
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

            using var dlg = new AlwaysRedactTermsForm(AlwaysRedactPolicy.GetTerms(_policy));
            if (dlg.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            AlwaysRedactPolicy.SetTerms(_policy, dlg.Terms);
            _dirty = true;
        }

        private void OnPdfRegions(object? sender, EventArgs e)
        {
            if (_policy is null)
            {
                return;
            }

            using var dlg = new PdfRegionsForm(_policy.Graphical.BoundingBoxes);
            if (dlg.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            _policy.Graphical.BoundingBoxes = dlg.Boxes;
            _dirty = true;
        }

        private void SetEditingEnabled(bool enabled)
        {
            _tabs.Enabled = enabled;
            _save.Enabled = enabled;
            _saveAs.Enabled = enabled;
            _delete.Enabled = enabled;
            _export.Enabled = enabled; // Import is always available; Export needs a loaded policy
            _description.Enabled = enabled;
            _ignoreList.Enabled = enabled;
            _alwaysRedact.Enabled = enabled;
            _pdfRegions.Enabled = enabled;
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
