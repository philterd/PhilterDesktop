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
using PhilterData;
using UglyToad.PdfPig;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Covers the in-memory PDF redaction path used by the preview (bytes in / bytes out, no temp
    /// file). Proves the redacted bytes carry no recoverable PII and that position-based spans can be
    /// re-burned in memory.
    /// </summary>
    public class PdfInMemoryRedactionTests
    {
        private const string Email = "george@fake.com";
        private const string Ssn = "123-45-6789";

        private static byte[] SamplePdfBytes =>
            File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "test-documents", "test1.pdf"));

        private static string ExtractText(byte[] pdf)
        {
            var sb = new StringBuilder();
            using PdfDocument doc = PdfDocument.Open(pdf);
            foreach (var page in doc.GetPages())
            {
                sb.AppendLine(page.Text);
            }
            return sb.ToString();
        }

        [Fact]
        public async Task RedactPdfBytesAsync_ReturnsBytesWithNoExtractablePii()
        {
            var policy = PolicySerializer.DeserializeFromJson(
                "{\"identifiers\":{\"ssn\":{},\"emailAddress\":{}}}");

            (byte[] document, List<RedactionSpanEntity> spans) =
                await RedactionService.RedactPdfBytesAsync(SamplePdfBytes, policy, "ctx", new FilterService());

            Assert.NotEmpty(document);
            Assert.NotEmpty(spans);

            string extracted = ExtractText(document);
            Assert.DoesNotContain(Ssn, extracted);
            Assert.DoesNotContain(Email, extracted);
        }

        [Fact]
        public async Task ApplyPdfSpansToBytesAsync_ReburnsDetectedSpansInMemory()
        {
            var policy = PolicySerializer.DeserializeFromJson(
                "{\"identifiers\":{\"ssn\":{},\"emailAddress\":{}}}");

            (byte[] _, List<RedactionSpanEntity> spans) =
                await RedactionService.RedactPdfBytesAsync(SamplePdfBytes, policy, "ctx", new FilterService());

            // Re-apply the detected (position-based) spans to the original bytes, as the preview does
            // when the reviewer edits the redaction list.
            byte[] reapplied = await RedactionService.ApplyPdfSpansToBytesAsync(SamplePdfBytes, spans);

            Assert.NotEmpty(reapplied);
            string extracted = ExtractText(reapplied);
            Assert.DoesNotContain(Ssn, extracted);
            Assert.DoesNotContain(Email, extracted);
        }
    }
}
