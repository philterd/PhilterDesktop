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
using PhilterData;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The Modify Redaction list shows each detection's engine confidence as a percentage; user-added
    /// spans (and any span with no recorded confidence) show nothing.
    /// </summary>
    public sealed class ConfidenceTextTests
    {
        private static string ConfidenceText(RedactionSpanEntity s)
        {
            MethodInfo m = typeof(ModifyRedactionForm).GetMethod("ConfidenceText", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("ConfidenceText not found");
            return (string)m.Invoke(null, new object[] { s })!;
        }

        [Theory]
        [InlineData(0.97, "97%")]
        [InlineData(0.5, "50%")]
        [InlineData(0.874, "87%")]
        [InlineData(1.0, "100%")]
        public void Detection_ShowsPercentage(double confidence, string expected)
        {
            Assert.Equal(expected, ConfidenceText(new RedactionSpanEntity { Confidence = confidence }));
        }

        [Fact]
        public void UserAddedSpan_ShowsBlank()
        {
            // A user-added term carries no detection, so its confidence cell stays empty even if a value slipped in.
            Assert.Equal(string.Empty, ConfidenceText(new RedactionSpanEntity { UserAdded = true, Confidence = 0.9 }));
        }

        [Fact]
        public void ZeroOrUnsetConfidence_ShowsBlank()
        {
            Assert.Equal(string.Empty, ConfidenceText(new RedactionSpanEntity { Confidence = 0 }));
        }
    }
}
