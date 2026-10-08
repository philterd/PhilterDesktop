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

using Phileas.Policy.Filters.Strategies;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// Edits a single filter strategy: which replacement strategy to use, its settings, and an optional
    /// condition. Offers every strategy Phileas supports for the filter (see <see cref="StrategyCatalog"/>);
    /// a strategy it can't edit is shown as-is and kept unchanged.
    ///
    /// The layout is built entirely from layout panels (no absolute coordinates), so it
    /// stays correct at any DPI / font scaling.
    /// </summary>
    internal sealed class AddFilterStrategyForm : Form
    {
        private readonly AbstractFilterStrategy _strategy;
        private readonly PhileasPolicy? _policy;
        private readonly StrategySettings _loaded;
        private readonly StrategyInfo? _keptAsIs; // the current strategy, when it isn't one we can edit

        private readonly ComboBox _strategyChoice = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 440 };
        private readonly Label _strategyDescription = new() { AutoSize = true, MaximumSize = new Size(440, 0), ForeColor = ModernTheme.SubtleText };
        private readonly Dictionary<string, Control> _panels = new(StringComparer.Ordinal);

        // REDACT / STATIC_REPLACE
        private readonly TextBox _redactionFormat = new() { Width = 440 };
        private readonly TextBox _staticValue = new() { Width = 440 };

        // RANDOM_REPLACE
        private readonly ComboBox _randomMethod = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
        private readonly TextBox _randomCandidates = new() { Width = 440, Height = 90, Multiline = true, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true };
        private readonly Label _randomCandidatesLabel = new() { Text = "Values to pick from, one per line:", AutoSize = true };
        private readonly CheckBox _randomConsistent = new() { Text = "Replace consistently across document contexts", AutoSize = true };

        // MASK
        private readonly TextBox _maskCharacter = new() { Width = 40, MaxLength = 1 };
        private readonly RadioButton _maskSameLength = new() { Text = "Same length as the value", AutoSize = true };
        private readonly RadioButton _maskFixedLength = new() { Text = "Fixed length:", AutoSize = true };
        private readonly NumericUpDown _maskLength = new() { Minimum = 1, Maximum = 1000, Value = 8, Width = 80 };

        // HASH_SHA256_REPLACE
        private readonly CheckBox _salt = new() { Text = "Add a random salt (the same value then gets a different hash each time)", AutoSize = true };

        // CRYPTO_REPLACE / FPE_ENCRYPT_REPLACE: environment variable names only, never key material
        private readonly TextBox _cryptoVariable = new() { Width = 260 };
        private readonly Label _cryptoStatus = new() { AutoSize = true, MaximumSize = new Size(440, 0) };
        private readonly TextBox _fpeKeyVariable = new() { Width = 260 };
        private readonly TextBox _fpeTweakVariable = new() { Width = 260 };
        private readonly Label _fpeStatus = new() { AutoSize = true, MaximumSize = new Size(440, 0) };

        // MAP_REPLACE
        private readonly DataGridView _mappings = new()
        {
            Width = 440,
            Height = 150,
            AllowUserToAddRows = true,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        };
        private readonly ComboBox _fallback = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
        private readonly CheckBox _caseSensitive = new() { Text = "Match values case-sensitively", AutoSize = true };
        private readonly CheckBox _mapConsistent = new() { Text = "Replace consistently across document contexts", AutoSize = true };

        // SHIFT
        private readonly NumericUpDown _shiftYears = new() { Minimum = -200, Maximum = 200, Width = 80 };
        private readonly NumericUpDown _shiftMonths = new() { Minimum = -2400, Maximum = 2400, Width = 80 };
        private readonly NumericUpDown _shiftDays = new() { Minimum = -73000, Maximum = 73000, Width = 80 };
        private readonly CheckBox _shiftRandom = new() { Text = "Shift by a random amount instead", AutoSize = true };
        private readonly CheckBox _futureDates = new() { Text = "Allow shifted dates in the future", AutoSize = true };

        // TRUNCATE (Phileas versions that honor its settings)
        private readonly NumericUpDown _truncateLeave = new() { Minimum = 1, Maximum = 1000, Value = StrategySettings.DefaultTruncateLeave, Width = 80 };
        private readonly ComboBox _truncateEnd = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160 };
        private readonly TextBox _truncateCharacter = new() { Width = 40, MaxLength = 1 };

        // Condition
        private readonly CheckBox _enableCondition = new() { Text = "Only apply when:", AutoSize = true };
        private readonly ComboBox _conditionField = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, DisplayMember = "Display" };
        private readonly ComboBox _conditionOp = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, DisplayMember = "Display" };
        private readonly TextBox _conditionVal = new() { Width = 220 };
        private readonly Label _conditionPreview = new() { AutoSize = true, ForeColor = ModernTheme.SubtleText };

        // Preserves an existing condition we couldn't show in the builder (e.g. a chained "and"),
        // unless the user actually edits the builder.
        private string? _unparsedCondition;
        private bool _loadingCondition;
        private bool _builderTouched;
        private readonly Button _ok = new() { Text = "OK", DialogResult = DialogResult.OK, Size = ModernTheme.StandardButtonSize };
        private readonly Button _cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = ModernTheme.StandardButtonSize };

        private TableLayoutPanel _root = null!;

        public AbstractFilterStrategy Strategy => _strategy;

        /// <param name="policy">The policy being edited, which holds the encryption key references. Without
        /// it the encryption strategies aren't offered.</param>
        public AddFilterStrategyForm(AbstractFilterStrategy strategy, string filterTypeDisplay, PhileasPolicy? policy = null)
        {
            _strategy = strategy;
            _policy = policy;
            _loaded = StrategySettings.Load(strategy, policy);

            Text = $"{filterTypeDisplay} Filter Strategy";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;

            List<StrategyInfo> offered = StrategyCatalog.For(strategy.GetType())
                .Where(s => policy is not null || (s.Name != StrategyCatalog.Crypto && s.Name != StrategyCatalog.Fpe))
                .ToList();
            if (!offered.Any(s => s.Name == _loaded.Strategy))
            {
                StrategyInfo? known = StrategyCatalog.Find(_loaded.Strategy);
                _keptAsIs = new StrategyInfo(_loaded.Strategy, $"{known?.Label ?? _loaded.Strategy} (kept as is)",
                    known is not null && !StrategyCatalog.Works(known, StrategyCatalog.InstalledPhileas)
                        ? "This strategy needs a newer version of Philter Desktop, so it's kept unchanged. Choose another strategy to replace it."
                        : "This strategy can't be edited here, so it's kept unchanged. Choose another strategy to replace it.");
                offered.Insert(0, _keptAsIs);
            }
            _strategyChoice.Items.AddRange(offered.Cast<object>().ToArray());

            BuildLayout();

            _strategyChoice.SelectedIndexChanged += (_, _) => ShowSelectedStrategy();
            _randomMethod.SelectedIndexChanged += (_, _) => SyncRandom();
            _maskFixedLength.CheckedChanged += (_, _) => _maskLength.Enabled = _maskFixedLength.Checked;
            _shiftRandom.CheckedChanged += (_, _) => SyncShift();
            _cryptoVariable.TextChanged += (_, _) => _cryptoStatus.Text = VariableStatus(_cryptoVariable.Text, _loaded.HasStoredCryptoKey);
            _fpeKeyVariable.TextChanged += (_, _) => UpdateFpeStatus();
            _fpeTweakVariable.TextChanged += (_, _) => UpdateFpeStatus();
            _enableCondition.CheckedChanged += (_, _) => { SyncConditionEnabled(); UpdateConditionPreview(); };
            _conditionField.SelectedIndexChanged += (_, _) => { PopulateOperators(); MarkConditionTouched(); UpdateConditionPreview(); };
            _conditionOp.SelectedIndexChanged += (_, _) => { MarkConditionTouched(); UpdateConditionPreview(); };
            _conditionVal.TextChanged += (_, _) => { MarkConditionTouched(); UpdateConditionPreview(); };
            _ok.Click += OnOk;

            LoadSettings(_loaded);
            LoadCondition();

            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_ok);
        }

        // --- Layout -------------------------------------------------------

        private void BuildLayout()
        {
            var strategy = StackPanel();
            strategy.Controls.Add(Indented(SubLabel("Strategy:"), 0, 8));
            strategy.Controls.Add(Indented(_strategyChoice, 0, 4));
            strategy.Controls.Add(Indented(_strategyDescription, 0, 4, bottom: 6));

            AddPanel(StrategyCatalog.Redact, Stack(
                SubLabel("Redaction format:"), _redactionFormat, Hint("%t is replaced by the filter type.")));
            AddPanel(StrategyCatalog.StaticReplace, Stack(SubLabel("Replace with:"), _staticValue));
            AddPanel(StrategyCatalog.RandomReplace, Stack(
                SubLabel("Kind of value:"), _randomMethod, _randomCandidatesLabel, _randomCandidates, _randomConsistent));
            AddPanel(StrategyCatalog.Mask, Stack(
                Row(SubLabel("Mask character:"), _maskCharacter), _maskSameLength, Row(_maskFixedLength, _maskLength)));
            AddPanel(StrategyCatalog.HashSha256, Stack(_salt));
            AddPanel(StrategyCatalog.Crypto, Stack(
                SubLabel("Environment variable holding the key:"), _cryptoVariable, _cryptoStatus,
                Hint("Set the variable to a 64-character hex AES-256 key (for example, from \"openssl rand -hex 32\"), " +
                     "then restart Philter Desktop. Every filter in this policy that encrypts uses the same key.", wrap: true)));
            AddPanel(StrategyCatalog.Fpe, Stack(
                SubLabel("Environment variable holding the key:"), _fpeKeyVariable,
                SubLabel("Environment variable holding the tweak:"), _fpeTweakVariable, _fpeStatus,
                Hint("Set the key variable to 64 hex characters and the tweak variable to 14 hex characters, then " +
                     "restart Philter Desktop. Every filter in this policy that uses this encryption shares them.", wrap: true)));
            AddPanel(StrategyCatalog.MapReplace, Stack(
                _mappings, Row(SubLabel("For values not in the table:"), _fallback), _caseSensitive, _mapConsistent));
            if (StrategyCatalog.TruncateSettingsSupported)
            {
                AddPanel(StrategyCatalog.Truncate, Stack(
                    Row(SubLabel("Keep"), _truncateLeave, SubLabel("characters"), _truncateEnd),
                    Row(SubLabel("Put this in place of the others:"), _truncateCharacter)));
                _truncateEnd.Items.AddRange(new object[] { "at the start", "at the end" });
            }
            AddPanel(StrategyCatalog.Shift, Stack(
                Row(SubLabel("Years:"), _shiftYears, SubLabel("Months:"), _shiftMonths, SubLabel("Days:"), _shiftDays),
                _shiftRandom, _futureDates));

            _mappings.Columns.Add("value", "Value");
            _mappings.Columns.Add("replacement", "Replacement");
            _randomMethod.Items.AddRange(new object[] { "A realistic value", "A value from a list", "A random ID (UUID)" });
            _fallback.Items.AddRange(StrategyCatalog.Fallbacks(StrategyCatalog.InstalledPhileas).Cast<object>().ToArray());

            foreach (Control panel in _panels.Values)
            {
                panel.Visible = false;
                strategy.Controls.Add(Indented(panel, 0, 6, bottom: 6));
            }

            _conditionField.Items.AddRange(ConditionBuilder.Fields.Cast<object>().ToArray());

            var conditionGrid = new TableLayoutPanel
            {
                ColumnCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(24, 6, 3, 2)
            };
            conditionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            conditionGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            AddGridRow(conditionGrid, "When:", _conditionField);
            AddGridRow(conditionGrid, "Is:", _conditionOp);
            AddGridRow(conditionGrid, "Value:", _conditionVal);

            var condition = StackPanel();
            condition.Controls.Add(Indented(_enableCondition, 0, 10));
            condition.Controls.Add(conditionGrid);
            condition.Controls.Add(Indented(_conditionPreview, 24, 2, bottom: 8));

            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 14, 0, 0)
            };
            _cancel.Margin = new Padding(8, 3, 0, 3);
            buttons.Controls.Add(_cancel);
            buttons.Controls.Add(_ok);

            _root = new TableLayoutPanel
            {
                ColumnCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(14)
            };
            _root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _root.Controls.Add(GroupHost("Filter Strategy", strategy));
            _root.Controls.Add(GroupHost("Conditional", condition));
            _root.Controls.Add(buttons);

            Controls.Add(_root);
        }

        // Size the window to its content once layout/scaling has settled, so the buttons are never
        // clipped (more reliable than Form.AutoSize for a dialog).
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            FitToContent();
            CenterToParent();
        }

        private void FitToContent()
        {
            _root.PerformLayout();
            ClientSize = _root.PreferredSize;
        }

        private void AddPanel(string strategy, Control panel) => _panels[strategy] = panel;

        // --- Layout helpers (all sizing is driven by the layout engine) -------

        private static TableLayoutPanel StackPanel() => new()
        {
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            GrowStyle = TableLayoutPanelGrowStyle.AddRows,
            Margin = Padding.Empty
        };

        private static TableLayoutPanel Stack(params Control[] controls)
        {
            TableLayoutPanel panel = StackPanel();
            panel.Dock = DockStyle.None;
            foreach (Control control in controls)
            {
                panel.Controls.Add(Indented(control, 0, 4));
            }
            return panel;
        }

        private static FlowLayoutPanel Row(params Control[] controls)
        {
            var row = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = Padding.Empty };
            foreach (Control control in controls)
            {
                control.Margin = new Padding(0, 3, 8, 3);
                control.Anchor = AnchorStyles.Left;
                row.Controls.Add(control);
            }
            return row;
        }

        private static Control Indented(Control control, int left, int top, int bottom = 0)
        {
            control.Margin = new Padding(left, top, 3, bottom);
            control.Anchor = AnchorStyles.Left;
            return control;
        }

        private static Label SubLabel(string text) => new() { Text = text, AutoSize = true };

        private static Label Hint(string text, bool wrap = false) => new()
        {
            Text = text,
            AutoSize = true,
            MaximumSize = wrap ? new Size(440, 0) : Size.Empty,
            ForeColor = ModernTheme.SubtleText
        };

        private static GroupBox GroupHost(string title, Control inner)
        {
            var box = new GroupBox
            {
                Text = title,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 4, 10, 10),
                Margin = new Padding(0, 0, 0, 10)
            };
            box.Controls.Add(inner);
            return box;
        }

        private static void AddGridRow(TableLayoutPanel grid, string label, Control control)
        {
            int row = grid.RowCount;
            grid.RowCount = row + 1;
            var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 7, 8, 6) };
            control.Margin = new Padding(0, 3, 0, 3);
            control.Anchor = AnchorStyles.Left;
            grid.Controls.Add(lbl, 0, row);
            grid.Controls.Add(control, 1, row);
        }

        // --- Strategy settings ---------------------------------------------

        internal ComboBox StrategyChoice => _strategyChoice;
        internal ComboBox ConditionFieldChoice => _conditionField;

        private StrategyInfo? SelectedStrategy => _strategyChoice.SelectedItem as StrategyInfo;

        private void ShowSelectedStrategy()
        {
            StrategyInfo? selected = SelectedStrategy;
            _strategyDescription.Text = selected?.Description ?? string.Empty;
            foreach ((string name, Control panel) in _panels)
            {
                panel.Visible = selected is not null && selected != _keptAsIs && name == selected.Name;
            }
            if (IsHandleCreated)
            {
                FitToContent();
            }
        }

        private void SyncRandom()
        {
            bool list = _randomMethod.SelectedIndex == 1;
            _randomCandidatesLabel.Visible = list;
            _randomCandidates.Visible = list;
            if (IsHandleCreated)
            {
                FitToContent();
            }
        }

        private void SyncShift()
        {
            bool random = _shiftRandom.Checked;
            _shiftYears.Enabled = !random;
            _shiftMonths.Enabled = !random;
            _shiftDays.Enabled = !random;
        }

        private void UpdateFpeStatus()
        {
            string key = VariableStatus(_fpeKeyVariable.Text, _loaded.HasStoredFpeKey);
            string tweak = VariableStatus(_fpeTweakVariable.Text, _loaded.HasStoredFpeKey);
            _fpeStatus.Text = key == tweak ? key : key + Environment.NewLine + tweak;
        }

        /// <summary>Whether the named variable is visible to this running copy of Philter Desktop.</summary>
        internal static string VariableStatus(string name, bool hasStoredKey)
        {
            name = name.Trim();
            if (name.Length == 0)
            {
                return hasStoredKey ? "This policy stores the key itself. Enter a variable name to use an environment variable instead." : string.Empty;
            }
            if (!StrategySettings.IsVariableName(name))
            {
                return "Not a valid variable name.";
            }
            return string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name))
                ? $"{name} isn't set for Philter Desktop yet. Redaction with this strategy stops with an error until it is."
                : $"{name} is set.";
        }

        private void LoadSettings(StrategySettings s)
        {
            _redactionFormat.Text = s.RedactionFormat;
            _staticValue.Text = s.StaticReplacement;
            _randomMethod.SelectedIndex = s.RandomMethod switch
            {
                StrategySettings.RandomFromList => 1,
                StrategySettings.RandomUuid => 2,
                _ => 0
            };
            _randomCandidates.Text = string.Join(Environment.NewLine, s.RandomCandidates);
            _randomConsistent.Checked = s.Consistent;
            _maskCharacter.Text = s.MaskCharacter;
            _maskSameLength.Checked = s.MaskLength is null;
            _maskFixedLength.Checked = s.MaskLength is not null;
            _maskLength.Value = Math.Clamp(s.MaskLength ?? 8, (int)_maskLength.Minimum, (int)_maskLength.Maximum);
            _maskLength.Enabled = s.MaskLength is not null;
            _salt.Checked = s.Salt;
            _cryptoVariable.Text = s.CryptoKeyVariable;
            _cryptoStatus.Text = VariableStatus(s.CryptoKeyVariable, s.HasStoredCryptoKey);
            _fpeKeyVariable.Text = s.FpeKeyVariable;
            _fpeTweakVariable.Text = s.FpeTweakVariable;
            UpdateFpeStatus();
            foreach ((string value, string replacement) in s.Mappings)
            {
                _mappings.Rows.Add(value, replacement);
            }
            _fallback.SelectedItem = _fallback.Items.Cast<StrategyInfo>().FirstOrDefault(f => f.Name == s.FallbackStrategy)
                                     ?? _fallback.Items.Cast<StrategyInfo>().FirstOrDefault();
            _caseSensitive.Checked = s.CaseSensitive;
            _mapConsistent.Checked = s.Consistent;
            _shiftYears.Value = Math.Clamp(s.ShiftYears, (int)_shiftYears.Minimum, (int)_shiftYears.Maximum);
            _shiftMonths.Value = Math.Clamp(s.ShiftMonths, (int)_shiftMonths.Minimum, (int)_shiftMonths.Maximum);
            _shiftDays.Value = Math.Clamp(s.ShiftDays, (int)_shiftDays.Minimum, (int)_shiftDays.Maximum);
            _shiftRandom.Checked = s.ShiftRandom;
            _futureDates.Checked = s.FutureDates;
            _truncateLeave.Value = Math.Clamp(s.TruncateLeaveCharacters, (int)_truncateLeave.Minimum, (int)_truncateLeave.Maximum);
            _truncateEnd.SelectedIndex = _truncateEnd.Items.Count == 0 ? -1 : s.TruncateTrailing ? 1 : 0;
            _truncateCharacter.Text = s.TruncateCharacter;
            SyncRandom();
            SyncShift();

            _strategyChoice.SelectedItem = _strategyChoice.Items.Cast<StrategyInfo>().FirstOrDefault(i => i.Name == s.Strategy);
            ShowSelectedStrategy();
        }

        // The dialog's current values, as settings for the selected strategy.
        internal StrategySettings ReadSettings()
        {
            StrategySettings s = StrategySettings.Load(_strategy, _policy);
            s.Strategy = SelectedStrategy?.Name ?? _loaded.Strategy;
            s.RedactionFormat = _redactionFormat.Text;
            s.StaticReplacement = _staticValue.Text;
            s.RandomMethod = _randomMethod.SelectedIndex switch
            {
                1 => StrategySettings.RandomFromList,
                2 => StrategySettings.RandomUuid,
                _ => StrategySettings.RandomRealistic
            };
            s.RandomCandidates = _randomCandidates.Lines.ToList();
            s.Consistent = s.Strategy == StrategyCatalog.MapReplace ? _mapConsistent.Checked : _randomConsistent.Checked;
            s.MaskCharacter = _maskCharacter.Text;
            s.MaskLength = _maskFixedLength.Checked ? (int)_maskLength.Value : null;
            s.Salt = _salt.Checked;
            s.CryptoKeyVariable = _cryptoVariable.Text.Trim();
            s.FpeKeyVariable = _fpeKeyVariable.Text.Trim();
            s.FpeTweakVariable = _fpeTweakVariable.Text.Trim();
            s.Mappings = _mappings.Rows.Cast<DataGridViewRow>()
                .Where(r => !r.IsNewRow)
                .Select(r => new KeyValuePair<string, string>(r.Cells[0].Value?.ToString() ?? string.Empty, r.Cells[1].Value?.ToString() ?? string.Empty))
                .ToList();
            s.FallbackStrategy = (_fallback.SelectedItem as StrategyInfo)?.Name ?? StrategyCatalog.Redact;
            s.CaseSensitive = _caseSensitive.Checked;
            s.ShiftYears = (int)_shiftYears.Value;
            s.ShiftMonths = (int)_shiftMonths.Value;
            s.ShiftDays = (int)_shiftDays.Value;
            s.ShiftRandom = _shiftRandom.Checked;
            s.FutureDates = _futureDates.Checked;
            s.TruncateLeaveCharacters = (int)_truncateLeave.Value;
            s.TruncateTrailing = _truncateEnd.SelectedIndex == 1;
            s.TruncateCharacter = _truncateCharacter.Text;
            return s;
        }

        // --- Condition ---------------------------------------------------------

        // Repopulates the operator list to match the selected field (text vs. numeric operators).
        private void PopulateOperators()
        {
            if (_conditionField.SelectedItem is not ConditionBuilder.ConditionField field)
            {
                return;
            }
            _conditionOp.Items.Clear();
            _conditionOp.Items.AddRange(ConditionBuilder.OperatorsFor(field).Cast<object>().ToArray());
            if (_conditionOp.Items.Count > 0)
            {
                _conditionOp.SelectedIndex = 0;
            }
        }

        private void SyncConditionEnabled()
        {
            bool on = _enableCondition.Checked;
            _conditionField.Enabled = on;
            _conditionOp.Enabled = on;
            _conditionVal.Enabled = on;
        }

        private void MarkConditionTouched()
        {
            if (!_loadingCondition)
            {
                _builderTouched = true;
            }
        }

        private void UpdateConditionPreview()
        {
            if (!string.IsNullOrEmpty(_unparsedCondition) && !_builderTouched)
            {
                _conditionPreview.Text = "Current (advanced) condition will be kept: " + _unparsedCondition;
                return;
            }
            if (_enableCondition.Checked &&
                _conditionField.SelectedItem is ConditionBuilder.ConditionField field &&
                _conditionOp.SelectedItem is ConditionBuilder.ConditionOperator op)
            {
                _conditionPreview.Text = "Condition: " + ConditionBuilder.Build(field, op, _conditionVal.Text);
            }
            else
            {
                _conditionPreview.Text = string.Empty;
            }
        }

        private void LoadCondition()
        {
            _loadingCondition = true;
            if (!string.IsNullOrWhiteSpace(_strategy.Condition))
            {
                _enableCondition.Checked = true;
                if (ConditionBuilder.TryParse(_strategy.Condition, out var f, out var o, out var v))
                {
                    _conditionField.SelectedItem = f; // triggers PopulateOperators
                    _conditionOp.SelectedItem = o;
                    _conditionVal.Text = v;
                }
                else
                {
                    // An advanced condition we can't represent in the builder — preserve it.
                    _unparsedCondition = _strategy.Condition;
                    _conditionField.SelectedIndex = 0;
                }
            }
            else
            {
                _conditionField.SelectedIndex = 0;
            }
            _loadingCondition = false;

            SyncConditionEnabled();
            UpdateConditionPreview();
        }

        // --- Accepting -------------------------------------------------------

        /// <summary>
        /// Validates a "Replace with a fixed value" entry. Returns an error message to show the
        /// user, or <c>null</c> if the value is acceptable. An empty or whitespace-only value is
        /// rejected because it would silently delete the matched text with no visible marker.
        /// </summary>
        internal static string? ValidateStaticReplacement(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? "Enter a value to replace matches with.\r\n\r\n" +
                  "An empty value would delete the matched text with no marker. " +
                  "To insert a placeholder, use a value such as \"REDACTED\"; to use a " +
                  "redaction format instead, choose \"Redact\"."
                : null;

        /// <summary>
        /// Validates the dialog and, when valid, writes it to the strategy (and any encryption key reference
        /// to the policy). Returns the problem to show the user, or null on success. Nothing is written on
        /// failure, so Cancel after a failed OK leaves the strategy as it was.
        /// </summary>
        internal string? Accept()
        {
            StrategySettings settings = ReadSettings();
            bool keep = SelectedStrategy is not null && SelectedStrategy == _keptAsIs;
            string? strategyError = keep ? null : settings.Validate();
            if (strategyError is not null)
            {
                return strategyError;
            }

            string? condition;
            if (!_enableCondition.Checked)
            {
                condition = string.Empty;
            }
            else if (!string.IsNullOrEmpty(_unparsedCondition) && !_builderTouched)
            {
                condition = _unparsedCondition; // keep the advanced condition as-is
            }
            else
            {
                var field = (ConditionBuilder.ConditionField)_conditionField.SelectedItem!;
                var op = (ConditionBuilder.ConditionOperator)_conditionOp.SelectedItem!;
                string value = _conditionVal.Text;
                if (field.Numeric && !ConditionBuilder.IsValidNumericValue(value))
                {
                    return $"Enter a plain number for \"{field.Display}\": digits only, with an optional decimal " +
                           "point (for example 0.85). Signs, exponents (1E3), and thousands separators aren't allowed.";
                }
                if (!field.Numeric && value.Contains('"'))
                {
                    return "The value can't contain a double-quote (\") character.";
                }
                condition = ConditionBuilder.Build(field, op, value);
            }

            if (!keep)
            {
                settings.ApplyTo(_strategy, _policy);
            }
            _strategy.Condition = condition;
            return null;
        }

        private void OnOk(object? sender, EventArgs e)
        {
            string? error = Accept();
            if (error is not null)
            {
                MessageBox.Show(this, error, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None; // keep the dialog open
            }
        }
    }
}
