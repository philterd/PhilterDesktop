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
    public class QueueSummaryTests
    {
        [Fact]
        public void Describe_Empty_SaysNoDocuments()
        {
            Assert.Equal("No documents in queue", QueueSummary.Describe(0, new Dictionary<string, int>()));
        }

        [Fact]
        public void Describe_SingleFile_UsesSingularAndStatus()
        {
            var counts = new Dictionary<string, int> { ["Completed"] = 1 };
            Assert.Equal("1 file  ·  1 completed", QueueSummary.Describe(1, counts));
        }

        [Fact]
        public void Describe_ListsStatusesInWorkflowOrder_AndOmitsZeros()
        {
            var counts = new Dictionary<string, int>
            {
                ["Completed"] = 1,
                ["Pending"] = 2,
                ["Failed"] = 3,
                // no "Processing" -> omitted
            };

            Assert.Equal("6 files  ·  2 pending  ·  1 completed  ·  3 failed", QueueSummary.Describe(6, counts));
        }

        [Fact]
        public void DescribeFilter_ShowsShownOfTotal()
        {
            Assert.Equal("Showing 2 of 5 documents", QueueSummary.DescribeFilter(2, 5));
        }

        [Fact]
        public void DescribeFilter_SingularTotal()
        {
            Assert.Equal("Showing 1 of 1 document", QueueSummary.DescribeFilter(1, 1));
        }
    }
}
