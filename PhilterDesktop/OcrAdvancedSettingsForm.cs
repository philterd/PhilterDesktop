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

        /// <summary>Text-coverage threshold as a fraction (0–1): below this a page is treated as scanned.</summary>
        public double TextCoverageThreshold => (double)_textCoverage.Value / 100.0;

        /// <summary>Image-coverage threshold as a fraction (0–1): at/above this a text page is also OCR'd.</summary>
        public double ImageCoverageThreshold => (double)_imageCoverage.Value / 100.0;

        public OcrAdvancedSettingsForm(double textCoverageFraction, double imageCoverageFraction)
        {
            Text = "Advanced OCR Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(440, 250);

            var intro = new Label
            {
                Location = new Point(14, 12),
                Size = new Size(412, 34),
                Text = "These control when a PDF page is read with OCR. The defaults suit most documents; " +
                       "lower values mean OCR runs on more pages (slower, but less likely to miss anything)."
            };

            var textLabel = new Label
            {
                Location = new Point(14, 58),
                Size = new Size(300, 20),
                Text = "Treat a page as scanned when its text covers under:"
            };
            _textCoverage = new NumericUpDown
            {
                Location = new Point(330, 56),
                Size = new Size(90, 23),
                DecimalPlaces = 1,
                Minimum = 0.1m,
                Maximum = 50m,
                Increment = 0.5m,
                Value = ClampPercent(textCoverageFraction, 0.1m, 50m)
            };
            var textPct = new Label { Location = new Point(424, 58), Size = new Size(16, 20), Text = "%" };
            var textHint = new Label
            {
                Location = new Point(14, 84),
                Size = new Size(412, 18),
                ForeColor = SystemColors.GrayText,
                Text = "Higher = more pages count as scanned and get OCR'd. Default 1%."
            };

            var imageLabel = new Label
            {
                Location = new Point(14, 120),
                Size = new Size(300, 20),
                Text = "Also OCR a text page when images cover at least:"
            };
            _imageCoverage = new NumericUpDown
            {
                Location = new Point(330, 118),
                Size = new Size(90, 23),
                DecimalPlaces = 0,
                Minimum = 5m,
                Maximum = 100m,
                Increment = 5m,
                Value = ClampPercent(imageCoverageFraction, 5m, 100m)
            };
            var imagePct = new Label { Location = new Point(424, 120), Size = new Size(16, 20), Text = "%" };
            var imageHint = new Label
            {
                Location = new Point(14, 146),
                Size = new Size(412, 32),
                ForeColor = SystemColors.GrayText,
                Text = "Catches a scan that also has some real text (e.g. a digital header over a scanned " +
                       "body). Lower = OCR more such pages. Default 50%."
            };

            var ok = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Size = ModernTheme.StandardButtonSize,
                Location = new Point(206, 204),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            var cancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Size = ModernTheme.StandardButtonSize,
                Location = new Point(322, 204),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            Controls.AddRange(new Control[]
            {
                intro, textLabel, _textCoverage, textPct, textHint,
                imageLabel, _imageCoverage, imagePct, imageHint, ok, cancel
            });

            AcceptButton = ok;
            CancelButton = cancel;

            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(ok);
        }

        private static decimal ClampPercent(double fraction, decimal min, decimal max)
        {
            decimal pct = (decimal)(fraction * 100.0);
            return Math.Clamp(pct, min, max);
        }
    }
}
