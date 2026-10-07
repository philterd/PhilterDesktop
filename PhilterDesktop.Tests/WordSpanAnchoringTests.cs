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

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Phileas.Services.Office;
using PhilterData;
using Xunit;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Saved Word spans are checked against the source's current paragraph text before Modify Redaction
    /// re-applies them, so a span whose offsets no longer line up is moved to its text or refused,
    /// never applied to the wrong characters.
    /// </summary>
    public sealed class WordSpanAnchoringTests
    {
        private static OfficeRedactionSpan Span(int paragraph, int start, int end, string text) =>
            new() { ParagraphIndex = paragraph, CharacterStart = start, CharacterEnd = end, Text = text, Replacement = "[R]" };

        private static WordSpanAnchoring.Result Anchor(string paragraph, params OfficeRedactionSpan[] spans) =>
            WordSpanAnchoring.Anchor(new[] { paragraph }, spans);

        // --- Anchor: spans that still line up -------------------------------------------------------

        [Fact]
        public void SpanStillAtItsText_IsKeptUnchanged()
        {
            OfficeRedactionSpan span = Span(0, 9, 25, "john@example.com");
            WordSpanAnchoring.Result result = Anchor("Write to john@example.com today.", span);

            Assert.Empty(result.Moved);
            Assert.Empty(result.Unmatched);
            Assert.Equal((9, 25, "john@example.com"), (span.CharacterStart, span.CharacterEnd, span.Text));
        }

        [Fact]
        public void SpanWithoutAParagraph_IsLeftAlone()
        {
            OfficeRedactionSpan span = Span(-1, 0, 4, "text in a shape");
            WordSpanAnchoring.Result result = Anchor("Anything", span);

            Assert.Empty(result.Moved);
            Assert.Empty(result.Unmatched);
            Assert.Equal((0, 4), (span.CharacterStart, span.CharacterEnd));
        }

        [Fact]
        public void SpanWithNoSavedText_InBounds_IsKept()
        {
            OfficeRedactionSpan span = Span(0, 0, 5, string.Empty);
            WordSpanAnchoring.Result result = Anchor("Hello world", span);

            Assert.Empty(result.Moved);
            Assert.Empty(result.Unmatched);
        }

        // --- Anchor: spans saved by Phileas 1.6.0 (paragraph text without breaks, tabs, etc.) -----------

        [Fact]
        public void LineBreakBeforeTheValue_MovesTheSpanRight()
        {
            // 1.6.0 read "Case notesWrite to john@example.com"; the email started at 19.
            OfficeRedactionSpan span = Span(0, 19, 35, "john@example.com");
            WordSpanAnchoring.Result result = Anchor("Case notes\nWrite to john@example.com today.", span);

            Assert.Same(span, Assert.Single(result.Moved));
            Assert.Equal((20, 36, "john@example.com"), (span.CharacterStart, span.CharacterEnd, span.Text));
        }

        [Fact]
        public void SeveralInsertedCharactersBefore_MoveTheSpanByTheirCount()
        {
            const string legacy = "ABCD email x@y.com";
            const string current = "A\nB\tC-D email x@y.com"; // a break, a tab and a non-breaking hyphen added
            OfficeRedactionSpan span = Span(0, legacy.IndexOf("x@y.com"), legacy.IndexOf("x@y.com") + 7, "x@y.com");

            Anchor(current, span);

            Assert.Equal("x@y.com", current[span.CharacterStart..span.CharacterEnd]);
            Assert.Equal(current.IndexOf("x@y.com"), span.CharacterStart);
        }

        [Fact]
        public void BreakInsideTheValue_IsCoveredByTheMovedSpan()
        {
            // 1.6.0 joined the lines, so the span ran on into "SSN" on the next line.
            OfficeRedactionSpan span = Span(0, 6, 25, "john@example.comSSN");
            WordSpanAnchoring.Result result = Anchor("Email john@example.com\nSSN 123-45-6789", span);

            Assert.Single(result.Moved);
            Assert.Equal((6, 26, "john@example.com\nSSN"), (span.CharacterStart, span.CharacterEnd, span.Text));
        }

        [Fact]
        public void NonBreakingHyphensInsideTheValue_AreCovered()
        {
            // 1.6.0 dropped w:noBreakHyphen, reading "SSN 123456789".
            OfficeRedactionSpan span = Span(0, 4, 13, "123456789");
            Anchor("SSN 123-45-6789 on file.", span);

            Assert.Equal((4, 15, "123-45-6789"), (span.CharacterStart, span.CharacterEnd, span.Text));
        }

        [Fact]
        public void ExtraSpaceFromASymbolCharacter_IsAllowedInsideTheValue()
        {
            OfficeRedactionSpan span = Span(0, 5, 15, "John Smith");
            Anchor("Name John  Smith here", span); // a w:sym became the second space

            Assert.Equal((5, 16, "John  Smith"), (span.CharacterStart, span.CharacterEnd, span.Text));
        }

        [Fact]
        public void TheOccurrenceConsistentWithTheShift_IsChosen_WhenTheTextRepeats()
        {
            // 1.6.0 text "John met John"; the first John at 0. Now a tab precedes it.
            OfficeRedactionSpan span = Span(0, 0, 4, "John");
            Anchor("\tJohn met John", span);

            Assert.Equal((1, 5), (span.CharacterStart, span.CharacterEnd));
        }

        // --- Anchor: spans that can't be placed ----------------------------------------------------------

        [Fact]
        public void TextNoLongerInTheParagraph_IsUnmatched()
        {
            OfficeRedactionSpan span = Span(0, 9, 25, "john@example.com");
            WordSpanAnchoring.Result result = Anchor("Write to someone@else.org today.", span);

            Assert.Same(span, Assert.Single(result.Unmatched));
            Assert.Equal((9, 25), (span.CharacterStart, span.CharacterEnd)); // not changed
        }

        [Fact]
        public void ParagraphIndexBeyondTheDocument_IsUnmatched()
        {
            OfficeRedactionSpan span = Span(3, 0, 4, "John");
            Assert.Single(Anchor("John", span).Unmatched);
        }

        [Theory]
        [InlineData(0, 50)]
        [InlineData(-1, 3)]
        [InlineData(4, 4)]
        public void OutOfBoundsSpanWithNoSavedText_IsUnmatched(int start, int end)
        {
            OfficeRedactionSpan span = Span(0, start, end, string.Empty);
            Assert.Single(Anchor("Short", span).Unmatched);
        }

        [Fact]
        public void OutOfBoundsSpanWhoseTextIsPresent_IsMovedToIt()
        {
            OfficeRedactionSpan span = Span(0, 40, 44, "John");
            WordSpanAnchoring.Result result = Anchor("Hello John", span);

            Assert.Single(result.Moved);
            Assert.Equal((6, 10), (span.CharacterStart, span.CharacterEnd));
        }

        [Fact]
        public void TextThatMovedInAnEditedParagraph_IsFound_WhenItOccursOnce()
        {
            OfficeRedactionSpan span = Span(0, 0, 4, "John");
            Anchor("Earlier we wrote to John.", span);
            Assert.Equal((20, 24), (span.CharacterStart, span.CharacterEnd));
        }

        [Fact]
        public void TextThatOccursMoreThanOnce_WithNoConsistentCandidate_IsUnmatched()
        {
            OfficeRedactionSpan span = Span(0, 20, 22, "AB");
            Assert.Single(Anchor("AB x AB", span).Unmatched);
        }

        [Fact]
        public void InsertedCharactersAreNotSkippedBeforeTheValue()
        {
            // A match must start on the value's first character, not on a preceding break.
            Assert.Equal((1, 5), WordSpanAnchoring.Locate("\nJohn", "John", 0));
        }

        [Fact]
        public void OtherCharactersInsideTheValue_PreventAMatch()
        {
            Assert.Null(WordSpanAnchoring.Locate("Jo_hn", "John", 0));
        }

        [Fact]
        public void MixedSpans_AreSortedIntoTheRightLists()
        {
            OfficeRedactionSpan kept = Span(0, 0, 5, "Alpha");
            OfficeRedactionSpan moved = Span(1, 0, 4, "Beta");
            OfficeRedactionSpan unmatched = Span(1, 5, 10, "Gamma");
            OfficeRedactionSpan shape = Span(-1, 0, 1, "x");

            WordSpanAnchoring.Result result = WordSpanAnchoring.Anchor(new[] { "Alpha one", "\nBeta two" }, new[] { kept, moved, unmatched, shape });

            Assert.Equal(new[] { moved }, result.Moved);
            Assert.Equal(new[] { unmatched }, result.Unmatched);
            Assert.Equal((1, 5), (moved.CharacterStart, moved.CharacterEnd));
        }

        // --- Re-applying through RedactionService (Modify Redaction's path) ----------------------------

        private sealed class TempDir : IDisposable
        {
            public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wsa-" + Guid.NewGuid().ToString("N"));
            public TempDir() => Directory.CreateDirectory(Path);
            public string File(string name) => System.IO.Path.Combine(Path, name);
            public void Dispose() { try { Directory.Delete(Path, true); } catch { /* best effort */ } }
        }

        private static void WriteDocx(string path, params string[] paragraphs)
        {
            using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            doc.AddMainDocumentPart().Document = new W.Document(new W.Body(paragraphs.Select(p =>
                new W.Paragraph(new W.Run(new W.Text(p) { Space = SpaceProcessingModeValues.Preserve })))));
        }

        private static string BodyText(string path)
        {
            using var doc = WordprocessingDocument.Open(path, false);
            return string.Join(" / ", doc.MainDocumentPart!.Document.Body!.Elements<W.Paragraph>().Select(p => p.InnerText));
        }

        private static RedactionSpanEntity Entity(int paragraph, int start, int end, string text) => new()
        {
            ParagraphIndex = paragraph, CharacterStart = start, CharacterEnd = end, Text = text, Replacement = "[REDACTED]"
        };

        [Fact]
        public async Task Reapply_UnchangedSource_RedactsAsSaved()
        {
            using var dir = new TempDir();
            WriteDocx(dir.File("in.docx"), "Write to john@example.com today.");

            await RedactionService.ApplySpansAsync(dir.File("in.docx"), dir.File("out.docx"), ".docx", false,
                new[] { Entity(0, 9, 25, "john@example.com") });

            Assert.Equal("Write to [REDACTED] today.", BodyText(dir.File("out.docx")));
        }

        [Fact]
        public async Task Reapply_ShiftedSpan_RedactsTheWholeValue_NotTheCharacterBeforeIt()
        {
            // Without the check, a span one character early redacts " john@example.co" and leaves "m".
            using var dir = new TempDir();
            WriteDocx(dir.File("in.docx"), "Write to john@example.com today.");

            await RedactionService.ApplySpansAsync(dir.File("in.docx"), dir.File("out.docx"), ".docx", false,
                new[] { Entity(0, 8, 24, "john@example.com") });

            Assert.Equal("Write to [REDACTED] today.", BodyText(dir.File("out.docx")));
        }

        [Fact]
        public async Task Reapply_EditedSource_ThrowsBeforeWritingAnything()
        {
            using var dir = new TempDir();
            WriteDocx(dir.File("in.docx"), "The address was removed from this paragraph.");
            var saved = new[] { Entity(0, 9, 25, "john@example.com") };

            StaleRedactionSpansException ex = await Assert.ThrowsAsync<StaleRedactionSpansException>(() =>
                RedactionService.ApplySpansAsync(dir.File("in.docx"), dir.File("out.docx"), ".docx", false, saved));

            Assert.False(File.Exists(dir.File("out.docx")));
            Assert.Equal("john@example.com", Assert.Single(ex.Unmatched).Text);
            Assert.Equal((9, 25), (saved[0].CharacterStart, saved[0].CharacterEnd)); // saved spans untouched
        }

        [Fact]
        public async Task Reapply_SpanForAParagraphThatNoLongerExists_ThrowsBeforeWriting()
        {
            using var dir = new TempDir();
            WriteDocx(dir.File("in.docx"), "Only one paragraph now.");

            await Assert.ThrowsAsync<StaleRedactionSpansException>(() =>
                RedactionService.ApplySpansAsync(dir.File("in.docx"), dir.File("out.docx"), ".docx", false,
                    new[] { Entity(2, 0, 4, "John") }));
            Assert.False(File.Exists(dir.File("out.docx")));
        }

        [Fact]
        public async Task Reapply_SpansWithoutAParagraph_DoNotBlock()
        {
            using var dir = new TempDir();
            WriteDocx(dir.File("in.docx"), "Write to john@example.com today.");

            await RedactionService.ApplySpansAsync(dir.File("in.docx"), dir.File("out.docx"), ".docx", false,
                new[] { Entity(0, 9, 25, "john@example.com"), Entity(-1, 0, 5, "chart") });

            Assert.Equal("Write to [REDACTED] today.", BodyText(dir.File("out.docx")));
        }

        // --- Modify Redaction helpers -------------------------------------------------------------

        [Theory]
        [InlineData(0, 6, 10, "John")]
        [InlineData(1, 0, 3, "Two")]
        [InlineData(2, 0, 3, "")]   // no such paragraph
        [InlineData(0, 6, 99, "")]  // past the end
        [InlineData(0, 6, 6, "")]   // empty range
        [InlineData(-1, 0, 3, "")]  // not a paragraph
        public void ParagraphSlice_ReturnsTheCoveredText_OrEmpty(int paragraph, int start, int stop, string expected) =>
            Assert.Equal(expected, ModifyRedactionForm.ParagraphSlice(new[] { "Hello John", "Two lines" }, paragraph, start, stop));

        [Fact]
        public void ParagraphSlice_WithoutParagraphs_IsEmpty() =>
            Assert.Equal(string.Empty, ModifyRedactionForm.ParagraphSlice(null, 0, 0, 1));

        [Fact]
        public void StaleSpansMessage_ListsTheRedactions_AndSaysNothingWasWritten()
        {
            var ex = new StaleRedactionSpansException(new[] { Span(0, 9, 25, "john@example.com"), Span(2, 3, 7, string.Empty) });

            string message = ModifyRedactionForm.StaleSpansMessage(ex);

            Assert.StartsWith("2 saved redactions no longer match the original document.", message);
            Assert.Contains("No new redacted copy was written.", message);
            Assert.Contains("Paragraph 1: \"john@example.com\"", message);
            Assert.Contains("Paragraph 3, characters 3-7", message);
            Assert.DoesNotContain("—", message); // no em-dashes in UI text
        }

        [Fact]
        public void StaleSpansMessage_ShowsFiveAndCountsTheRest()
        {
            var spans = Enumerable.Range(0, 8).Select(i => Span(i, 0, 4, "T" + i)).ToArray();
            string message = ModifyRedactionForm.StaleSpansMessage(new StaleRedactionSpansException(spans));

            Assert.Contains("\"T4\"", message);
            Assert.DoesNotContain("\"T5\"", message);
            Assert.Contains("and 3 more", message);
        }

        [Fact]
        public void StaleSpansMessage_UsesSingularForOne()
        {
            string message = ModifyRedactionForm.StaleSpansMessage(new StaleRedactionSpansException(new[] { Span(0, 0, 4, "John") }));
            Assert.StartsWith("1 saved redaction no longer matches the original document.", message);
        }
    }
}
