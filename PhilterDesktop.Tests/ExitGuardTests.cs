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
    public class ExitGuardTests
    {
        [Theory]
        [InlineData("Pending", true)]
        [InlineData("Processing", true)]
        [InlineData("pending", true)]   // case-insensitive
        [InlineData("Completed", false)]
        [InlineData("Failed", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsActiveStatus_OnlyPendingOrProcessing(string? status, bool expected)
        {
            Assert.Equal(expected, ExitGuard.IsActiveStatus(status));
        }

        [Fact]
        public void CountActive_CountsOnlyInFlightWork()
        {
            var statuses = new string?[] { "Pending", "Completed", "Processing", "Failed", "Pending" };
            Assert.Equal(3, ExitGuard.CountActive(statuses));
        }

        [Fact]
        public void CountActive_NoneActive_IsZero()
        {
            Assert.Equal(0, ExitGuard.CountActive(new string?[] { "Completed", "Failed" }));
        }

        [Fact]
        public void Message_Singular()
        {
            string message = ExitGuard.Message(1);
            Assert.Contains("1 document is still being redacted", message);
            Assert.Contains("Exit anyway?", message);
        }

        [Fact]
        public void Message_Plural()
        {
            Assert.Contains("3 documents are still being redacted", ExitGuard.Message(3));
        }
    }
}
