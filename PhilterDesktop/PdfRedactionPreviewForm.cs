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

using Phileas.Policy;
using Phileas.Services;
using PhilterData;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop
{
    /// <summary>
    /// Prototype "preview-first" redaction workspace for a single <c>.pdf</c>: pick a policy/context,
    /// see the redacted PDF rendered next to the original, and only write the output on Save. The
    /// preview redacts to a temporary file (which Save then copies to the final location). Unlike the
    /// text preview there is no span editing — PDF redactions are coordinate-based and image-rendered.
    /// </summary>
    public partial class PdfRedactionPreviewForm : Form
    {
        private readonly string _sourcePath = string.Empty;
        private readonly PolicyRepository _policies = null!;
        private readonly ContextRepository _contexts = null!;
        private readonly SettingsEntity _settings = new();
        private readonly FilterService _filterService = new();

        private byte[] _originalBytes = Array.Empty<byte>();
        private string? _tempOutputPath;
        private List<RedactionSpanEntity> _spans = new();
        private bool _loading;
        private bool _busy;

        public string OutputPath { get; private set; } = string.Empty;
        public string SelectedPolicy { get; private set; } = string.Empty;
        public string SelectedContext { get; private set; } = string.Empty;
        public IReadOnlyList<RedactionSpanEntity> CapturedSpans => _spans;

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public PdfRedactionPreviewForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_save);
        }

        public PdfRedactionPreviewForm(string sourcePath, PolicyRepository policies, ContextRepository contexts, SettingsEntity settings)
            : this()
        {
            _sourcePath = sourcePath;
            _policies = policies;
            _contexts = contexts;
            _settings = settings;
            _fileLabel.Text = sourcePath;
        }

        private async void PdfRedactionPreviewForm_Load(object? sender, EventArgs e)
        {
            _loading = true;
            try
            {
                _originalBytes = File.ReadAllBytes(_sourcePath);
                LoadNames(_policyCombo, _policies.GetAll().Select(p => p.Name));
                LoadNames(_contextCombo, _contexts.GetAll().Select(c => c.Name));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not open the file: {ex.Message}", "Redact (Preview)",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _loading = false;
            }

            await DetectAsync();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            TryDeleteTemp();
        }

        private static void LoadNames(ComboBox combo, IEnumerable<string> names)
        {
            combo.Items.Clear();
            foreach (string name in names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            {
                combo.Items.Add(name);
            }
            int def = combo.Items.IndexOf("default");
            if (combo.Items.Count > 0)
            {
                combo.SelectedIndex = def >= 0 ? def : 0;
            }
        }

        private async void Selection_Changed(object? sender, EventArgs e)
        {
            if (!_loading)
            {
                await DetectAsync();
            }
        }

        // Redacts to a temp file for the chosen policy/context and shows it next to the original.
        // PDF redaction/rendering can take a moment; it's awaited (not blocked) so the UI stays
        // responsive, with a wait cursor until it finishes.
        private async Task DetectAsync()
        {
            if (_busy || _policyCombo.SelectedItem is null || _contextCombo.SelectedItem is null || _originalBytes.Length == 0)
            {
                return;
            }

            _busy = true;
            UseWaitCursor = true;
            _policyCombo.Enabled = false;
            _contextCombo.Enabled = false;
            try
            {
                PolicyEntity? entity = _policies.FindByName(_policyCombo.Text);
                PhileasPolicy policy = PolicySerializer.DeserializeFromJson(
                    string.IsNullOrWhiteSpace(entity?.Json) ? "{}" : entity!.Json);

                _tempOutputPath ??= Path.Combine(Path.GetTempPath(), "philter-preview-" + Guid.NewGuid().ToString("N") + ".pdf");
                _spans = await RedactionService.RedactFileAsync(_sourcePath, _tempOutputPath, policy, _contextCombo.Text, _filterService);

                byte[] redacted = await File.ReadAllBytesAsync(_tempOutputPath);
                _view.SetDocuments(_originalBytes, redacted, "Original", "Redacted (preview)");
                _save.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Redaction failed: {ex.Message}", "Redact (Preview)",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _save.Enabled = false;
            }
            finally
            {
                UseWaitCursor = false;
                _policyCombo.Enabled = true;
                _contextCombo.Enabled = true;
                _busy = false;
            }
        }

        private void OnSave(object? sender, EventArgs e)
        {
            if (_tempOutputPath is null || !File.Exists(_tempOutputPath))
            {
                return;
            }

            string suggested = RedactionService.GetOutputPath(_sourcePath, _settings);
            using var dialog = new SaveFileDialog
            {
                Title = "Save redacted file",
                Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
                DefaultExt = "pdf",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = Path.GetFileName(suggested),
                InitialDirectory = Path.GetDirectoryName(suggested) is { Length: > 0 } d ? d : Path.GetDirectoryName(_sourcePath)
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            string output = dialog.FileName;
            try
            {
                File.Copy(_tempOutputPath, output, overwrite: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not write the redacted file: {ex.Message}", "Redact (Preview)",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_openAfter.Checked)
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = output, UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Could not open the file: {ex.Message}", "Redact (Preview)",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            OutputPath = output;
            SelectedPolicy = _policyCombo.Text;
            SelectedContext = _contextCombo.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void TryDeleteTemp()
        {
            if (_tempOutputPath is not null)
            {
                try { File.Delete(_tempOutputPath); } catch { /* best effort */ }
            }
        }
    }
}
