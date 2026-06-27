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

using PhilterData;

namespace PhilterDesktop
{
    /// <summary>
    /// Shows the result of a post-redaction verification pass: a clear "all clear" when nothing
    /// detectable remains, or a loud, itemized list of residual findings (type, the text still present,
    /// and its location) so the user can fix the redaction before relying on the output.
    /// </summary>
    public partial class VerificationResultForm : Form
    {
        private static readonly Color WarnColor = Color.FromArgb(0x8A, 0x1C, 0x1C);
        private static readonly Color OkColor = Color.FromArgb(0x1B, 0x5E, 0x20);

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public VerificationResultForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
        }

        internal VerificationResultForm(string fileName, VerificationOutcome outcome) : this()
        {
            switch (outcome.Status)
            {
                case VerificationStatus.Clean:
                    _summary.ForeColor = OkColor;
                    _summary.Text = $"No detectable PII remains in {fileName}.\nThe redacted output passed verification.";
                    _list.Visible = false;
                    break;

                case VerificationStatus.ResidualsFound:
                    _summary.ForeColor = WarnColor;
                    _summary.Text =
                        $"{outcome.Count} possible item{(outcome.Count == 1 ? "" : "s")} may still be present in {fileName}.\n" +
                        "Review the redaction (adjust the policy or use Modify Redaction) before sharing this file.";
                    PopulateFindings(outcome.Residuals);
                    break;

                case VerificationStatus.Error:
                default:
                    _summary.ForeColor = WarnColor;
                    _summary.Text = $"Verification could not be completed for {fileName}.\n{outcome.Error}";
                    _list.Visible = false;
                    break;
            }
        }

        private void PopulateFindings(IReadOnlyList<RedactionSpanEntity> residuals)
        {
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (RedactionSpanEntity s in residuals)
            {
                var item = new ListViewItem(RedactionReport.FriendlyType(s));
                item.SubItems.Add(s.Text);
                item.SubItems.Add(RedactionReport.LocationOf(s));
                _list.Items.Add(item);
            }
            _list.EndUpdate();
        }
    }
}
