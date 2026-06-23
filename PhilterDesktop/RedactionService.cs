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

using Phileas.Services;
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
            FilterService? filterService = null)
        {
            filterService ??= new FilterService();
            string extension = Path.GetExtension(inputPath).ToLowerInvariant();

            if (extension == ".docx")
            {
                // Word redaction is synchronous; run it off the calling thread.
                await Task.Run(() => WordDocumentRedactor.Redact(
                    inputPath,
                    outputPath,
                    text => filterService.Filter(policy, context, 0, text).FilteredText));
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
