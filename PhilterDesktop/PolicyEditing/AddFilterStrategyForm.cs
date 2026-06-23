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
    /// </summary>
    internal sealed class AddFilterStrategyForm : Form
    {
        private readonly AbstractFilterStrategy _strategy;

        private readonly RadioButton _redactRadio = new() { Text = "Redact (replace with a redaction format)", AutoSize = true };
        private readonly RadioButton _staticRadio = new() { Text = "Replace with a static value", AutoSize = true };
        private readonly RadioButton _randomRadio = new() { Text = "Replace with a random value", AutoSize = true };
        private readonly TextBox _redactionFormat = new() { Text = "{{{REDACTED-%t}}}" };
        private readonly TextBox _staticValue = new();
        private readonly CheckBox _scopeContext = new() { Text = "Replace consistently across document contexts", AutoSize = true };
        private readonly CheckBox _enableCondition = new() { Text = "Only apply when:", AutoSize = true };
        private readonly TextBox _conditionValue = new();
        private readonly Button _ok = new() { Text = "OK", DialogResult = DialogResult.OK };
        private readonly Button _cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel };

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
            ClientSize = new Size(460, 340);
            AcceptButton = _ok;
            CancelButton = _cancel;

            BuildLayout();

            _redactRadio.CheckedChanged += (_, _) => SyncEnabled();
            _staticRadio.CheckedChanged += (_, _) => SyncEnabled();
            _randomRadio.CheckedChanged += (_, _) => SyncEnabled();
            _enableCondition.CheckedChanged += (_, _) => _conditionValue.Enabled = _enableCondition.Checked;
            _ok.Click += OnOk;

            LoadFromStrategy();
            SyncEnabled();

            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_ok);
        }

        private void BuildLayout()
        {
            var strategyBox = new GroupBox { Text = "Filter Strategy", Location = new Point(12, 12), Size = new Size(436, 210) };

            _redactRadio.Location = new Point(15, 25);
            var fmtLabel = new Label { Text = "Redaction format:", AutoSize = true, Location = new Point(35, 50) };
            _redactionFormat.SetBounds(150, 47, 270, 23);
            var fmtHint = new Label { Text = "%t is replaced by the filter type.", AutoSize = true, ForeColor = ModernTheme.SubtleText, Location = new Point(150, 73) };

            _staticRadio.Location = new Point(15, 100);
            var staticLabel = new Label { Text = "Static value:", AutoSize = true, Location = new Point(35, 125) };
            _staticValue.SetBounds(150, 122, 270, 23);

            _randomRadio.Location = new Point(15, 155);
            _scopeContext.Location = new Point(35, 180);

            strategyBox.Controls.AddRange(new Control[]
            {
                _redactRadio, fmtLabel, _redactionFormat, fmtHint,
                _staticRadio, staticLabel, _staticValue,
                _randomRadio, _scopeContext
            });

            var conditionBox = new GroupBox { Text = "Conditional", Location = new Point(12, 228), Size = new Size(436, 50) };
            _enableCondition.Location = new Point(15, 20);
            _conditionValue.SetBounds(150, 17, 270, 23);
            conditionBox.Controls.AddRange(new Control[] { _enableCondition, _conditionValue });

            _ok.SetBounds(262, 294, 90, 34);
            _cancel.SetBounds(358, 294, 90, 34);

            Controls.AddRange(new Control[] { strategyBox, conditionBox, _ok, _cancel });
        }

        private void SyncEnabled()
        {
            _redactionFormat.Enabled = _redactRadio.Checked;
            _staticValue.Enabled = _staticRadio.Checked;
            _scopeContext.Enabled = _randomRadio.Checked;
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

            if (!string.IsNullOrWhiteSpace(_strategy.Condition))
            {
                _enableCondition.Checked = true;
                _conditionValue.Text = _strategy.Condition;
            }
            _conditionValue.Enabled = _enableCondition.Checked;
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

            _strategy.Condition = _enableCondition.Checked ? _conditionValue.Text : string.Empty;
        }
    }
}
