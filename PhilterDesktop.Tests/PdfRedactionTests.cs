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
using Phileas.Policy;
using Phileas.Services;
using PhilterDesktop;
using UglyToad.PdfPig;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Proves the documented PDF guarantee: redacting a PDF doesn't just paint over the text, it
    /// removes it — there is no extractable/recoverable text layer behind the redaction.
    /// </summary>
    public class PdfRedactionTests
    {
        // test1.pdf (copied to the test output) contains this PII (same as test1.txt).
        private const string Email = "george@fake.com";
        private const string Ssn = "123-45-6789";

        private static string SamplePdf =>
            Path.Combine(AppContext.BaseDirectory, "sample-documents", "test1.pdf");

        private static string ExtractText(string pdfPath)
        {
            var sb = new StringBuilder();
            using PdfDocument doc = PdfDocument.Open(pdfPath);
            foreach (var page in doc.GetPages())
            {
                sb.AppendLine(page.Text);
            }
            return sb.ToString();
        }

        [Fact]
        public void Sanity_OriginalPdf_ContainsThePii()
        {
            // Guards the test itself: the source really does have extractable PII to remove.
            Assert.True(File.Exists(SamplePdf), $"Sample PDF not found at {SamplePdf}");
            string text = ExtractText(SamplePdf);
            Assert.Contains(Ssn, text);
            Assert.Contains(Email, text);
        }

        [Fact]
        public async Task RedactedPdf_HasNoExtractablePiiTextLayer()
        {
            var policy = PolicySerializer.DeserializeFromJson(
                "{\"identifiers\":{\"ssn\":{},\"emailAddress\":{}}}");

            string output = Path.Combine(Path.GetTempPath(), "pdf-redact-" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                await RedactionService.RedactFileAsync(SamplePdf, output, policy, "ctx", new FilterService());

                Assert.True(File.Exists(output));

                // The whole point: extracting text from the redacted PDF must not reveal the PII.
                string extracted = ExtractText(output);
                Assert.DoesNotContain(Ssn, extracted);
                Assert.DoesNotContain(Email, extracted);
            }
            finally
            {
                try { File.Delete(output); } catch { /* best effort */ }
            }
        }
    }
}
