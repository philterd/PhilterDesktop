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

using Phileas.Model;
using Phileas.Services;
using Phileas.Services.Pdf;
using PhilterData;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop
{
    /// <summary>The outcome category of a verification pass, persisted on the queue item.</summary>
    internal enum VerificationStatus
    {
        NotRun,
        Clean,
        ResidualsFound,
        Error
    }

    /// <summary>The result of scanning a redacted output for residual detectable PII.</summary>
    internal sealed class VerificationOutcome
    {
        public VerificationStatus Status { get; init; }

        /// <summary>What the detector still found in the output (empty when clean).</summary>
        public IReadOnlyList<RedactionSpanEntity> Residuals { get; init; } = Array.Empty<RedactionSpanEntity>();

        /// <summary>A plain-language reason when <see cref="Status"/> is <see cref="VerificationStatus.Error"/>.</summary>
        public string? Error { get; init; }

        public int Count => Residuals.Count;
    }

    /// <summary>
    /// The post-redaction verification pass: re-reads a <b>written redacted output</b> and runs the
    /// detector over it to surface any residual PII the policy missed (the false-negative case that
    /// matters most to a redaction user). It reuses each format's existing detection path, so it reads
    /// the output exactly as the redactor would, and runs entirely on-device.
    ///
    /// UI-independent and testable: it returns findings; the caller decides how to surface and store them.
    /// </summary>
    internal static class RedactionVerifier
    {
        /// <summary>
        /// Scans <paramref name="outputPath"/> (the redacted file) with <paramref name="policy"/> and
        /// reports residual detections. The format is taken from the output extension, so an Outlook
        /// <c>.msg</c> redacted to <c>.eml</c> verifies as email.
        /// </summary>
        public static VerificationOutcome Verify(
            string outputPath, PhileasPolicy policy, string context, FilterService filterService)
        {
            if (string.IsNullOrEmpty(outputPath) || !File.Exists(outputPath))
            {
                return new VerificationOutcome { Status = VerificationStatus.Error, Error = "The redacted output file was not found." };
            }

            try
            {
                // Match the redaction detection setup (e.g. inject the on-device name-detection model).
                PhEyeModel.Prepare(policy);
                TextFilterResult Filter(string text) => filterService.Filter(policy, context, 0, text);

                string ext = Path.GetExtension(outputPath).ToLowerInvariant();
                List<RedactionSpanEntity> residuals = ext switch
                {
                    ".docx" => WordDocumentRedactor.Detect(outputPath, Filter),
                    ".eml" or ".msg" => EmailRedactor.Detect(outputPath, Filter),
                    ".xlsx" => XlsxRedactor.Detect(outputPath, Filter),
                    ".rtf" => MapTextSpans(Filter(RtfRedactor.ReadText(outputPath)).Spans),
                    ".pdf" => DetectPdf(outputPath, policy, context, filterService),
                    _ => MapTextSpans(Filter(File.ReadAllText(outputPath)).Spans) // .txt, .csv
                };

                return new VerificationOutcome
                {
                    Status = residuals.Count == 0 ? VerificationStatus.Clean : VerificationStatus.ResidualsFound,
                    Residuals = residuals
                };
            }
            catch (Exception ex)
            {
                return new VerificationOutcome { Status = VerificationStatus.Error, Error = ex.Message };
            }
        }

        // Runs the engine's PDF detection over the output bytes (it extracts the text layer internally),
        // collecting any residual detections. The redacted document the engine also produces is ignored.
        private static List<RedactionSpanEntity> DetectPdf(
            string outputPath, PhileasPolicy policy, string context, FilterService filterService)
        {
            byte[] bytes = File.ReadAllBytes(outputPath);
            BinaryDocumentFilterResult result = new PdfFilterService(filterService)
                .Filter(policy, context, bytes, MimeType.ApplicationPdf);
            return MapPdfSpans(result.Spans);
        }

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
    }
}
