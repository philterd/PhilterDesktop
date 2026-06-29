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

using System.Text.RegularExpressions;
using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>Tests the ReDoS guard: the process-wide regex timeout and custom-pattern validation.</summary>
    public sealed class RegexSafetyTests
    {
        [Fact]
        public void InstallDefaultMatchTimeout_SetsAppDomainDefaultToFiveSeconds()
        {
            RegexSafety.InstallDefaultMatchTimeout();

            object? value = AppDomain.CurrentDomain.GetData("REGEX_DEFAULT_MATCH_TIMEOUT_MS");
            var timeout = Assert.IsType<TimeSpan>(value);
            Assert.Equal(RegexSafety.DefaultMatchTimeout, timeout);
            Assert.Equal(TimeSpan.FromSeconds(5), timeout);
        }

        [Theory]
        [InlineData(@"\d{3}-\d{2}-\d{4}")]   // a normal pattern
        [InlineData(@"(a+)+$")]              // valid syntax even though it backtracks badly
        [InlineData("")]                      // empty is allowed (no-op)
        [InlineData(null)]
        public void IsValidPattern_AcceptsValidOrEmpty(string? pattern)
        {
            Assert.True(RegexSafety.IsValidPattern(pattern, out string? error));
            Assert.Null(error);
        }

        [Theory]
        [InlineData("(unclosed")]
        [InlineData("a{2,1}")]               // invalid quantifier range
        [InlineData("[z-a]")]                // invalid character range
        public void IsValidPattern_RejectsInvalidSyntax(string pattern)
        {
            Assert.False(RegexSafety.IsValidPattern(pattern, out string? error));
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Fact]
        public void KnownReDoSPattern_TimesOut_RatherThanHanging()
        {
            // Demonstrates the danger and the cure: a catastrophic-backtracking pattern against an input
            // that forces backtracking aborts at the timeout instead of running unbounded. (Uses an
            // explicit short timeout so the test is fast and deterministic.)
            var regex = new Regex("(a+)+$", RegexOptions.None, TimeSpan.FromMilliseconds(100));
            string adversarial = new string('a', 40) + "!";

            Assert.Throws<RegexMatchTimeoutException>(() => regex.IsMatch(adversarial));
        }
    }
}
