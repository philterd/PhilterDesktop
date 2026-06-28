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

using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Phileas.Policy;
using Phileas.Services;
using PhilterData;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop
{
    /// <summary>
    /// "Preview-first" redaction workspace for a single email (<c>.eml</c> or <c>.msg</c>). Shows a
    /// field-by-field text diff (subject, addresses, body) of how the redacted email will read, with an
    /// editable redaction list, and writes the real redacted email (always <c>.eml</c>) only on Save.
    /// Email redactions are anchored to a specific field. Detected ones can be reviewed, have their
    /// replacement changed, or be removed; a reviewer can also add a redaction by selecting text in a
    /// message <b>body</b> (where a character position is meaningful). Header fields aren't hand-editable.
    /// </summary>
    public partial class EmailRedactionPreviewForm : Form
    {
        // How body parts are joined in the selectable "Select text to redact" box (length matters for
        // mapping a selection back to a field offset).
        private static readonly string BodySeparator = Environment.NewLine + Environment.NewLine;

        private static readonly Color DeletedColor = Color.FromArgb(255, 224, 224);
        private static readonly Color InsertedColor = Color.FromArgb(224, 255, 224);
        private static readonly Color ModifiedColor = Color.FromArgb(255, 246, 213);
        private static readonly Color ImaginaryColor = Color.FromArgb(245, 245, 245);

        private readonly string _sourcePath = string.Empty;
        private readonly PolicyRepository _policies = null!;
        private readonly ContextRepository _contexts = null!;
        private readonly SettingsEntity _settings = new();
        private readonly FilterService _filterService = SharedFilterService.Instance;

        private (string Label, string Text, bool IsBody)[] _fields = Array.Empty<(string, string, bool)>();
        private int[] _bodyFieldIndices = Array.Empty<int>(); // field indices that are message bodies
        private List<RedactionSpanEntity> _spans = new();
        private bool _loading;

        public string OutputPath { get; private set; } = string.Empty;
        public string SelectedPolicy { get; private set; } = string.Empty;
        public string SelectedContext { get; private set; } = string.Empty;
        public IReadOnlyList<RedactionSpanEntity> CapturedSpans => _spans;
        /// <summary>Time taken to write the redacted output on save, in milliseconds.</summary>
        public long RedactionDurationMs { get; private set; }

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public EmailRedactionPreviewForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_save);
            if (!PhEyeModel.IsAvailable) WarningBanner.AddTo(this, PhEyeModel.UnavailableWarning);
        }

        public EmailRedactionPreviewForm(string sourcePath, PolicyRepository policies, ContextRepository contexts, SettingsEntity settings)
            : this()
        {
            _sourcePath = sourcePath;
            _policies = policies;
            _contexts = contexts;
            _settings = settings;
            _fileLabel.Text = sourcePath;
        }

        private void EmailRedactionPreviewForm_Load(object? sender, EventArgs e)
        {
            _loading = true;
            try
            {
                _fields = EmailRedactor.ReadFields(_sourcePath).ToArray();
                _bodyFieldIndices = _fields.Select((f, i) => (f, i)).Where(x => x.f.IsBody).Select(x => x.i).ToArray();
                _originalBox.Text = string.Join(BodySeparator, _bodyFieldIndices.Select(i => _fields[i].Text));
                LoadNames(_policyCombo, _policies.GetAll().Select(p => p.Name), _settings.LastPolicy);
                LoadNames(_contextCombo, _contexts.GetAll().Select(c => c.Name), _settings.LastContext);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, UserError.Describe(ex, _sourcePath, writing: false), "Redact Email (Preview)",
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

        private void Detect()
        {
            if (_policyCombo.SelectedItem is null || _contextCombo.SelectedItem is null)
            {
                _spans = new List<RedactionSpanEntity>();
                RefreshAll();
                return;
            }

            Cursor? previousCursor = Cursor.Current;
            UseWaitCursor = true;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                PolicyEntity? entity = _policies.FindByName(_policyCombo.Text);
                PhileasPolicy policy = PolicySerializer.DeserializeFromJson(
                    string.IsNullOrWhiteSpace(entity?.Json) ? "{}" : entity!.Json);
                GlobalLists.Apply(policy, _settings); // global always-redact/ignore on top of every policy
                PhEyeModel.Prepare(policy);

                string context = _contextCombo.Text;
                _spans = EmailRedactor.Detect(_sourcePath, text => _filterService.Filter(policy, context, 0, text));
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Redaction failed: {ex.Message}", "Redact Email (Preview)",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _spans = new List<RedactionSpanEntity>();
            }
            finally
            {
                Cursor.Current = previousCursor;
                UseWaitCursor = false;
            }

            RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshList();
            RefreshPreview();
            _save.Enabled = _fields.Length > 0;
        }

        // Applies the working spans to each field's text (grouped by field index).
        private string[] RedactedFields()
        {
            string[] result = _fields.Select(f => f.Text).ToArray();
            foreach (IGrouping<int, RedactionSpanEntity> group in _spans.GroupBy(s => s.ParagraphIndex))
            {
                if (group.Key < 0 || group.Key >= result.Length)
                {
                    continue;
                }
                result[group.Key] = RedactionSpanMath.ApplySpans(result[group.Key], group, RedactionService.DefaultReplacement);
            }
            return result;
        }

        // Builds a labeled, line-based view of the fields so the diff reads "[Subject] …" etc. The
        // "[Label]" header lines are identical on both sides, so only the field contents diff.
        private static string Compose((string Label, string Text, bool IsBody)[] fields, string[] texts)
        {
            var lines = new List<string>();
            for (int i = 0; i < fields.Length; i++)
            {
                lines.Add($"[{fields[i].Label}]");
                lines.Add(texts[i]);
                lines.Add(string.Empty);
            }
            return string.Join("\n", lines);
        }

        private void RefreshPreview()
        {
            string before = Compose(_fields, _fields.Select(f => f.Text).ToArray());
            string after = Compose(_fields, RedactedFields());
            SideBySideDiffModel model = SideBySideDiffBuilder.Diff(before, after);

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
            foreach (RedactionSpanEntity s in _spans.OrderBy(s => s.ParagraphIndex).ThenBy(s => s.CharacterStart))
            {
                var item = new ListViewItem(s.UserAdded ? "Added" : Display(s.Classification)) { Tag = s };
                item.SubItems.Add(s.Text);
                item.SubItems.Add(s.Replacement);
                item.SubItems.Add(FieldLabel(s.ParagraphIndex));
                _spanList.Items.Add(item);
            }
            _spanList.EndUpdate();
            UpdateSpanButtons();
        }

        private string FieldLabel(int index) =>
            index >= 0 && index < _fields.Length ? _fields[index].Label : string.Empty;

        private void SpanList_SelectedIndexChanged(object? sender, EventArgs e) => UpdateSpanButtons();

        private void UpdateSpanButtons()
        {
            bool hasSel = _spanList.SelectedItems.Count > 0;
            _edit.Enabled = hasSel;
            _remove.Enabled = hasSel;
        }

        // Manual redaction: turn the reviewer's selection in the "Select text to redact" tab (which shows
        // only the message body parts) into user-added spans. The select box joins the body parts with
        // BodySeparator; map the selection per body part, then translate the body-part index back to the
        // real field index so the span applies to the right field on save.
        private void OnRedactSelection(object? sender, EventArgs e)
        {
            List<string> bodyTexts = _bodyFieldIndices.Select(i => _fields[i].Text).ToList();
            List<RedactionSpanEntity> added = ManualRedaction.FromParagraphSelection(
                bodyTexts, _originalBox.SelectionStart, _originalBox.SelectionLength, BodySeparator.Length);

            if (added.Count == 0)
            {
                _leftTabs.SelectedTab = _selectTab;
                _originalBox.Focus();
                string message = _bodyFieldIndices.Length == 0
                    ? "This email has no redactable body text to select."
                    : "Select the text you want to redact in the \"Select text to redact\" tab, then click \"Redact selection\".";
                MessageBox.Show(this, message, "Redact Email (Preview)", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (RedactionSpanEntity span in added)
            {
                span.ParagraphIndex = _bodyFieldIndices[span.ParagraphIndex]; // body-part index -> field index
            }
            _spans.AddRange(added);
            RefreshAll();
        }

        // Email redactions are field-anchored, so only the replacement text is editable (no position).
        private void OnEdit(object? sender, EventArgs e)
        {
            if (_spanList.SelectedItems.Count == 0 || _spanList.SelectedItems[0].Tag is not RedactionSpanEntity span)
            {
                return;
            }
            using var dlg = new SpanEditForm("Edit Redaction", SpanPositionKind.TextOffset, span, positionEditable: false);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
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
            // .msg is read but written back as .eml, so the suggested name already carries .eml.
            string suggested = RedactionService.GetOutputPath(_sourcePath, _settings);
            using var dialog = new SaveFileDialog
            {
                Title = "Save redacted email",
                Filter = "Email (*.eml)|*.eml|All files (*.*)|*.*",
                DefaultExt = "eml",
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
                EmailRedactor.ApplySpans(_sourcePath, output, _spans, _settings.ScrubEmailHeaders);
                stopwatch.Stop();
                RedactionDurationMs = stopwatch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, UserError.Describe(ex, output, writing: true), "Redact Email (Preview)",
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
                MessageBox.Show(this, $"Could not open the file: {ex.Message}", "Redact Email (Preview)",
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
