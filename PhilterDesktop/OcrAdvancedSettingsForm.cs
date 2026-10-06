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

namespace PhilterDesktop
{
    /// <summary>
    /// Advanced OCR tuning: the two thresholds that decide when a PDF page is OCR'd. Values are shown as
    /// percentages of the page area; <see cref="TextCoverageThreshold"/> / <see cref="ImageCoverageThreshold"/>
    /// expose them back as fractions (0–1). Built in code (no designer) since it's a small dialog.
    /// </summary>
    internal sealed class OcrAdvancedSettingsForm : Form
    {
        private readonly NumericUpDown _textCoverage;
        private readonly NumericUpDown _imageCoverage;
        private readonly NumericUpDown _maxPages;

        /// <summary>Text-coverage threshold as a fraction (0–1): below this a page is treated as scanned.</summary>
        public double TextCoverageThreshold => (double)_textCoverage.Value / 100.0;

        /// <summary>Image-coverage threshold as a fraction (0–1): at/above this a text page is also OCR'd.</summary>
        public double ImageCoverageThreshold => (double)_imageCoverage.Value / 100.0;

        /// <summary>Maximum pages to OCR in one PDF before redaction stops with an error (0 = no limit).</summary>
        public int MaxPages => (int)_maxPages.Value;

        public OcrAdvancedSettingsForm(double textCoverageFraction, double imageCoverageFraction, int maxPages)
        {
            Text = "Advanced OCR Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            // The dialog fits its content: a fixed-width table whose rows grow with their (wrapping) text.
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            var layout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 3,
                MinimumSize = new Size(440, 0),
                MaximumSize = new Size(440, 0),
                Padding = new Padding(11, 9, 11, 9)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            void AddWide(Control control)
            {
                layout.Controls.Add(control);
                layout.SetColumnSpan(control, 3);
            }

            void AddSetting(string label, NumericUpDown input, string? unit)
            {
                layout.Controls.Add(Wrapping(label));
                input.Anchor = AnchorStyles.Left;
                input.Width = 90;
                layout.Controls.Add(input);
                layout.Controls.Add(new Label { Text = unit ?? string.Empty, AutoSize = true, Anchor = AnchorStyles.Left });
            }

            Label Hint(string text)
            {
                Label hint = Wrapping(text);
                hint.ForeColor = SystemColors.GrayText;
                hint.Margin = new Padding(3, 0, 3, 12);
                return hint;
            }

            AddWide(Wrapping("These control when a PDF page is read with OCR. The defaults suit most documents; " +
                             "lower values mean OCR runs on more pages (slower, but less likely to miss anything)."));
            layout.GetControlFromPosition(0, 0)!.Margin = new Padding(3, 0, 3, 12);

            _textCoverage = new NumericUpDown
            {
                DecimalPlaces = 1,
                Minimum = 0.1m,
                Maximum = 50m,
                Increment = 0.5m,
                Value = ClampPercent(textCoverageFraction, 0.1m, 50m)
            };
            AddSetting("Treat a page as scanned when its text covers under:", _textCoverage, "%");
            AddWide(Hint("Higher = more pages count as scanned and get OCR'd. Default 1%."));

            _imageCoverage = new NumericUpDown
            {
                DecimalPlaces = 0,
                Minimum = 5m,
                Maximum = 100m,
                Increment = 5m,
                Value = ClampPercent(imageCoverageFraction, 5m, 100m)
            };
            AddSetting("Also OCR a text page when images cover at least:", _imageCoverage, "%");
            AddWide(Hint("Catches a scan that also has some real text (e.g. a digital header over a scanned " +
                         "body). Lower = OCR more such pages. Default 50%."));

            _maxPages = new NumericUpDown
            {
                DecimalPlaces = 0,
                Minimum = 0m,
                Maximum = 100000m,
                Increment = 50m,
                Value = Math.Clamp(maxPages, 0, 100000)
            };
            AddSetting("Maximum pages to OCR in one PDF:", _maxPages, null);
            AddWide(Hint("If a PDF needs OCR on more pages than this, redaction stops with an error instead " +
                         "of partly processing it. 0 = no limit. Default 200."));

            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = ModernTheme.StandardButtonSize };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = ModernTheme.StandardButtonSize };
            var buttons = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 6, 0, 0)
            };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            AddWide(buttons);

            Controls.Add(layout);
            AcceptButton = ok;
            CancelButton = cancel;

            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(ok);
        }

        // An auto-sized label that wraps to its table column's width.
        private static Label Wrapping(string text) => new()
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Right
        };

        private static decimal ClampPercent(double fraction, decimal min, decimal max)
        {
            decimal pct = (decimal)(fraction * 100.0);
            return Math.Clamp(pct, min, max);
        }
    }
}
