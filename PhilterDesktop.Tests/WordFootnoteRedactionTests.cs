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

using DocumentFormat.OpenXml.Packaging;
using Phileas.Model;
using Phileas.Services;
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Footnote and endnote text was never sent to the redaction engine — only the body and
    /// headers/footers were — so PII in a note shipped in the output.
    /// These pin that footnotes and endnotes are now redacted like body paragraphs.
    /// </summary>
    public sealed class WordFootnoteRedactionTests : IDisposable
    {
        private static readonly PhileasPolicy EmailPolicy = new()
        {
            Name = "email",
            Identifiers = new Phileas.Policy.Identifiers { EmailAddress = new Phileas.Policy.Filters.EmailAddress() }
        };

        private static Func<string, TextFilterResult> Filter =>
            t => new FilterService().Filter(EmailPolicy, "ctx", 0, t);

        private readonly string _dir;

        public WordFootnoteRedactionTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-notes-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private string NewPath(string name) => Path.Combine(_dir, name);

        [Fact]
        public void Redact_RedactsFootnoteText()
        {
            string input = NewPath("fn.docx");
            string output = NewPath("fn_out.docx");
            WordDocs.CreateWithNotes(input, footnoteText: "see foot@example.com", endnoteText: null, "body plain");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.False(WordDocs.AnyPartContains(output, "@example.com"), "footnote PII must not survive in any part");
            Assert.Contains(WordDocs.FootnoteTexts(output), t => t.Contains("REDACTED"));
        }

        [Fact]
        public void Redact_RedactsEndnoteText()
        {
            string input = NewPath("en.docx");
            string output = NewPath("en_out.docx");
            WordDocs.CreateWithNotes(input, footnoteText: null, endnoteText: "see end@example.com", "body plain");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
            Assert.Contains(WordDocs.EndnoteTexts(output), t => t.Contains("REDACTED"));
        }

        [Fact]
        public void Redact_RedactsBothNotesAndBody()
        {
            string input = NewPath("both.docx");
            string output = NewPath("both_out.docx");
            WordDocs.CreateWithNotes(input, "foot foot@example.com", "end end@example.com", "body body@example.com");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
            Assert.Contains(WordDocs.FootnoteTexts(output), t => t.Contains("REDACTED"));
            Assert.Contains(WordDocs.EndnoteTexts(output), t => t.Contains("REDACTED"));
            Assert.DoesNotContain(WordDocs.BodyParagraphs(output), p => p.Contains("@example.com"));
        }

        [Fact]
        public void Redact_NoteWithoutPii_IsUnchanged()
        {
            string input = NewPath("clean.docx");
            string output = NewPath("clean_out.docx");
            WordDocs.CreateWithNotes(input, "nothing sensitive", null, "body");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.Contains(WordDocs.FootnoteTexts(output), t => t.Contains("nothing sensitive"));
        }

        [Fact]
        public void Detect_IncludesFootnoteAndEndnoteText()
        {
            string input = NewPath("detect.docx");
            WordDocs.CreateWithNotes(input, "foot fa@example.com", "end ea@example.com", "body ba@example.com");

            List<OfficeRedactionSpan> spans = WordDocumentRedactor.Detect(input, Filter);

            Assert.Contains(spans, s => s.Text == "fa@example.com");
            Assert.Contains(spans, s => s.Text == "ea@example.com");
            Assert.Contains(spans, s => s.Text == "ba@example.com");
        }

        [Fact]
        public void ReadParagraphs_IncludesNotes()
        {
            string input = NewPath("read.docx");
            WordDocs.CreateWithNotes(input, "footnote line", "endnote line", "body line");

            List<string> paragraphs = WordDocumentRedactor.ReadParagraphs(input);

            Assert.Contains("body line", paragraphs);
            Assert.Contains("footnote line", paragraphs);
            Assert.Contains("endnote line", paragraphs);
        }

        [Fact]
        public void Detect_KeepsBodyIndexStable_NotesAppendedAfterBody()
        {
            string input = NewPath("idx.docx");
            WordDocs.CreateWithNotes(input, "foot fa@example.com", null, "body ba@example.com");

            List<OfficeRedactionSpan> spans = WordDocumentRedactor.Detect(input, Filter);

            OfficeRedactionSpan body = Assert.Single(spans, s => s.Text == "ba@example.com");
            OfficeRedactionSpan foot = Assert.Single(spans, s => s.Text == "fa@example.com");
            Assert.Equal(0, body.ParagraphIndex);
            Assert.True(foot.ParagraphIndex > body.ParagraphIndex);
        }

        [Fact]
        public void ApplySpans_RedactsNotes()
        {
            string input = NewPath("apply.docx");
            string output = NewPath("apply_out.docx");
            WordDocs.CreateWithNotes(input, "foot fa@example.com", "end ea@example.com", "body");

            List<OfficeRedactionSpan> spans = WordDocumentRedactor.Detect(input, Filter);
            WordDocumentRedactor.ApplySpans(input, output, spans, highlight: false);

            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
        }

        [Fact]
        public void Redact_SeparatorFootnotes_NotMangled_DocumentValid()
        {
            string input = NewPath("sep.docx");
            string output = NewPath("sep_out.docx");
            WordDocs.CreateWithNotes(input, "foot fa@example.com", null, "body");

            WordDocumentRedactor.Redact(input, output, Filter);

            // The note is redacted, the document still opens, and the footnotes part survives.
            using WordprocessingDocument doc = WordprocessingDocument.Open(output, false);
            Assert.NotNull(doc.MainDocumentPart?.Document?.Body);
            Assert.NotNull(doc.MainDocumentPart?.FootnotesPart);
        }

        [Fact]
        public async Task RedactFileAsync_Notes_EndToEnd()
        {
            string input = NewPath("svc.docx");
            string output = NewPath("svc_out.docx");
            WordDocs.CreateWithNotes(input, "foot fa@example.com", "end ea@example.com", "body");

            await RedactionService.RedactFileAsync(input, output, EmailPolicy, "ctx", new SettingsEntity(), new FilterService());

            Assert.True(File.Exists(output));
            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
        }

        [Fact]
        public void Redact_DocumentWithoutNotes_StillWorks()
        {
            string input = NewPath("none.docx");
            string output = NewPath("none_out.docx");
            WordDocs.Create(input, "body with no@example.com note");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.DoesNotContain(WordDocs.BodyParagraphs(output), p => p.Contains("@example.com"));
        }

        // --- Edge cases -----------------------------------------------------------------------------

        [Fact]
        public void Redact_PiiOnlyInFootnote_BodyClean_IsRemoved()
        {
            string input = NewPath("only.docx");
            string output = NewPath("only_out.docx");
            WordDocs.CreateWithNotes(input, "see secret@example.com", null, "nothing sensitive here");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
        }

        [Fact]
        public void Redact_MultipleFootnotesAndEndnotes_AllRedacted()
        {
            string input = NewPath("multi.docx");
            string output = NewPath("multi_out.docx");
            WordDocs.CreateWithMultipleNotes(
                input,
                footnotes: new[] { "f1 fa@example.com", "f2 fb@example.com" },
                endnotes: new[] { "e1 ea@example.com", "e2 eb@example.com" },
                "body");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
            Assert.Equal(2, WordDocs.FootnoteTexts(output).Length);
            Assert.Equal(2, WordDocs.EndnoteTexts(output).Length);
            Assert.All(WordDocs.FootnoteTexts(output), t => Assert.Contains("REDACTED", t));
            Assert.All(WordDocs.EndnoteTexts(output), t => Assert.Contains("REDACTED", t));
        }

        [Fact]
        public void Redact_MultiParagraphFootnote_AllParagraphsRedacted()
        {
            string input = NewPath("mp.docx");
            string output = NewPath("mp_out.docx");
            WordDocs.CreateWithMultiParagraphFootnote(
                input,
                new[] { "first p1@example.com", "second p2@example.com", "third plain" },
                "body");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
        }

        [Fact]
        public void Redact_MultiplePiiInOneFootnoteParagraph_AllRedacted()
        {
            string input = NewPath("multipii.docx");
            string output = NewPath("multipii_out.docx");
            WordDocs.CreateWithNotes(input, "contact a@example.com or b@example.com today", null, "body");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
            // Both addresses became the replacement token (two redactions in one paragraph).
            Assert.Equal(2, WordDocs.FootnoteTexts(output).Sum(t => CountOccurrences(t, "REDACTED")));
        }

        [Fact]
        public void Redact_WithHighlight_HighlightsFootnoteReplacement()
        {
            string input = NewPath("hl.docx");
            string output = NewPath("hl_out.docx");
            WordDocs.CreateWithNotes(input, "see hl@example.com", null, "body");

            WordDocumentRedactor.Redact(input, output, Filter, highlight: true);

            using WordprocessingDocument doc = WordprocessingDocument.Open(output, false);
            string footnotesXml = doc.MainDocumentPart!.FootnotesPart!.Footnotes!.OuterXml;
            Assert.Contains("highlight", footnotesXml);
            Assert.DoesNotContain("@example.com", footnotesXml);
        }

        [Fact]
        public void Redact_FootnoteContainingTextBox_RedactsBoxText_PreservesDrawing()
        {
            // A drawing/text box inside a footnote must be handled by the same drawing-safe rebuild
            // while the note text is redacted.
            string input = NewPath("fnbox.docx");
            string output = NewPath("fnbox_out.docx");
            WordDocs.CreateWithRawFootnote(input, WordDocs.TextBoxParaXml("box bx@example.com", before: "note nx@example.com "), "body");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
            using WordprocessingDocument doc = WordprocessingDocument.Open(output, false);
            bool drawingSurvives = doc.MainDocumentPart!.FootnotesPart!.Footnotes!
                .Descendants<DocumentFormat.OpenXml.Wordprocessing.Drawing>().Any();
            Assert.True(drawingSurvives, "the footnote's drawing/text box must survive redaction");
        }

        [Fact]
        public void Redact_AllPartsTogether_EveryNoteAndRegionRedacted_StableOrder()
        {
            string input = NewPath("full.docx");
            WordDocs.CreateWithHeaderFooterAndNotes(
                input, "hdr h@example.com", "ftr f@example.com", "fn fn@example.com", "en en@example.com", "body b@example.com");

            List<OfficeRedactionSpan> spans = WordDocumentRedactor.Detect(input, Filter);

            // Body first (index 0), then header/footer, then notes — strictly increasing indexes.
            OfficeRedactionSpan body = Assert.Single(spans, s => s.Text == "b@example.com");
            OfficeRedactionSpan footnote = Assert.Single(spans, s => s.Text == "fn@example.com");
            OfficeRedactionSpan endnote = Assert.Single(spans, s => s.Text == "en@example.com");
            Assert.Equal(0, body.ParagraphIndex);
            Assert.True(footnote.ParagraphIndex > body.ParagraphIndex);
            Assert.True(endnote.ParagraphIndex > footnote.ParagraphIndex);

            string output = NewPath("full_out.docx");
            WordDocumentRedactor.Redact(input, output, Filter);
            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
        }

        [Fact]
        public void ApplySpans_AllPartsTogether_RedactsEverything()
        {
            string input = NewPath("full2.docx");
            string output = NewPath("full2_out.docx");
            WordDocs.CreateWithHeaderFooterAndNotes(
                input, "hdr h@example.com", "ftr f@example.com", "fn fn@example.com", "en en@example.com", "body b@example.com");

            List<OfficeRedactionSpan> spans = WordDocumentRedactor.Detect(input, Filter);
            WordDocumentRedactor.ApplySpans(input, output, spans, highlight: false);

            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
        }

        [Fact]
        public void Redact_NotesUnchangedWhenNoMatch_RoundTripsValidly()
        {
            string input = NewPath("nomatch.docx");
            string output = NewPath("nomatch_out.docx");
            WordDocs.CreateWithMultipleNotes(input, new[] { "plain one", "plain two" }, new[] { "plain three" }, "body");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.Equal(new[] { "plain one", "plain two" }, WordDocs.FootnoteTexts(output));
            Assert.Equal(new[] { "plain three" }, WordDocs.EndnoteTexts(output));
        }

        private static int CountOccurrences(string haystack, string needle)
        {
            int count = 0, i = 0;
            while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) >= 0) { count++; i += needle.Length; }
            return count;
        }
    }
}
