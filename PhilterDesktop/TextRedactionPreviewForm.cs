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
    /// Prototype "preview-first" redaction workspace for a single <c>.txt</c> document: pick a policy
    /// and context, see a live diff of how the redacted file will look, tweak the redactions, and only
    /// then write the output file. Detected redactions refresh when the policy/context changes.
    /// </summary>
    public partial class TextRedactionPreviewForm : Form
    {
        private static readonly Color DeletedColor = Color.FromArgb(255, 224, 224);
        private static readonly Color InsertedColor = Color.FromArgb(224, 255, 224);
        private static readonly Color ModifiedColor = Color.FromArgb(255, 246, 213);
        private static readonly Color ImaginaryColor = Color.FromArgb(245, 245, 245);

        private readonly string _sourcePath = string.Empty;
        private readonly PolicyRepository _policies = null!;
        private readonly ContextRepository _contexts = null!;
        private readonly SettingsEntity _settings = new();
        private readonly FilterService _filterService = new();

        private string _originalText = string.Empty;
        private List<RedactionSpanEntity> _spans = new();
        private bool _loading;

        // --- Results (read by the caller after DialogResult.OK) ---
        public string OutputPath { get; private set; } = string.Empty;
        public string SelectedPolicy { get; private set; } = string.Empty;
        public string SelectedContext { get; private set; } = string.Empty;
        public IReadOnlyList<RedactionSpanEntity> CapturedSpans => _spans;

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public TextRedactionPreviewForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_save);
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
                _originalText = File.ReadAllText(_sourcePath);
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

            Detect();
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

        private void Selection_Changed(object? sender, EventArgs e)
        {
            if (!_loading)
            {
                Detect();
            }
        }

        // Re-runs detection for the chosen policy/context, resetting the working redactions. Detection
        // (and AI name detection in particular) is synchronous and can take a moment, so show a wait
        // cursor until it (and the preview refresh) finishes.
        private void Detect()
        {
            Cursor? previousCursor = Cursor.Current;
            UseWaitCursor = true;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                if (_policyCombo.SelectedItem is null || _contextCombo.SelectedItem is null)
                {
                    _spans = new List<RedactionSpanEntity>();
                    RefreshAll();
                    return;
                }

                try
                {
                    PolicyEntity? entity = _policies.FindByName(_policyCombo.Text);
                    PhileasPolicy policy = PolicySerializer.DeserializeFromJson(
                        string.IsNullOrWhiteSpace(entity?.Json) ? "{}" : entity!.Json);
                    PhEyeModel.Prepare(policy);

                    TextFilterResult result = _filterService.Filter(policy, _contextCombo.Text, 0, _originalText);
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

                RefreshAll();
            }
            finally
            {
                Cursor.Current = previousCursor;
                UseWaitCursor = false;
            }
        }

        private void RefreshAll()
        {
            RefreshList();
            RefreshPreview();
            _save.Enabled = !string.IsNullOrEmpty(_originalText) || _spans.Count > 0;
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

        private void OnAdd(object? sender, EventArgs e)
        {
            using var dlg = new SpanEditForm("Add Redaction", SpanPositionKind.TextOffset,
                new RedactionSpanEntity { UserAdded = true, Replacement = RedactionService.DefaultReplacement, ParagraphIndex = -1 },
                positionEditable: true);
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
            using var dlg = new SpanEditForm("Edit Redaction", SpanPositionKind.TextOffset, span, positionEditable: span.UserAdded);
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
            string suggested = RedactionService.GetOutputPath(_sourcePath, _settings);
            using var dialog = new SaveFileDialog
            {
                Title = "Save redacted file",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "txt",
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
                File.WriteAllText(output, CurrentRedactedText());
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
