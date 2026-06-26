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

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// A short, question-driven wizard that builds a starting-point policy: pick a use case, confirm
    /// which kinds of information to remove (a category checklist discovered from the engine, so it
    /// never drifts), choose how replacements look, then name and review. Produces schema-valid policy
    /// JSON via <see cref="PolicyWizard"/>.
    /// </summary>
    internal sealed class PolicyWizardForm : Form
    {
        private readonly Func<string, bool> _nameExists;

        // Use-case options → the template whose enabled filters seed the checklist (null = blank).
        private static readonly (string Label, string? TemplateId)[] UseCases =
        {
            ("General office / everyday documents", "common-pii"),
            ("Healthcare (HIPAA-style)", "hipaa-safe-harbor"),
            ("Legal / court filings", "legal-court-filing"),
            ("Financial records", "financial-records"),
            ("Start from scratch (I'll choose everything)", null),
        };

        private string? _baselineTemplateId = "common-pii";
        private HashSet<string> _selected;
        private bool _includeNames;
        private PolicyWizard.ReplacementStyle _style = PolicyWizard.ReplacementStyle.LabeledMarker;
        private string _policyName = "My Policy";

        private int _step;
        private const int LastStep = 3;

        private readonly Panel _content = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(16, 12, 16, 8) };
        private readonly Label _heading = new() { Dock = DockStyle.Top, AutoSize = false, Height = 36, Padding = new Padding(16, 8, 16, 0), Font = new Font(ModernTheme.UiFont.FontFamily, 11f, FontStyle.Bold) };
        private readonly Button _back = new() { Text = "Back", Size = ModernTheme.StandardButtonSize, Enabled = false };
        private readonly Button _next = new() { Text = "Next", Size = ModernTheme.StandardButtonSize };
        private readonly Button _cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = ModernTheme.StandardButtonSize };

        /// <summary>The policy name entered by the user (valid only after the dialog returns OK).</summary>
        public string ResultName { get; private set; } = string.Empty;

        /// <summary>The built, schema-valid policy JSON (valid only after the dialog returns OK).</summary>
        public string ResultJson { get; private set; } = string.Empty;

        public PolicyWizardForm(Func<string, bool> nameExists)
        {
            _nameExists = nameExists;
            (_selected, _includeNames) = PolicyWizard.BaselineFrom(_baselineTemplateId!);

            Text = "New Policy Wizard";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(640, 600);

            var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 52, Padding = new Padding(8) };
            _cancel.Margin = _next.Margin = _back.Margin = new Padding(6, 3, 0, 3);
            bar.Controls.Add(_cancel);
            bar.Controls.Add(_next);
            bar.Controls.Add(_back);

            Controls.Add(_content);
            Controls.Add(_heading);
            Controls.Add(bar);

            _back.Click += (_, _) => { if (_step > 0) { _step--; ShowStep(); } };
            _next.Click += OnNext;

            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_next);

            ShowStep();
        }

        private void OnNext(object? sender, EventArgs e)
        {
            if (_step < LastStep)
            {
                _step++;
                ShowStep();
                return;
            }
            Finish();
        }

        private void ShowStep()
        {
            _content.SuspendLayout();
            _content.Controls.Clear();
            switch (_step)
            {
                case 0: BuildUseCaseStep(); break;
                case 1: BuildFiltersStep(); break;
                case 2: BuildReplacementStep(); break;
                default: BuildReviewStep(); break;
            }
            _content.ResumeLayout();

            _back.Enabled = _step > 0;
            _next.Text = _step == LastStep ? "Finish" : "Next";
        }

        // Dock=Top (not Fill) with AutoSize so the host panel's AutoScroll can scroll long steps —
        // a Fill child sizes to the viewport and clips its overflow instead of scrolling.
        private static FlowLayoutPanel Column() => new()
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        // Step 1: use case.
        private void BuildUseCaseStep()
        {
            _heading.Text = "1. What kind of documents are you redacting?";
            var col = Column();
            col.Controls.Add(new Label { Text = "This sets a sensible starting point. You can adjust everything on the next step.", AutoSize = true, ForeColor = ModernTheme.SubtleText, Margin = new Padding(3, 0, 3, 10) });

            foreach ((string label, string? id) in UseCases)
            {
                var radio = new RadioButton { Text = label, AutoSize = true, Checked = _baselineTemplateId == id, Margin = new Padding(3, 4, 3, 4) };
                radio.CheckedChanged += (_, _) =>
                {
                    if (!radio.Checked)
                    {
                        return;
                    }
                    _baselineTemplateId = id;
                    (_selected, _includeNames) = id is null
                        ? (new HashSet<string>(StringComparer.Ordinal), false)
                        : PolicyWizard.BaselineFrom(id);
                };
                col.Controls.Add(radio);
            }
            _content.Controls.Add(col);
        }

        // Step 2: which kinds of information to remove. Categories are laid out in two balanced columns
        // (each category is its own panel) so the whole checklist fits without much scrolling.
        private void BuildFiltersStep()
        {
            _heading.Text = "2. What should be removed?";

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = Padding.Empty
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            FlowLayoutPanel left = ColumnPanel();
            FlowLayoutPanel right = ColumnPanel();
            grid.Controls.Add(left, 0, 0);
            grid.Controls.Add(right, 1, 0);

            // Build each category (and the Names group) as a panel, then spread them across the two
            // columns to roughly balance height.
            var groups = new List<(FlowLayoutPanel Panel, int Weight)>();

            foreach (IGrouping<string, FilterCatalog.FilterInfo> group in FilterCatalog.Grouped())
            {
                FlowLayoutPanel panel = GroupPanel(group.Key);
                foreach (FilterCatalog.FilterInfo f in group)
                {
                    panel.Controls.Add(FilterCheckBox(f.Display, _selected.Contains(f.Property), f.Property));
                }
                groups.Add((panel, group.Count() + 1));
            }

            FlowLayoutPanel namesPanel = GroupPanel("Names");
            var namesCb = new CheckBox { Text = "People's names (on-device AI)", AutoSize = true, Checked = _includeNames, Margin = new Padding(16, 1, 3, 1) };
            namesCb.CheckedChanged += (_, _) => _includeNames = namesCb.Checked;
            namesPanel.Controls.Add(namesCb);
            groups.Add((namesPanel, 2));

            int leftWeight = 0, rightWeight = 0;
            foreach ((FlowLayoutPanel panel, int weight) in groups)
            {
                if (leftWeight <= rightWeight) { left.Controls.Add(panel); leftWeight += weight; }
                else { right.Controls.Add(panel); rightWeight += weight; }
            }

            _content.Controls.Add(grid);
        }

        private static FlowLayoutPanel ColumnPanel() => new()
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = Padding.Empty
        };

        private FlowLayoutPanel GroupPanel(string title)
        {
            var panel = ColumnPanel();
            panel.Margin = new Padding(0, 0, 0, 8);
            panel.Controls.Add(new Label { Text = title, AutoSize = true, Font = new Font(ModernTheme.UiFont, FontStyle.Bold), Margin = new Padding(3, 6, 3, 2) });
            return panel;
        }

        private CheckBox FilterCheckBox(string display, bool isChecked, string property)
        {
            var cb = new CheckBox { Text = display, AutoSize = true, Checked = isChecked, Margin = new Padding(16, 1, 3, 1) };
            cb.CheckedChanged += (_, _) =>
            {
                if (cb.Checked) { _selected.Add(property); } else { _selected.Remove(property); }
            };
            return cb;
        }

        // Step 3: how to replace what's found.
        private void BuildReplacementStep()
        {
            _heading.Text = "3. How should the removed information be replaced?";
            var col = Column();

            AddStyleRadio(col, PolicyWizard.ReplacementStyle.LabeledMarker,
                "A label naming what was removed", "e.g. a Social Security number becomes {{{REDACTED-SSN}}}");
            AddStyleRadio(col, PolicyWizard.ReplacementStyle.FixedWord,
                $"A fixed word", $"every removed item becomes \"{PolicyWizard.FixedWordValue}\"");
            AddStyleRadio(col, PolicyWizard.ReplacementStyle.ConsistentStandIn,
                "A consistent stand-in value", "the same original always maps to the same stand-in");

            _content.Controls.Add(col);
        }

        private void AddStyleRadio(FlowLayoutPanel col, PolicyWizard.ReplacementStyle style, string title, string detail)
        {
            var radio = new RadioButton { Text = title, AutoSize = true, Checked = _style == style, Margin = new Padding(3, 8, 3, 0) };
            radio.CheckedChanged += (_, _) => { if (radio.Checked) { _style = style; } };
            col.Controls.Add(radio);
            col.Controls.Add(new Label { Text = detail, AutoSize = true, ForeColor = ModernTheme.SubtleText, Margin = new Padding(22, 0, 3, 2) });
        }

        // Step 4: name and review.
        private void BuildReviewStep()
        {
            _heading.Text = "4. Name your policy";
            var col = Column();

            col.Controls.Add(new Label { Text = "Policy name:", AutoSize = true, Margin = new Padding(3, 4, 3, 2) });
            var nameBox = new TextBox { Text = _policyName, Width = 320, Margin = new Padding(3, 0, 3, 12) };
            nameBox.TextChanged += (_, _) => _policyName = nameBox.Text;
            col.Controls.Add(nameBox);

            col.Controls.Add(new Label { Text = "Summary", AutoSize = true, Font = new Font(ModernTheme.UiFont, FontStyle.Bold), Margin = new Padding(3, 6, 3, 2) });
            col.Controls.Add(new Label { Text = BuildSummary(), AutoSize = true, ForeColor = ModernTheme.SubtleText, Margin = new Padding(3, 0, 3, 10) });

            col.Controls.Add(new Label
            {
                Text = "This is a starting point, not a compliance guarantee. Review the policy and always check " +
                       "redacted documents before relying on them.",
                AutoSize = true,
                MaximumSize = new Size(500, 0),
                ForeColor = Color.FromArgb(176, 92, 0),
                Margin = new Padding(3, 4, 3, 4)
            });

            _content.Controls.Add(col);
        }

        private string BuildSummary()
        {
            string useCase = UseCases.FirstOrDefault(u => u.TemplateId == _baselineTemplateId).Label ?? "Custom";
            int count = _selected.Count + (_includeNames ? 1 : 0);
            string replacement = _style switch
            {
                PolicyWizard.ReplacementStyle.FixedWord => $"replaced with \"{PolicyWizard.FixedWordValue}\"",
                PolicyWizard.ReplacementStyle.ConsistentStandIn => "replaced with a consistent stand-in",
                _ => "replaced with a label (e.g. {{{REDACTED-SSN}}})",
            };
            return $"Starting point: {useCase}" + Environment.NewLine +
                   $"Removing {count} kind{(count == 1 ? "" : "s")} of information" + Environment.NewLine +
                   $"Each is {replacement}.";
        }

        private void Finish()
        {
            string name = _policyName.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show(this, "Please enter a name for the policy.", "New Policy Wizard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
            if (_nameExists(name))
            {
                MessageBox.Show(this, $"A policy named '{name}' already exists.", "New Policy Wizard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }
            if (_selected.Count == 0 && !_includeNames)
            {
                MessageBox.Show(this, "Choose at least one kind of information to remove (step 2).", "New Policy Wizard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            string json = PolicyWizard.BuildJson(name, _selected, _includeNames, _style);
            PolicyValidationResult validation = PolicyValidator.Validate(json);
            if (!validation.IsValid)
            {
                MessageBox.Show(this,
                    "The wizard produced a policy that didn't pass validation:" + Environment.NewLine + Environment.NewLine +
                    string.Join(Environment.NewLine, validation.Errors.Take(10)),
                    "New Policy Wizard", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            ResultName = name;
            ResultJson = json;
            DialogResult = DialogResult.OK;
        }
    }
}
