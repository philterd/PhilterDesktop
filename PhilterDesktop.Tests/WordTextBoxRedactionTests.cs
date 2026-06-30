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

using DocumentFormat.OpenXml.Packaging;
using Phileas.Model;
using Phileas.Services;
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Text boxes / drawings live in a run inside an outer body paragraph, so the old recursive
    /// <c>Descendants&lt;Paragraph&gt;()</c> + wipe-and-rebuild both double-processed their text and
    /// destroyed the drawing. These tests pin the fix: text-box content
    /// is redacted as its own unit, the outer paragraph is redacted using only its own text, and the
    /// drawing is never destroyed.
    /// </summary>
    public sealed class WordTextBoxRedactionTests : IDisposable
    {
        private const string Token = "{{{REDACTED-email-address}}}";

        private static readonly PhileasPolicy EmailPolicy = new()
        {
            Name = "email",
            Identifiers = new Phileas.Policy.Identifiers { EmailAddress = new Phileas.Policy.Filters.EmailAddress() }
        };

        private static Func<string, TextFilterResult> Filter =>
            t => new FilterService().Filter(EmailPolicy, "ctx", 0, t);

        private readonly string _dir;

        public WordTextBoxRedactionTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-textbox-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private string NewPath(string name) => Path.Combine(_dir, name);

        private string Redacted(string bodyXml, bool highlight = false)
        {
            string input = NewPath("in_" + Guid.NewGuid().ToString("N") + ".docx");
            string output = NewPath("out_" + Guid.NewGuid().ToString("N") + ".docx");
            WordDocs.CreateRaw(input, bodyXml);
            WordDocumentRedactor.Redact(input, output, Filter, highlight);
            return output;
        }

        // --- Core corruption / double-processing guarantees ----------------------------------------

        [Fact]
        public void TextBox_InnerText_IsRedacted()
        {
            string output = Redacted(WordDocs.TextBoxParaXml("contact box@example.com please"));

            Assert.DoesNotContain("@example.com", WordDocs.DocumentXml(output));
            Assert.Contains(WordDocs.TextBoxTexts(output), t => t.Contains("REDACTED"));
        }

        [Fact]
        public void TextBox_DrawingIsPreserved_NotCorrupted()
        {
            string output = Redacted(WordDocs.TextBoxParaXml("box@example.com"));

            Assert.True(WordDocs.HasDrawingOrPicture(output), "the drawing/text box must survive redaction");
            Assert.True(CanOpen(output), "the redacted document must remain a valid .docx");
        }

        [Fact]
        public void TextBox_OnlyBoxText_NotDoubleProcessed()
        {
            // Old bug: the outer paragraph's InnerText included the box text, so it was redacted twice
            // (and the box destroyed). The replacement must appear exactly once and the box must remain.
            string output = Redacted(WordDocs.TextBoxParaXml("box@example.com"));

            Assert.Equal(1, WordDocs.Occurrences(output, Token));
            Assert.True(WordDocs.HasDrawingOrPicture(output));
        }

        [Fact]
        public void TextBox_OuterAndBoxText_BothRedactedExactlyOnce()
        {
            // Distinct e-mails in the outer paragraph's own text and inside the box: each redacted once,
            // the box preserved, and neither original address left behind.
            string output = Redacted(WordDocs.TextBoxParaXml("box@example.com", before: "see alice@example.com ", after: " and bob@example.com"));

            Assert.Equal(3, WordDocs.Occurrences(output, Token)); // alice, bob (outer own text) + box
            Assert.DoesNotContain("@example.com", WordDocs.DocumentXml(output));
            Assert.True(WordDocs.HasDrawingOrPicture(output));
            Assert.Contains(WordDocs.TextBoxTexts(output), t => t.Contains("REDACTED"));
        }

        [Fact]
        public void TextBox_UnchangedWhenNoPii_DocumentStaysValid()
        {
            string output = Redacted(WordDocs.TextBoxParaXml("nothing to redact here", before: "plain text"));

            Assert.Equal(0, WordDocs.Occurrences(output, Token));
            Assert.True(WordDocs.HasDrawingOrPicture(output));
            Assert.Contains(WordDocs.TextBoxTexts(output), t => t.Contains("nothing to redact here"));
        }

        // --- Other drawing shapes -------------------------------------------------------------------

        [Fact]
        public void InlineDrawing_WithCaption_RedactsCaption_KeepsDrawing()
        {
            string output = Redacted(WordDocs.DrawingParaXml(caption: "figure 1 fig@example.com "));

            Assert.DoesNotContain("@example.com", WordDocs.DocumentXml(output));
            Assert.True(WordDocs.HasDrawingOrPicture(output), "a non-text drawing must not be deleted");
        }

        [Fact]
        public void DrawingOnly_Paragraph_IsPreserved_WhileOtherParagraphsRedact()
        {
            string body = WordDocs.DrawingParaXml() + WordDocs.ParaXml("email me@example.com now");
            string input = NewPath("in.docx");
            string output = NewPath("out.docx");
            WordDocs.CreateRaw(input, body);

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.True(WordDocs.HasDrawingOrPicture(output));
            Assert.DoesNotContain("@example.com", WordDocs.DocumentXml(output));
        }

        [Fact]
        public void VmlTextBox_InnerText_Redacted_PicturePreserved()
        {
            string output = Redacted(WordDocs.VmlTextBoxParaXml("legacy vml@example.com box"));

            Assert.DoesNotContain("@example.com", WordDocs.DocumentXml(output));
            Assert.True(WordDocs.HasDrawingOrPicture(output), "the VML picture/text box must survive");
            Assert.Contains(WordDocs.TextBoxTexts(output), t => t.Contains("REDACTED"));
        }

        [Fact]
        public void TextBoxInHeader_IsRedacted_DrawingPreserved()
        {
            // Build a doc whose header part contains a text box, exercising the header/footer path.
            string input = NewPath("hdr.docx");
            string output = NewPath("hdr_out.docx");
            CreateWithHeaderXml(input, WordDocs.TextBoxParaXml("header hdr@example.com"), bodyText: "body plain");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.DoesNotContain("@example.com", HeaderXml(output));
            Assert.True(HeaderHasDrawing(output), "the header's drawing must survive");
        }

        // --- Highlight ------------------------------------------------------------------------------

        [Fact]
        public void TextBox_Redaction_WithHighlight_HighlightsReplacement_KeepsDrawing()
        {
            string output = Redacted(WordDocs.TextBoxParaXml("box@example.com"), highlight: true);

            string xml = WordDocs.DocumentXml(output);
            Assert.Contains("highlight", xml);
            Assert.True(WordDocs.HasDrawingOrPicture(output));
        }

        // --- ReadParagraphs / Detect: own-text, no double counting ---------------------------------

        [Fact]
        public void ReadParagraphs_UsesOwnText_BoxIsSeparateEntry()
        {
            string input = NewPath("read.docx");
            WordDocs.CreateRaw(input, WordDocs.TextBoxParaXml("box@example.com", before: "outer text "));

            List<string> paragraphs = WordDocumentRedactor.ReadParagraphs(input);

            // The outer paragraph reports only its own text (not the box text), and the box is its own entry.
            Assert.Contains(paragraphs, p => p.Contains("outer text") && !p.Contains("box@example.com"));
            Assert.Contains(paragraphs, p => p == "box@example.com");
        }

        [Fact]
        public void Detect_ProducesSeparateSpans_NoDoubleCount()
        {
            string input = NewPath("detect.docx");
            WordDocs.CreateRaw(input, WordDocs.TextBoxParaXml("box@example.com", before: "outer alice@example.com "));

            List<RedactionSpanEntity> spans = WordDocumentRedactor.Detect(input, Filter);

            // Exactly two e-mails, on two different paragraph indexes (outer own text + box), none repeated.
            Assert.Equal(2, spans.Count);
            Assert.Equal(2, spans.Select(s => s.ParagraphIndex).Distinct().Count());
            Assert.Contains(spans, s => s.Text == "alice@example.com");
            Assert.Contains(spans, s => s.Text == "box@example.com");
        }

        // --- ApplySpans (Modify Redaction) ---------------------------------------------------------

        [Fact]
        public void ApplySpans_DetectedSpans_RedactBox_PreserveDrawing()
        {
            string input = NewPath("apply_in.docx");
            string output = NewPath("apply_out.docx");
            WordDocs.CreateRaw(input, WordDocs.TextBoxParaXml("box@example.com", before: "outer alice@example.com "));

            List<RedactionSpanEntity> spans = WordDocumentRedactor.Detect(input, Filter);
            WordDocumentRedactor.ApplySpans(input, output, spans, highlight: false);

            Assert.DoesNotContain("@example.com", WordDocs.DocumentXml(output));
            Assert.True(WordDocs.HasDrawingOrPicture(output));
            Assert.Contains(WordDocs.TextBoxTexts(output), t => t.Contains("REDACTED"));
        }

        [Fact]
        public void ApplySpans_MatchesRedact_ForTextBoxDocument()
        {
            string input = NewPath("parity_in.docx");
            WordDocs.CreateRaw(input, WordDocs.TextBoxParaXml("box@example.com", before: "outer alice@example.com "));

            string viaRedact = NewPath("via_redact.docx");
            WordDocumentRedactor.Redact(input, viaRedact, Filter);

            string viaApply = NewPath("via_apply.docx");
            List<RedactionSpanEntity> spans = WordDocumentRedactor.Detect(input, Filter);
            WordDocumentRedactor.ApplySpans(input, viaApply, spans, highlight: false);

            Assert.Equal(WordDocs.TextBoxTexts(viaRedact), WordDocs.TextBoxTexts(viaApply));
            Assert.Equal(WordDocs.Occurrences(viaRedact, Token), WordDocs.Occurrences(viaApply, Token));
        }

        // --- Integration: full RedactionService pipeline -------------------------------------------

        [Fact]
        public async Task RedactFileAsync_TextBoxDocx_RedactsBox_PreservesDrawing_OutputValid()
        {
            string input = NewPath("svc_in.docx");
            string output = NewPath("svc_out.docx");
            WordDocs.CreateRaw(input, WordDocs.TextBoxParaXml("box@example.com", before: "outer alice@example.com "));

            await RedactionService.RedactFileAsync(input, output, EmailPolicy, "ctx", new SettingsEntity(), new FilterService());

            Assert.True(File.Exists(output));
            Assert.DoesNotContain("@example.com", WordDocs.DocumentXml(output));
            Assert.True(WordDocs.HasDrawingOrPicture(output));
            Assert.True(CanOpen(output));
        }

        [Fact]
        public async Task RedactFileAsync_TextBoxDocx_WithMetadataScrub_StillPreservesDrawing()
        {
            string input = NewPath("svc_scrub_in.docx");
            string output = NewPath("svc_scrub_out.docx");
            WordDocs.CreateRaw(input, WordDocs.TextBoxParaXml("box@example.com"));

            var settings = new SettingsEntity { ScrubDocumentMetadata = true };
            await RedactionService.RedactFileAsync(input, output, EmailPolicy, "ctx", settings, new FilterService());

            Assert.DoesNotContain("@example.com", WordDocs.DocumentXml(output));
            Assert.True(WordDocs.HasDrawingOrPicture(output));
            Assert.True(CanOpen(output));
        }

        // --- helpers --------------------------------------------------------------------------------

        private static bool CanOpen(string path)
        {
            try
            {
                using WordprocessingDocument doc = WordprocessingDocument.Open(path, false);
                return doc.MainDocumentPart?.Document?.Body is not null;
            }
            catch
            {
                return false;
            }
        }

        private static void CreateWithHeaderXml(string path, string headerParaXml, string bodyText)
        {
            const string ns =
                "xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" " +
                "xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\" " +
                "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" " +
                "xmlns:wps=\"http://schemas.microsoft.com/office/word/2010/wordprocessingShape\" " +
                "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"";

            using WordprocessingDocument doc = WordprocessingDocument.Create(path, DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
            MainDocumentPart main = doc.AddMainDocumentPart();

            HeaderPart headerPart = main.AddNewPart<HeaderPart>();
            using (Stream hs = headerPart.GetStream(FileMode.Create, FileAccess.Write))
            using (var hw = new StreamWriter(hs))
            {
                hw.Write($"<w:hdr {ns}>{headerParaXml}</w:hdr>");
            }
            string headerId = main.GetIdOfPart(headerPart);

            string bodyXml =
                $"<w:document {ns}><w:body>" +
                $"<w:p><w:r><w:t>{bodyText}</w:t></w:r></w:p>" +
                $"<w:sectPr><w:headerReference w:type=\"default\" r:id=\"{headerId}\"/></w:sectPr>" +
                "</w:body></w:document>";
            using (Stream ds = main.GetStream(FileMode.Create, FileAccess.Write))
            using (var dw = new StreamWriter(ds))
            {
                dw.Write(bodyXml);
            }
        }

        private static string HeaderXml(string path)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(path, false);
            return string.Concat(doc.MainDocumentPart!.HeaderParts.Select(h => h.Header!.OuterXml));
        }

        private static bool HeaderHasDrawing(string path)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(path, false);
            return doc.MainDocumentPart!.HeaderParts.Any(h =>
                h.Header!.Descendants<DocumentFormat.OpenXml.Wordprocessing.Drawing>().Any()
                || h.Header!.Descendants<DocumentFormat.OpenXml.Wordprocessing.Picture>().Any());
        }
    }
}
