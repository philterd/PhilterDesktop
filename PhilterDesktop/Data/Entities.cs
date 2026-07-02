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

using LiteDB;

namespace PhilterData
{
    public class ContextEntity
    {
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ContextEntryEntity
    {
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();
        public string Token { get; set; } = string.Empty;
        public string Replacement { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;

        /// <summary>UTC time the mapping was first stored. Default for entries written before this field existed.</summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>UTC time the mapping's replacement was last written (equals <see cref="CreatedAtUtc"/> until first updated).</summary>
        public DateTime UpdatedAtUtc { get; set; }
    }

    public class PolicyEntity
    {
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();
        public string Name { get; set; } = string.Empty;
        public string Json { get; set; } = "{\"Identifiers\": {}}";

        /// <summary>
        /// A free-text description to help the user understand what the policy is for. Editor-only
        /// metadata — not part of the engine policy JSON and not used during redaction.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class RedactionQueueEntity
    {
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string Policy { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;

        /// <summary>
        /// Plain-language reason the most recent redaction attempt failed; empty when not failed.
        /// Persisted so the user can see why a "Failed" row failed after any toast has gone.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>When true, redacted replacements in .docx output are highlighted.</summary>
        public bool Highlight { get; set; }

        /// <summary>
        /// For spreadsheets (.xlsx/.csv): 1-based column indices whose every data cell should be
        /// redacted in full (the column header is preserved). Empty for all other documents and for
        /// spreadsheets queued without column selection.
        /// </summary>
        public List<int> FullyRedactedColumns { get; set; } = new();

        /// <summary>
        /// For an <c>.xlsx</c> file: the single worksheet (by name) to redact. Empty means the whole
        /// workbook (legacy) or a non-spreadsheet document.
        /// </summary>
        public string Worksheet { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // --- Post-redaction verification (residual-PII self-check) -----------------------------------

        /// <summary>
        /// The result of the verification pass over this item's redacted output: "NotRun", "Clean",
        /// "ResidualsFound", or "Error". Stored so it survives restarts and can feed the report.
        /// </summary>
        public string VerificationStatus { get; set; } = "NotRun";

        /// <summary>How many residual items the verification pass detected (0 when clean/not run).</summary>
        public int VerificationFindingCount { get; set; }

        /// <summary>When the verification pass last ran (UTC); null if it has not run.</summary>
        public DateTime? VerificationCheckedAt { get; set; }
    }

    /// <summary>
    /// A single redacted span belonging to a <see cref="RedactionVersionEntity"/>. Carries enough
    /// location detail to re-apply it to the source for every supported file type: character offsets
    /// (.txt and, with <see cref="ParagraphIndex"/>, .docx) and page/coordinates (.pdf).
    /// </summary>
    public class RedactionSpanEntity
    {
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();

        /// <summary>The <see cref="RedactionVersionEntity.Id"/> this span belongs to.</summary>
        public ObjectId VersionId { get; set; } = ObjectId.Empty;

        /// <summary>Stable display/apply order within the version.</summary>
        public int Order { get; set; }

        /// <summary>The original (detected) text. For user-added spans this is the term to redact.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>The replacement text written in place of <see cref="Text"/>.</summary>
        public string Replacement { get; set; } = string.Empty;

        /// <summary>Filter type / label (e.g. "email-address", "ssn"), informational.</summary>
        public string Classification { get; set; } = string.Empty;

        /// <summary>True for a span the user added by hand (located by term search when re-applied).</summary>
        public bool UserAdded { get; set; }

        // --- Explanation detail (why the engine flagged this) ---------------------------------------
        // Populated from the engine's Span when a detection is captured. Empty/zero for user-added
        // spans. Used by the "Export Explanation (JSON)" feature.

        /// <summary>The engine filter that matched (e.g. "EMAIL_ADDRESS", "SSN").</summary>
        public string FilterType { get; set; } = string.Empty;

        /// <summary>The engine's confidence in this detection (0–1).</summary>
        public double Confidence { get; set; }

        /// <summary>The rule/regex pattern that matched, when the filter is pattern-based.</summary>
        public string Pattern { get; set; } = string.Empty;

        /// <summary>The surrounding tokens (context window) the engine considered, when available.</summary>
        public List<string> Window { get; set; } = new();

        // --- Text location (.txt and .docx) ---
        public int CharacterStart { get; set; }
        public int CharacterEnd { get; set; }

        /// <summary>For .docx: index into the document's canonical paragraph enumeration; -1 otherwise.</summary>
        public int ParagraphIndex { get; set; } = -1;

        // --- PDF location ---
        public int PageNumber { get; set; }
        public double LowerLeftX { get; set; }
        public double LowerLeftY { get; set; }
        public double UpperRightX { get; set; }
        public double UpperRightY { get; set; }
    }

    /// <summary>
    /// One redaction of a document: the initial automatic redaction is version 1, and each manual
    /// modification (add/edit/remove spans) creates a new version. A version owns a set of
    /// <see cref="RedactionSpanEntity"/> (one-to-many) and points at the output file it produced.
    /// It also carries everything needed to re-apply against the original source.
    /// </summary>
    public class RedactionVersionEntity
    {
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();

        /// <summary>The <see cref="RedactionQueueEntity.Id"/> this version belongs to.</summary>
        public ObjectId DocumentId { get; set; } = ObjectId.Empty;

        /// <summary>1-based version number within the document.</summary>
        public int Version { get; set; }

        /// <summary>Original (source) document path the spans apply to.</summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>The redacted output file this version produced.</summary>
        public string OutputPath { get; set; } = string.Empty;

        /// <summary>Source extension (".txt", ".docx", ".pdf").</summary>
        public string FileType { get; set; } = string.Empty;

        public string Policy { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;

        /// <summary>When true, .docx replacements are highlighted.</summary>
        public bool Highlight { get; set; }

        /// <summary>For an <c>.xlsx</c> redaction: the single worksheet (by name) that was redacted.</summary>
        public string Worksheet { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// End-to-end time, in milliseconds, taken to produce this redaction (detect + write, plus the
        /// verification pass when enabled). 0 when not measured.
        /// </summary>
        public long DurationMs { get; set; }
    }

    /// <summary>
    /// A folder that Philter Desktop continuously monitors for new .txt/.docx/.pdf files.
    /// Each new file is redacted with the configured policy/context and written to
    /// <see cref="OutputFolder"/>.
    /// </summary>
    public class WatchedFolderEntity
    {
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();

        /// <summary>The folder being monitored for new files.</summary>
        public string FolderPath { get; set; } = string.Empty;

        /// <summary>Name of the policy to redact with.</summary>
        public string Policy { get; set; } = string.Empty;

        /// <summary>Name of the redaction context to use.</summary>
        public string Context { get; set; } = string.Empty;

        /// <summary>When true, redacted replacements in .docx output are highlighted.</summary>
        public bool Highlight { get; set; }

        /// <summary>When true, a tray notification is shown when a file in this folder is redacted.</summary>
        public bool Notify { get; set; }

        /// <summary>The folder redacted output is written to.</summary>
        public string OutputFolder { get; set; } = string.Empty;

        /// <summary>
        /// When true, subfolders are watched too and the redacted output mirrors the source
        /// subfolder structure under <see cref="OutputFolder"/>.
        /// </summary>
        public bool IncludeSubfolders { get; set; }

        /// <summary>
        /// File extensions (lowercase, with the leading dot, e.g. ".pdf") this folder redacts.
        /// An empty list means all supported types.
        /// </summary>
        public List<string> FileTypes { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// A single activity entry for a watched folder: when a file was found, redacted (and where),
    /// skipped, or failed.
    /// </summary>
    public class WatchedFolderLogEntity
    {
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();

        /// <summary>The <see cref="WatchedFolderEntity.Id"/> this entry belongs to.</summary>
        public ObjectId FolderId { get; set; } = ObjectId.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>"Info", "Warning", or "Error".</summary>
        public string Level { get; set; } = "Info";

        public string Message { get; set; } = string.Empty;
    }
}
