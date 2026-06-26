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
    /// <summary>
    /// Covers the pure comparison the queue column sorter uses: alphabetical for normal columns,
    /// workflow order for the Status column.
    /// </summary>
    public class QueueColumnSorterTests
    {
        [Fact]
        public void CompareValues_TextColumn_IsCaseInsensitiveAlphabetical()
        {
            Assert.True(QueueColumnSorter.CompareValues("apple.txt", "Banana.txt", 0) < 0);
            Assert.True(QueueColumnSorter.CompareValues("Banana.txt", "apple.txt", 0) > 0);
            Assert.Equal(0, QueueColumnSorter.CompareValues("File.TXT", "file.txt", 0));
        }

        [Fact]
        public void CompareValues_StatusColumn_OrdersByWorkflowNotAlphabet()
        {
            // Alphabetically "Completed" < "Pending" < "Processing", but workflow order is
            // Pending -> Processing -> Completed -> Failed.
            Assert.True(QueueColumnSorter.CompareValues("Pending", "Processing", 1) < 0);
            Assert.True(QueueColumnSorter.CompareValues("Processing", "Completed", 1) < 0);
            Assert.True(QueueColumnSorter.CompareValues("Completed", "Failed", 1) < 0);
            Assert.True(QueueColumnSorter.CompareValues("Failed", "Pending", 1) > 0);
        }

        [Fact]
        public void CompareValues_StatusColumn_IsCaseInsensitive()
        {
            Assert.Equal(0, QueueColumnSorter.CompareValues("pending", "Pending", 1));
        }

        [Fact]
        public void CompareValues_StatusColumn_UnknownStatusSortsLast()
        {
            Assert.True(QueueColumnSorter.CompareValues("Failed", "Mystery", 1) < 0);
            Assert.True(QueueColumnSorter.CompareValues("Mystery", "Pending", 1) > 0);
        }

        [Fact]
        public void Comparer_RespectsAscendingAndDescending()
        {
            var sorter = new QueueColumnSorter { Column = 0, Ascending = true };
            var a = new ListViewItem("a.txt");
            var b = new ListViewItem("b.txt");

            Assert.True(sorter.Compare(a, b) < 0);

            sorter.Ascending = false;
            Assert.True(sorter.Compare(a, b) > 0);
        }

        [Fact]
        public void Comparer_FallsBackToFileNameForEqualStatus()
        {
            // Two rows with the same status sort by file name (column 0) so order is deterministic.
            var sorter = new QueueColumnSorter { Column = 1, Ascending = true };
            var first = MakeRow("alpha.txt", "Completed");
            var second = MakeRow("beta.txt", "Completed");

            Assert.True(sorter.Compare(first, second) < 0);
            Assert.True(sorter.Compare(second, first) > 0);
        }

        private static ListViewItem MakeRow(string name, string status)
        {
            var item = new ListViewItem(name);
            item.SubItems.Add(status);
            return item;
        }
    }
}
