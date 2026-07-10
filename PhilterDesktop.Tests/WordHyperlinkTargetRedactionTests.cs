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

using Phileas.Model;
using Phileas.Services;
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// A hyperlink's <b>URL target</b> lives in the part's relationships (referenced by w:hyperlink
    /// r:id), not in the visible run text. Redacting the display text alone left the target — a
    /// mailto: address, an intranet URL carrying an id, a file:// path — shipping intact. These pin
    /// that PII-bearing targets are neutralized (in both the Redact and Modify/ApplySpans paths),
    /// benign targets are preserved, and the document stays valid (no dangling r:id).
    /// </summary>
    public sealed class WordHyperlinkTargetRedactionTests : IDisposable
    {
        private static readonly PhileasPolicy EmailPolicy = new()
        {
            Name = "email",
            Identifiers = new Phileas.Policy.Identifiers { EmailAddress = new Phileas.Policy.Filters.EmailAddress() }
        };

        private static readonly PhileasPolicy UrlPolicy = new()
        {
            Name = "url",
            Identifiers = new Phileas.Policy.Identifiers { Url = new Phileas.Policy.Filters.Url() }
        };

        private static Func<string, TextFilterResult> FilterWith(PhileasPolicy policy) =>
            t => new FilterService().Filter(policy, "ctx", 0, t);

        private static Func<string, TextFilterResult> EmailFilter => FilterWith(EmailPolicy);

        private readonly string _dir;

        public WordHyperlinkTargetRedactionTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-link-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private string NewPath(string name) => Path.Combine(_dir, name);

        // --- Redact: neutralization ---------------------------------------------------

        [Fact]
        public void Redact_MailtoTargetWithEmail_IsNeutralized()
        {
            string input = NewPath("mailto.docx");
            string output = NewPath("mailto_out.docx");
            WordDocs.CreateWithHyperlink(input, "Email us", "mailto:john@example.com", "body");
            Assert.Contains("mailto:john@example.com", WordDocs.HyperlinkTargets(input));

            WordDocumentRedactor.Redact(input, output, EmailFilter);

            // The target lives in the part's .rels, so HyperlinkTargets (not AnyPartContains) is the real check.
            Assert.DoesNotContain(WordDocs.HyperlinkTargets(output), t => t.Contains("john@example.com"));
            Assert.Contains("https://redacted.invalid/", WordDocs.HyperlinkTargets(output));
        }

        [Fact]
        public void Redact_TargetWithEmbeddedEmail_IsNeutralized()
        {
            // PII inside a query string of an otherwise-ordinary URL target.
            string input = NewPath("embedded.docx");
            string output = NewPath("embedded_out.docx");
            WordDocs.CreateWithHyperlink(input, "Contact form", "https://intranet.corp/contact?to=jane@example.com", "body");

            WordDocumentRedactor.Redact(input, output, EmailFilter);

            Assert.DoesNotContain(WordDocs.HyperlinkTargets(output), t => t.Contains("jane@example.com"));
        }

        [Fact]
        public void Redact_FileTargetWithEmbeddedPii_IsNeutralized()
        {
            // file:// targets (named explicitly in the issue) are covered by the same part walk.
            string input = NewPath("file.docx");
            string output = NewPath("file_out.docx");
            WordDocs.CreateWithHyperlink(input, "Open file", "file://intranet/mailboxes/john@example.com/report.txt", "body");

            WordDocumentRedactor.Redact(input, output, EmailFilter);

            Assert.DoesNotContain(WordDocs.HyperlinkTargets(output), t => t.Contains("john@example.com"));
        }

        [Fact]
        public void Redact_BenignTarget_IsPreserved()
        {
            // Email-only policy: a URL target with no email is left exactly as it was.
            string input = NewPath("benign.docx");
            string output = NewPath("benign_out.docx");
            WordDocs.CreateWithHyperlink(input, "Help", "https://example.com/help", "body");

            WordDocumentRedactor.Redact(input, output, EmailFilter);

            Assert.Contains("https://example.com/help", WordDocs.HyperlinkTargets(output));
        }

        [Fact]
        public void Redact_UrlPolicy_UrlTargetNeutralized()
        {
            // When the policy redacts URLs, hyperlink targets are neutralized too (parity with body URLs).
            string input = NewPath("url.docx");
            string output = NewPath("url_out.docx");
            WordDocs.CreateWithHyperlink(input, "Portal", "https://patient-portal.example.com/record/9182", "body");

            WordDocumentRedactor.Redact(input, output, FilterWith(UrlPolicy));

            Assert.DoesNotContain(WordDocs.HyperlinkTargets(output), t => t.Contains("patient-portal.example.com"));
        }

        [Fact]
        public void Redact_HyperlinkInHeader_TargetNeutralized()
        {
            // The target lives on the header part, not the main part — proves the part walk reaches it.
            string input = NewPath("hdrlink.docx");
            string output = NewPath("hdrlink_out.docx");
            WordDocs.CreateWithHyperlinkInHeader(input, "Mail", "mailto:hdr@example.com", "body");
            Assert.Contains("mailto:hdr@example.com", WordDocs.HyperlinkTargets(input));

            WordDocumentRedactor.Redact(input, output, EmailFilter);

            Assert.DoesNotContain(WordDocs.HyperlinkTargets(output), t => t.Contains("hdr@example.com"));
        }

        [Fact]
        public void Redact_DisplayTextAndTarget_BothRedacted()
        {
            // The visible text and the underlying target both carry the address; both must go.
            string input = NewPath("both.docx");
            string output = NewPath("both_out.docx");
            WordDocs.CreateWithHyperlink(input, "write to john@example.com", "mailto:john@example.com", "body");

            WordDocumentRedactor.Redact(input, output, EmailFilter);

            Assert.False(WordDocs.AnyPartContains(output, "john@example.com"), "the visible link text must not survive");
            Assert.DoesNotContain(WordDocs.HyperlinkTargets(output), t => t.Contains("john@example.com"));
        }

        [Fact]
        public void Redact_MixedTargets_OnlyPiiNeutralized()
        {
            string input = NewPath("mixed.docx");
            string output = NewPath("mixed_out.docx");
            using (var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(input, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
            {
                var main = doc.AddMainDocumentPart();
                var body = new DocumentFormat.OpenXml.Wordprocessing.Body();
                var pii = main.AddHyperlinkRelationship(new Uri("mailto:secret@example.com"), true);
                var benign = main.AddHyperlinkRelationship(new Uri("https://example.com/docs"), true);
                body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
                    new DocumentFormat.OpenXml.Wordprocessing.Hyperlink(
                        new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("a"))) { Id = pii.Id }));
                body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
                    new DocumentFormat.OpenXml.Wordprocessing.Hyperlink(
                        new DocumentFormat.OpenXml.Wordprocessing.Run(new DocumentFormat.OpenXml.Wordprocessing.Text("b"))) { Id = benign.Id }));
                main.Document = new DocumentFormat.OpenXml.Wordprocessing.Document(body);
            }

            WordDocumentRedactor.Redact(input, output, EmailFilter);

            Assert.DoesNotContain(WordDocs.HyperlinkTargets(output), t => t.Contains("secret@example.com"));
            Assert.Contains("https://example.com/docs", WordDocs.HyperlinkTargets(output));
        }

        // --- Captured spans / report --------------------------------------------------

        [Fact]
        public void Redact_ReturnsCapturedHyperlinkSpan()
        {
            string input = NewPath("cap.docx");
            string output = NewPath("cap_out.docx");
            WordDocs.CreateWithHyperlink(input, "Email", "mailto:john@example.com", "body");

            List<OfficeRedactionSpan> spans = WordDocumentRedactor.Redact(input, output, EmailFilter);

            OfficeRedactionSpan? link = spans.FirstOrDefault(s => s.Classification == "hyperlink-target");
            Assert.NotNull(link);
            Assert.Equal("mailto:john@example.com", link!.Text);
            Assert.Equal("https://redacted.invalid/", link.Replacement);
            Assert.Equal(-1, link.ParagraphIndex);
        }

        [Fact]
        public void Redact_BenignTarget_CapturesNoHyperlinkSpan()
        {
            string input = NewPath("nocap.docx");
            string output = NewPath("nocap_out.docx");
            WordDocs.CreateWithHyperlink(input, "Help", "https://example.com/help", "body");

            List<OfficeRedactionSpan> spans = WordDocumentRedactor.Redact(input, output, EmailFilter);

            Assert.DoesNotContain(spans, s => s.Classification == "hyperlink-target");
        }

        // --- Validity -----------------------------------------------------------------

        [Fact]
        public void Redact_NeutralizedDocument_StaysValid_NoDanglingReference()
        {
            string input = NewPath("valid.docx");
            string output = NewPath("valid_out.docx");
            WordDocs.CreateWithHyperlink(input, "Email", "mailto:john@example.com", "body");

            WordDocumentRedactor.Redact(input, output, EmailFilter);

            using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(output, false);
            Assert.NotNull(doc.MainDocumentPart?.Document?.Body);
            Assert.True(WordDocs.HyperlinkIdsAllResolve(output), "the hyperlink r:id must still resolve to a relationship");
        }

        [Fact]
        public void Redact_TwiceIsIdempotent_StillCleanAndValid()
        {
            string input = NewPath("idem.docx");
            string once = NewPath("idem1.docx");
            string twice = NewPath("idem2.docx");
            WordDocs.CreateWithHyperlink(input, "Email", "mailto:john@example.com", "body");

            WordDocumentRedactor.Redact(input, once, EmailFilter);
            WordDocumentRedactor.Redact(once, twice, EmailFilter);

            Assert.DoesNotContain(WordDocs.HyperlinkTargets(twice), t => t.Contains("john@example.com"));
            Assert.True(WordDocs.HyperlinkIdsAllResolve(twice));
        }

        [Fact]
        public void Redact_NoHyperlinks_StillRedactsBody()
        {
            string input = NewPath("none.docx");
            string output = NewPath("none_out.docx");
            WordDocs.Create(input, "body has body@example.com");

            WordDocumentRedactor.Redact(input, output, EmailFilter);

            Assert.False(WordDocs.AnyPartContains(output, "body@example.com"));
        }

        // --- ApplySpans (Modify path) -------------------------------------------------

        [Fact]
        public void ApplySpans_WithDrawingFilter_NeutralizesTarget()
        {
            string input = NewPath("apply.docx");
            string output = NewPath("apply_out.docx");
            WordDocs.CreateWithHyperlink(input, "Email", "mailto:john@example.com", "body");

            List<OfficeRedactionSpan> spans = WordDocumentRedactor.Detect(input, EmailFilter);
            WordDocumentRedactor.ApplySpans(input, output, spans, highlight: false, drawingFilter: EmailFilter);

            Assert.DoesNotContain(WordDocs.HyperlinkTargets(output), t => t.Contains("john@example.com"));
            Assert.True(WordDocs.HyperlinkIdsAllResolve(output));
        }

        [Fact]
        public void ApplySpans_WithoutFilter_LeavesTarget()
        {
            // Without a filter, only stored (paragraph) spans re-apply; the target requires the filter.
            string input = NewPath("nofilter.docx");
            string output = NewPath("nofilter_out.docx");
            WordDocs.CreateWithHyperlink(input, "Email", "mailto:john@example.com", "body");

            List<OfficeRedactionSpan> spans = WordDocumentRedactor.Detect(input, EmailFilter);
            WordDocumentRedactor.ApplySpans(input, output, spans, highlight: false);

            Assert.Contains("mailto:john@example.com", WordDocs.HyperlinkTargets(output));
        }

        // --- End to end ---------------------------------------------------------------

        [Fact]
        public async Task RedactFileAsync_HyperlinkTarget_EndToEnd()
        {
            string input = NewPath("svc.docx");
            string output = NewPath("svc_out.docx");
            WordDocs.CreateWithHyperlink(input, "Email", "mailto:john@example.com", "body");

            await RedactionService.RedactFileAsync(input, output, EmailPolicy, "ctx", new SettingsEntity(), new FilterService());

            Assert.True(File.Exists(output));
            Assert.DoesNotContain(WordDocs.HyperlinkTargets(output), t => t.Contains("john@example.com"));
            Assert.True(WordDocs.HyperlinkIdsAllResolve(output));
        }
    }
}
