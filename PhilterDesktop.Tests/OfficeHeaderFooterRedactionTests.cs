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
using Phileas.Services.Office;

using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Verifies the "Redact text in page headers and footers" option for Word (.docx) and Excel (.xlsx):
    /// PII in a running header/footer (e.g. "Confidential — John Doe" printed on every page) is redacted
    /// when the option is on, kept when off, and the body/cell redaction and paragraph-index stability are
    /// unaffected either way.
    /// </summary>
    public sealed class OfficeHeaderFooterRedactionTests : IDisposable
    {
        private readonly string _dir;
        private readonly FilterService _fs = new();

        public OfficeHeaderFooterRedactionTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-hf-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose() { try { Directory.Delete(_dir, true); } catch { /* best effort */ } }

        private static PhileasPolicy SsnPolicy() => new()
        {
            Name = "ssn",
            Identifiers = new Identifiers { Ssn = new Ssn() }
        };

        // --- .xlsx ------------------------------------------------------------

        [Fact]
        public async Task Xlsx_Enabled_RedactsHeaderFooterText_KeepsFieldCodes_AndCells()
        {
            string input = Path.Combine(_dir, "in.xlsx");
            SpreadsheetTestHelper.CreateXlsxWithHeaderFooter(
                input,
                new[]
                {
                    new string?[] { "Name", "Notes" },
                    new string?[] { "Alice", "SSN 123-45-6789" }
                },
                oddHeader: "&CConfidential SSN 111-22-3333",
                oddFooter: "&LPage &P");
            string output = Path.Combine(_dir, "out.xlsx");

            await RedactionService.RedactFileAsync(input, output, SsnPolicy(), "ctx");

            string headerFooter = SpreadsheetTestHelper.HeaderFooterText(output);
            Assert.DoesNotContain("111-22-3333", headerFooter); // header PII gone
            Assert.Contains("&C", headerFooter);                // section field code preserved
            Assert.Contains("&P", headerFooter);                // page-number field code preserved
            // Cell PII is still redacted.
            Assert.DoesNotContain("123-45-6789", SpreadsheetTestHelper.AllText(output));
        }

        [Fact]
        public async Task Xlsx_Disabled_KeepsHeaderFooterText_StillRedactsCells()
        {
            string input = Path.Combine(_dir, "in.xlsx");
            SpreadsheetTestHelper.CreateXlsxWithHeaderFooter(
                input,
                new[]
                {
                    new string?[] { "Name", "Notes" },
                    new string?[] { "Alice", "SSN 123-45-6789" }
                },
                oddHeader: "&CConfidential SSN 111-22-3333",
                oddFooter: "&LPrinted");
            string output = Path.Combine(_dir, "out.xlsx");

            await RedactionService.RedactFileAsync(input, output, SsnPolicy(), "ctx",
                redactOfficeHeadersFooters: false);

            Assert.Contains("111-22-3333", SpreadsheetTestHelper.HeaderFooterText(output)); // gated by the option
            Assert.DoesNotContain("123-45-6789", SpreadsheetTestHelper.AllText(output));    // cells still redacted
        }

        [Fact]
        public async Task Xlsx_Verification_FlagsResidualHeaderPii()
        {
            // Redact with header/footer off so header PII remains, then verify: the pass must scan the
            // header/footer and report the residual.
            string input = Path.Combine(_dir, "in.xlsx");
            SpreadsheetTestHelper.CreateXlsxWithHeaderFooter(
                input,
                new[] { new string?[] { "Name" }, new string?[] { "Alice" } },
                oddHeader: "&CConfidential SSN 111-22-3333",
                oddFooter: "&LPrinted");
            string output = Path.Combine(_dir, "out.xlsx");
            await RedactionService.RedactFileAsync(input, output, SsnPolicy(), "ctx",
                redactOfficeHeadersFooters: false);

            VerificationOutcome outcome = RedactionVerifier.Verify(output, SsnPolicy(), "ctx", _fs);

            Assert.Contains(outcome.Residuals, r => r.Text.Contains("111-22-3333"));
        }

        [Fact]
        public async Task Xlsx_HeaderFooterSpans_AreReportedAsHeaderFooterLocation()
        {
            string input = Path.Combine(_dir, "in.xlsx");
            SpreadsheetTestHelper.CreateXlsxWithHeaderFooter(
                input,
                new[] { new string?[] { "Name" }, new string?[] { "Alice" } },
                oddHeader: "&CConfidential SSN 111-22-3333",
                oddFooter: "&LPrinted");
            string output = Path.Combine(_dir, "out.xlsx");

            List<RedactionSpanEntity> spans = await RedactionService.RedactFileAsync(input, output, SsnPolicy(), "ctx");

            // The header span isn't a cell, so it's captured with ParagraphIndex -1.
            Assert.Contains(spans, s => s.ParagraphIndex == -1 && s.Text.Contains("111-22-3333"));
        }

        // --- .docx ------------------------------------------------------------

        [Fact]
        public async Task Docx_Enabled_RedactsHeaderAndFooterText()
        {
            string input = Path.Combine(_dir, "in.docx");
            WordDocs.CreateWithHeaderFooter(input,
                headerText: "Confidential 111-22-3333",
                footerText: "Reviewed 222-33-4444",
                "Body SSN 123-45-6789");
            string output = Path.Combine(_dir, "out.docx");

            await RedactionService.RedactFileAsync(input, output, SsnPolicy(), "ctx");

            Assert.DoesNotContain("111-22-3333", WordDocs.HeadersText(output));
            Assert.DoesNotContain("222-33-4444", WordDocs.FootersText(output));
            Assert.DoesNotContain("123-45-6789", WordDocs.AllBodyText(output));
        }

        [Fact]
        public async Task Docx_Disabled_KeepsHeaderAndFooter_StillRedactsBody()
        {
            string input = Path.Combine(_dir, "in.docx");
            WordDocs.CreateWithHeaderFooter(input,
                headerText: "Confidential 111-22-3333",
                footerText: "Reviewed 222-33-4444",
                "Body SSN 123-45-6789");
            string output = Path.Combine(_dir, "out.docx");

            await RedactionService.RedactFileAsync(input, output, SsnPolicy(), "ctx",
                redactOfficeHeadersFooters: false);

            Assert.Contains("111-22-3333", WordDocs.HeadersText(output)); // gated by the option
            Assert.Contains("222-33-4444", WordDocs.FootersText(output));
            Assert.DoesNotContain("123-45-6789", WordDocs.AllBodyText(output)); // body still redacted
        }

        [Fact]
        public void Docx_Disabled_BodyParagraphIndicesStayStable_ForReapply()
        {
            // With header/footer redaction off, the header paragraph is still enumerated (just not
            // filtered), so a body span's paragraph index is unchanged and re-applies to the right place.
            string input = Path.Combine(_dir, "in.docx");
            WordDocs.CreateWithHeaderFooter(input,
                headerText: "Header 111-22-3333",
                footerText: "Footer 222-33-4444",
                "Body SSN 123-45-6789");

            string redacted = Path.Combine(_dir, "redacted.docx");
            List<OfficeRedactionSpan> spans = WordDocumentRedactor.Redact(
                input, redacted, t => _fs.Filter(SsnPolicy(), "ctx", 0, t), highlight: false,
                redactHeadersFooters: false);

            // Only the body SSN produced a span (headers/footers skipped).
            Assert.Single(spans);
            Assert.DoesNotContain("123-45-6789", WordDocs.AllBodyText(redacted));

            // Re-applying the stored spans to the original reproduces the same body redaction, proving the
            // stored paragraph index still points at the body paragraph.
            string reapplied = Path.Combine(_dir, "reapplied.docx");
            WordDocumentRedactor.ApplySpans(input, reapplied, spans, highlight: false);
            Assert.DoesNotContain("123-45-6789", WordDocs.AllBodyText(reapplied));
            Assert.Contains("111-22-3333", WordDocs.HeadersText(reapplied)); // untouched, as expected
        }
    }
}
