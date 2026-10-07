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

using System.Reflection;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using LiteDB;
using PhilterData;
using Xunit;
using static PhilterDesktop.Tests.DpiLayoutTests;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// TextBoxText (CRLF display text with positions mapped back to the original), and its use in the
    /// text redaction preview, where a selection in the box becomes a span over the original text.
    /// </summary>
    public sealed class TextBoxTextTests
    {
        [Theory]
        [InlineData("a\nb", "a\r\nb")]
        [InlineData("a\r\nb", "a\r\nb")]
        [InlineData("a\rb", "a\r\nb")]
        [InlineData("a\n\nb\r\nc\rd", "a\r\n\r\nb\r\nc\r\nd")]
        [InlineData("a\r\rb", "a\r\n\r\nb")]
        [InlineData("a\n\rb", "a\r\n\r\nb")] // LF then CR is two breaks, not one
        [InlineData("\nstart", "\r\nstart")]
        [InlineData("end\n", "end\r\n")]
        [InlineData("no breaks", "no breaks")]
        [InlineData("", "")]
        public void Display_HasEveryLineBreakAsCrLf(string original, string display)
        {
            var text = new TextBoxText(original);
            Assert.Equal(display, text.Display);
            Assert.Equal(original, text.Original);
        }

        [Fact]
        public void Null_Throws() => Assert.Throws<ArgumentNullException>(() => new TextBoxText(null!));

        [Theory]
        [InlineData("plain text")]
        [InlineData("one\r\ntwo\r\nthree")]
        public void TextWithoutLoneBreaks_MapsOneToOne(string original)
        {
            var text = new TextBoxText(original);
            for (int i = 0; i <= original.Length; i++)
            {
                Assert.Equal(i, text.ToOriginal(i));
            }
        }

        // Every non-break character in the display maps back to the same character in the original.
        [Theory]
        [InlineData("first line\nsecond line\nthird")]
        [InlineData("mac\rline\rendings")]
        [InlineData("mixed\nunix\r\nwindows\rmac\n\nend")]
        public void EveryCharacter_MapsBackToItself(string original)
        {
            var text = new TextBoxText(original);
            for (int d = 0; d < text.Display.Length; d++)
            {
                char c = text.Display[d];
                if (c != '\r' && c != '\n')
                {
                    Assert.Equal(c, original[text.ToOriginal(d)]);
                }
            }
            Assert.Equal(original.Length, text.ToOriginal(text.Display.Length));
        }

        [Theory]
        [InlineData("first line\nsecond SECRET line\nthird")]
        [InlineData("first line\rsecond SECRET line\rthird")]
        [InlineData("first line\r\nsecond SECRET line\r\nthird")]
        [InlineData("a\nb\r\nc\rsecond SECRET line\n")]
        public void WordSelection_MapsToTheSameWordInTheOriginal(string original)
        {
            var text = new TextBoxText(original);
            int d = text.Display.IndexOf("SECRET", StringComparison.Ordinal);
            int start = text.ToOriginal(d);
            int end = text.ToOriginal(d + "SECRET".Length);
            Assert.Equal("SECRET", original[start..end]);
        }

        [Theory]
        [InlineData("one\ntwo", "one\ntwo")]
        [InlineData("one\rtwo", "one\rtwo")]
        [InlineData("one\r\ntwo", "one\r\ntwo")]
        public void SelectionAcrossABreak_IncludesTheOriginalBreak(string original, string expected)
        {
            var text = new TextBoxText(original);
            int end = text.ToOriginal(text.Display.Length);
            Assert.Equal(expected, original[text.ToOriginal(0)..end]);
        }

        // Selecting only half of a widened break selects nothing from the original.
        [Theory]
        [InlineData("one\ntwo")]
        [InlineData("one\rtwo")]
        public void PositionInsideAWidenedBreak_MapsToTheBreaksStart(string original)
        {
            var text = new TextBoxText(original);
            Assert.Equal(3, text.ToOriginal(3)); // before the break
            Assert.Equal(3, text.ToOriginal(4)); // between the CR and LF shown
            Assert.Equal(4, text.ToOriginal(5)); // after the break: 't' in the original
        }

        [Fact]
        public void OutOfRangePositions_AreClamped()
        {
            var text = new TextBoxText("a\nb");
            Assert.Equal(0, text.ToOriginal(-5));
            Assert.Equal(3, text.ToOriginal(100));
        }

        [Fact]
        public void LargeUnixFile_MapsTheLastLine()
        {
            string original = string.Join("\n", Enumerable.Range(0, 50_000).Select(i => "line " + i)) + "\nTAIL";
            var text = new TextBoxText(original);
            int d = text.Display.LastIndexOf("TAIL", StringComparison.Ordinal);
            Assert.Equal(original.Length - 4, text.ToOriginal(d));
        }

        // --- Text redaction preview ---------------------------------------------------------------

        private const string UnixText = "first line\nsecond SECRET line\nthird";

        private static void WithPreview(string content, Action<TextRedactionPreviewForm, TextBox> test) => Sta(() =>
        {
            string txtPath = Path.Combine(Path.GetTempPath(), "tbt-" + Guid.NewGuid().ToString("N") + ".txt");
            string dbPath = Path.Combine(Path.GetTempPath(), "tbt-" + Guid.NewGuid().ToString("N") + ".db");
            File.WriteAllText(txtPath, content);
            try
            {
                using var db = new LiteDatabase(dbPath);
                using var form = new TextRedactionPreviewForm(txtPath, new PolicyRepository(db), new ContextRepository(db), new SettingsEntity());
                form.StartPosition = FormStartPosition.Manual;
                form.ShowInTaskbar = false;
                form.Show(); // runs Load, which reads the file into the box
                Application.DoEvents();
                var box = (TextBox)typeof(TextRedactionPreviewForm)
                    .GetField("_originalBox", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;
                test(form, box);
                form.Close();
            }
            finally
            {
                try { File.Delete(dbPath); } catch { /* best effort */ }
                try { File.Delete(txtPath); } catch { /* best effort */ }
            }
        });

        private static void RedactSelection(TextRedactionPreviewForm form) =>
            typeof(TextRedactionPreviewForm).GetMethod("OnRedactSelection", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(form, new object?[] { form, EventArgs.Empty });

        // Asks the native edit control which line each word is drawn on (TextBox.Lines splits in managed
        // code on any line ending, so it can't tell whether the control actually breaks the line).
        [Theory]
        [InlineData(UnixText)]
        [InlineData("first line\rsecond SECRET line\rthird")]
        public void Preview_ShowsEachLineOnItsOwnLine(string content) => WithPreview(content, (_, box) =>
        {
            int Line(string word) => box.GetLineFromCharIndex(box.Text.IndexOf(word, StringComparison.Ordinal));
            Assert.Equal(0, Line("first"));
            Assert.Equal(1, Line("second"));
            Assert.Equal(2, Line("third"));
        });

        [Theory]
        [InlineData(UnixText)]
        [InlineData("first line\rsecond SECRET line\rthird")]
        [InlineData("first line\r\nsecond SECRET line\r\nthird")]
        public void Preview_RedactSelection_SpansTheOriginalText(string content) => WithPreview(content, (form, box) =>
        {
            int d = box.Text.IndexOf("SECRET", StringComparison.Ordinal);
            box.Select(d, "SECRET".Length);
            RedactSelection(form);

            RedactionSpanEntity span = Assert.Single(form.CapturedSpans, s => s.UserAdded);
            Assert.Equal("SECRET", span.Text);
            Assert.Equal(content.IndexOf("SECRET", StringComparison.Ordinal), span.CharacterStart);
            Assert.Equal(span.CharacterStart + 6, span.CharacterEnd);
        });

        [Fact]
        public void Preview_RedactSelectionAcrossLines_KeepsTheOriginalLineBreak() => WithPreview(UnixText, (form, box) =>
        {
            int d = box.Text.IndexOf("line", StringComparison.Ordinal);
            int e = box.Text.IndexOf("second", StringComparison.Ordinal) + "second".Length;
            box.Select(d, e - d);
            RedactSelection(form);

            RedactionSpanEntity span = Assert.Single(form.CapturedSpans, s => s.UserAdded);
            Assert.Equal("line\nsecond", span.Text);
            Assert.Equal(UnixText.IndexOf("line", StringComparison.Ordinal), span.CharacterStart);
        });

        // --- Email redaction preview ----------------------------------------------------------------

        // Two plain-text body parts, each on several lines; written with the given line ending throughout.
        private static string TwoBodyEmail(string nl) => string.Join(nl,
            "From: sender@example.org",
            "To: rcpt@example.org",
            "Subject: hi",
            "MIME-Version: 1.0",
            "Content-Type: multipart/mixed; boundary=\"b1\"",
            "",
            "--b1",
            "Content-Type: text/plain; charset=utf-8",
            "",
            "first body",
            "second SECRET line",
            "--b1",
            "Content-Type: text/plain; charset=utf-8",
            "",
            "other body",
            "has TOKEN here",
            "--b1--",
            "");

        // Outlook .msg bodies keep a lone CR (MIME parsing turns every break in an .eml into CRLF, so
        // only .msg input reaches the box with a break the TextBox won't draw).
        private static void WriteMsg(string path, string body)
        {
            using var email = new MsgKit.Email(new MsgKit.Sender("sender@example.org", "Sender"), "hi") { BodyText = body };
            email.Recipients.AddTo("rcpt@example.org", "Rcpt");
            email.Save(path);
        }

        private const string LoneCrBody = "first body\rsecond SECRET line\rthird";

        private static void WithEmailPreview(string content, Action<EmailRedactionPreviewForm, TextBox, string[]> test) =>
            WithEmailPreview(".eml", path => File.WriteAllText(path, content), test);

        private static void WithMsgPreview(string body, Action<EmailRedactionPreviewForm, TextBox, string[]> test) =>
            WithEmailPreview(".msg", path => WriteMsg(path, body), test);

        private static void WithEmailPreview(string extension, Action<string> write, Action<EmailRedactionPreviewForm, TextBox, string[]> test) => Sta(() =>
        {
            string emlPath = Path.Combine(Path.GetTempPath(), "tbt-" + Guid.NewGuid().ToString("N") + extension);
            string dbPath = Path.Combine(Path.GetTempPath(), "tbt-" + Guid.NewGuid().ToString("N") + ".db");
            write(emlPath);
            try
            {
                using var db = new LiteDatabase(dbPath);
                using var form = new EmailRedactionPreviewForm(emlPath, new PolicyRepository(db), new ContextRepository(db), new SettingsEntity());
                form.StartPosition = FormStartPosition.Manual;
                form.ShowInTaskbar = false;
                form.Show(); // runs Load, which reads the bodies into the box
                Application.DoEvents();
                const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var box = (TextBox)typeof(EmailRedactionPreviewForm).GetField("_originalBox", Flags)!.GetValue(form)!;
                var fields = ((Array)typeof(EmailRedactionPreviewForm).GetField("_fields", Flags)!.GetValue(form)!)
                    .Cast<(string Label, string Text, bool IsBody)>().Select(f => f.Text).ToArray();
                test(form, box, fields);
                form.Close();
            }
            finally
            {
                try { File.Delete(dbPath); } catch { /* best effort */ }
                try { File.Delete(emlPath); } catch { /* best effort */ }
            }
        });

        private static void RedactEmailSelection(EmailRedactionPreviewForm form) =>
            typeof(EmailRedactionPreviewForm).GetMethod("OnRedactSelection", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(form, new object?[] { form, EventArgs.Empty });

        private static void SelectBetween(TextBox box, string from, string throughEndOf)
        {
            int start = box.Text.IndexOf(from, StringComparison.Ordinal);
            int end = box.Text.IndexOf(throughEndOf, start, StringComparison.Ordinal) + throughEndOf.Length;
            box.Select(start, end - start);
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public void EmailPreview_ShowsEachBodyLineOnItsOwnLine(string nl) => WithEmailPreview(TwoBodyEmail(nl), (_, box, _) =>
        {
            int Line(string word) => box.GetLineFromCharIndex(box.Text.IndexOf(word, StringComparison.Ordinal));
            Assert.True(Line("second") > Line("first"), "a body's second line is drawn on its first");
            Assert.True(Line("TOKEN") > Line("other"), "the other body's second line is drawn on its first");
            Assert.True(Line("other") > Line("second"), "the second body starts below the first");
        });

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public void EmailPreview_RedactSelection_SpansTheRightBodyAndOffsets(string nl) => WithEmailPreview(TwoBodyEmail(nl), (form, box, fields) =>
        {
            SelectBetween(box, "TOKEN", "TOKEN");
            RedactEmailSelection(form);

            RedactionSpanEntity span = Assert.Single(form.CapturedSpans, s => s.UserAdded);
            Assert.Equal("TOKEN", span.Text);
            string body = fields[span.ParagraphIndex];
            Assert.Contains("other body", body); // anchored to the second body part's field
            Assert.Equal(body.IndexOf("TOKEN", StringComparison.Ordinal), span.CharacterStart);
            Assert.Equal("TOKEN", body[span.CharacterStart..span.CharacterEnd]);
        });

        [Fact]
        public void EmailPreview_MsgBodyWithLoneCr_ShowsEachLineOnItsOwnLine() => WithMsgPreview(LoneCrBody, (_, box, fields) =>
        {
            Assert.Contains(fields, f => f.Contains("line\rthird")); // the lone CR reaches the preview
            int Line(string word) => box.GetLineFromCharIndex(box.Text.IndexOf(word, StringComparison.Ordinal));
            Assert.True(Line("second") > Line("first"));
            Assert.True(Line("third") > Line("second"));
        });

        [Fact]
        public void EmailPreview_MsgBodyWithLoneCr_RedactSelectionMapsToTheBody() => WithMsgPreview(LoneCrBody, (form, box, fields) =>
        {
            SelectBetween(box, "SECRET", "third");
            RedactEmailSelection(form);

            RedactionSpanEntity span = Assert.Single(form.CapturedSpans, s => s.UserAdded);
            Assert.Equal("SECRET line\rthird", span.Text); // the original lone CR, not the CRLF shown
            Assert.Equal(span.Text, fields[span.ParagraphIndex][span.CharacterStart..span.CharacterEnd]);
        });

        [Fact]
        public void EmailPreview_RedactSelectionAcrossLines_KeepsTheBodysOwnLineBreak() => WithEmailPreview(TwoBodyEmail("\n"), (form, box, fields) =>
        {
            SelectBetween(box, "body", "second");
            RedactEmailSelection(form);

            RedactionSpanEntity span = Assert.Single(form.CapturedSpans, s => s.UserAdded);
            string body = fields[span.ParagraphIndex];
            string expected = body[body.IndexOf("body", StringComparison.Ordinal)..(body.IndexOf("second", StringComparison.Ordinal) + "second".Length)];
            Assert.Equal(expected, span.Text);
            Assert.Equal(expected, body[span.CharacterStart..span.CharacterEnd]);
        });

        [Fact]
        public void EmailPreview_RedactSelectionAcrossBodies_GivesOneSpanPerBody() => WithEmailPreview(TwoBodyEmail("\n"), (form, box, fields) =>
        {
            SelectBetween(box, "SECRET", "other");
            RedactEmailSelection(form);

            List<RedactionSpanEntity> spans = form.CapturedSpans.Where(s => s.UserAdded).OrderBy(s => s.ParagraphIndex).ToList();
            Assert.Equal(2, spans.Count);
            Assert.StartsWith("SECRET", spans[0].Text);
            Assert.Equal("other", spans[1].Text);
            Assert.All(spans, s => Assert.Equal(s.Text, fields[s.ParagraphIndex][s.CharacterStart..s.CharacterEnd]));
        });

        // --- Word redaction preview -----------------------------------------------------------------

        // Word never writes a line break inside w:t, but other tools can, and it reaches the preview as-is.
        private static void WriteDocx(string path, params string[] paragraphs)
        {
            using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            MainDocumentPart main = doc.AddMainDocumentPart();
            main.Document = new W.Document(new W.Body(paragraphs.Select(p =>
                new W.Paragraph(new W.Run(new W.Text(p) { Space = SpaceProcessingModeValues.Preserve })))));
        }

        private static readonly string[] WordParagraphs = { "intro paragraph", "first half\nsecond SECRET half", "closing TOKEN here" };

        private static void WithWordPreview(Action<WordRedactionPreviewForm, TextBox, string[]> test) => Sta(() =>
        {
            string docxPath = Path.Combine(Path.GetTempPath(), "tbt-" + Guid.NewGuid().ToString("N") + ".docx");
            string dbPath = Path.Combine(Path.GetTempPath(), "tbt-" + Guid.NewGuid().ToString("N") + ".db");
            WriteDocx(docxPath, WordParagraphs);
            try
            {
                using var db = new LiteDatabase(dbPath);
                using var form = new WordRedactionPreviewForm(docxPath, new PolicyRepository(db), new ContextRepository(db), new SettingsEntity());
                form.StartPosition = FormStartPosition.Manual;
                form.ShowInTaskbar = false;
                form.Show(); // runs Load, which reads the paragraphs into the box
                Application.DoEvents();
                const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
                var box = (TextBox)typeof(WordRedactionPreviewForm).GetField("_originalBox", Flags)!.GetValue(form)!;
                var paragraphs = (string[])typeof(WordRedactionPreviewForm).GetField("_paragraphs", Flags)!.GetValue(form)!;
                test(form, box, paragraphs);
                form.Close();
            }
            finally
            {
                try { File.Delete(dbPath); } catch { /* best effort */ }
                try { File.Delete(docxPath); } catch { /* best effort */ }
            }
        });

        private static void RedactWordSelection(WordRedactionPreviewForm form) =>
            typeof(WordRedactionPreviewForm).GetMethod("OnRedactSelection", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(form, new object?[] { form, EventArgs.Empty });

        [Fact]
        public void WordPreview_LineFeedInsideAParagraph_IsDrawnAsALineBreak() => WithWordPreview((_, box, paragraphs) =>
        {
            Assert.Contains(paragraphs, p => p.Contains("half\nsecond")); // the LF reaches the preview
            int Line(string word) => box.GetLineFromCharIndex(box.Text.IndexOf(word, StringComparison.Ordinal));
            Assert.Equal(0, Line("intro"));
            Assert.Equal(1, Line("first"));
            Assert.Equal(2, Line("second"));
            Assert.Equal(3, Line("closing"));
        });

        [Fact]
        public void WordPreview_RedactSelectionAfterTheLineFeed_HitsTheRightParagraphAndOffsets() => WithWordPreview((form, box, paragraphs) =>
        {
            SelectBetween(box, "TOKEN", "TOKEN");
            RedactWordSelection(form);

            RedactionSpanEntity span = Assert.Single(form.CapturedSpans, s => s.UserAdded);
            Assert.Equal(2, span.ParagraphIndex);
            Assert.Equal("TOKEN", span.Text);
            Assert.Equal("TOKEN", paragraphs[2][span.CharacterStart..span.CharacterEnd]);
        });

        [Fact]
        public void WordPreview_RedactSelectionAcrossTheLineFeed_KeepsTheOriginalCharacter() => WithWordPreview((form, box, paragraphs) =>
        {
            SelectBetween(box, "half", "SECRET");
            RedactWordSelection(form);

            RedactionSpanEntity span = Assert.Single(form.CapturedSpans, s => s.UserAdded);
            Assert.Equal(1, span.ParagraphIndex);
            Assert.Equal("half\nsecond SECRET", span.Text);
            Assert.Equal(span.Text, paragraphs[1][span.CharacterStart..span.CharacterEnd]);
        });

        [Fact]
        public void WordPreview_RedactSelectionAcrossParagraphs_GivesOneSpanPerParagraph() => WithWordPreview((form, box, paragraphs) =>
        {
            SelectBetween(box, "SECRET", "closing");
            RedactWordSelection(form);

            List<RedactionSpanEntity> spans = form.CapturedSpans.Where(s => s.UserAdded).OrderBy(s => s.ParagraphIndex).ToList();
            Assert.Equal(new[] { 1, 2 }, spans.Select(s => s.ParagraphIndex));
            Assert.Equal("SECRET half", spans[0].Text);
            Assert.Equal("closing", spans[1].Text);
            Assert.All(spans, s => Assert.Equal(s.Text, paragraphs[s.ParagraphIndex][s.CharacterStart..s.CharacterEnd]));
        });
    }
}
