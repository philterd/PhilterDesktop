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
using System.Runtime.ExceptionServices;
using PhilterData;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Manual span offsets must be validated: non-negative, start before stop, and within the document
    /// length — and the offset fields must be bounded to the real text (per selected paragraph for Word)
    /// so an out-of-range span can't be added and then silently dropped when applied.
    /// </summary>
    public sealed class SpanEditFormTests
    {
        [Theory]
        [InlineData(0, 5, 100)]    // normal
        [InlineData(0, 100, 100)]  // stop exactly at the end is allowed
        [InlineData(10, 11, 100)]  // minimal valid span
        [InlineData(0, 5, 0)]      // unknown length (0) skips the upper-bound check
        public void ValidOffsets_ReturnNull(int start, int stop, int maxOffset)
        {
            Assert.Null(SpanEditForm.ValidateTextOffsets(start, stop, maxOffset));
        }

        [Theory]
        [InlineData(-1, 5, 100)]   // negative start
        [InlineData(0, -1, 100)]   // negative stop
        public void NegativeOffsets_AreRejected(int start, int stop, int maxOffset)
        {
            Assert.Contains("negative", SpanEditForm.ValidateTextOffsets(start, stop, maxOffset));
        }

        [Theory]
        [InlineData(5, 5, 100)]    // zero-length
        [InlineData(10, 4, 100)]   // inverted
        public void StopNotAfterStart_IsRejected(int start, int stop, int maxOffset)
        {
            Assert.Contains("greater than the start", SpanEditForm.ValidateTextOffsets(start, stop, maxOffset));
        }

        [Theory]
        [InlineData(0, 101, 100)]  // stop past the end
        [InlineData(90, 200, 100)]
        public void StopPastDocumentLength_IsRejected(int start, int stop, int maxOffset)
        {
            Assert.Contains("past the end", SpanEditForm.ValidateTextOffsets(start, stop, maxOffset));
        }

        // --- offset fields are bounded to the real text (guards the silent-drop bug) ---

        private static void OnSta(Action body)
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                try { body(); }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
            })
            { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            captured?.Throw();
        }

        private static NumericUpDown Num(SpanEditForm form, string name) =>
            (NumericUpDown)typeof(SpanEditForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;

        [Fact]
        public void Paragraph_CapsOffsetsToSelectedParagraphLength_AndTracksParagraphChanges()
        {
            OnSta(() =>
            {
                using var form = new SpanEditForm("Add Redaction", SpanPositionKind.Paragraph,
                    new RedactionSpanEntity { UserAdded = true, ParagraphIndex = 0 },
                    positionEditable: true, paragraphLengths: new[] { 5, 20, 3 });
                form.CreateControl();

                // The paragraph field can't exceed the paragraph count, and the offsets are capped at the
                // first paragraph's length.
                Assert.Equal(3m, Num(form, "_paragraph").Maximum);
                Assert.Equal(5m, Num(form, "_start").Maximum);
                Assert.Equal(5m, Num(form, "_stop").Maximum);

                // Selecting another paragraph re-caps the offsets to that paragraph's length.
                Num(form, "_paragraph").Value = 2;
                Assert.Equal(20m, Num(form, "_start").Maximum);
                Assert.Equal(20m, Num(form, "_stop").Maximum);

                Num(form, "_paragraph").Value = 3;
                Assert.Equal(3m, Num(form, "_stop").Maximum);
            });
        }

        [Fact]
        public void TextOffset_CapsOffsetsToDocumentLength()
        {
            OnSta(() =>
            {
                using var form = new SpanEditForm("Add Redaction", SpanPositionKind.TextOffset,
                    new RedactionSpanEntity { UserAdded = true, ParagraphIndex = -1 },
                    positionEditable: true, maxOffset: 50);
                form.CreateControl();

                Assert.Equal(50m, Num(form, "_start").Maximum);
                Assert.Equal(50m, Num(form, "_stop").Maximum);
            });
        }

        [Fact]
        public void Paragraph_SeededOffsetIsClampedIntoTheSelectedParagraph()
        {
            OnSta(() =>
            {
                // A stored span whose stop (99) runs past its short paragraph (length 5) is clamped back
                // into range rather than kept — so re-applying it can't silently miss.
                using var form = new SpanEditForm("Edit Redaction", SpanPositionKind.Paragraph,
                    new RedactionSpanEntity { UserAdded = true, ParagraphIndex = 0, CharacterStart = 0, CharacterEnd = 99 },
                    positionEditable: true, paragraphLengths: new[] { 5 });
                form.CreateControl();

                Assert.True(Num(form, "_stop").Value <= 5m);
            });
        }
    }
}
