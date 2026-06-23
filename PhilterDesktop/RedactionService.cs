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
    /// <summary>
    /// Core (UI-independent) redaction logic: resolving output paths and redacting a
    /// single file by type. Extracted from MainForm so it can be unit/integration tested.
    /// </summary>
    internal static class RedactionService
    {
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
        /// setting and appending a "_redacted" suffix while preserving the extension.
        /// </summary>
        public static string GetOutputPath(string inputPath, SettingsEntity settings)
        {
            string directory = settings.OutputToOriginalLocation
                ? Path.GetDirectoryName(inputPath) ?? string.Empty
                : settings.CustomOutputFolder;

            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);
            string extension = Path.GetExtension(inputPath);

            return Path.Combine(directory, $"{fileNameWithoutExt}_redacted{extension}");
        }

        /// <summary>
        /// Redacts <paramref name="inputPath"/> to <paramref name="outputPath"/> using
        /// <paramref name="policy"/>, dispatching by file type: .docx via
        /// <see cref="WordDocumentRedactor"/>, everything else as plain text.
        /// </summary>
        public static async Task RedactFileAsync(
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
                // Word redaction is synchronous; run it off the calling thread.
                // highlight applies only to Word documents.
                await Task.Run(() => WordDocumentRedactor.Redact(
                    inputPath,
                    outputPath,
                    text => filterService.Filter(policy, context, 0, text),
                    highlight));
            }
            else if (extension == ".pdf")
            {
                // The engine renders the output PDF at 25% scale by default, which forces the
                // reader to zoom to ~400% to read it. Render each page at full size (and a higher
                // DPI) so the redacted PDF is legible at 100% without zooming.
                policy.Config.Pdf.Scale = 1.0f;
                policy.Config.Pdf.Dpi = Math.Max(policy.Config.Pdf.Dpi, 200);

                // PDF redaction rasterizes each page (the output has no recoverable text layer).
                // It's CPU-heavy and synchronous, so run it off the calling thread.
                byte[] input = await File.ReadAllBytesAsync(inputPath);
                BinaryDocumentFilterResult result = await Task.Run(() =>
                    new PdfFilterService(filterService).Filter(policy, context, input, MimeType.ApplicationPdf));
                await File.WriteAllBytesAsync(outputPath, result.Document);
            }
            else
            {
                string input = await File.ReadAllTextAsync(inputPath);
                var result = filterService.Filter(policy, context, 0, input);
                await File.WriteAllTextAsync(outputPath, result.FilteredText);
            }
        }
    }
}
