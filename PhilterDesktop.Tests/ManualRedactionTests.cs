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

using PhilterData;
using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Tests the manual "redact selection" core: a reviewer's text selection becomes a user-added span
    /// with the right offsets, and applying it on save actually removes the selected text.
    /// </summary>
    public sealed class ManualRedactionTests
    {
        private const string Text = "Call John Smith today.";

        [Fact]
        public void FromSelection_ValidRange_BuildsUserAddedSpan()
        {
            // "John Smith" starts at index 5, length 10.
            RedactionSpanEntity? span = ManualRedaction.FromSelection(Text, 5, 10);

            Assert.NotNull(span);
            Assert.True(span!.UserAdded);
            Assert.Equal(-1, span.ParagraphIndex);          // whole-text offset model
            Assert.Equal(5, span.CharacterStart);
            Assert.Equal(15, span.CharacterEnd);
            Assert.Equal("John Smith", span.Text);
            Assert.False(string.IsNullOrEmpty(span.Replacement));
        }

        [Fact]
        public void FromSelection_AppliesOnSave_RemovesSelectedText()
        {
            RedactionSpanEntity span = ManualRedaction.FromSelection(Text, 5, 10)!;

            string redacted = RedactionSpanMath.ApplySpans(Text, new[] { span }, RedactionService.DefaultReplacement);

            Assert.DoesNotContain("John Smith", redacted);
            Assert.StartsWith("Call ", redacted);
            Assert.EndsWith(" today.", redacted);
        }

        [Fact]
        public void FromSelection_ClampsToTextLength()
        {
            // Selection length runs past the end of the text — it should clamp, not throw.
            RedactionSpanEntity? span = ManualRedaction.FromSelection(Text, 16, 999);

            Assert.NotNull(span);
            Assert.Equal(Text.Length, span!.CharacterEnd);
            Assert.Equal("today.", span.Text);
        }

        [Fact]
        public void RtfSelection_AppliesOnSave_RemovesSelectedText_PreservesRtf()
        {
            // The shared preview form uses this same path for .rtf: selection offsets index into the
            // visible text, and RtfRedactor.ApplySpans re-applies them while preserving formatting.
            string input = Path.Combine(Path.GetTempPath(), "philter-rtfsel-" + Guid.NewGuid().ToString("N") + ".rtf");
            string output = Path.Combine(Path.GetTempPath(), "philter-rtfsel-" + Guid.NewGuid().ToString("N") + ".rtf");
            File.WriteAllText(input, @"{\rtf1\ansi Call John Smith today.}");
            try
            {
                string visible = RtfRedactor.ReadText(input);
                int idx = visible.IndexOf("John Smith", StringComparison.Ordinal);
                Assert.True(idx >= 0);

                RedactionSpanEntity span = ManualRedaction.FromSelection(visible, idx, "John Smith".Length)!;
                RtfRedactor.ApplySpans(input, output, new[] { span });

                string redacted = RtfRedactor.ReadText(output);
                Assert.DoesNotContain("John Smith", redacted);
                Assert.Contains("Call", redacted);
                Assert.Contains("today", redacted);
            }
            finally
            {
                try { File.Delete(input); } catch { /* best effort */ }
                try { File.Delete(output); } catch { /* best effort */ }
            }
        }

        [Fact]
        public void FromParagraphSelection_WithinOneParagraph_BuildsOneSpan()
        {
            var paragraphs = new[] { "Alpha bravo", "Charlie delta" };

            List<RedactionSpanEntity> spans = ManualRedaction.FromParagraphSelection(paragraphs, 6, 5, 2); // "bravo"

            RedactionSpanEntity span = Assert.Single(spans);
            Assert.True(span.UserAdded);
            Assert.Equal(0, span.ParagraphIndex);
            Assert.Equal(6, span.CharacterStart);
            Assert.Equal(11, span.CharacterEnd);
            Assert.Equal("bravo", span.Text);
        }

        [Fact]
        public void FromParagraphSelection_AcrossParagraphs_BuildsOneSpanPerParagraph()
        {
            // Joined as "Alpha bravo\r\nCharlie delta"; select from "bravo" (6) through "Charlie".
            var paragraphs = new[] { "Alpha bravo", "Charlie delta" };

            List<RedactionSpanEntity> spans = ManualRedaction.FromParagraphSelection(paragraphs, 6, 14, 2);

            Assert.Equal(2, spans.Count);
            Assert.Equal(0, spans[0].ParagraphIndex);
            Assert.Equal("bravo", spans[0].Text);
            Assert.Equal(1, spans[1].ParagraphIndex);
            Assert.Equal(0, spans[1].CharacterStart);
            Assert.Equal("Charlie", spans[1].Text);
        }

        [Fact]
        public void FromParagraphSelection_EmptyOrInvalid_ReturnsEmpty()
        {
            Assert.Empty(ManualRedaction.FromParagraphSelection(new[] { "abc" }, 0, 0, 2));
            Assert.Empty(ManualRedaction.FromParagraphSelection(new[] { "abc" }, -1, 2, 2));
        }

        [Fact]
        public void WordSelection_AppliesOnSave_RemovesSelectedText()
        {
            string input = Path.Combine(Path.GetTempPath(), "philter-wordsel-" + Guid.NewGuid().ToString("N") + ".docx");
            string output = Path.Combine(Path.GetTempPath(), "philter-wordsel-" + Guid.NewGuid().ToString("N") + ".docx");
            WordDocs.Create(input, "Email alice here.", "Call Bob now.");
            try
            {
                string[] paragraphs = WordDocumentRedactor.ReadParagraphs(input).ToArray();
                string joined = string.Join(Environment.NewLine, paragraphs);
                int idx = joined.IndexOf("alice", StringComparison.Ordinal);
                Assert.True(idx >= 0);

                List<RedactionSpanEntity> spans =
                    ManualRedaction.FromParagraphSelection(paragraphs, idx, "alice".Length, Environment.NewLine.Length);
                WordDocumentRedactor.ApplySpans(input, output, PhilterDesktop.OfficeSpanMapping.ToOfficeSpans(spans), highlight: false);

                string allText = string.Concat(WordDocs.BodyParagraphs(output));
                Assert.DoesNotContain("alice", allText);
                Assert.Contains("Call Bob now.", allText); // the other paragraph is untouched
            }
            finally
            {
                try { File.Delete(input); } catch { /* best effort */ }
                try { File.Delete(output); } catch { /* best effort */ }
            }
        }

        [Theory]
        [InlineData(5, 0)]    // empty selection
        [InlineData(-1, 4)]   // negative start
        [InlineData(100, 4)]  // start beyond the text
        public void FromSelection_InvalidSelection_ReturnsNull(int start, int length)
        {
            Assert.Null(ManualRedaction.FromSelection(Text, start, length));
        }
    }
}
