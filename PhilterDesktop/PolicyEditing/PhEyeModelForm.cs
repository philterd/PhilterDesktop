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

using Phileas.Policy.Filters;

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// Edits a single user-added local PhEye model: the on-disk GLiNER model folder, the entity types it
    /// detects, and the minimum confidence threshold. Runs on-device — no network endpoint is involved.
    /// </summary>
    internal sealed class PhEyeModelForm : Form
    {
        private readonly PhEyeConfiguration _config;
        private readonly TextBox _modelPath = new();
        private readonly Button _browse = new() { Text = "Browse…" };
        private readonly TextBox _labels = new();
        private readonly NumericUpDown _threshold = new()
        {
            Minimum = 0.00M,
            Maximum = 1.00M,
            DecimalPlaces = 2,
            Increment = 0.05M
        };
        private readonly Button _ok = new() { Text = "OK", DialogResult = DialogResult.OK };
        private readonly Button _cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel };

        public PhEyeModelForm(PhEye phEye)
        {
            _config = phEye.PhEyeConfiguration ??= new PhEyeConfiguration();

            Text = "PhEye Model";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 210);
            AcceptButton = _ok;
            CancelButton = _cancel;

            var pathLabel = new Label { Text = "Model folder:", AutoSize = true, Location = new Point(12, 18) };
            _modelPath.SetBounds(110, 15, 300, 23);
            _browse.SetBounds(416, 14, 92, 26);

            var labelsLabel = new Label { Text = "Entity types:", AutoSize = true, Location = new Point(12, 55) };
            _labels.SetBounds(110, 52, 398, 23);
            var labelsHint = new Label
            {
                Text = "Comma-separated, e.g. person, location, organization",
                AutoSize = true,
                ForeColor = ModernTheme.SubtleText,
                Location = new Point(110, 78)
            };

            var thresholdLabel = new Label { Text = "Threshold:", AutoSize = true, Location = new Point(12, 110) };
            _threshold.SetBounds(110, 107, 70, 23);
            var thresholdHint = new Label
            {
                Text = "Minimum confidence (0–1) for a detection.",
                AutoSize = true,
                ForeColor = ModernTheme.SubtleText,
                Location = new Point(190, 110)
            };

            _ok.SetBounds(322, 162, 90, 34);
            _cancel.SetBounds(418, 162, 90, 34);

            Controls.AddRange(new Control[]
            {
                pathLabel, _modelPath, _browse,
                labelsLabel, _labels, labelsHint,
                thresholdLabel, _threshold, thresholdHint,
                _ok, _cancel
            });

            _modelPath.Text = _config.ModelPath ?? string.Empty;
            _labels.Text = string.Join(", ", _config.Labels ?? new List<string>());
            _threshold.Value = ClampThreshold(_config.Threshold);

            _browse.Click += OnBrowse;
            _ok.Click += OnOk;

            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_ok);
        }

        private void OnBrowse(object? sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description = "Select the PhEye (GLiNER) model folder",
                UseDescriptionForTitle = true
            };
            if (!string.IsNullOrWhiteSpace(_modelPath.Text) && Directory.Exists(_modelPath.Text))
            {
                dlg.SelectedPath = _modelPath.Text;
            }
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _modelPath.Text = dlg.SelectedPath;
            }
        }

        private void OnOk(object? sender, EventArgs e)
        {
            string path = _modelPath.Text.Trim();
            if (!PhEyeModel.HasModelFiles(path))
            {
                MessageBox.Show(this,
                    "Select a folder containing a GLiNER model — an ONNX model (model.onnx), the SentencePiece " +
                    "tokenizer (spm.model), and gliner_config.json.",
                    "PhEye Model", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None; // keep the dialog open so it can be fixed
                return;
            }

            List<string> labels = ParseLabels(_labels.Text);
            if (labels.Count == 0)
            {
                MessageBox.Show(this,
                    "Enter at least one entity type for this model to detect.",
                    "PhEye Model", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            _config.ModelPath = path;
            _config.Labels = labels;
            _config.Threshold = (double)_threshold.Value;
        }

        private static decimal ClampThreshold(double value) => value switch
        {
            <= 0 => (decimal)PhEyeModel.DefaultThreshold,
            > 1 => 1.00M,
            _ => (decimal)value
        };

        // Split on commas/newlines, trim, drop blanks, and de-duplicate (case-insensitively).
        private static List<string> ParseLabels(string text) => text
            .Split(new[] { ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
