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

using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Manual span offsets must be validated: non-negative, start before stop, and within the document
    /// length.
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
    }
}
