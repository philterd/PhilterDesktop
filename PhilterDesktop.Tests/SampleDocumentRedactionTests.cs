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
using Phileas.Services;
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Integration tests that redact the sample documents in <c>sample-documents/</c> end-to-end
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

        private static readonly string SamplesDir = Path.Combine(AppContext.BaseDirectory, "sample-documents");

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
            // (RTF redaction works on the body only; this is the drop the app warns about).
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
            // rendered HTML while the plain-text path was clean.
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
            // The .docx View Diff builds from ReadReviewLines, which must include shape/chart text.
            string input = Path.Combine(SamplesDir, "docx-with-chart.docx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");

            List<string> review = WordDocumentRedactor.ReadReviewLines(input);
            Assert.Contains(review, l => l.Contains("chart@example.com"));                          // chart label present
            Assert.DoesNotContain(WordDocumentRedactor.ReadParagraphs(input), l => l.Contains("chart@example.com")); // not in body-only read
        }

        [Fact]
        public async Task DocxWithChartSample_RedactionRecordsChartAndBodySpans()
        {
            // Shape/chart redactions are captured as spans (so the report/explanation count them).
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
            // An RTF comment must not be glued into the visible body.
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
            // An Outlook .msg whose only body is RTF is recovered as text and redacted (not dropped).
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
        public async Task XlsxSample_MultiSheet_RedactsChosenWorksheetOnly()
        {
            // The workbook has an "Employees" sheet and a "Vendors" sheet; redaction targets one sheet.
            string input = Path.Combine(SamplesDir, "multi-sheet.xlsx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "multi-sheet_redacted.xlsx");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx", worksheet: "Employees");

            string all = SpreadsheetTestHelper.AllText(output);
            // The Employees sheet is redacted.
            Assert.DoesNotContain("jane.doe@example.com", all);
            Assert.DoesNotContain("john.smith@example.com", all);
            Assert.DoesNotContain("123-45-6789", all);
            Assert.DoesNotContain("234-56-7890", all);
            // The Vendors sheet is left untouched.
            Assert.Contains("bob@vendor.example", all);
            Assert.Contains("alice@vendor.example", all);
        }

        [Fact]
        public async Task WordSample_HeaderFooter_RedactsHeaderAndFooterText()
        {
            // header-footer.docx: header "…SSN 123-45-6789", footer "…george@fake.com", body "…078-05-1120".
            string input = Path.Combine(SamplesDir, "header-footer.docx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "header-footer_redacted.docx");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            Assert.DoesNotContain("123-45-6789", WordDocs.HeadersText(output)); // header PII gone
            Assert.DoesNotContain("george@fake.com", WordDocs.FootersText(output)); // footer PII gone
            Assert.DoesNotContain("078-05-1120", WordDocs.AllBodyText(output)); // body PII gone
        }

        [Fact]
        public async Task WordSample_HeaderFooter_OptionOff_KeepsHeaderFooter_StillRedactsBody()
        {
            string input = Path.Combine(SamplesDir, "header-footer.docx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "header-footer_keep.docx");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx",
                redactOfficeHeadersFooters: false);

            Assert.Contains("123-45-6789", WordDocs.HeadersText(output)); // gated by the option
            Assert.Contains("george@fake.com", WordDocs.FootersText(output));
            Assert.DoesNotContain("078-05-1120", WordDocs.AllBodyText(output)); // body still redacted
        }

        [Fact]
        public async Task XlsxSample_HeaderFooter_RedactsHeaderFooterText_KeepsFieldCodes()
        {
            // header-footer.xlsx: header "&CConfidential SSN 123-45-6789", footer "&LContact george@fake.com&RPage &P".
            string input = Path.Combine(SamplesDir, "header-footer.xlsx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "header-footer_redacted.xlsx");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            string headerFooter = SpreadsheetTestHelper.HeaderFooterText(output);
            Assert.DoesNotContain("123-45-6789", headerFooter);     // header PII gone
            Assert.DoesNotContain("george@fake.com", headerFooter); // footer PII gone
            Assert.Contains("&C", headerFooter);                    // section field code preserved
            Assert.Contains("&P", headerFooter);                    // page-number field code preserved
            Assert.DoesNotContain("078-05-1120", SpreadsheetTestHelper.AllText(output)); // cell PII gone
        }

        [Fact]
        public async Task EmailSample_WithAttachment_RemoveOn_DropsAttachment_RedactsBody()
        {
            // email-with-attachment.eml: body with george@fake.com + SSN, plus a "john_smith_ssn.pdf" attachment.
            string input = Path.Combine(SamplesDir, "email-with-attachment.eml");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "email-noattach.eml");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx",
                removeEmailAttachments: true);

            string eml = await File.ReadAllTextAsync(output);
            Assert.DoesNotContain("john_smith_ssn.pdf", eml); // attachment (and its filename) gone
            Assert.DoesNotContain("U0VDUkVU", eml);           // attachment content gone

            using var stream = File.OpenRead(output);
            MimeKit.MimeMessage message = MimeKit.MimeMessage.Load(stream);
            Assert.Empty(message.Attachments);
            Assert.DoesNotContain(Email, message.TextBody ?? string.Empty); // body still redacted
            Assert.DoesNotContain(Ssn, message.TextBody ?? string.Empty);
        }

        [Fact]
        public async Task EmailSample_WithAttachment_RemoveOff_KeepsAttachment_RedactsBody()
        {
            string input = Path.Combine(SamplesDir, "email-with-attachment.eml");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "email-keepattach.eml");

            // Default: attachments are kept (only the message body is redacted).
            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            string eml = await File.ReadAllTextAsync(output);
            Assert.Contains("john_smith_ssn.pdf", eml); // attachment preserved
            Assert.DoesNotContain(Email, eml);          // body still redacted
            Assert.DoesNotContain(Ssn, eml);
        }

        [Fact]
        public async Task EmailSample_NestedMessage_RedactsForwardedHeadersAndBody()
        {
            // nested-message.eml wraps a forwarded message/rfc822 whose own headers and body carry PII.
            string input = Path.Combine(SamplesDir, "nested-message.eml");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "nested-redacted.eml");

            // Header scrubs on (the app's defaults) so the forwarded Received/Bcc are covered too.
            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx",
                scrubEmailHeaders: true, removeCommonEmailHeaders: true);

            string eml = await File.ReadAllTextAsync(output);
            // Outer message PII.
            Assert.DoesNotContain("outer@example.com", eml);
            Assert.DoesNotContain("outer.rcpt@example.com", eml);
            Assert.DoesNotContain("outer-body@example.com", eml);
            // Forwarded (nested) message headers + body — the leak this fixes.
            Assert.DoesNotContain("inner-from@example.com", eml); // nested From
            Assert.DoesNotContain("inner-to@example.com", eml);   // nested To
            Assert.DoesNotContain("inner-cc@example.com", eml);   // nested Cc
            Assert.DoesNotContain("john@example.com", eml);       // nested Subject
            Assert.DoesNotContain("inner-body@example.com", eml); // nested body
            Assert.DoesNotContain("123-45-6789", eml);            // nested body SSN
            // Nested technical/identity headers removed by the scrubs.
            Assert.DoesNotContain("10.9.8.7", eml);               // nested Received IP
            Assert.DoesNotContain("inner-bcc@example.com", eml);  // nested Bcc
        }

        [Fact]
        public async Task EmailSample_WithAttachment_KeptAttachment_VerificationWarns()
        {
            string input = Path.Combine(SamplesDir, "email-with-attachment.eml");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            var fs = new FilterService();

            // Kept attachments -> verification carries the attachment caveat.
            string kept = Path.Combine(_tempDir, "email-verify-keep.eml");
            await RedactionService.RedactFileAsync(input, kept, SamplePolicy(), "ctx");
            VerificationOutcome keptOutcome = RedactionVerifier.Verify(kept, SamplePolicy(), "ctx", fs);
            Assert.NotNull(keptOutcome.FidelityNote);
            Assert.Contains("attachment", keptOutcome.FidelityNote!, StringComparison.OrdinalIgnoreCase);

            // Removed attachments -> no attachment caveat.
            string removed = Path.Combine(_tempDir, "email-verify-removed.eml");
            await RedactionService.RedactFileAsync(input, removed, SamplePolicy(), "ctx", removeEmailAttachments: true);
            VerificationOutcome removedOutcome = RedactionVerifier.Verify(removed, SamplePolicy(), "ctx", fs);
            Assert.Null(removedOutcome.FidelityNote);
        }

        [Fact]
        public async Task WordSample_TrackedDeletion_RedactsDeletedText()
        {
            // tracked-deletion.docx hides "george@fake.com" and an SSN inside a tracked deletion (w:delText),
            // not the visible body. Redaction (with the tracked-changes scrub off, the default here) must
            // still remove it so it can't be recovered with "Reject Changes".
            string input = Path.Combine(SamplesDir, "tracked-deletion.docx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "tracked-deletion_redacted.docx");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            string deleted = string.Concat(WordDocs.DeletedTexts(output));
            Assert.NotEmpty(deleted);                       // the deletion is preserved...
            Assert.DoesNotContain(Email, deleted);          // ...but its PII is redacted
            Assert.DoesNotContain(Ssn, deleted);
            Assert.False(WordDocs.AnyPartContains(output, Email)); // nothing anywhere still holds it
            Assert.False(WordDocs.AnyPartContains(output, Ssn));
        }

        [Fact]
        public async Task XlsxSample_Comments_RedactsLegacyAndThreadedCommentPii()
        {
            // xlsx-with-comments.xlsx hides an SSN in a legacy comment and an email in a threaded comment
            // (with an email author), none of which is in any cell.
            string input = Path.Combine(SamplesDir, "xlsx-with-comments.xlsx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "xlsx-comments_redacted.xlsx");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            string commentXml = SpreadsheetTestHelper.AllCommentXml(output);
            Assert.DoesNotContain(Ssn, commentXml);                 // legacy comment SSN
            Assert.DoesNotContain(Email, commentXml);               // threaded comment email
            Assert.DoesNotContain("reviewer@example.com", commentXml); // threaded author display name
            Assert.NotEmpty(SpreadsheetTestHelper.AllCommentText(output)); // comments preserved (redacted, not dropped)
        }

        [Fact]
        public async Task WordSample_Chart_RedactsTitleAndCachedSeriesValues()
        {
            // sample-documents/chart-sample.docx embeds a chart whose title, cached series name, and cached
            // category value carry PII the cells do not.
            string input = Path.Combine(SamplesDir, "chart-sample.docx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "chart-sample_redacted.docx");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            Assert.False(WordDocs.AnyPartContains(output, "john@example.com"));  // chart title
            Assert.False(WordDocs.AnyPartContains(output, "123-45-6789"));       // cached series name
            Assert.False(WordDocs.AnyPartContains(output, "alice@example.com")); // cached category
        }

        [Fact]
        public async Task XlsxSample_Chart_RedactsTitleAndCachedSeriesValues()
        {
            string input = Path.Combine(SamplesDir, "chart-sample.xlsx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "chart-sample_redacted.xlsx");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            string chartXml = SpreadsheetTestHelper.AllChartXml(output);
            Assert.DoesNotContain("john@example.com", chartXml);
            Assert.DoesNotContain("123-45-6789", chartXml);
            Assert.DoesNotContain("alice@example.com", chartXml);
        }

        [Fact]
        public async Task DocxSample_EmbeddedWorkbook_IsRedactedInPlace()
        {
            // docx-embedded-workbook.docx: a Word doc with an embedded Excel workbook (Insert > Object) with PII.
            string input = Path.Combine(SamplesDir, "docx-embedded-workbook.docx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "docx-embed_redacted.docx");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            string embedded = WordDocs.EmbeddedPackageAllTextAsXlsx(output);
            Assert.DoesNotContain(Email, embedded);
            Assert.DoesNotContain("123-45-6789", embedded);
        }

        [Fact]
        public async Task XlsxSample_EmbeddedWorkbook_IsRedactedInPlace()
        {
            // xlsx-embedded-workbook.xlsx: a workbook with an embedded Excel workbook carrying PII.
            string input = Path.Combine(SamplesDir, "xlsx-embedded-workbook.xlsx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "xlsx-embed_redacted.xlsx");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            string embedded = SpreadsheetTestHelper.XlsxEmbeddedPackageAllText(output);
            Assert.DoesNotContain(Email, embedded);
            Assert.DoesNotContain("123-45-6789", embedded);
        }

        [Fact]
        public async Task DocxSample_ChartEmbeddedWorkbook_RedactsUnplottedSourcePii()
        {
            // docx-chart-embedded-workbook.docx: a chart whose embedded source workbook has an unplotted
            // email + SSN column — a full copy of source data behind the chart.
            string input = Path.Combine(SamplesDir, "docx-chart-embedded-workbook.docx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "chart-embed_redacted.docx");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            string embedded = WordDocs.EmbeddedChartWorkbookAllText(output);
            Assert.DoesNotContain(Email, embedded);
            Assert.DoesNotContain("123-45-6789", embedded);
        }

        [Fact]
        public async Task DocxSample_FieldCodes_RedactsInstructionEmailAndBodySsn()
        {
            // docx-with-field-codes.docx: a HYPERLINK field whose instruction carries an email + a body SSN.
            string input = Path.Combine(SamplesDir, "docx-with-field-codes.docx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "field-codes_redacted.docx");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            Assert.False(WordDocs.AnyPartContains(output, Email));           // field-instruction email
            Assert.False(WordDocs.AnyPartContains(output, "123-45-6789"));    // body SSN
            Assert.True(WordDocs.AnyPartContains(output, "HYPERLINK"));       // field keyword preserved
        }

        [Fact]
        public async Task XlsxSample_PivotCache_RedactsDenormalizedCopy()
        {
            // xlsx-with-pivot-cache.xlsx: a pivot cache whose shared items/records hold an email + an SSN.
            string input = Path.Combine(SamplesDir, "xlsx-with-pivot-cache.xlsx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "pivot_redacted.xlsx");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            string pivot = SpreadsheetTestHelper.AllPivotXml(output);
            Assert.DoesNotContain(Email, pivot);           // shared item
            Assert.DoesNotContain("123-45-6789", pivot);    // inline record value
            Assert.True(SpreadsheetTestHelper.AnyPivotRefreshOnLoad(output));
        }

        [Fact]
        public async Task XlsxSample_TextBox_RedactsShapeTextAndCell()
        {
            // xlsx-with-textbox.xlsx: an email in a cell + an SSN inside a text box (drawing XML).
            string input = Path.Combine(SamplesDir, "xlsx-with-textbox.xlsx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "textbox_redacted.xlsx");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            Assert.DoesNotContain(Email, SpreadsheetTestHelper.AllText(output));       // the cell
            Assert.DoesNotContain("123-45-6789", SpreadsheetTestHelper.AllDrawingXml(output)); // the text box
        }

        [Fact]
        public async Task XlsxSample_FormulaCache_Enabled_RedactsCachedResultAndClearsCaches()
        {
            // formula-cache.xlsx: A2 = an email, B2 = "=A2" whose cache duplicates it, C2 = a benign formula.
            string input = Path.Combine(SamplesDir, "formula-cache.xlsx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "formula-cache_redacted.xlsx");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx"); // formula redaction on by default

            Assert.DoesNotContain(Email, SpreadsheetTestHelper.AllText(output)); // the cached copy is gone too
            Assert.False(SpreadsheetTestHelper.IsFormulaCell(output, "B2"));     // redacted cache -> static value
            Assert.True(string.IsNullOrEmpty(SpreadsheetTestHelper.CachedValue(output, "C2"))); // benign cache cleared
            Assert.True(SpreadsheetTestHelper.FullCalcOnLoad(output));           // Excel recomputes on open
        }

        [Fact]
        public async Task XlsxSample_FormulaCache_Disabled_LeavesCachedResult()
        {
            string input = Path.Combine(SamplesDir, "formula-cache.xlsx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "formula-cache_keep.xlsx");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx",
                redactCachedFormulaValues: false);

            // The plain cell is still redacted, but the formula's cached copy of the email survives.
            Assert.False(SpreadsheetTestHelper.IsFormulaCell(output, "A2"));
            Assert.Equal(Email, SpreadsheetTestHelper.CachedValue(output, "B2"));
            Assert.True(SpreadsheetTestHelper.IsFormulaCell(output, "B2"));
        }

        // email-inline-image.eml: an HTML body (with PII) that references a cid: inline image, plus an
        // attachment. Covers the "Remove attachments" / "Also remove inline images" option matrix.
        private const string InlineImageSample = "email-inline-image.eml";

        private static string? EmailFidelityNote(string output) =>
            RedactionVerifier.Verify(output, SamplePolicy(), "ctx", new FilterService(), sourcePath: output).FidelityNote;

        [Fact]
        public async Task EmailSample_InlineImage_BothOff_KeepsImageAndAttachment_Warns()
        {
            string input = Path.Combine(SamplesDir, InlineImageSample);
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "inline-bothoff.eml");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx"); // both off (default)

            string eml = await File.ReadAllTextAsync(output);
            Assert.DoesNotContain(Email, eml);              // body PII redacted
            Assert.DoesNotContain(Ssn, eml);
            Assert.Contains("image/png", eml);              // inline image kept
            Assert.Contains("report_sensitive.pdf", eml);   // attachment kept

            string? note = EmailFidelityNote(output);
            Assert.NotNull(note);
            Assert.Contains("attachment", note!, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("image", note!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task EmailSample_InlineImage_AttachmentsOnly_RemovesAttachment_KeepsImage_Warns()
        {
            string input = Path.Combine(SamplesDir, InlineImageSample);
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "inline-attachonly.eml");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx",
                removeEmailAttachments: true, removeEmailInlineImages: false);

            string eml = await File.ReadAllTextAsync(output);
            Assert.DoesNotContain("report_sensitive.pdf", eml); // attachment removed
            Assert.Contains("image/png", eml);                   // inline image kept

            string? note = EmailFidelityNote(output);
            Assert.NotNull(note);
            Assert.Contains("image", note!, StringComparison.OrdinalIgnoreCase); // inline-image caveat
            Assert.DoesNotContain("attachment", note!, StringComparison.OrdinalIgnoreCase); // no attachment caveat
        }

        [Fact]
        public async Task EmailSample_InlineImage_BothOn_RemovesBoth_NeutralizesCid_NoWarning()
        {
            string input = Path.Combine(SamplesDir, InlineImageSample);
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "inline-bothon.eml");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx",
                removeEmailAttachments: true, removeEmailInlineImages: true);

            string eml = await File.ReadAllTextAsync(output);
            Assert.DoesNotContain("report_sensitive.pdf", eml); // attachment removed
            Assert.DoesNotContain("image/png", eml);             // inline image removed
            Assert.DoesNotContain("cid:logo001", eml);           // cid reference neutralized
            Assert.Contains("about:blank", eml);                 // ...to a harmless placeholder
            Assert.DoesNotContain(Email, eml);                   // body still redacted

            Assert.Null(EmailFidelityNote(output)); // nothing un-inspected remains -> no caveat
        }

        [Fact]
        public async Task PdfSample_Annotations_ReportsPiiInAnnotationsAndFormFields()
        {
            // pdf-with-annotations.pdf hides an SSN in a FreeText annotation and an email in a form field —
            // neither is in the page content. They're removed from the image-only output, and must be
            // detected and reported.
            string input = Path.Combine(SamplesDir, "pdf-with-annotations.pdf");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "pdf-annot_redacted.pdf");

            List<RedactionSpanEntity> spans = await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            Assert.Contains(spans, s => s.Text.Contains(Ssn));   // annotation
            Assert.Contains(spans, s => s.Text.Contains(Email)); // form field

            // Flattened to images, so the annotation/form text isn't in the output either.
            string outBytes = System.Text.Encoding.Latin1.GetString(await File.ReadAllBytesAsync(output));
            Assert.DoesNotContain(Ssn, outBytes);
            Assert.DoesNotContain(Email, outBytes);
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
