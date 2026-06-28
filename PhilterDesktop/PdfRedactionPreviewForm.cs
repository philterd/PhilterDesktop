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
    /// preview redacts entirely in memory — the redacted bytes are held in <see cref="_redactedBytes"/>
    /// and are written to disk only when the reviewer saves, so no redacted PII ever lands in a temp
    /// file. The reviewer can also add redactions the detector missed by drawing a box on the original
    /// page; those join the redaction list (flagged user-added) and are burned into the rasterized
    /// output on Save like a detected one, so the covered content is destroyed, not merely hidden.
    /// </summary>
    public partial class PdfRedactionPreviewForm : Form
    {
        private readonly string _sourcePath = string.Empty;
        private readonly PolicyRepository _policies = null!;
        private readonly ContextRepository _contexts = null!;
        private readonly SettingsEntity _settings = new();
        private readonly FilterService _filterService = SharedFilterService.Instance;

        private byte[] _originalBytes = Array.Empty<byte>();
        private byte[] _redactedBytes = Array.Empty<byte>();
        private List<RedactionSpanEntity> _spans = new();
        private bool _loading;
        private bool _busy;

        // Side panel for reviewer-added redactions (built in code; see BuildRedactionPanel).
        private Panel _sidePanel = null!;
        private Button _drawButton = null!;
        private ListView _redactionList = null!;
        private Button _removeButton = null!;

        public string OutputPath { get; private set; } = string.Empty;
        public string SelectedPolicy { get; private set; } = string.Empty;
        public string SelectedContext { get; private set; } = string.Empty;
        public IReadOnlyList<RedactionSpanEntity> CapturedSpans => _spans;
        /// <summary>Time taken to produce the redacted PDF, in milliseconds.</summary>
        public long RedactionDurationMs { get; private set; }

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public PdfRedactionPreviewForm()
        {
            InitializeComponent();
            BuildRedactionPanel();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_save);
            if (!PhEyeModel.IsAvailable) WarningBanner.AddTo(this, PhEyeModel.UnavailableWarning);

            _view.RegionDrawn += View_RegionDrawn;
        }

        // A right-docked panel listing redactions, with a "draw a box to redact" toggle and a Remove
        // button. Built in code and slotted between the top/bottom bars so the viewer still fills the rest.
        private void BuildRedactionPanel()
        {
            _sidePanel = new Panel { Dock = DockStyle.Right, Width = 240, Padding = new Padding(8) };

            var title = new Label
            {
                Dock = DockStyle.Top,
                Text = "Redactions",
                Font = new Font(ModernTheme.UiFont, FontStyle.Bold),
                Height = 24
            };

            _drawButton = new Button
            {
                Dock = DockStyle.Top,
                Height = 34,
                Text = "Add Redaction (draw)",
                UseVisualStyleBackColor = true
            };
            _drawButton.Click += DrawButton_Click;

            var hint = new Label
            {
                Dock = DockStyle.Top,
                Height = 48,
                ForeColor = ModernTheme.SubtleText,
                Text = "Turn on, then drag a box on the original (left) page to redact a region the detector missed."
            };

            _redactionList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                HideSelection = false
            };
            _redactionList.Columns.Add("Type", 110);
            _redactionList.Columns.Add("Location", 100);

            _removeButton = new Button
            {
                Dock = DockStyle.Bottom,
                Height = 34,
                Text = "Remove Selected",
                Enabled = false,
                UseVisualStyleBackColor = true
            };
            _removeButton.Click += RemoveButton_Click;
            _redactionList.SelectedIndexChanged += (_, _) => UpdateRemoveEnabled();

            // Add the fill control first, then docked edges, so the list fills the remaining space.
            _sidePanel.Controls.Add(_redactionList);
            _sidePanel.Controls.Add(_removeButton);
            _sidePanel.Controls.Add(hint);
            _sidePanel.Controls.Add(_drawButton);
            _sidePanel.Controls.Add(title);

            Controls.Add(_sidePanel);
            // Keep full-width top/bottom bars: dock the side panel inside them but outside the viewer.
            Controls.SetChildIndex(_sidePanel, Controls.GetChildIndex(_view) + 1);
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
                LoadNames(_policyCombo, _policies.GetAll().Select(p => p.Name), _settings.LastPolicy);
                LoadNames(_contextCombo, _contexts.GetAll().Select(c => c.Name), _settings.LastContext);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, UserError.Describe(ex, _sourcePath, writing: false), "Redact (Preview)",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _loading = false;
            }

            await DetectAsync();
        }

        private static void LoadNames(ComboBox combo, IEnumerable<string> names, string? preferred)
        {
            combo.Items.Clear();
            foreach (string name in names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            {
                combo.Items.Add(name);
            }
            ComboSelection.Select(combo, preferred);
        }

        private async void Selection_Changed(object? sender, EventArgs e)
        {
            if (!_loading)
            {
                await DetectAsync();
            }
        }

        // Redacts the PDF in memory for the chosen policy/context and shows it next to the original.
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
                PhileasPolicy policy = CurrentPolicy();

                // Keep any reviewer-added regions across a re-detect (e.g. when the policy/context changes).
                List<RedactionSpanEntity> userAdded = _spans.Where(s => s.UserAdded).ToList();

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                (byte[] redacted, List<RedactionSpanEntity> spans) = await RedactionService.RedactPdfBytesAsync(
                    _originalBytes, policy, _contextCombo.Text, _filterService,
                    ocrScannedPdfs: _settings.OcrScannedPdfs,
                    ocrTextCoverage: _settings.OcrTextCoverageThreshold,
                    ocrImageCoverage: _settings.OcrImageCoverageThreshold,
                    ocrMaxPages: _settings.OcrMaxPages);
                _spans = spans;
                _spans.AddRange(userAdded);

                // RedactPdfBytesAsync produced the detected spans only; re-burn if there are added ones.
                if (userAdded.Count > 0)
                {
                    redacted = await RedactionService.ApplyPdfSpansToBytesAsync(_originalBytes, _spans, policy, _filterService);
                }
                stopwatch.Stop();
                RedactionDurationMs = stopwatch.ElapsedMilliseconds;

                _redactedBytes = redacted;
                _view.SetDocuments(_originalBytes, _redactedBytes, "Original", "Redacted (preview)");
                RefreshRedactionList();
                _save.Enabled = true;
            }
            catch (OcrPageLimitExceededException ex)
            {
                MessageBox.Show(this, ex.Message, "Too many pages to OCR",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                _save.Enabled = false;
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

        // The selected policy (with global lists applied), used for both detection and re-apply.
        private PhileasPolicy CurrentPolicy()
        {
            PolicyEntity? entity = _policies.FindByName(_policyCombo.Text);
            PhileasPolicy policy = PolicySerializer.DeserializeFromJson(
                string.IsNullOrWhiteSpace(entity?.Json) ? "{}" : entity!.Json);
            GlobalLists.Apply(policy, _settings); // global always-redact/ignore on top of every policy
            return policy;
        }

        private void DrawButton_Click(object? sender, EventArgs e)
        {
            bool on = !_view.DrawToRedact;
            _view.DrawToRedact = on;
            _drawButton.Text = on ? "Drawing… drag a box (click to stop)" : "Add Redaction (draw)";
        }

        // A reviewer drew a box on the original page: add it as a user-added span and re-apply.
        private async void View_RegionDrawn(object? sender, PdfRegionDrawnEventArgs e)
        {
            _spans.Add(new RedactionSpanEntity
            {
                UserAdded = true,
                Order = _spans.Count,
                PageNumber = e.PageNumber,
                LowerLeftX = e.LowerLeftX,
                LowerLeftY = e.LowerLeftY,
                UpperRightX = e.UpperRightX,
                UpperRightY = e.UpperRightY,
                Text = string.Empty,
                Classification = string.Empty
            });
            RefreshRedactionList();
            await ReapplyAsync();
        }

        private async void RemoveButton_Click(object? sender, EventArgs e)
        {
            if (_redactionList.SelectedItems.Count == 0 || _redactionList.SelectedItems[0].Tag is not RedactionSpanEntity span)
            {
                return;
            }
            if (!span.UserAdded)
            {
                MessageBox.Show(this, "Only redactions you added here can be removed. To keep a detected item, adjust the policy.",
                    "Remove Redaction", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _spans.Remove(span);
            RefreshRedactionList();
            await ReapplyAsync();
        }

        private void UpdateRemoveEnabled()
        {
            _removeButton.Enabled = _redactionList.SelectedItems.Count > 0
                && _redactionList.SelectedItems[0].Tag is RedactionSpanEntity { UserAdded: true };
        }

        // Re-burns the current span set (detected + reviewer-added) into the in-memory redacted bytes
        // and refreshes only the redacted pane, keeping the reviewer on the current page.
        private async Task ReapplyAsync()
        {
            if (_busy || _originalBytes.Length == 0)
            {
                return;
            }
            _busy = true;
            UseWaitCursor = true;
            try
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                _redactedBytes = await RedactionService.ApplyPdfSpansToBytesAsync(
                    _originalBytes, _spans, CurrentPolicy(), _filterService);
                stopwatch.Stop();
                RedactionDurationMs = stopwatch.ElapsedMilliseconds;

                _view.UpdateAfter(_redactedBytes);
                _save.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Redaction failed: {ex.Message}", "Redact (Preview)",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                UseWaitCursor = false;
                _busy = false;
            }
        }

        private void RefreshRedactionList()
        {
            _redactionList.BeginUpdate();
            _redactionList.Items.Clear();
            foreach (RedactionSpanEntity span in _spans)
            {
                var item = new ListViewItem(RedactionReport.FriendlyType(span)) { Tag = span };
                item.SubItems.Add(RedactionReport.LocationOf(span));
                _redactionList.Items.Add(item);
            }
            _redactionList.EndUpdate();
            UpdateRemoveEnabled();
        }

        private void OnSave(object? sender, EventArgs e)
        {
            if (_redactedBytes.Length == 0)
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
                InitialDirectory = RedactionService.InitialSaveDirectory(_settings, suggested, _sourcePath)
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            string output = dialog.FileName;
            try
            {
                File.WriteAllBytes(output, _redactedBytes);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, UserError.Describe(ex, output, writing: true), "Redact (Preview)",
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
    }
}
