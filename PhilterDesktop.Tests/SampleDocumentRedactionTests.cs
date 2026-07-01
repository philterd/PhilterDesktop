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

using Phileas.Policy;
using Phileas.Policy.Filters;
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Integration tests that redact the sample documents in <c>test-documents/</c> end-to-end
    /// through <see cref="RedactionService"/> and compare the redacted output to the expected
    /// result. The .txt and .docx samples contain identical text, so they must redact identically.
    /// </summary>
    public sealed class SampleDocumentRedactionTests : IDisposable
    {
        // The samples contain: "...an email george@fake.com and SSN 123-45-6789."
        private const string Email = "george@fake.com";
        private const string Ssn = "123-45-6789";
        private const string ExpectedRedacted =
            "This is a sample document with an email {{{REDACTED-email-address}}} and SSN {{{REDACTED-ssn}}}.";

        private static readonly string SamplesDir = Path.Combine(AppContext.BaseDirectory, "test-documents");

        private readonly string _tempDir;

        public SampleDocumentRedactionTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "philter-sample-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        }

        private static readonly string[] Names = { "John", "Smith", "Mary", "Johnson" };

        private static PhileasPolicy SamplePolicy() => new()
        {
            Name = "sample",
            Identifiers = new Identifiers { EmailAddress = new EmailAddress(), Ssn = new Ssn() }
        };

        private static PhileasPolicy NamePolicy() => new()
        {
            Name = "names",
            Identifiers = new Identifiers { FirstName = new FirstName(), Surname = new Surname() }
        };

        [Fact]
        public async Task TextSample2_RedactsPersonNames()
        {
            string input = Path.Combine(SamplesDir, "test2.txt");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "test2_redacted.txt");

            await RedactionService.RedactFileAsync(input, output, NamePolicy(), "ctx");

            string result = await File.ReadAllTextAsync(output);
            foreach (string name in Names)
            {
                Assert.DoesNotContain(name, result);
            }
            Assert.Contains("Patient", result);
            Assert.Contains("REDACTED", result);
        }

        [Fact]
        public async Task WordSample2_RedactsPersonNames()
        {
            string input = Path.Combine(SamplesDir, "test2.docx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "test2_redacted.docx");

            await RedactionService.RedactFileAsync(input, output, NamePolicy(), "ctx");

            string result = WordDocs.AllBodyText(output);
            foreach (string name in Names)
            {
                Assert.DoesNotContain(name, result);
            }
            Assert.Contains("Patient", result);
        }

        [Fact]
        public async Task TextSample_RedactsToExpectedResult()
        {
            string input = Path.Combine(SamplesDir, "test1.txt");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "test1_redacted.txt");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            string result = File.ReadAllText(output).Trim();
            Assert.Equal(ExpectedRedacted, result);
            Assert.DoesNotContain(Email, result);
            Assert.DoesNotContain(Ssn, result);
        }

        [Fact]
        public async Task WordSample_RedactsToExpectedResult()
        {
            string input = Path.Combine(SamplesDir, "test1.docx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "test1_redacted.docx");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            string result = WordDocs.AllBodyText(output);
            Assert.Equal(ExpectedRedacted, result);
            Assert.DoesNotContain(Email, result);
            Assert.DoesNotContain(Ssn, result);

            // The original sample must be untouched.
            Assert.Contains(Ssn, WordDocs.AllBodyText(input));
        }

        [Fact]
        public async Task TextAndWordSamples_RedactToSameText()
        {
            string txtOut = Path.Combine(_tempDir, "t.txt");
            string docxOut = Path.Combine(_tempDir, "t.docx");
            await RedactionService.RedactFileAsync(Path.Combine(SamplesDir, "test1.txt"), txtOut, SamplePolicy(), "ctx");
            await RedactionService.RedactFileAsync(Path.Combine(SamplesDir, "test1.docx"), docxOut, SamplePolicy(), "ctx");

            Assert.Equal(File.ReadAllText(txtOut).Trim(), WordDocs.AllBodyText(docxOut));
        }

        [Fact]
        public async Task RtfWithHeaderFooter_DropsNonBodyContent_AndFlagsIt()
        {
            string input = Path.Combine(SamplesDir, "header-footer.rtf");
            Assert.True(File.Exists(input), $"Sample not found: {input}");

            // The source genuinely carries header/footer content, and it's flagged as a fidelity concern.
            Assert.True(RtfFidelity.HasDroppedContent(input));
            string sourceRaw = File.ReadAllText(input);
            Assert.Contains("records@brannon-legal.com", sourceRaw); // header email present in the source
            Assert.Contains("Jane Doe", sourceRaw);                  // footer content present in the source

            string output = Path.Combine(_tempDir, "header-footer_redacted.rtf");
            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            // The body is redacted and re-serialized; its non-PII text survives.
            string body = RtfRedactor.ReadText(output);
            Assert.Contains("Client Matter Summary", body);
            Assert.DoesNotContain(Email, body);
            Assert.DoesNotContain(Ssn, body);

            // The header and footer — and their PII — are not carried into the redacted RTF at all
            // (RTF redaction works on the body only; this is the #541 drop the app warns about).
            string outputRaw = File.ReadAllText(output);
            Assert.DoesNotContain("records@brannon-legal.com", outputRaw); // header email gone
            Assert.DoesNotContain("Brannon & Associates", outputRaw);      // header text gone
            Assert.DoesNotContain("Jane Doe", outputRaw);                  // footer content gone
            Assert.DoesNotContain("Case No. 2025-CV-01234", outputRaw);    // footer content gone
            Assert.DoesNotContain("\\header", outputRaw);                  // no header destination emitted
            Assert.DoesNotContain("\\footer", outputRaw);                  // no footer destination emitted

            // The original sample is untouched — header/footer still there for anyone else.
            Assert.Contains("records@brannon-legal.com", File.ReadAllText(input));
        }

        [Fact]
        public async Task EmailSample_EncodedHtml_RedactsEntityAndTagSplitAddresses()
        {
            // This .eml's HTML body hides addresses as an HTML entity (john&#64;…), split by a tag
            // (jane<span>@</span>…), and in a mailto: href — the exact forms that used to survive in the
            // rendered HTML while the plain-text path was clean (#540).
            string input = Path.Combine(SamplesDir, "email-encoded-html.eml");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "email-encoded-html_redacted.eml");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            using var stream = File.OpenRead(output);
            string html = MimeKit.MimeMessage.Load(stream).HtmlBody ?? string.Empty;

            Assert.DoesNotContain("john@example.com", html);     // entity-encoded, decoded form
            Assert.DoesNotContain("john&#64;example.com", html); // entity-encoded, raw form
            Assert.DoesNotContain("@example.com", html);         // covers the tag-split and href addresses
            Assert.DoesNotContain("secret@example.com", html);
            Assert.Contains("open a ticket", html);              // non-PII link text survives
        }

        [Fact]
        public void DocxWithChartSample_ReviewLines_IncludeDrawingText()
        {
            // #562: the .docx View Diff builds from ReadReviewLines, which must include shape/chart text.
            string input = Path.Combine(SamplesDir, "docx-with-chart.docx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");

            List<string> review = WordDocumentRedactor.ReadReviewLines(input);
            Assert.Contains(review, l => l.Contains("chart@example.com"));                          // chart label present
            Assert.DoesNotContain(WordDocumentRedactor.ReadParagraphs(input), l => l.Contains("chart@example.com")); // not in body-only read
        }

        [Fact]
        public async Task DocxWithChartSample_RedactionRecordsChartAndBodySpans()
        {
            // #561: shape/chart redactions are captured as spans (so the report/explanation count them).
            string input = Path.Combine(SamplesDir, "docx-with-chart.docx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "docx-with-chart_redacted.docx");

            List<RedactionSpanEntity> spans = await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            Assert.Contains(spans, s => s.Text == "chart@example.com"); // drawing redaction recorded
            Assert.Contains(spans, s => s.Text == "body@example.com");  // body redaction recorded
            Assert.False(WordDocs.AnyPartContains(output, "@example.com"), "no email may survive in any part");
        }

        [Fact]
        public async Task RtfWithCommentSample_CommentRemoved_NotFlattenedIntoBody()
        {
            // #542: an RTF comment must not be glued into the visible body.
            string input = Path.Combine(SamplesDir, "rtf-with-comment.rtf");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "rtf-with-comment_redacted.rtf");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            string body = RtfRedactor.ReadText(output);
            Assert.Contains("The signed contract", body);
            Assert.Contains("Please proceed", body);
            Assert.DoesNotContain("Reviewer note", body);                           // comment text not merged in
            Assert.DoesNotContain("555-12-3456", body);                             // comment PII gone
            Assert.DoesNotContain("for review.Reviewer", body);                     // no "glued comment" corruption
        }

        [Fact]
        public async Task MsgWithRtfOnlyBodySample_IsRecoveredAndRedacted()
        {
            // #523: an Outlook .msg whose only body is RTF is recovered as text and redacted (not dropped).
            string input = Path.Combine(SamplesDir, "rtf-only-body.msg");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "rtf-only-body.eml"); // .msg redacts to .eml

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            using var stream = File.OpenRead(output);
            string body = MimeKit.MimeMessage.Load(stream).TextBody ?? string.Empty;
            Assert.Contains("Quarterly figures", body);        // the RTF body was recovered, not dropped
            Assert.DoesNotContain("secret@example.com", body); // and its PII is redacted
        }

        [Fact]
        public async Task PdfSample_RedactsToImageBasedPdf()
        {
            string input = Path.Combine(SamplesDir, "test1.pdf");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "test1_redacted.pdf");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            byte[] bytes = await File.ReadAllBytesAsync(output);
            Assert.True(bytes.Length > 0);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));

            // Pages are rasterized, so the original PII has no recoverable text layer.
            string asBytes = System.Text.Encoding.Latin1.GetString(bytes);
            Assert.DoesNotContain(Email, asBytes);
            Assert.DoesNotContain(Ssn, asBytes);
        }
    }
}
