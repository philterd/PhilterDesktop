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
using Windows.Media.Ocr;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The <see cref="SettingsEntity"/> overload of <c>RedactFileAsync</c> is the single path every
    /// entry point (queue, watched folders, CLI, Find &amp; Redact) uses, so OCR can't be silently
    /// skipped on one of them. These prove the overload actually honors
    /// <see cref="SettingsEntity.OcrScannedPdfs"/> rather than ignoring it.
    /// </summary>
    public sealed class RedactFileSettingsOverloadTests
    {
        private static readonly PhileasPolicy SsnPolicy = new()
        {
            Name = "ssn",
            Identifiers = new Phileas.Policy.Identifiers { Ssn = new Phileas.Policy.Filters.Ssn() }
        };

        private static string ScannedPdf =>
            Path.Combine(AppContext.BaseDirectory, "test-documents", "scanned-medical-record.pdf");

        [SkippableFact]
        public async Task SettingsOverload_WithOcrEnabled_RedactsScannedPdf()
        {
            Skip.If(OcrEngine.TryCreateFromUserProfileLanguages() is null, "No OCR language is available on this machine.");
            Skip.IfNot(File.Exists(ScannedPdf), "scanned-medical-record.pdf not found in test-documents.");

            string output = Path.Combine(Path.GetTempPath(), "ocr-on-" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                var settings = new SettingsEntity { OcrScannedPdfs = true };
                var spans = await RedactionService.RedactFileAsync(
                    ScannedPdf, output, SsnPolicy, "ctx", settings, new FilterService());

                Assert.NotEmpty(spans); // OCR ran, so the SSN in the scanned image was found
            }
            finally
            {
                try { File.Delete(output); } catch { /* best effort */ }
            }
        }

        [SkippableFact]
        public async Task SettingsOverload_WithOcrDisabled_LeavesScannedPdfUnredacted()
        {
            Skip.IfNot(File.Exists(ScannedPdf), "scanned-medical-record.pdf not found in test-documents.");

            string output = Path.Combine(Path.GetTempPath(), "ocr-off-" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                var settings = new SettingsEntity { OcrScannedPdfs = false };
                var spans = await RedactionService.RedactFileAsync(
                    ScannedPdf, output, SsnPolicy, "ctx", settings, new FilterService());

                // With OCR off there is no extractable text layer, so nothing is detected — this is the
                // difference the flag controls, confirming the overload forwards it (not hardcoded).
                Assert.Empty(spans);
            }
            finally
            {
                try { File.Delete(output); } catch { /* best effort */ }
            }
        }
    }
}
