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
        private static readonly Color CaveatColor = Color.DarkOrange;

        private readonly LinkLabel _scopeLink;

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public VerificationResultForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);

            // This check runs the same policy against the output; it does not measure the policy's
            // precision and recall against known data. Point users who want that to Philter Scope.
            // Sits at the bottom-left next to Close; tagged utm_medium=verification.
            _scopeLink = Links.CreateLink(
                "Score this policy against known data with Philter Scope →",
                Links.ScopeUrl("verification"));
            _scopeLink.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _scopeLink.Location = new Point(14, 460);
            Controls.Add(_scopeLink);
            _scopeLink.BringToFront();
        }

        internal VerificationResultForm(string fileName, VerificationOutcome outcome) : this()
        {
            // Offer policy scoring whenever a verification actually ran (clean or residuals found),
            // but not when the check itself failed.
            _scopeLink.Visible = outcome.Status != VerificationStatus.Error;

            switch (outcome.Status)
            {
                case VerificationStatus.Clean:
                    // A fidelity caveat (e.g. an RTF's headers/footers weren't re-scanned) tempers the
                    // "all clear": show it in amber with the note so "clean" doesn't overstate integrity.
                    _summary.ForeColor = outcome.FidelityNote is null ? OkColor : CaveatColor;
                    _summary.Text = outcome.FidelityNote is null
                        ? $"No detectable PII remains in {fileName}.\nThe redacted output passed verification."
                        : $"No detectable PII remains in the body text of {fileName}.\n\n{outcome.FidelityNote}";
                    _list.Visible = false;
                    break;

                case VerificationStatus.ResidualsFound:
                    _summary.ForeColor = WarnColor;
                    _summary.Text =
                        $"{outcome.Count} possible item{(outcome.Count == 1 ? "" : "s")} may still be present in {fileName}.\n" +
                        "Review the redaction (adjust the policy or use Modify Redaction) before sharing this file." +
                        (outcome.FidelityNote is null ? "" : $"\n\n{outcome.FidelityNote}");
                    PopulateFindings(outcome.Residuals, Path.GetExtension(fileName));
                    break;

                case VerificationStatus.Error:
                default:
                    _summary.ForeColor = WarnColor;
                    _summary.Text = $"Verification could not be completed for {fileName}.\n{outcome.Error}";
                    _list.Visible = false;
                    break;
            }
        }

        private void PopulateFindings(IReadOnlyList<RedactionSpanEntity> residuals, string fileType)
        {
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (RedactionSpanEntity s in residuals)
            {
                var item = new ListViewItem(RedactionReport.FriendlyType(s));
                item.SubItems.Add(s.Text);
                item.SubItems.Add(RedactionReport.LocationOf(s, fileType));
                _list.Items.Add(item);
            }
            _list.EndUpdate();
        }
    }
}
