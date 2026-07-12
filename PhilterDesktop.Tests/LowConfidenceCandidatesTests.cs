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
using Phileas.Policy;
using Phileas.Policy.Filters;
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The "Show low-confidence (not redacted)" scan re-detects with the PhEye threshold lowered and diffs
    /// against the normal scan. These guard the two pure helpers that underpin it.
    /// </summary>
    public sealed class LowConfidenceCandidatesTests
    {
        private static void LowerPhEyeThresholds(PhileasPolicy policy)
        {
            MethodInfo m = typeof(ModifyRedactionForm).GetMethod("LowerPhEyeThresholds", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("LowerPhEyeThresholds not found");
            m.Invoke(null, new object[] { policy });
        }

        private static string SpanKey(RedactionSpanEntity s)
        {
            MethodInfo m = typeof(ModifyRedactionForm).GetMethod("SpanKey", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("SpanKey not found");
            return (string)m.Invoke(null, new object[] { s })!;
        }

        [Theory]
        [InlineData(0.5, 0.25)]   // a margin (0.25) below the filter's own threshold
        [InlineData(0.7, 0.45)]
        [InlineData(0.3, 0.10)]   // clamped up to the safety floor
        [InlineData(0.15, 0.10)]
        public void LowerPhEyeThresholds_ScansRelativeToEachFilterThreshold(double original, double expected)
        {
            var policy = new PhileasPolicy
            {
                Name = "p",
                Identifiers = new Identifiers
                {
                    PhEyes = new List<PhEye>
                    {
                        new()
                        {
                            PhEyeConfiguration = new PhEyeConfiguration { Threshold = original },
                            Thresholds = new Dictionary<string, double> { ["PERSON"] = 0.7 }
                        }
                    }
                }
            };

            LowerPhEyeThresholds(policy);

            PhEye p = policy.Identifiers.PhEyes![0];
            Assert.Equal(expected, p.PhEyeConfiguration!.Threshold, precision: 3); // relative, floor-clamped
            Assert.Empty(p.Thresholds);                                            // per-label gates dropped
        }

        [Fact]
        public void LowerPhEyeThresholds_NeverStricterThanTheRealThreshold()
        {
            // A filter whose threshold is already at/below the safety floor must not be made stricter, or the
            // lowered scan would find fewer than the normal one and the diff would be meaningless.
            var policy = new PhileasPolicy
            {
                Name = "p",
                Identifiers = new Identifiers
                {
                    PhEyes = new List<PhEye> { new() { PhEyeConfiguration = new PhEyeConfiguration { Threshold = 0.05 } } }
                }
            };

            LowerPhEyeThresholds(policy);

            Assert.Equal(0.05, policy.Identifiers.PhEyes![0].PhEyeConfiguration!.Threshold, precision: 3);
        }

        [Fact]
        public void SpanKey_EqualForSameSpan_DiffersByPositionOrText()
        {
            var a = new RedactionSpanEntity { CharacterStart = 0, CharacterEnd = 4, ParagraphIndex = 2, Text = "John" };
            var same = new RedactionSpanEntity { CharacterStart = 0, CharacterEnd = 4, ParagraphIndex = 2, Text = "John" };
            var otherText = new RedactionSpanEntity { CharacterStart = 0, CharacterEnd = 4, ParagraphIndex = 2, Text = "Jane" };
            var otherPos = new RedactionSpanEntity { CharacterStart = 5, CharacterEnd = 9, ParagraphIndex = 2, Text = "John" };

            Assert.Equal(SpanKey(a), SpanKey(same));
            Assert.NotEqual(SpanKey(a), SpanKey(otherText));
            Assert.NotEqual(SpanKey(a), SpanKey(otherPos));
        }
    }
}
