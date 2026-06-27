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

using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    public sealed class DurationFormatTests
    {
        [Theory]
        [InlineData(0, "—")]
        [InlineData(-5, "—")]
        [InlineData(1, "1 ms")]
        [InlineData(850, "850 ms")]
        [InlineData(999, "999 ms")]
        [InlineData(1000, "1.00 s")]
        [InlineData(1500, "1.50 s")]
        [InlineData(12345, "12.35 s")]
        public void Humanize_FormatsByMagnitude(long milliseconds, string expected)
        {
            Assert.Equal(expected, DurationFormat.Humanize(milliseconds));
        }
    }
}
