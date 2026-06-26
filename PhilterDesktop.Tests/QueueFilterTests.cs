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
    public class QueueFilterTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Matches_BlankQuery_MatchesEverything(string? query)
        {
            Assert.True(QueueFilter.Matches(query, @"C:\docs\report.txt", "Completed", "default", "default"));
        }

        [Fact]
        public void Matches_FileName_CaseInsensitiveSubstring()
        {
            Assert.True(QueueFilter.Matches("REPORT", @"C:\docs\Report.txt", "Pending", "p", "c"));
            Assert.False(QueueFilter.Matches("invoice", @"C:\docs\Report.txt", "Pending", "p", "c"));
        }

        [Fact]
        public void Matches_AnyField_Status_Policy_Or_Context()
        {
            Assert.True(QueueFilter.Matches("failed", @"a.txt", "Failed", "p", "c"));
            Assert.True(QueueFilter.Matches("medical", @"a.txt", "Completed", "medical-policy", "c"));
            Assert.True(QueueFilter.Matches("case42", @"a.txt", "Completed", "p", "case42"));
        }

        [Fact]
        public void Matches_QueryIsTrimmed()
        {
            Assert.True(QueueFilter.Matches("  report  ", @"C:\docs\report.txt", "Pending", "p", "c"));
        }

        [Fact]
        public void Matches_IgnoresNullOrEmptyFields()
        {
            Assert.False(QueueFilter.Matches("x", null, "", null));
        }
    }
}
