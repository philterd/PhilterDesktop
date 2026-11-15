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
    /// Shows the result of a leftover check: a headline coloured by the result, what it means, any caveat,
    /// and, when something was found, a list of each item (type, the text still present, and where). The
    /// window fits its content; it only grows and resizes when there's a list to show.
    /// </summary>
    public sealed class VerificationResultForm : Form
    {
        private static readonly Color OkColor = Color.FromArgb(0x1B, 0x5E, 0x20);
        private static readonly Color WarnColor = Color.FromArgb(0x8A, 0x1C, 0x1C);
        private static readonly Color CaveatColor = Color.FromArgb(0x8A, 0x4B, 0x00);
        private const int TextWidth = 560;

        private const string Disclaimer =
            "This check is not a guarantee that all sensitive information was correctly identified and removed. " +
            "Review the redacted file carefully before sharing it.";

        private readonly Label _headline = new()
        {
            AutoSize = true,
            MaximumSize = new Size(TextWidth, 0),
            Margin = new Padding(0, 0, 0, 6)
        };
        private readonly Label _summary = new() { AutoSize = true, MaximumSize = new Size(TextWidth, 0), Margin = new Padding(0, 0, 0, 8) };
        private readonly Label _note = new() { AutoSize = true, MaximumSize = new Size(TextWidth, 0), Margin = new Padding(0, 0, 0, 8), Visible = false };
        private readonly ListView _list = new()
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            Margin = new Padding(0, 4, 0, 10),
            Visible = false
        };
        private readonly Label _disclaimer = new() { Text = Disclaimer, AutoSize = true, MaximumSize = new Size(TextWidth, 0), Margin = new Padding(0, 0, 0, 12) };
        private readonly Button _close = new() { Text = "&Close", DialogResult = DialogResult.OK, Size = new Size(110, 34), Anchor = AnchorStyles.Right };
        private readonly TableLayoutPanel _layout = new()
        {
            ColumnCount = 1,
            Padding = new Padding(18, 16, 18, 14),
            MinimumSize = new Size(TextWidth + 36, 0), // the same width whatever the result
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };

        /// <summary>An empty result window (for the designer and smoke tests).</summary>
        public VerificationResultForm()
        {
            Text = "Leftover Check";
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            AcceptButton = _close;
            CancelButton = _close;

            _list.Columns.Add("Type", 150);
            _list.Columns.Add("Text still present", 250);
            _list.Columns.Add("Location", 150);
            _list.Resize += (_, _) => FitTextColumn();

            // One control per row; hidden ones collapse. The list's row stretches once it's shown.
            Control[] rows = { _headline, _summary, _note, _list, _disclaimer, _close };
            _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _layout.RowCount = rows.Length;
            for (int row = 0; row < rows.Length; row++)
            {
                _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                _layout.Controls.Add(rows[row], 0, row);
            }
            Controls.Add(_layout);

            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_close);
            _headline.Font = new Font(ModernTheme.UiFont.FontFamily, 12f, FontStyle.Bold);
            _disclaimer.ForeColor = ModernTheme.SubtleText;
            _note.ForeColor = CaveatColor;
        }

        internal VerificationResultForm(string fileName, VerificationOutcome outcome) : this()
        {
            switch (outcome.Status)
            {
                case VerificationStatus.Clean:
                    // A caveat (e.g. an RTF's headers/footers weren't re-scanned) means it still needs a look.
                    bool caveat = outcome.FidelityNote is not null;
                    SetHeadline(caveat ? "Review needed" : "No leftover PII found", caveat ? CaveatColor : OkColor);
                    _summary.Text = caveat
                        ? $"No leftover PII was detected in the body text of {fileName}."
                        : $"No leftover PII was detected in {fileName}.";
                    ShowNote(outcome.FidelityNote);
                    break;

                case VerificationStatus.ResidualsFound:
                    SetHeadline($"{outcome.Count} possible item{(outcome.Count == 1 ? "" : "s")} of leftover PII", WarnColor);
                    _summary.Text =
                        $"{(outcome.Count == 1 ? "This may" : "These may")} still be present in {fileName}. " +
                        "Fix the redaction (adjust the policy or use Modify Redaction) before sharing this file.";
                    ShowNote(outcome.FidelityNote);
                    ShowFindings(outcome.Residuals, Path.GetExtension(fileName));
                    break;

                case VerificationStatus.Error:
                default:
                    SetHeadline("The check could not be completed", WarnColor);
                    _summary.Text = $"{fileName} wasn't checked." +
                        (string.IsNullOrWhiteSpace(outcome.Error) ? "" : $" {outcome.Error}");
                    break;
            }
        }

        private void SetHeadline(string text, Color color)
        {
            _headline.Text = text;
            _headline.ForeColor = color;
        }

        private void ShowNote(string? note)
        {
            _note.Text = note ?? string.Empty;
            _note.Visible = note is not null;
        }

        // With findings, the list takes the spare space and the window becomes resizable.
        private void ShowFindings(IReadOnlyList<RedactionSpanEntity> residuals, string fileType)
        {
            _list.BeginUpdate();
            foreach (RedactionSpanEntity s in residuals)
            {
                var item = new ListViewItem(RedactionReport.FriendlyType(s));
                item.SubItems.Add(s.Text);
                item.SubItems.Add(RedactionReport.LocationOf(s, fileType));
                _list.Items.Add(item);
            }
            _list.EndUpdate();
            _list.Visible = true;

            AutoSize = false;
            FormBorderStyle = FormBorderStyle.Sizable;
            _layout.AutoSize = false;
            _layout.Dock = DockStyle.Fill;
            _layout.RowStyles[_layout.GetRow(_list)] = new RowStyle(SizeType.Percent, 100);
            ClientSize = new Size(LogicalToDeviceUnits(TextWidth + 36), LogicalToDeviceUnits(440));
            MinimumSize = new Size(LogicalToDeviceUnits(460), LogicalToDeviceUnits(360));
            FitTextColumn();
        }

        // The text column takes whatever the type and location columns leave.
        private void FitTextColumn()
        {
            if (_list.Columns.Count < 3)
            {
                return;
            }
            int spare = _list.ClientSize.Width - _list.Columns[0].Width - _list.Columns[2].Width;
            _list.Columns[1].Width = Math.Max(LogicalToDeviceUnits(120), spare);
        }
    }
}
