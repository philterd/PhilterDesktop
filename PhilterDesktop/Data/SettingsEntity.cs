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
    /// <summary>
    /// Entity representing application settings stored in LiteDB.
    /// </summary>
    public class SettingsEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the settings record.
        /// </summary>
        [BsonId]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether redacted files should be output to their original location.
        /// If false, files will be output to a custom folder specified by <see cref="CustomOutputFolder"/>.
        /// </summary>
        public bool OutputToOriginalLocation { get; set; } = true;

        /// <summary>
        /// Gets or sets the custom output folder path for redacted files.
        /// Only used when <see cref="OutputToOriginalLocation"/> is false.
        /// </summary>
        public string CustomOutputFolder { get; set; } = string.Empty;

        /// <summary>
        /// Suffix appended to the redacted output file name (before the extension). Defaults to
        /// <c>_redacted-draft</c> — deliberately not just "_redacted", so the name doesn't imply the
        /// output is verified safe (redaction is statistical and must always be reviewed by a person).
        /// </summary>
        public string RedactedSuffix { get; set; } = "_redacted-draft";

        /// <summary>
        /// Gets or sets a value indicating whether logging is enabled.
        /// </summary>
        public bool LoggingEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the one-time "still running in the tray" hint
        /// has been shown (so closing the window to the tray is only explained once).
        /// </summary>
        public bool TrayHintShown { get; set; } = false;

        /// <summary>Whether a tray balloon is shown when a document finishes redacting (default on).</summary>
        public bool NotificationsEnabled { get; set; } = true;

        /// <summary>
        /// How many watched-folder files may be redacted at once (default 1). Higher values speed up
        /// bursts of small files; large files always run alone regardless. Clamped to 1–4 in the UI.
        /// </summary>
        public int WatchedFolderMaxConcurrency { get; set; } = 1;

        /// <summary>
        /// Whether to automatically verify each redaction by re-scanning the written output for residual
        /// PII (the false-negative self-check). On by default; can be toggled in Settings. Verification
        /// can also be run on demand for any completed item.
        /// </summary>
        public bool VerifyAfterRedaction { get; set; } = true;

        /// <summary>
        /// When verifying, scan with a broad "all detectors on" policy instead of the policy used to
        /// redact. <b>On by default</b>, because a verification with the redaction's own policy cannot
        /// surface the missed-PII (false-negative) case it exists to catch — it can only re-find types
        /// already redacted. Broad scanning flags PII <i>types</i> the redaction policy didn't cover (to
        /// review), while the document's own inserted replacements are never reported.
        /// </summary>
        public bool VerificationUseBroadPolicy { get; set; } = true;

        /// <summary>
        /// Strip document metadata (author, company, last-modified-by, title, keywords, custom fields)
        /// from redacted Office output so a "redacted" file doesn't leak identifying info through its
        /// properties. On by default.
        /// </summary>
        public bool ScrubDocumentMetadata { get; set; } = true;

        /// <summary>Remove reviewer comments from redacted Word output. On by default.</summary>
        public bool ScrubWordComments { get; set; } = true;

        /// <summary>Accept/strip tracked changes (revisions) in redacted Word output. On by default.</summary>
        public bool ScrubWordTrackedChanges { get; set; } = true;

        /// <summary>Remove hidden text from redacted Word output. On by default.</summary>
        public bool ScrubWordHiddenText { get; set; } = true;

        /// <summary>
        /// Redact detected PII in the print headers and footers of Word (.docx) and Excel (.xlsx) files.
        /// Header/footer text (e.g. "Confidential — John Doe" printed on every page) is scanned like body
        /// text; legitimate content such as page numbers, dates, and logos is preserved. On by default.
        /// </summary>
        public bool RedactOfficeHeadersFooters { get; set; } = true;

        /// <summary>
        /// Redact detected PII inside embedded charts in Word (.docx) and Excel (.xlsx) files: chart
        /// titles, axis/data labels, and the <b>cached series and category values</b> (numCache/strCache)
        /// that copy the source cells and would otherwise survive. On by default. Redacting a cached value
        /// can change how the chart looks, so review charts in the output.
        /// </summary>
        public bool RedactOfficeCharts { get; set; } = true;

        /// <summary>
        /// Redact the <b>cached results of formula cells</b> in Excel (.xlsx). A formula stores a copy of
        /// its last computed value, which can duplicate PII from a now-redacted cell and ships as plaintext
        /// until Excel recalculates. When on, a formula whose cached result holds detected PII becomes a
        /// static redacted value, and the remaining formula caches are cleared with the workbook set to
        /// recalculate on open. On by default. A side effect: a program that reads the file <em>without</em>
        /// recalculating (i.e. not Excel) sees blank formula values until it recalculates.
        /// </summary>
        public bool RedactCachedFormulaValues { get; set; } = true;

        /// <summary>
        /// Redact the <b>pivot cache</b> in Excel (.xlsx). A pivot table keeps a denormalized snapshot of its
        /// source data (the cache definition's shared items and the cache records), so redacting the source
        /// cells alone leaves an intact copy of the PII. When on, the cached string values in the pivot cache
        /// definition, cache records, and pivot table captions are scanned and redacted, and the cache is set
        /// to refresh on open. On by default. Redacting the cache changes what the pivot shows until Excel
        /// refreshes it from the (redacted) source.
        /// </summary>
        public bool RedactPivotCaches { get; set; } = true;

        /// <summary>
        /// Remove <b>embedded objects that Philter Desktop can't inspect</b> from redacted Word (.docx) and
        /// Excel (.xlsx) files. An embedded object (Insert &gt; Object) carries its own full content; an
        /// embedded Word/Excel document is redacted in place, but an opaque OLE object (a legacy binary
        /// object, or another program's document) can't be read and would otherwise ship verbatim. When on,
        /// those un-inspectable objects are deleted. On by default. When off, they are kept and verification
        /// warns that their content wasn't inspected.
        /// </summary>
        public bool RemoveUninspectableEmbeddedObjects { get; set; } = true;

        /// <summary>
        /// Remove identifying technical headers from redacted email output (the originating IP, the
        /// sending mail client, and the server-hop trail): <c>Received</c>, <c>Return-Path</c>,
        /// <c>Message-Id</c>, <c>User-Agent</c>, <c>DKIM-Signature</c>, authentication results, and all
        /// <c>X-</c>/<c>ARC-</c> headers. On by default.
        /// </summary>
        public bool ScrubEmailHeaders { get; set; } = true;

        /// <summary>
        /// Remove common identity/delivery headers from redacted email output: <c>Bcc</c>,
        /// <c>Reply-To</c>, <c>Sender</c>, and the <c>Resent-*</c> headers. These carry recipient/sender
        /// addresses that aren't part of the scanned From/To/Cc fields (notably <c>Bcc</c>, which names
        /// blind-copy recipients and is carried through from <c>.msg</c> input). On by default.
        /// </summary>
        public bool RemoveCommonEmailHeaders { get; set; } = true;

        /// <summary>
        /// Remove the <c>Date</c> header from redacted email output. The header is dropped outright (not
        /// scanned for a date to redact), so the send time is removed regardless of its format. Off by
        /// default — the send date is usually wanted and isn't personally identifying on its own.
        /// </summary>
        public bool RemoveEmailDateHeader { get; set; } = false;

        /// <summary>
        /// Remove attachments from redacted email output. Attachments are <b>deleted entirely, not
        /// redacted</b> — their content is never inspected — so an attached file (and its filename) is gone
        /// from the output. Off by default, because it discards content the user may need; turn it on when
        /// an email's attachments could carry sensitive information that must not ship in the redacted copy.
        /// </summary>
        public bool RemoveEmailAttachments { get; set; } = false;

        /// <summary>
        /// Also remove <b>inline images</b> (cid-referenced pictures embedded in the message body — logos,
        /// signatures, pasted screenshots) from redacted email output. A dependent option of
        /// <see cref="RemoveEmailAttachments"/>: it only takes effect when that is on. Image content is
        /// never inspected or redacted, so the images are deleted outright and their <c>cid:</c> references
        /// in the HTML body are neutralized. Off by default.
        /// </summary>
        public bool RemoveEmailInlineImages { get; set; } = false;

        /// <summary>
        /// Read scanned (image-only) PDF pages with on-device OCR so their text can be detected and
        /// redacted. OCR runs entirely on this computer (nothing is uploaded). On by default; it is
        /// slower and best-effort (it can miss low-quality scans and handwriting), so the redacted
        /// output should still be reviewed.
        /// </summary>
        public bool OcrScannedPdfs { get; set; } = true;

        /// <summary>
        /// Advanced OCR tuning: a PDF page whose text-layer glyphs cover less than this fraction of the
        /// page is treated as scanned and OCR'd. Lower = OCR fewer pages. Default 0.01 (1%).
        /// </summary>
        public double OcrTextCoverageThreshold { get; set; } = 0.01;

        /// <summary>
        /// Advanced OCR tuning: a page that still has real text but whose embedded images cover at least
        /// this fraction of the page is also OCR'd (to catch PII inside a large scan). Default 0.5 (50%).
        /// </summary>
        public double OcrImageCoverageThreshold { get; set; } = 0.5;

        /// <summary>
        /// Safety cap on how many pages of a single PDF may be OCR'd. If a document needs OCR on more
        /// pages than this, redaction stops with a clear error rather than producing a partially-OCR'd
        /// output that could leave PII on the un-OCR'd pages. 0 means no limit. Default 200.
        /// </summary>
        public int OcrMaxPages { get; set; } = 200;

        /// <summary>
        /// When redacting a PDF, also black out raster images that repeat across its pages — logos,
        /// watermarks, and similar recurring graphics — wherever they appear, without needing a fixed
        /// region. Off by default. Only catches placed raster images (not vector-drawn logos), and may
        /// also cover other images that recur across pages.
        /// </summary>
        public bool RedactRecurringImages { get; set; }

        /// <summary>
        /// Hard upper bound (in megabytes) on the size of a file the non-interactive paths
        /// (watched folders and the command line) will redact. Larger files are skipped and logged,
        /// since redaction loads a document into memory. 0 means no limit. Default 500 MB.
        /// </summary>
        public int MaxInputFileSizeMb { get; set; } = 500;

        /// <summary>
        /// Maximum time (in seconds) a single detection pattern may run before it is aborted, so a slow
        /// or malformed custom-identifier regular expression can't hang redaction. Clamped to 5–15
        /// seconds (see <see cref="RegexSafety"/>). Default 5.
        /// </summary>
        public int RegexMatchTimeoutSeconds { get; set; } = 5;

        /// <summary>Global "always redact" terms (one per line), applied on top of every policy.</summary>
        public string GlobalAlwaysRedact { get; set; } = string.Empty;

        /// <summary>Global "always ignore" terms (one per line), applied on top of every policy.</summary>
        public string GlobalAlwaysIgnore { get; set; } = string.Empty;

        /// <summary>The policy chosen most recently, pre-selected next time a redaction is started.</summary>
        public string LastPolicy { get; set; } = string.Empty;

        /// <summary>The context chosen most recently, pre-selected next time a redaction is started.</summary>
        public string LastContext { get; set; } = string.Empty;

        /// <summary>The folder a preview redaction was last saved to (used to seed the Save dialog).</summary>
        public string LastSaveFolder { get; set; } = string.Empty;

        // --- Remembered main-window layout (restored on next launch) -------------------------------

        /// <summary>Last normal (non-maximized) window position/size. Width 0 means "never saved".</summary>
        public int WindowX { get; set; }
        public int WindowY { get; set; }
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }

        /// <summary>Whether the window was maximized when last closed.</summary>
        public bool WindowMaximized { get; set; }

        /// <summary>Queue sort column index and direction.</summary>
        public int SortColumn { get; set; }
        public bool SortAscending { get; set; } = true;

        /// <summary>Comma-separated queue column widths, e.g. "350,180,120,120". Empty means default.</summary>
        public string ColumnWidths { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time when the settings were last modified.
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Copies the fields that are managed <i>outside</i> the Settings dialog — the remembered window
        /// layout, the last-used policy/context/folder, the global always-redact/ignore lists, and the
        /// tray hint — from <paramref name="source"/> onto this instance. The Settings dialog reads the
        /// whole singleton on open and would otherwise persist its open-time snapshot of these fields,
        /// reverting any change a background redaction, the main window, or the Global Lists dialog made
        /// while it was open. Re-reading and adopting them just before Save keeps this dialog to the
        /// fields it actually owns.
        /// </summary>
        public void CopyExternallyManagedFieldsFrom(SettingsEntity source)
        {
            TrayHintShown = source.TrayHintShown;
            GlobalAlwaysRedact = source.GlobalAlwaysRedact;
            GlobalAlwaysIgnore = source.GlobalAlwaysIgnore;
            LastPolicy = source.LastPolicy;
            LastContext = source.LastContext;
            LastSaveFolder = source.LastSaveFolder;
            WindowX = source.WindowX;
            WindowY = source.WindowY;
            WindowWidth = source.WindowWidth;
            WindowHeight = source.WindowHeight;
            WindowMaximized = source.WindowMaximized;
            SortColumn = source.SortColumn;
            SortAscending = source.SortAscending;
            ColumnWidths = source.ColumnWidths;
        }
    }
}