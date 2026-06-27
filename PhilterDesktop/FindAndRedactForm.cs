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

using System.Diagnostics;
using Phileas.Services;
using PhilterData;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop
{
    /// <summary>
    /// Ad-hoc "Find &amp; Redact": pick one document, type (or import) the exact terms to remove, and
    /// produce a redacted copy — no policy needed. A quick one-off for "redact this name in this file."
    /// </summary>
    internal sealed partial class FindAndRedactForm : Form
    {
        private readonly SettingsEntity _settings;
        private readonly FilterService _filterService = new();

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public FindAndRedactForm() : this(new SettingsEntity())
        {
        }

        public FindAndRedactForm(SettingsEntity settings, string? initialSource = null)
        {
            _settings = settings;
            InitializeComponent();

            if (!string.IsNullOrEmpty(initialSource))
            {
                _source.Text = initialSource;
            }

            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_redact);
        }

        private void OnBrowse(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Choose a document",
                Filter = "Supported documents (*.txt;*.docx;*.pdf;*.rtf;*.xlsx;*.csv;*.eml;*.msg)|*.txt;*.docx;*.pdf;*.rtf;*.xlsx;*.csv;*.eml;*.msg|All files (*.*)|*.*",
                CheckFileExists = true
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _source.Text = dlg.FileName;
            }
        }

        private void OnImport(object? sender, EventArgs e) => TermFileImporter.PromptAndAppend(this, _terms);

        private async void OnRedact(object? sender, EventArgs e)
        {
            string source = _source.Text.Trim();
            if (string.IsNullOrEmpty(source) || !File.Exists(source))
            {
                MessageBox.Show(this, "Choose a document to redact.", "Find & Redact", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!RedactionService.IsSupported(source))
            {
                MessageBox.Show(this, "Unsupported file type. Choose a .txt, .docx, .pdf, .rtf, .xlsx, .csv, .eml, or .msg file.", "Find & Redact", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<string> terms = TermFileImporter.Parse(_terms.Text);
            if (terms.Count == 0)
            {
                MessageBox.Show(this, "Enter at least one term to redact (one per line).", "Find & Redact", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string output = RedactionService.GetOutputPath(source, _settings);
            PhileasPolicy policy = FindAndRedact.BuildPolicy(terms);

            _redact.Enabled = _browse.Enabled = _import.Enabled = false;
            Cursor = Cursors.WaitCursor;
            try
            {
                List<RedactionSpanEntity> spans =
                    await RedactionService.RedactFileAsync(source, output, policy, string.Empty, _filterService);

                Cursor = Cursors.Default;
                string summary = $"Redacted {spans.Count} item{(spans.Count == 1 ? "" : "s")}." +
                                 Environment.NewLine + Environment.NewLine + "Saved to:" + Environment.NewLine + output +
                                 Environment.NewLine + Environment.NewLine + "Open the containing folder?";
                if (MessageBox.Show(this, summary, "Find & Redact", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    try { Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{output}\"") { UseShellExecute = true }); }
                    catch { /* best effort */ }
                }
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                MessageBox.Show(this, UserError.Describe(ex, output, writing: true), "Find & Redact", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                _redact.Enabled = _browse.Enabled = _import.Enabled = true;
            }
        }
    }
}
