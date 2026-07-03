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
using Phileas.Model;
using Phileas.Services;
using Phileas.Services.Pdf;
using PhilterData;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop
{
    /// <summary>
    /// Core (UI-independent) redaction logic: resolving output paths, redacting a single file by
    /// type (capturing the applied spans), and re-applying an edited span set to the source.
    /// </summary>
    internal static class RedactionService
    {
        /// <summary>Default replacement used for user-added spans that don't specify one.</summary>
        public const string DefaultReplacement = "{{{REDACTED-custom}}}";

        /// <summary>
        /// Default output file-name suffix. Deliberately "_redacted-draft" rather than "_redacted" so
        /// the name doesn't imply the output is verified safe — redaction is statistical and must be
        /// reviewed by a person before the file is shared.
        /// </summary>
        public const string DefaultSuffix = "_redacted-draft";

        /// <summary>
        /// Returns a usable file-name suffix: the configured value with invalid file-name characters
        /// removed, or <see cref="DefaultSuffix"/> when it is empty/whitespace.
        /// </summary>
        public static string NormalizeSuffix(string? suffix)
        {
            if (string.IsNullOrWhiteSpace(suffix))
            {
                return DefaultSuffix;
            }
            string cleaned = string.Concat(suffix.Where(c => !Path.GetInvalidFileNameChars().Contains(c))).Trim();
            return cleaned.Length == 0 ? DefaultSuffix : cleaned;
        }

        /// <summary>Builds the redacted output file name (<c>name + suffix + ext</c>) for a source path.</summary>
        public static string ApplySuffix(string sourcePath, string? suffix) =>
            $"{Path.GetFileNameWithoutExtension(sourcePath)}{NormalizeSuffix(suffix)}{OutputExtension(sourcePath)}";

        /// <summary>
        /// The extension the redacted output takes for a given source. It matches the input except for
        /// Outlook <c>.msg</c> files, which are written back as standard <c>.eml</c> — the proprietary
        /// <c>.msg</c> container is read, but not faithfully rewritten.
        /// </summary>
        public static string OutputExtension(string sourcePath)
        {
            string ext = Path.GetExtension(sourcePath);
            return ext.Equals(".msg", StringComparison.OrdinalIgnoreCase) ? ".eml" : ext;
        }

        /// <summary>
        /// The file extensions Philter Desktop can redact (legacy binary Word .doc is not supported).
        /// This is the single source of truth used by the UI file pickers/drag-drop. Email files
        /// (<c>.eml</c>, <c>.msg</c>) are redacted to <c>.eml</c> (see <see cref="OutputExtension"/>).
        /// </summary>
        public static readonly string[] SupportedExtensions = { ".txt", ".docx", ".pdf", ".rtf", ".xlsx", ".csv", ".eml", ".msg" };

        /// <summary>Returns true if the file's extension is a supported, redactable type.</summary>
        public static bool IsSupported(string path) =>
            SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// True for formats whose redaction spans are located by an <b>ordinal</b> — a spreadsheet cell
        /// (<c>.xlsx</c>, <c>.csv</c>) or an email field (<c>.eml</c>, <c>.msg</c>) — rather than a
        /// user-addressable position. For these, Modify Redaction can review, remove, or change the
        /// replacement of a span, but a span cannot be <i>added</i> by hand (there's no position to pick,
        /// and a hand-placed span couldn't be matched back to a cell/field). Accepts a path or a bare
        /// extension (e.g. <c>".csv"</c>).
        /// </summary>
        public static bool UsesOrdinalSpanAddressing(string pathOrExtension)
        {
            string ext = pathOrExtension.StartsWith('.') ? pathOrExtension : Path.GetExtension(pathOrExtension);
            return ext.ToLowerInvariant() is ".xlsx" or ".csv" or ".eml" or ".msg";
        }

        /// <summary>
        /// Computes the redacted output path for an input file, honoring the output-location
        /// setting and appending the configured suffix (default "_redacted-draft") while preserving
        /// the extension.
        /// </summary>
        public static string GetOutputPath(string inputPath, SettingsEntity settings) =>
            GetOutputPath(inputPath, settings.OutputToOriginalLocation, settings.CustomOutputFolder, settings.RedactedSuffix);

        /// <summary>
        /// The redacted output path from the output-location pieces (rather than a whole
        /// <see cref="SettingsEntity"/>), so callers that only carry those options — e.g. Modify
        /// Redaction — resolve the path exactly the same way as the main redaction paths.
        /// </summary>
        public static string GetOutputPath(string inputPath, bool outputToOriginalLocation, string? customOutputFolder, string? suffix)
        {
            string directory = outputToOriginalLocation
                ? Path.GetDirectoryName(inputPath) ?? string.Empty
                : customOutputFolder ?? string.Empty;

            return Path.Combine(directory, ApplySuffix(inputPath, suffix));
        }

        /// <summary>
        /// Returns <paramref name="desiredPath"/> if no file is already there, otherwise the next free
        /// name with a counter suffix — <c>name (2).ext</c>, <c>name (3).ext</c>, … — so two source
        /// files with the same name (from different folders) don't silently overwrite each other in a
        /// shared output folder. Used by the direct-write paths (queue, CLI, Find &amp; Redact);
        /// the preview forms' Save dialog handles overwrites itself.
        /// </summary>
        public static string GetUniqueOutputPath(string desiredPath)
        {
            if (!File.Exists(desiredPath))
            {
                return desiredPath;
            }

            string dir = Path.GetDirectoryName(desiredPath) ?? string.Empty;
            string name = Path.GetFileNameWithoutExtension(desiredPath);
            string ext = Path.GetExtension(desiredPath); // includes the leading dot (or empty)

            for (int counter = 2; counter < 100_000; counter++)
            {
                string candidate = Path.Combine(dir, $"{name} ({counter}){ext}");
                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }

            // Pathological fallback (should never happen): a name that can't collide.
            return Path.Combine(dir, $"{name} ({Guid.NewGuid():N}){ext}");
        }

        /// <summary>
        /// The actual redacted-output path for a document: the latest version whose stored output
        /// still exists on disk (honoring a custom Save-As location or a later Modify-Redaction
        /// output), falling back to <paramref name="defaultPath"/>.
        /// </summary>
        /// <param name="versionsOldestFirst">The document's versions, ordered oldest-to-newest.</param>
        public static string ResolveOutputPath(IEnumerable<RedactionVersionEntity> versionsOldestFirst, string defaultPath)
        {
            string? stored = versionsOldestFirst
                .Where(v => !string.IsNullOrEmpty(v.OutputPath) && File.Exists(v.OutputPath))
                .Select(v => v.OutputPath)
                .LastOrDefault();
            return stored ?? defaultPath;
        }

        /// <summary>
        /// The folder to open a "Save redacted file" dialog in: the folder the user last saved a
        /// preview to (if it still exists), otherwise the suggested output folder, otherwise the
        /// source file's folder.
        /// </summary>
        public static string InitialSaveDirectory(SettingsEntity settings, string suggestedPath, string sourcePath)
        {
            if (!string.IsNullOrEmpty(settings.LastSaveFolder) && Directory.Exists(settings.LastSaveFolder))
            {
                return settings.LastSaveFolder;
            }
            string? dir = Path.GetDirectoryName(suggestedPath);
            return !string.IsNullOrEmpty(dir) ? dir : (Path.GetDirectoryName(sourcePath) ?? string.Empty);
        }

        /// <summary>
        /// Redacts using all the per-run options carried by <paramref name="settings"/> (OCR, Word/Excel
        /// metadata scrubbing, email-header handling). Every entry point (queue, watched folders, CLI,
        /// Find &amp; Redact) goes through here so they redact identically — no call site can forget an
        /// option (the cause of).
        /// </summary>
        public static Task<List<RedactionSpanEntity>> RedactFileAsync(
            string inputPath,
            string outputPath,
            PhileasPolicy policy,
            string context,
            SettingsEntity settings,
            FilterService? filterService = null,
            bool highlight = false,
            IReadOnlyCollection<int>? fullyRedactedColumns = null,
            string? worksheet = null) =>
            RedactFileAsync(
                inputPath, outputPath, policy, context, filterService, highlight, fullyRedactedColumns,
                worksheet: worksheet,
                wordScrub: DocumentMetadata.OptionsFor(settings),
                ocrScannedPdfs: settings.OcrScannedPdfs,
                ocrTextCoverage: settings.OcrTextCoverageThreshold,
                ocrImageCoverage: settings.OcrImageCoverageThreshold,
                ocrMaxPages: settings.OcrMaxPages,
                scrubEmailHeaders: settings.ScrubEmailHeaders,
                removeCommonEmailHeaders: settings.RemoveCommonEmailHeaders,
                removeEmailDateHeader: settings.RemoveEmailDateHeader,
                removeEmailAttachments: settings.RemoveEmailAttachments,
                redactOfficeHeadersFooters: settings.RedactOfficeHeadersFooters,
                redactOfficeCharts: settings.RedactOfficeCharts);

        /// <summary>
        /// Redacts <paramref name="inputPath"/> to <paramref name="outputPath"/> using
        /// <paramref name="policy"/>, dispatching by file type, and returns the spans it applied
        /// (so they can be stored and later edited / re-applied).
        /// </summary>
        public static async Task<List<RedactionSpanEntity>> RedactFileAsync(
            string inputPath,
            string outputPath,
            PhileasPolicy policy,
            string context,
            FilterService? filterService = null,
            bool highlight = false,
            IReadOnlyCollection<int>? fullyRedactedColumns = null,
            string? worksheet = null,
            WordScrubOptions wordScrub = WordScrubOptions.None,
            bool ocrScannedPdfs = false,
            double ocrTextCoverage = 0.01,
            double ocrImageCoverage = 0.5,
            int ocrMaxPages = 0,
            bool scrubEmailHeaders = false,
            bool removeCommonEmailHeaders = false,
            bool removeEmailDateHeader = false,
            bool removeEmailAttachments = false,
            bool redactOfficeHeadersFooters = true,
            bool redactOfficeCharts = true)
        {
            filterService ??= new FilterService();

            // Inject the bundled on-device PhEye model path into any PhEye filters the policy
            // enables (or strip them if no model is bundled), so name detection runs locally.
            PhEyeModel.Prepare(policy);

            string extension = Path.GetExtension(inputPath).ToLowerInvariant();

            // Word/Excel files that are password-protected or corrupt fail deep inside the Open XML SDK
            // with an opaque "corrupt data" error. Detect those up front and surface a clear message.
            if (extension == ".docx" || extension == ".xlsx")
            {
                string app = extension == ".docx" ? "Word" : "Excel";
                switch (OfficeDocument.Inspect(inputPath))
                {
                    case OfficeFileState.PasswordProtected:
                        throw new DocumentLoadException(
                            $"\"{Path.GetFileName(inputPath)}\" is password-protected, so it can't be redacted. " +
                            $"Open it in {app}, remove the password (File → Info → Protect Document), save it, and try again.");
                    case OfficeFileState.NotReadable:
                        throw new DocumentLoadException(
                            $"\"{Path.GetFileName(inputPath)}\" could not be opened. It may be corrupted, or it may not be a real {app} file.");
                }
            }

            if (extension == ".docx")
            {
                // Word redaction is synchronous; run it off the calling thread. highlight applies
                // only to Word documents. Returns the paragraph-indexed spans it applied.
                List<RedactionSpanEntity> docxSpans = await Task.Run(() => WordDocumentRedactor.Redact(
                    inputPath,
                    outputPath,
                    text => filterService.Filter(policy, context, 0, text),
                    highlight,
                    redactOfficeHeadersFooters,
                    redactOfficeCharts));
                if (wordScrub != WordScrubOptions.None)
                {
                    await Task.Run(() => DocumentMetadata.ScrubDocx(outputPath, wordScrub));
                }
                return docxSpans;
            }

            if (extension == ".eml" || extension == ".msg")
            {
                // Email (.eml round-trips; .msg is read and written back as .eml). Field-indexed spans.
                return await Task.Run(() => EmailRedactor.Redact(
                    inputPath,
                    outputPath,
                    text => filterService.Filter(policy, context, 0, text),
                    scrubEmailHeaders,
                    removeCommonEmailHeaders,
                    removeEmailDateHeader,
                    removeEmailAttachments));
            }

            if (extension == ".rtf")
            {
                // RTF is redacted via the RichTextBox engine (preserves control words/formatting).
                return await Task.Run(() => RtfRedactor.Redact(
                    inputPath,
                    outputPath,
                    text => filterService.Filter(policy, context, 0, text)));
            }

            if (extension == ".xlsx")
            {
                // Spreadsheet: redact per cell; optionally fully redact selected columns.
                List<RedactionSpanEntity> xlsxSpans = await Task.Run(() => XlsxRedactor.Redact(
                    inputPath,
                    outputPath,
                    text => filterService.Filter(policy, context, 0, text),
                    fullyRedactedColumns,
                    worksheet,
                    redactOfficeHeadersFooters,
                    redactOfficeCharts));
                // Strip identifying document properties so the redacted spreadsheet doesn't leak them
                // (same "Remove document metadata" setting that governs Word).
                if (wordScrub.HasFlag(WordScrubOptions.Metadata))
                {
                    await Task.Run(() => DocumentMetadata.ScrubXlsx(outputPath));
                }
                return xlsxSpans;
            }

            if (extension == ".csv")
            {
                // CSV: redact per field; optionally fully redact selected columns.
                return await Task.Run(() => CsvRedactor.Redact(
                    inputPath,
                    outputPath,
                    text => filterService.Filter(policy, context, 0, text),
                    fullyRedactedColumns));
            }

            if (extension == ".pdf")
            {
                byte[] input = await File.ReadAllBytesAsync(inputPath);
                (byte[] document, List<RedactionSpanEntity> pdfSpans) = await RedactPdfBytesAsync(
                    input, policy, context, filterService, ocrScannedPdfs, ocrTextCoverage, ocrImageCoverage, ocrMaxPages);
                await SafeOutput.WriteAsync(outputPath, document);
                return pdfSpans;
            }

            // Plain text. Run the filter off the calling thread too (consistent with the other
            // formats), so a large .txt — especially with on-device name detection — never blocks the
            // UI thread that drives the queue/preview.
            Encoding textEncoding = TextEncodingDetector.Detect(inputPath); // preserve the source encoding/BOM

            // A very large .txt is streamed in overlapping windows so neither the whole input nor the whole
            // filtered output is ever resident (avoids a memory spike / OutOfMemoryException). Normal-sized
            // files take the simpler whole-string path, which is byte-for-byte identical.
            if (new FileInfo(inputPath).Length > TextStreamRedactor.StreamAboveBytes)
            {
                List<Span> streamed = await Task.Run(() =>
                    TextStreamRedactor.Redact(inputPath, outputPath,
                        t => filterService.Filter(policy, context, 0, t), textEncoding));
                return MapTextSpans(streamed);
            }

            string text = await File.ReadAllTextAsync(inputPath);
            TextFilterResult textResult = await Task.Run(() => filterService.Filter(policy, context, 0, text));
            await SafeOutput.WriteTextAsync(outputPath, textResult.FilteredText, textEncoding);
            return MapTextSpans(textResult.Spans);
        }

        /// <summary>
        /// Re-applies an explicit (edited) span set to the original source, writing a new redacted
        /// document at <paramref name="outputPath"/>. Used by the Modify Redaction feature.
        /// </summary>
        public static async Task ApplySpansAsync(
            string sourcePath,
            string outputPath,
            string fileType,
            bool highlight,
            IReadOnlyList<RedactionSpanEntity> spans,
            PhileasPolicy? policy = null,
            FilterService? filterService = null,
            WordScrubOptions wordScrub = WordScrubOptions.None,
            bool scrubEmailHeaders = false,
            bool removeCommonEmailHeaders = false,
            bool removeEmailDateHeader = false,
            bool removeEmailAttachments = false,
            bool redactOfficeHeadersFooters = true,
            bool redactOfficeCharts = true,
            string? worksheet = null)
        {
            filterService ??= new FilterService();
            switch (fileType.ToLowerInvariant())
            {
                case ".docx":
                    Func<string, TextFilterResult>? docxDrawingFilter = policy is null
                        ? null
                        : text => filterService.Filter(policy, string.Empty, 0, text);
                    await Task.Run(() => WordDocumentRedactor.ApplySpans(sourcePath, outputPath, spans, highlight, docxDrawingFilter, redactOfficeCharts));
                    if (wordScrub != WordScrubOptions.None)
                    {
                        await Task.Run(() => DocumentMetadata.ScrubDocx(outputPath, wordScrub));
                    }
                    break;
                case ".pdf":
                    await ApplyPdfSpansAsync(sourcePath, outputPath, spans, policy, filterService);
                    break;
                case ".eml":
                case ".msg":
                    await Task.Run(() => EmailRedactor.ApplySpans(sourcePath, outputPath, spans, scrubEmailHeaders, removeCommonEmailHeaders, removeEmailDateHeader, removeEmailAttachments));
                    break;
                case ".rtf":
                    await Task.Run(() => RtfRedactor.ApplySpans(sourcePath, outputPath, spans));
                    break;
                case ".xlsx":
                    // Comments and (optionally) header/footer text aren't stored by position, so re-redact
                    // them via the policy filter as Modify re-renders the output.
                    Func<string, TextFilterResult>? xlsxFilter = policy is null
                        ? null
                        : text => filterService.Filter(policy, string.Empty, 0, text);
                    await Task.Run(() => XlsxRedactor.ApplySpans(sourcePath, outputPath, spans, worksheet, xlsxFilter, redactOfficeHeadersFooters, redactOfficeCharts));
                    if (wordScrub.HasFlag(WordScrubOptions.Metadata))
                    {
                        await Task.Run(() => DocumentMetadata.ScrubXlsx(outputPath));
                    }
                    break;
                case ".csv":
                    await Task.Run(() => CsvRedactor.ApplySpans(sourcePath, outputPath, spans));
                    break;
                default:
                    await ApplyTextSpansAsync(sourcePath, outputPath, spans);
                    break;
            }
        }

        // --- Capture mapping --------------------------------------------------

        private static List<RedactionSpanEntity> MapTextSpans(IList<Span> spans)
        {
            var list = new List<RedactionSpanEntity>();
            int order = 0;
            foreach (Span s in spans.OrderBy(s => s.CharacterStart))
            {
                var entity = new RedactionSpanEntity
                {
                    Order = order++,
                    ParagraphIndex = -1,
                    CharacterStart = s.CharacterStart,
                    CharacterEnd = s.CharacterEnd,
                    Text = s.Text ?? string.Empty,
                    Replacement = s.Replacement ?? string.Empty,
                    Classification = s.Classification ?? string.Empty
                };
                SpanExplanation.Populate(entity, s);
                list.Add(entity);
            }
            return list;
        }

        private static List<RedactionSpanEntity> MapPdfSpans(IList<Span> spans)
        {
            var list = new List<RedactionSpanEntity>();
            int order = 0;
            foreach (Span s in spans)
            {
                var entity = new RedactionSpanEntity
                {
                    Order = order++,
                    ParagraphIndex = -1,
                    Text = s.Text ?? string.Empty,
                    Replacement = s.Replacement ?? string.Empty,
                    Classification = s.Classification ?? string.Empty,
                    PageNumber = s.PageNumber,
                    LowerLeftX = s.LowerLeftX,
                    LowerLeftY = s.LowerLeftY,
                    UpperRightX = s.UpperRightX,
                    UpperRightY = s.UpperRightY
                };
                SpanExplanation.Populate(entity, s);
                list.Add(entity);
            }
            return list;
        }

        // --- Re-apply ---------------------------------------------------------

        private static async Task ApplyTextSpansAsync(string sourcePath, string outputPath, IReadOnlyList<RedactionSpanEntity> spans)
        {
            Encoding textEncoding = TextEncodingDetector.Detect(sourcePath); // preserve the source encoding/BOM
            string text = await File.ReadAllTextAsync(sourcePath);

            // Spans are applied by character position (start/stop), regardless of how they originated.
            var ranges = new List<ReplacementRange>();
            foreach (RedactionSpanEntity s in spans)
            {
                if (s.CharacterStart >= 0 && s.CharacterEnd <= text.Length && s.CharacterEnd > s.CharacterStart)
                {
                    string repl = string.IsNullOrEmpty(s.Replacement) ? DefaultReplacement : s.Replacement;
                    ranges.Add(new ReplacementRange(s.CharacterStart, s.CharacterEnd, repl));
                }
            }

            var sb = new StringBuilder(text);
            foreach (ReplacementRange r in RedactionSpanMath.ResolveNonOverlapping(ranges).OrderByDescending(r => r.Start))
            {
                sb.Remove(r.Start, r.End - r.Start);
                sb.Insert(r.Start, r.Replacement ?? string.Empty);
            }
            await SafeOutput.WriteTextAsync(outputPath, sb.ToString(), textEncoding);
        }

        private static async Task ApplyPdfSpansAsync(
            string sourcePath, string outputPath, IReadOnlyList<RedactionSpanEntity> spans,
            PhileasPolicy? policy, FilterService filterService)
        {
            byte[] bytes = await File.ReadAllBytesAsync(sourcePath);
            byte[] outBytes = await ApplyPdfSpansToBytesAsync(bytes, spans, policy, filterService);
            await SafeOutput.WriteAsync(outputPath, outBytes);
        }

        /// <summary>
        /// Redacts a PDF entirely in memory (bytes in, bytes out) and returns the detected spans.
        /// Used by the PDF preview so no redacted PII is ever written to disk until the user saves.
        /// </summary>
        public static async Task<(byte[] Document, List<RedactionSpanEntity> Spans)> RedactPdfBytesAsync(
            byte[] input, PhileasPolicy policy, string context, FilterService? filterService = null,
            bool ocrScannedPdfs = false, double ocrTextCoverage = 0.01, double ocrImageCoverage = 0.5,
            int ocrMaxPages = 0)
        {
            filterService ??= new FilterService();
            PhEyeModel.Prepare(policy);
            PreparePdfPolicy(policy, input);

            // When the OCR setting is on, use a hybrid extractor that OCRs scanned (text-layer-less)
            // pages so their PII is detected; otherwise the default text-layer extractor is used.
            Phileas.Services.Pdf.ITextExtractor? pdfExtractor = null;
            if (ocrScannedPdfs)
            {
                var hybrid = new HybridTextExtractor(ocrTextCoverage, ocrImageCoverage);

                // Pre-flight the OCR page cap here (a cheap count, no OCR) and fail loud before doing any
                // work, rather than throwing from deep inside the extractor on a background thread. The
                // throw happens on this async method's own context, so the caller's catch handles it
                // cleanly (and the debugger doesn't flag it as user-unhandled across a Task boundary).
                if (ocrMaxPages > 0)
                {
                    int pagesNeedingOcr = await Task.Run(() => hybrid.CountPagesNeedingOcr(input));
                    if (pagesNeedingOcr > ocrMaxPages)
                    {
                        throw new OcrPageLimitExceededException(pagesNeedingOcr, ocrMaxPages);
                    }
                }

                pdfExtractor = hybrid;
            }

            BinaryDocumentFilterResult result = await Task.Run(() =>
                new PdfFilterService(filterService, pdfExtractor).Filter(policy, context, input, MimeType.ApplicationPdf));
            return (result.Document, MapPdfSpans(result.Spans));
        }

        /// <summary>
        /// Re-applies a set of position-based PDF spans to the original bytes in memory (bytes in,
        /// bytes out). Used by the PDF preview when the reviewer edits/adds/removes redactions.
        /// </summary>
        public static async Task<byte[]> ApplyPdfSpansToBytesAsync(
            byte[] input, IReadOnlyList<RedactionSpanEntity> spans,
            PhileasPolicy? policy = null, FilterService? filterService = null)
        {
            filterService ??= new FilterService();

            // PDF spans are applied by page + bounding box (position), regardless of origin.
            var spanList = new List<Span>();
            foreach (RedactionSpanEntity s in spans.Where(s => s.PageNumber > 0))
            {
                spanList.Add(new Span
                {
                    PageNumber = s.PageNumber,
                    LowerLeftX = s.LowerLeftX,
                    LowerLeftY = s.LowerLeftY,
                    UpperRightX = s.UpperRightX,
                    UpperRightY = s.UpperRightY,
                    Text = s.Text,
                    Replacement = string.IsNullOrEmpty(s.Replacement) ? DefaultReplacement : s.Replacement,
                    Classification = s.Classification,
                    Applied = true
                });
            }

            PhileasPolicy applyPolicy = policy ?? new PhileasPolicy { Name = "reapply" };
            PreparePdfPolicy(applyPolicy, input);

            return await Task.Run(() =>
                new PdfFilterService(filterService).Apply(applyPolicy, input, spanList, MimeType.ApplicationPdf));
        }

        // The engine renders the output PDF at 25% scale by default (forcing ~400% zoom to read);
        // render at full size and a higher DPI so the result is legible at 100%.
        private static void PreparePdfPolicy(PhileasPolicy policy, byte[] input)
        {
            policy.Config.Pdf.Scale = 1.0f;
            policy.Config.Pdf.Dpi = Math.Max(policy.Config.Pdf.Dpi, 200);

            // A bounding box with page < 1 means "all pages". The engine matches an exact page, so expand
            // it into one box per page using the document's real page count.
            if (policy.Graphical.BoundingBoxes.Any(b => b.Page < 1))
            {
                ExpandAllPagesBoundingBoxes(policy, ReadPageCount(input));
            }
        }

        private static int ReadPageCount(byte[] input)
        {
            try
            {
                using UglyToad.PdfPig.PdfDocument doc = UglyToad.PdfPig.PdfDocument.Open(input);
                return doc.NumberOfPages;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Replaces each "all pages" bounding box (page &lt; 1) with one box per page of a
        /// <paramref name="pageCount"/>-page document; boxes pinned to a specific page are kept as-is.
        /// </summary>
        internal static void ExpandAllPagesBoundingBoxes(PhileasPolicy policy, int pageCount)
        {
            var expanded = new List<Phileas.Policy.BoundingBox>();
            foreach (Phileas.Policy.BoundingBox b in policy.Graphical.BoundingBoxes)
            {
                if (b.Page >= 1)
                {
                    expanded.Add(b);
                    continue;
                }
                for (int page = 1; page <= pageCount; page++)
                {
                    expanded.Add(new Phileas.Policy.BoundingBox
                    {
                        Page = page, X = b.X, Y = b.Y, W = b.W, H = b.H, Color = b.Color, Enabled = b.Enabled
                    });
                }
            }
            policy.Graphical.BoundingBoxes = expanded;
        }
    }
}
