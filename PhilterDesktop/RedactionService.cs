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
using Phileas.Policy;
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
            $"{Path.GetFileNameWithoutExtension(sourcePath)}{NormalizeSuffix(suffix)}{Path.GetExtension(sourcePath)}";

        /// <summary>
        /// The file extensions Philter Desktop can redact (legacy binary Word .doc is not supported).
        /// This is the single source of truth used by the UI file pickers/drag-drop.
        /// </summary>
        public static readonly string[] SupportedExtensions = { ".txt", ".docx", ".pdf" };

        /// <summary>Returns true if the file's extension is a supported, redactable type.</summary>
        public static bool IsSupported(string path) =>
            SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Computes the redacted output path for an input file, honoring the output-location
        /// setting and appending the configured suffix (default "_redacted-draft") while preserving
        /// the extension.
        /// </summary>
        public static string GetOutputPath(string inputPath, SettingsEntity settings)
        {
            string directory = settings.OutputToOriginalLocation
                ? Path.GetDirectoryName(inputPath) ?? string.Empty
                : settings.CustomOutputFolder;

            return Path.Combine(directory, ApplySuffix(inputPath, settings.RedactedSuffix));
        }

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
            bool highlight = false)
        {
            filterService ??= new FilterService();

            // Inject the bundled on-device PhEye model path into any PhEye filters the policy
            // enables (or strip them if no model is bundled), so name detection runs locally.
            PhEyeModel.Prepare(policy);

            string extension = Path.GetExtension(inputPath).ToLowerInvariant();

            if (extension == ".docx")
            {
                // Word redaction is synchronous; run it off the calling thread. highlight applies
                // only to Word documents. Returns the paragraph-indexed spans it applied.
                return await Task.Run(() => WordDocumentRedactor.Redact(
                    inputPath,
                    outputPath,
                    text => filterService.Filter(policy, context, 0, text),
                    highlight));
            }

            if (extension == ".pdf")
            {
                PreparePdfPolicy(policy);

                byte[] input = await File.ReadAllBytesAsync(inputPath);
                BinaryDocumentFilterResult result = await Task.Run(() =>
                    new PdfFilterService(filterService).Filter(policy, context, input, MimeType.ApplicationPdf));
                await File.WriteAllBytesAsync(outputPath, result.Document);
                return MapPdfSpans(result.Spans);
            }

            string text = await File.ReadAllTextAsync(inputPath);
            TextFilterResult textResult = filterService.Filter(policy, context, 0, text);
            await File.WriteAllTextAsync(outputPath, textResult.FilteredText);
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
            FilterService? filterService = null)
        {
            filterService ??= new FilterService();
            switch (fileType.ToLowerInvariant())
            {
                case ".docx":
                    await Task.Run(() => WordDocumentRedactor.ApplySpans(sourcePath, outputPath, spans, highlight));
                    break;
                case ".pdf":
                    await ApplyPdfSpansAsync(sourcePath, outputPath, spans, policy, filterService);
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
                list.Add(new RedactionSpanEntity
                {
                    Order = order++,
                    ParagraphIndex = -1,
                    CharacterStart = s.CharacterStart,
                    CharacterEnd = s.CharacterEnd,
                    Text = s.Text ?? string.Empty,
                    Replacement = s.Replacement ?? string.Empty,
                    Classification = s.Classification ?? string.Empty
                });
            }
            return list;
        }

        private static List<RedactionSpanEntity> MapPdfSpans(IList<Span> spans)
        {
            var list = new List<RedactionSpanEntity>();
            int order = 0;
            foreach (Span s in spans)
            {
                list.Add(new RedactionSpanEntity
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
                });
            }
            return list;
        }

        // --- Re-apply ---------------------------------------------------------

        private static async Task ApplyTextSpansAsync(string sourcePath, string outputPath, IReadOnlyList<RedactionSpanEntity> spans)
        {
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
            await File.WriteAllTextAsync(outputPath, sb.ToString());
        }

        private static async Task ApplyPdfSpansAsync(
            string sourcePath, string outputPath, IReadOnlyList<RedactionSpanEntity> spans,
            PhileasPolicy? policy, FilterService filterService)
        {
            byte[] bytes = await File.ReadAllBytesAsync(sourcePath);

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
            PreparePdfPolicy(applyPolicy);

            byte[] outBytes = await Task.Run(() =>
                new PdfFilterService(filterService).Apply(applyPolicy, bytes, spanList, MimeType.ApplicationPdf));
            await File.WriteAllBytesAsync(outputPath, outBytes);
        }

        // The engine renders the output PDF at 25% scale by default (forcing ~400% zoom to read);
        // render at full size and a higher DPI so the result is legible at 100%.
        private static void PreparePdfPolicy(PhileasPolicy policy)
        {
            policy.Config.Pdf.Scale = 1.0f;
            policy.Config.Pdf.Dpi = Math.Max(policy.Config.Pdf.Dpi, 200);
        }
    }
}
