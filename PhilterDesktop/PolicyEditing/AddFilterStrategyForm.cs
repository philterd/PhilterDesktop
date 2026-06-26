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

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// Edits a single filter strategy. Replaces the 19 near-identical VB
    /// "AddXFilterStrategyForm" dialogs with one form that works against the
    /// shared <see cref="AbstractFilterStrategy"/> base type.
    ///
    /// The layout is built entirely from layout panels (no absolute coordinates), so it
    /// stays correct at any DPI / font scaling.
    /// </summary>
    internal sealed class AddFilterStrategyForm : Form
    {
        private readonly AbstractFilterStrategy _strategy;

        private readonly RadioButton _redactRadio = new() { Text = "Redact (replace with a redaction format)", AutoSize = true };
        private readonly RadioButton _staticRadio = new() { Text = "Replace with a static value", AutoSize = true };
        private readonly RadioButton _randomRadio = new() { Text = "Replace with a random value", AutoSize = true };
        private readonly TextBox _redactionFormat = new() { Text = "{{{REDACTED-%t}}}", Width = 440 };
        private readonly TextBox _staticValue = new() { Width = 440 };
        private readonly CheckBox _scopeContext = new() { Text = "Replace consistently across document contexts", AutoSize = true };
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

        public AddFilterStrategyForm(AbstractFilterStrategy strategy, string filterTypeDisplay)
        {
            _strategy = strategy;

            Text = $"{filterTypeDisplay} Filter Strategy";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;

            BuildLayout();

            _redactRadio.CheckedChanged += (_, _) => SyncEnabled();
            _staticRadio.CheckedChanged += (_, _) => SyncEnabled();
            _randomRadio.CheckedChanged += (_, _) => SyncEnabled();
            _enableCondition.CheckedChanged += (_, _) => { SyncConditionEnabled(); UpdateConditionPreview(); };
            _conditionField.SelectedIndexChanged += (_, _) => { PopulateOperators(); MarkConditionTouched(); UpdateConditionPreview(); };
            _conditionOp.SelectedIndexChanged += (_, _) => { MarkConditionTouched(); UpdateConditionPreview(); };
            _conditionVal.TextChanged += (_, _) => { MarkConditionTouched(); UpdateConditionPreview(); };
            _ok.Click += OnOk;

            LoadFromStrategy();
            SyncEnabled();

            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_ok);
        }

        private void BuildLayout()
        {
            var strategy = StackPanel();
            strategy.Controls.Add(Indented(_redactRadio, 0, 10));
            strategy.Controls.Add(Indented(SubLabel("Redaction format:"), 24, 12));
            strategy.Controls.Add(Indented(_redactionFormat, 24, 4));
            strategy.Controls.Add(Indented(Hint("%t is replaced by the filter type."), 24, 4, bottom: 14));
            strategy.Controls.Add(Indented(_staticRadio, 0, 12));
            strategy.Controls.Add(Indented(SubLabel("Static value:"), 24, 12));
            strategy.Controls.Add(Indented(_staticValue, 24, 4, bottom: 14));
            strategy.Controls.Add(Indented(_randomRadio, 0, 12));
            strategy.Controls.Add(Indented(_scopeContext, 24, 8, bottom: 8));

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
            ClientSize = _root.PreferredSize;
            CenterToParent();
        }

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

        private static Control Indented(Control control, int left, int top, int bottom = 0)
        {
            control.Margin = new Padding(left, top, 3, bottom);
            control.Anchor = AnchorStyles.Left;
            return control;
        }

        private static Label SubLabel(string text) => new() { Text = text, AutoSize = true };

        private static Label Hint(string text) => new() { Text = text, AutoSize = true, ForeColor = ModernTheme.SubtleText };

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

        private void SyncEnabled()
        {
            _redactionFormat.Enabled = _redactRadio.Checked;
            _staticValue.Enabled = _staticRadio.Checked;
            _scopeContext.Enabled = _randomRadio.Checked;
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

        private void LoadFromStrategy()
        {
            switch (_strategy.Strategy)
            {
                case AbstractFilterStrategy.StaticReplace:
                    _staticRadio.Checked = true;
                    _staticValue.Text = _strategy.StaticReplacement ?? string.Empty;
                    break;
                case AbstractFilterStrategy.RandomReplace:
                    _randomRadio.Checked = true;
                    _scopeContext.Checked = _strategy.ReplacementScope == AbstractFilterStrategy.ReplacementScopeContext;
                    break;
                default:
                    _redactRadio.Checked = true;
                    _redactionFormat.Text = string.IsNullOrEmpty(_strategy.RedactionFormat)
                        ? "{{{REDACTED-%t}}}"
                        : _strategy.RedactionFormat;
                    break;
            }

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

        private void OnOk(object? sender, EventArgs e)
        {
            if (_staticRadio.Checked)
            {
                _strategy.Strategy = AbstractFilterStrategy.StaticReplace;
                _strategy.StaticReplacement = _staticValue.Text;
            }
            else if (_randomRadio.Checked)
            {
                _strategy.Strategy = AbstractFilterStrategy.RandomReplace;
                _strategy.ReplacementScope = _scopeContext.Checked
                    ? AbstractFilterStrategy.ReplacementScopeContext
                    : AbstractFilterStrategy.ReplacementScopeDocument;
            }
            else
            {
                _strategy.Strategy = AbstractFilterStrategy.Redact;
                _strategy.RedactionFormat = _redactionFormat.Text;
            }

            if (!_enableCondition.Checked)
            {
                _strategy.Condition = string.Empty;
                return;
            }

            if (!string.IsNullOrEmpty(_unparsedCondition) && !_builderTouched)
            {
                _strategy.Condition = _unparsedCondition; // keep the advanced condition as-is
                return;
            }

            var field = (ConditionBuilder.ConditionField)_conditionField.SelectedItem!;
            var op = (ConditionBuilder.ConditionOperator)_conditionOp.SelectedItem!;
            string value = _conditionVal.Text;

            if (field.Numeric && !double.TryParse(value.Trim(), out _))
            {
                MessageBox.Show(this, $"Enter a number for \"{field.Display}\".",
                    "Condition", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None; // keep the dialog open
                return;
            }
            if (!field.Numeric && value.Contains('"'))
            {
                MessageBox.Show(this, "The value can't contain a double-quote (\") character.",
                    "Condition", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            _strategy.Condition = ConditionBuilder.Build(field, op, value);
        }
    }
}
