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

using PhilterData;
using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Tests the span → redacted-text logic extracted from the preview forms — the correctness-critical
    /// step that turns the (possibly hand-edited) span list into the saved output.
    /// </summary>
    public class RedactionSpanMathTests
    {
        private static RedactionSpanEntity Span(int start, int end, string? replacement = null) =>
            new() { CharacterStart = start, CharacterEnd = end, Replacement = replacement ?? string.Empty };

        [Fact]
        public void ApplySpans_ReplacesEachSpan_WithItsReplacement()
        {
            // "Hello Bob and Sue" -> redact "Bob" (6..9) and "Sue" (14..17)
            string text = "Hello Bob and Sue";
            var spans = new[] { Span(6, 9, "[N1]"), Span(14, 17, "[N2]") };

            string result = RedactionSpanMath.ApplySpans(text, spans, "[X]");

            Assert.Equal("Hello [N1] and [N2]", result);
        }

        [Fact]
        public void ApplySpans_UsesDefaultReplacement_WhenSpanHasNone()
        {
            string result = RedactionSpanMath.ApplySpans("secret here", new[] { Span(0, 6) }, "[REDACTED]");
            Assert.Equal("[REDACTED] here", result);
        }

        [Fact]
        public void ApplySpans_AppliesRightToLeft_SoOffsetsStayValid()
        {
            // Replacements change length; applying left-to-right naively would corrupt later offsets.
            string text = "aaaa bbbb cccc";
            var spans = new[] { Span(0, 4, "X"), Span(5, 9, "YYYYYYYY"), Span(10, 14, "Z") };

            string result = RedactionSpanMath.ApplySpans(text, spans, "[X]");

            Assert.Equal("X YYYYYYYY Z", result);
        }

        [Fact]
        public void ApplySpans_DropsOutOfBoundsAndEmptySpans()
        {
            string text = "short";
            var spans = new[]
            {
                Span(-1, 2, "A"),   // negative start
                Span(2, 99, "B"),   // end past length
                Span(3, 3, "C"),    // empty (end == start)
                Span(0, 2, "OK"),   // valid
            };

            string result = RedactionSpanMath.ApplySpans(text, spans, "[X]");

            Assert.Equal("OKort", result);
        }

        [Fact]
        public void ApplySpans_KeepsEarlierSpan_WhenTwoOverlap()
        {
            // Overlapping spans: the earliest-start one wins; the overlapping one is dropped.
            string text = "0123456789";
            var spans = new[] { Span(2, 6, "[A]"), Span(4, 8, "[B]") };

            string result = RedactionSpanMath.ApplySpans(text, spans, "[X]");

            Assert.Equal("01[A]6789", result);
        }

        [Fact]
        public void ApplySpans_NoSpans_ReturnsOriginal()
        {
            Assert.Equal("unchanged", RedactionSpanMath.ApplySpans("unchanged", System.Array.Empty<RedactionSpanEntity>(), "[X]"));
        }
    }
}
