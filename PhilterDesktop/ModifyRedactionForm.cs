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
using LiteDB;
using Phileas.Policy;
using PhilterData;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop
{
    /// <summary>
    /// Reviews and modifies a document's redactions. A TreeView lists the document's versions; the
    /// selected version's spans appear in an editable ListView (add/edit/remove, saved to that
    /// version). <b>New Version</b> creates a copy of the latest version's spans to work on, and
    /// <b>Redact</b> applies the selected version's spans to the original source to (re)produce its
    /// output file. Version 1 (the initial redaction) cannot be deleted or edited.
    /// </summary>
    public partial class ModifyRedactionForm : Form
    {
        private readonly ObjectId _documentId = null!;
        private readonly RedactionVersionRepository _versions = null!;
        private readonly RedactionSpanRepository _spans = null!;
        private readonly PolicyRepository _policies = null!;

        private List<RedactionSpanEntity> _working = new();
        private RedactionVersionEntity? _selectedVersion;
        private readonly string _redactedSuffix = RedactionService.DefaultSuffix;
        private readonly WordScrubOptions _wordScrub = WordScrubOptions.None;
        private readonly bool _scrubEmailHeaders;
        private readonly bool _removeCommonEmailHeaders;
        private readonly bool _removeEmailDateHeader;
        private readonly bool _removeEmailAttachments;
        private readonly bool _removeEmailInlineImages;
        private readonly bool _redactOfficeHeadersFooters = true;
        private readonly bool _redactOfficeCharts = true;
        private readonly bool _redactCachedFormulaValues = true;
        private readonly bool _redactPivotCaches = true;
        private readonly bool _outputToOriginalLocation = true;
        private readonly string _customOutputFolder = string.Empty;
        private readonly string _globalAlwaysRedact = string.Empty;
        private readonly string _globalAlwaysIgnore = string.Empty;

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public ModifyRedactionForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_redact);
        }

        public ModifyRedactionForm(
            ObjectId documentId,
            RedactionVersionRepository versions,
            RedactionSpanRepository spans,
            PolicyRepository policies,
            string? redactedSuffix = null,
            WordScrubOptions wordScrub = WordScrubOptions.None,
            bool scrubEmailHeaders = false,
            bool removeCommonEmailHeaders = false,
            bool removeEmailDateHeader = false,
            bool removeEmailAttachments = false,
            bool removeEmailInlineImages = false,
            bool redactOfficeHeadersFooters = true,
            bool redactOfficeCharts = true,
            bool redactCachedFormulaValues = true,
            bool redactPivotCaches = true,
            bool outputToOriginalLocation = true,
            string? customOutputFolder = null,
            string? globalAlwaysRedact = null,
            string? globalAlwaysIgnore = null)
            : this()
        {
            _documentId = documentId;
            _versions = versions;
            _spans = spans;
            _policies = policies;
            _redactedSuffix = RedactionService.NormalizeSuffix(redactedSuffix);
            _wordScrub = wordScrub;
            _scrubEmailHeaders = scrubEmailHeaders;
            _removeCommonEmailHeaders = removeCommonEmailHeaders;
            _removeEmailDateHeader = removeEmailDateHeader;
            _removeEmailAttachments = removeEmailAttachments;
            _removeEmailInlineImages = removeEmailInlineImages;
            _redactOfficeHeadersFooters = redactOfficeHeadersFooters;
            _redactOfficeCharts = redactOfficeCharts;
            _redactCachedFormulaValues = redactCachedFormulaValues;
            _redactPivotCaches = redactPivotCaches;
            _outputToOriginalLocation = outputToOriginalLocation;
            _customOutputFolder = customOutputFolder ?? string.Empty;
            _globalAlwaysRedact = globalAlwaysRedact ?? string.Empty;
            _globalAlwaysIgnore = globalAlwaysIgnore ?? string.Empty;

            ReloadVersions(selectLatest: true);
        }

        // --- Designer-wired event handlers ------------------------------------

        private void VersionTree_AfterSelect(object? sender, TreeViewEventArgs e) =>
            OnVersionSelected(e.Node?.Tag as RedactionVersionEntity);

        private void SpanList_SelectedIndexChanged(object? sender, EventArgs e) => UpdateButtons();

        // --- Versions ---------------------------------------------------------

        private void ReloadVersions(bool selectLatest, ObjectId? select = null)
        {
            IReadOnlyList<RedactionVersionEntity> versions = _versions.GetForDocument(_documentId);

            _versionTree.BeginUpdate();
            _versionTree.Nodes.Clear();
            TreeNode? toSelect = null;
            foreach (RedactionVersionEntity v in versions)
            {
                string label = $"Version {v.Version}  ({v.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm})";
                if (string.IsNullOrEmpty(v.OutputPath))
                {
                    label += "  — not redacted";
                }
                var node = new TreeNode(label) { Tag = v };
                _versionTree.Nodes.Add(node);
                if ((select is not null && v.Id == select) || (select is null && selectLatest))
                {
                    toSelect = node; // last matching node (latest, when selectLatest)
                }
            }
            _versionTree.EndUpdate();

            if (toSelect is not null)
            {
                _versionTree.SelectedNode = toSelect;
                OnVersionSelected(toSelect.Tag as RedactionVersionEntity);
            }
            else
            {
                _selectedVersion = null;
                _working = new List<RedactionSpanEntity>();
                RefreshSpanList();
            }
        }

        private void OnVersionSelected(RedactionVersionEntity? version)
        {
            _selectedVersion = version;
            _working = version is null
                ? new List<RedactionSpanEntity>()
                : _spans.GetForVersion(version.Id).Select(Clone).ToList();
            RefreshSpanList();
        }

        private void RefreshSpanList()
        {
            _spanList.BeginUpdate();
            _spanList.Items.Clear();
            foreach (RedactionSpanEntity s in _working)
            {
                var item = new ListViewItem(s.UserAdded ? "Added" : Display(s.Classification)) { Tag = s };
                item.SubItems.Add(s.Text);
                item.SubItems.Add(s.Replacement);
                item.SubItems.Add(StartText(s));
                item.SubItems.Add(StopText(s));
                item.SubItems.Add(DescribeLocation(s));
                _spanList.Items.Add(item);
            }
            _spanList.EndUpdate();
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            bool hasVersion = _selectedVersion is not null;
            bool hasSpan = _spanList.SelectedItems.Count > 0;

            // Version 1 (the original redaction) is read-only: its spans can't be added/edited/removed,
            // and the version itself can't be deleted. Make a new version to change anything.
            bool editable = hasVersion && _selectedVersion!.Version != 1;

            // Cell/field-indexed formats (spreadsheets, email) address spans by an ordinal, not by a
            // user-pickable position, so there's no coherent way to *add* a redaction by hand — only
            // to remove one or change its replacement. Disable Add for them (it would otherwise be a
            // silent no-op, since a hand-placed span can't be matched back to a cell/field).
            bool indexed = hasVersion && RedactionService.UsesOrdinalSpanAddressing(_selectedVersion!.FileType);

            _add.Enabled = editable && !indexed;
            _edit.Enabled = hasSpan && editable;
            _remove.Enabled = hasSpan && editable;
            _redact.Enabled = hasVersion;
            _deleteVersion.Enabled = editable;
        }

        private bool IsReadOnlyVersion => _selectedVersion is null || _selectedVersion.Version == 1;

        // Persist the in-memory working spans back to the selected version.
        private void SaveWorking()
        {
            if (_selectedVersion is null)
            {
                return;
            }
            _spans.DeleteForVersion(_selectedVersion.Id);
            int order = 0;
            var stored = _working.Select(s => { RedactionSpanEntity c = Clone(s); c.VersionId = _selectedVersion.Id; c.Order = order++; return c; }).ToList();
            if (stored.Count > 0)
            {
                _spans.InsertBulk(stored);
            }
        }

        private void OnNewVersion(object? sender, EventArgs e)
        {
            IReadOnlyList<RedactionVersionEntity> versions = _versions.GetForDocument(_documentId);
            if (versions.Count == 0)
            {
                return;
            }
            RedactionVersionEntity latest = versions[^1]; // highest version number

            var created = new RedactionVersionEntity
            {
                DocumentId = _documentId,
                Version = _versions.NextVersionNumber(_documentId),
                SourcePath = latest.SourcePath,
                OutputPath = string.Empty, // produced when the user clicks Redact
                FileType = latest.FileType,
                Policy = latest.Policy,
                Context = latest.Context,
                Highlight = latest.Highlight
            };
            _versions.Insert(created);

            int order = 0;
            var copied = _spans.GetForVersion(latest.Id)
                .Select(s => { RedactionSpanEntity c = Clone(s); c.VersionId = created.Id; c.Order = order++; return c; })
                .ToList();
            if (copied.Count > 0)
            {
                _spans.InsertBulk(copied);
            }

            ReloadVersions(selectLatest: false, select: created.Id);
        }

        private void OnDeleteVersion(object? sender, EventArgs e)
        {
            if (_selectedVersion is null || _selectedVersion.Version == 1)
            {
                return;
            }
            if (MessageBox.Show(
                    $"Delete version {_selectedVersion.Version}? Its output file is left on disk.",
                    "Modify Redaction", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            _spans.DeleteForVersion(_selectedVersion.Id);
            _versions.Delete(_selectedVersion.Id);
            ReloadVersions(selectLatest: true);
        }

        private void OnAdd(object? sender, EventArgs e)
        {
            if (IsReadOnlyVersion || RedactionService.UsesOrdinalSpanAddressing(_selectedVersion!.FileType))
            {
                return; // no per-cell/field add UI (the button is disabled for these formats)
            }
            SpanPositionKind kind = KindFor(_selectedVersion!.FileType);
            var template = new RedactionSpanEntity
            {
                UserAdded = true,
                Replacement = RedactionService.DefaultReplacement,
                ParagraphIndex = kind == SpanPositionKind.Paragraph ? 0 : -1
            };
            using var dlg = new SpanEditForm("Add Redaction", kind, template, positionEditable: true,
                maxOffset: kind == SpanPositionKind.TextOffset ? SourceTextLength() : 0,
                paragraphLengths: kind == SpanPositionKind.Paragraph ? SourceParagraphLengths() : null);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _working.Add(FromDialog(new RedactionSpanEntity { UserAdded = true }, kind, dlg));
                SaveWorking();
                RefreshSpanList();
            }
        }

        private void OnEdit(object? sender, EventArgs e)
        {
            if (IsReadOnlyVersion || _spanList.SelectedItems.Count == 0 || _spanList.SelectedItems[0].Tag is not RedactionSpanEntity span)
            {
                return;
            }
            SpanPositionKind kind = KindFor(_selectedVersion!.FileType);
            // A detected span's position is fixed (anchored to where it was found); only user-added
            // spans can have their position changed. Cell/field-indexed formats have no positional UI,
            // so there the replacement is the only editable part. The replacement is always editable.
            bool positionEditable = span.UserAdded && !RedactionService.UsesOrdinalSpanAddressing(_selectedVersion!.FileType);
            using var dlg = new SpanEditForm("Edit Redaction", kind, span, positionEditable,
                maxOffset: kind == SpanPositionKind.TextOffset ? SourceTextLength() : 0,
                paragraphLengths: kind == SpanPositionKind.Paragraph ? SourceParagraphLengths() : null);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                if (positionEditable)
                {
                    FromDialog(span, kind, dlg);
                }
                else
                {
                    span.Replacement = dlg.Replacement;
                }
                SaveWorking();
                RefreshSpanList();
            }
        }

        private static SpanPositionKind KindFor(string fileType) => fileType.ToLowerInvariant() switch
        {
            ".docx" => SpanPositionKind.Paragraph,
            ".pdf" => SpanPositionKind.Pdf,
            _ => SpanPositionKind.TextOffset
        };

        // The redactable text length of the source (for plain-text/RTF span-offset bounds), so a manually
        // added span can't point past the end and then be silently dropped when applied.
        private int SourceTextLength()
        {
            try
            {
                string path = _selectedVersion!.SourcePath;
                return _selectedVersion.FileType.ToLowerInvariant() == ".rtf"
                    ? RtfRedactor.ReadText(path).Length
                    : File.ReadAllText(path).Length;
            }
            catch
            {
                return 0; // source unreadable — fall back to no cap rather than blocking edits
            }
        }

        // The length of each Word paragraph's text (by paragraph index), for the same offset bounds.
        private IReadOnlyList<int>? SourceParagraphLengths()
        {
            try
            {
                return WordDocumentRedactor.ReadParagraphs(_selectedVersion!.SourcePath).Select(p => p.Length).ToList();
            }
            catch
            {
                return null;
            }
        }

        // Copies the dialog's position + replacement onto the span according to the document type.
        private static RedactionSpanEntity FromDialog(RedactionSpanEntity span, SpanPositionKind kind, SpanEditForm dlg)
        {
            span.Replacement = dlg.Replacement;
            switch (kind)
            {
                case SpanPositionKind.Paragraph:
                    span.ParagraphIndex = dlg.Paragraph;
                    span.CharacterStart = dlg.Start;
                    span.CharacterEnd = dlg.Stop;
                    break;
                case SpanPositionKind.Pdf:
                    span.PageNumber = dlg.Page;
                    span.LowerLeftX = dlg.LowerLeftX;
                    span.LowerLeftY = dlg.LowerLeftY;
                    span.UpperRightX = dlg.UpperRightX;
                    span.UpperRightY = dlg.UpperRightY;
                    break;
                default:
                    span.ParagraphIndex = -1;
                    span.CharacterStart = dlg.Start;
                    span.CharacterEnd = dlg.Stop;
                    break;
            }
            return span;
        }

        private void OnRemove(object? sender, EventArgs e)
        {
            if (IsReadOnlyVersion || _spanList.SelectedItems.Count == 0 || _spanList.SelectedItems[0].Tag is not RedactionSpanEntity span)
            {
                return;
            }
            _working.Remove(span);
            SaveWorking();
            RefreshSpanList();
        }

        // Enables/disables all interactive controls around the async re-redaction. On finishing, the
        // per-control enabled states are restored via UpdateButtons() (which respects the selection).
        private void SetBusy(bool busy)
        {
            UseWaitCursor = busy;
            _versionTree.Enabled = !busy;
            _spanList.Enabled = !busy;
            _newVersion.Enabled = !busy;
            _close.Enabled = !busy;
            if (busy)
            {
                _add.Enabled = _edit.Enabled = _remove.Enabled = _redact.Enabled = _deleteVersion.Enabled = false;
            }
            else
            {
                UpdateButtons();
            }
        }

        /// <summary>
        /// True once the user re-redacted to a new output during this session. The caller uses it to
        /// reset the document's (now stale) verification verdict, since the new output is unverified.
        /// </summary>
        public bool RedactionChanged { get; private set; }

        private async void OnRedact(object? sender, EventArgs e)
        {
            if (_selectedVersion is null)
            {
                return;
            }
            RedactionVersionEntity version = _selectedVersion;

            if (!File.Exists(version.SourcePath))
            {
                MessageBox.Show(
                    $"The original document was not found:\n{version.SourcePath}\n\nIt is required to create a redaction.",
                    "Modify Redaction", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Resolve the output the same way the main redaction paths do — honoring the configured
            // output location (original folder vs. custom folder) and the suffix — then make it unique so
            // re-redacting never silently overwrites an earlier output (e.g. version 1's original file).
            string output = RedactionService.GetUniqueOutputPath(
                RedactionService.GetOutputPath(version.SourcePath, _outputToOriginalLocation, _customOutputFolder, _redactedSuffix));

            PhileasPolicy? policy = LoadPolicy(version.Policy);

            // Lock the form while the re-redaction runs so the user can't edit spans, switch versions,
            // or close mid-write (which would race the async output).
            SetBusy(true);
            try
            {
                await RedactionService.ApplySpansAsync(version.SourcePath, output, version.FileType, version.Highlight, _working, policy,
                    wordScrub: _wordScrub,
                    scrubEmailHeaders: _scrubEmailHeaders,
                    removeCommonEmailHeaders: _removeCommonEmailHeaders,
                    removeEmailDateHeader: _removeEmailDateHeader,
                    removeEmailAttachments: _removeEmailAttachments,
                    removeEmailInlineImages: _removeEmailInlineImages,
                    redactOfficeHeadersFooters: _redactOfficeHeadersFooters,
                    redactOfficeCharts: _redactOfficeCharts,
                    redactCachedFormulaValues: _redactCachedFormulaValues,
                    redactPivotCaches: _redactPivotCaches,
                    worksheet: version.Worksheet);
            }
            catch (Exception ex)
            {
                MessageBox.Show(UserError.Describe(ex, output, writing: true), "Modify Redaction", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            finally
            {
                SetBusy(false);
            }

            version.OutputPath = output;
            _versions.Update(version);
            RedactionChanged = true; // the new output is unverified; the caller resets the stale verdict
            ReloadVersions(selectLatest: false, select: version.Id);

            // Open the freshly redacted document in the system default application.
            OpenInDefaultApp(output);
        }

        private static void OpenInDefaultApp(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open the file: {ex.Message}", "Modify Redaction", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private PhileasPolicy? LoadPolicy(string name)
        {
            try
            {
                PolicyEntity? entity = _policies.FindByName(name);
                if (entity is null)
                {
                    return null;
                }
                PhileasPolicy policy = PolicySerializer.DeserializeFromJson(string.IsNullOrWhiteSpace(entity.Json) ? "{}" : entity.Json);
                // Merge the global always-redact/ignore lists like every other redaction path, so a
                // re-render (e.g. of docx drawings) still enforces them instead of letting terms reappear.
                GlobalLists.ApplyTerms(policy, GlobalLists.ParseTerms(_globalAlwaysRedact), GlobalLists.ParseTerms(_globalAlwaysIgnore));
                return policy;
            }
            catch
            {
                return null;
            }
        }

        private static string Display(string classification) =>
            string.IsNullOrEmpty(classification) ? "Detected" : classification;

        private string DescribeLocation(RedactionSpanEntity s)
        {
            if (s.PageNumber > 0)
            {
                return $"Page {s.PageNumber}";
            }
            if (s.ParagraphIndex < 0)
            {
                return string.Empty;
            }
            // The "paragraph index" doubles as a cell ordinal (spreadsheets) or field ordinal (email);
            // label it for what it actually is so the location isn't misleading.
            return _selectedVersion?.FileType.ToLowerInvariant() switch
            {
                ".xlsx" or ".csv" => $"Cell {s.ParagraphIndex + 1}",
                ".eml" or ".msg" => $"Field {s.ParagraphIndex + 1}",
                _ => $"¶ {s.ParagraphIndex + 1}"
            };
        }

        // PDF spans are located by page + coordinates, not character offsets, so leave start/stop blank.
        private static string StartText(RedactionSpanEntity s) =>
            s.PageNumber > 0 ? string.Empty : s.CharacterStart.ToString();

        private static string StopText(RedactionSpanEntity s) =>
            s.PageNumber > 0 ? string.Empty : s.CharacterEnd.ToString();

        private static RedactionSpanEntity Clone(RedactionSpanEntity s) => new()
        {
            VersionId = s.VersionId,
            Order = s.Order,
            Text = s.Text,
            Replacement = s.Replacement,
            Classification = s.Classification,
            UserAdded = s.UserAdded,
            CharacterStart = s.CharacterStart,
            CharacterEnd = s.CharacterEnd,
            ParagraphIndex = s.ParagraphIndex,
            PageNumber = s.PageNumber,
            LowerLeftX = s.LowerLeftX,
            LowerLeftY = s.LowerLeftY,
            UpperRightX = s.UpperRightX,
            UpperRightY = s.UpperRightY
        };
    }
}
