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

using System.Text;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Phileas.Model;
using Phileas.Policy;
using Phileas.Services;
using PhilterData;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop
{
    /// <summary>
    /// "Preview-first" redaction workspace for a single plain-text (<c>.txt</c>) or Rich Text
    /// (<c>.rtf</c>) document: pick a policy and context, see a live diff of how the redacted file will
    /// look, tweak the redactions, and only then write the output. For RTF the diff is over the visible
    /// text and the save is format-preserving (so the .rtf keeps its formatting). Detected redactions
    /// refresh when the policy/context changes.
    /// </summary>
    public partial class TextRedactionPreviewForm : Form
    {
        private bool IsRtf => _sourcePath.EndsWith(".rtf", StringComparison.OrdinalIgnoreCase);

        private static readonly Color DeletedColor = Color.FromArgb(255, 224, 224);
        private static readonly Color InsertedColor = Color.FromArgb(224, 255, 224);
        private static readonly Color ModifiedColor = Color.FromArgb(255, 246, 213);
        private static readonly Color ImaginaryColor = Color.FromArgb(245, 245, 245);

        private readonly string _sourcePath = string.Empty;
        private readonly PolicyRepository _policies = null!;
        private readonly ContextRepository _contexts = null!;
        private readonly SettingsEntity _settings = new();
        private readonly FilterService _filterService = SharedFilterService.Instance;

        private string _originalText = string.Empty;
        private List<RedactionSpanEntity> _spans = new();
        private bool _loading;
        private bool _busy; // guards against overlapping async detections

        // --- Results (read by the caller after DialogResult.OK) ---
        public string OutputPath { get; private set; } = string.Empty;
        public string SelectedPolicy { get; private set; } = string.Empty;
        public string SelectedContext { get; private set; } = string.Empty;
        public IReadOnlyList<RedactionSpanEntity> CapturedSpans => _spans;
        /// <summary>Time taken to write the redacted output on save, in milliseconds.</summary>
        public long RedactionDurationMs { get; private set; }

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public TextRedactionPreviewForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_save);
            if (!PhEyeModel.IsAvailable) WarningBanner.AddTo(this, PhEyeModel.UnavailableWarning);
        }

        public TextRedactionPreviewForm(string sourcePath, PolicyRepository policies, ContextRepository contexts, SettingsEntity settings)
            : this()
        {
            _sourcePath = sourcePath;
            _policies = policies;
            _contexts = contexts;
            _settings = settings;
            _fileLabel.Text = sourcePath;
        }

        private void TextRedactionPreviewForm_Load(object? sender, EventArgs e)
        {
            _loading = true;
            try
            {
                _originalText = IsRtf ? RtfRedactor.ReadText(_sourcePath) : File.ReadAllText(_sourcePath);
                _originalBox.Text = _originalText; // selectable copy for manual "redact selection"
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

            _ = DetectAsync();
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

        // Re-runs detection for the chosen policy/context, resetting the working redactions. Detection
        // (and AI name detection in particular) can take a moment on a large file, so it runs off the UI
        // thread (awaited, not blocked) with the inputs disabled and a wait cursor, so the window never
        // goes "Not Responding".
        private async Task DetectAsync()
        {
            if (_busy)
            {
                return;
            }
            if (_policyCombo.SelectedItem is null || _contextCombo.SelectedItem is null)
            {
                _spans = new List<RedactionSpanEntity>();
                RefreshAll();
                return;
            }

            _busy = true;
            UseWaitCursor = true;
            SetInputsEnabled(false);
            try
            {
                PolicyEntity? entity = _policies.FindByName(_policyCombo.Text);
                PhileasPolicy policy = PolicySerializer.DeserializeFromJson(
                    string.IsNullOrWhiteSpace(entity?.Json) ? "{}" : entity!.Json);
                GlobalLists.Apply(policy, _settings); // global always-redact/ignore on top of every policy
                PhEyeModel.Prepare(policy);
                string context = _contextCombo.Text;
                string text = _originalText;

                TextFilterResult result = await Task.Run(() => _filterService.Filter(policy, context, 0, text));
                _spans = result.Spans
                    .Where(s => s.CharacterStart >= 0 && s.CharacterEnd <= _originalText.Length && s.CharacterEnd > s.CharacterStart)
                    .OrderBy(s => s.CharacterStart)
                    .Select(s => new RedactionSpanEntity
                    {
                        ParagraphIndex = -1,
                        CharacterStart = s.CharacterStart,
                        CharacterEnd = s.CharacterEnd,
                        Text = s.Text ?? string.Empty,
                        Replacement = s.Replacement ?? string.Empty,
                        Classification = s.Classification ?? string.Empty
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Redaction failed: {ex.Message}", "Redact (Preview)",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _spans = new List<RedactionSpanEntity>();
            }
            finally
            {
                UseWaitCursor = false;
                SetInputsEnabled(true);
                RefreshAll();
                _busy = false;
            }
        }

        // Disables/re-enables the inputs that could start another detection or save mid-run.
        private void SetInputsEnabled(bool enabled)
        {
            _policyCombo.Enabled = enabled;
            _contextCombo.Enabled = enabled;
            if (!enabled)
            {
                _save.Enabled = false; // re-enabled by RefreshAll once detection finishes
            }
        }

        // A policy and context must both be selected before the redacted file can be written.
        // Without them no detection has run, so saving would write an unredacted copy that merely
        // looks like a redacted draft.
        private bool PolicyChosen => _policyCombo.SelectedItem is not null && _contextCombo.SelectedItem is not null;

        private void RefreshAll()
        {
            RefreshList();
            RefreshPreview();
            _save.Enabled = PolicyChosen && (!string.IsNullOrEmpty(_originalText) || _spans.Count > 0);
        }

        private string CurrentRedactedText() =>
            RedactionSpanMath.ApplySpans(_originalText, _spans, RedactionService.DefaultReplacement);

        private void RefreshPreview()
        {
            SideBySideDiffModel model = SideBySideDiffBuilder.Diff(_originalText, CurrentRedactedText());
            _preview.SuspendLayout();
            _preview.Rows.Clear();
            int lines = Math.Max(model.OldText.Lines.Count, model.NewText.Lines.Count);
            for (int i = 0; i < lines; i++)
            {
                DiffPiece? left = i < model.OldText.Lines.Count ? model.OldText.Lines[i] : null;
                DiffPiece? right = i < model.NewText.Lines.Count ? model.NewText.Lines[i] : null;
                int row = _preview.Rows.Add(LineText(left), LineText(right));
                _preview.Rows[row].Cells[0].Style.BackColor = ColorFor(left);
                _preview.Rows[row].Cells[1].Style.BackColor = ColorFor(right);
            }
            _preview.ResumeLayout();
            _preview.ClearSelection();
        }

        private void RefreshList()
        {
            _spanList.BeginUpdate();
            _spanList.Items.Clear();
            foreach (RedactionSpanEntity s in _spans)
            {
                var item = new ListViewItem(s.UserAdded ? "Added" : Display(s.Classification)) { Tag = s };
                item.SubItems.Add(s.Text);
                item.SubItems.Add(s.Replacement);
                item.SubItems.Add(s.CharacterStart.ToString());
                item.SubItems.Add(s.CharacterEnd.ToString());
                _spanList.Items.Add(item);
            }
            _spanList.EndUpdate();
            UpdateSpanButtons();
        }

        private void SpanList_SelectedIndexChanged(object? sender, EventArgs e) => UpdateSpanButtons();

        private void UpdateSpanButtons()
        {
            bool hasSel = _spanList.SelectedItems.Count > 0;
            _edit.Enabled = hasSel;
            _remove.Enabled = hasSel;
        }

        // Manual redaction: turn the reviewer's text selection (in the "Select text to redact" tab) into
        // a user-added span over that character range. Offsets index directly into the original text.
        private void OnRedactSelection(object? sender, EventArgs e)
        {
            RedactionSpanEntity? span = ManualRedaction.FromSelection(
                _originalText, _originalBox.SelectionStart, _originalBox.SelectionLength);
            if (span is null)
            {
                _leftTabs.SelectedTab = _selectTab;
                _originalBox.Focus();
                MessageBox.Show(this,
                    "Select the text you want to redact in the \"Select text to redact\" tab, then click \"Redact selection\".",
                    "Redact (Preview)", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _spans.Add(span);
            _spans = _spans.OrderBy(s => s.CharacterStart).ToList();
            RefreshAll();
        }

        private void OnAdd(object? sender, EventArgs e)
        {
            using var dlg = new SpanEditForm("Add Redaction", SpanPositionKind.TextOffset,
                new RedactionSpanEntity { UserAdded = true, Replacement = RedactionService.DefaultReplacement, ParagraphIndex = -1 },
                positionEditable: true, maxOffset: _originalText.Length);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _spans.Add(new RedactionSpanEntity
                {
                    UserAdded = true,
                    ParagraphIndex = -1,
                    CharacterStart = dlg.Start,
                    CharacterEnd = dlg.Stop,
                    Replacement = dlg.Replacement
                });
                _spans = _spans.OrderBy(s => s.CharacterStart).ToList();
                RefreshAll();
            }
        }

        private void OnEdit(object? sender, EventArgs e)
        {
            if (_spanList.SelectedItems.Count == 0 || _spanList.SelectedItems[0].Tag is not RedactionSpanEntity span)
            {
                return;
            }
            using var dlg = new SpanEditForm("Edit Redaction", SpanPositionKind.TextOffset, span, positionEditable: span.UserAdded, maxOffset: _originalText.Length);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                if (span.UserAdded)
                {
                    span.CharacterStart = dlg.Start;
                    span.CharacterEnd = dlg.Stop;
                }
                span.Replacement = dlg.Replacement;
                RefreshAll();
            }
        }

        private void OnRemove(object? sender, EventArgs e)
        {
            if (_spanList.SelectedItems.Count == 0 || _spanList.SelectedItems[0].Tag is not RedactionSpanEntity span)
            {
                return;
            }
            _spans.Remove(span);
            RefreshAll();
        }

        private void OnSave(object? sender, EventArgs e)
        {
            // Defense in depth: the Save button is disabled until a policy/context is chosen, but
            // guard here too so the redacted file is never written without a detection pass.
            if (!PolicyChosen)
            {
                MessageBox.Show(this, "Choose a policy and a context before saving.", "Redact (Preview)",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string suggested = RedactionService.GetOutputPath(_sourcePath, _settings);
            using var dialog = new SaveFileDialog
            {
                Title = "Save redacted file",
                Filter = IsRtf ? "Rich Text (*.rtf)|*.rtf|All files (*.*)|*.*" : "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = IsRtf ? "rtf" : "txt",
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
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                if (IsRtf)
                {
                    // Re-apply the (possibly edited) spans to the source so the .rtf keeps its formatting.
                    RtfRedactor.ApplySpans(_sourcePath, output, _spans);
                }
                else
                {
                    File.WriteAllText(output, CurrentRedactedText());
                }
                stopwatch.Stop();
                RedactionDurationMs = stopwatch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, UserError.Describe(ex, output, writing: true), "Redact (Preview)",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_openAfter.Checked)
            {
                OpenInDefaultApp(output);
            }

            OutputPath = output;
            SelectedPolicy = _policyCombo.Text;
            SelectedContext = _contextCombo.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OpenInDefaultApp(string path)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not open the file: {ex.Message}", "Redact (Preview)",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static string LineText(DiffPiece? piece) =>
            piece is null || piece.Type == ChangeType.Imaginary ? string.Empty : piece.Text ?? string.Empty;

        private static Color ColorFor(DiffPiece? piece) => piece?.Type switch
        {
            ChangeType.Deleted => DeletedColor,
            ChangeType.Inserted => InsertedColor,
            ChangeType.Modified => ModifiedColor,
            ChangeType.Imaginary => ImaginaryColor,
            _ => Color.White
        };

        private static string Display(string classification) =>
            string.IsNullOrEmpty(classification) ? "Detected" : classification;
    }
}
