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

using System.Collections;
using System.Reflection;
using PhilterData;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The Modify Redaction list sorts by clicked column: numeric columns (confidence, character
    /// offsets, location ordinal) order by the row's underlying value, text columns order by their text,
    /// direction reverses, and equal keys keep their natural (stored) order.
    /// </summary>
    public sealed class SpanColumnSorterTests
    {
        // Column order mirrors RefreshSpanList: Type(0) Confidence(1) Text(2) Replacement(3) Start(4) Stop(5) Location(6)
        private const int Confidence = 1, Text = 2, Start = 4, Stop = 5, Location = 6;

        private static IComparer Sorter(int column, bool descending)
        {
            Type t = typeof(ModifyRedactionForm).GetNestedType("SpanColumnSorter", BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("SpanColumnSorter not found");
            object s = Activator.CreateInstance(t, nonPublic: true)!;
            t.GetProperty("Column")!.SetValue(s, column);
            t.GetProperty("Descending")!.SetValue(s, descending);
            return (IComparer)s;
        }

        private static ListViewItem Row(RedactionSpanEntity e, params string[] cells)
        {
            var item = new ListViewItem(cells.Length > 0 ? cells[0] : string.Empty) { Tag = e };
            for (int i = 1; i < cells.Length; i++) item.SubItems.Add(cells[i]);
            return item;
        }

        [Fact]
        public void Confidence_SortsByUnderlyingValue_NotText()
        {
            // As text "9%" would sort after "100%"; by value 0.09 must sort before 1.0.
            var low = Row(new RedactionSpanEntity { Confidence = 0.09 }, "", "9%");
            var high = Row(new RedactionSpanEntity { Confidence = 1.0 }, "", "100%");
            Assert.True(Sorter(Confidence, descending: false).Compare(low, high) < 0);
            Assert.True(Sorter(Confidence, descending: false).Compare(high, low) > 0);
        }

        [Fact]
        public void Start_And_Stop_SortNumerically()
        {
            var a = Row(new RedactionSpanEntity { CharacterStart = 3, CharacterEnd = 40 });
            var b = Row(new RedactionSpanEntity { CharacterStart = 20, CharacterEnd = 9 });
            Assert.True(Sorter(Start, false).Compare(a, b) < 0); // 3 < 20
            Assert.True(Sorter(Stop, false).Compare(a, b) > 0);  // 40 > 9
        }

        [Fact]
        public void Location_SortsByPageThenOrdinal_Numerically()
        {
            var page2 = Row(new RedactionSpanEntity { PageNumber = 2 });
            var page10 = Row(new RedactionSpanEntity { PageNumber = 10 });
            Assert.True(Sorter(Location, false).Compare(page2, page10) < 0); // 2 before 10 (text would reverse)

            var para2 = Row(new RedactionSpanEntity { ParagraphIndex = 2 });
            var para10 = Row(new RedactionSpanEntity { ParagraphIndex = 10 });
            Assert.True(Sorter(Location, false).Compare(para2, para10) < 0);
        }

        [Fact]
        public void TextColumn_SortsAlphabeticallyByCellText()
        {
            // Cells are Type(0), Confidence(1), Text(2) — put the compared value in the Text column.
            var apple = Row(new RedactionSpanEntity(), "name", "", "Apple");
            var banana = Row(new RedactionSpanEntity(), "name", "", "banana");
            Assert.True(Sorter(Text, false).Compare(apple, banana) < 0);
        }

        [Fact]
        public void Descending_ReversesOrder()
        {
            var low = Row(new RedactionSpanEntity { Confidence = 0.2 });
            var high = Row(new RedactionSpanEntity { Confidence = 0.8 });
            Assert.True(Sorter(Confidence, descending: true).Compare(low, high) > 0);
        }

        [Fact]
        public void EqualKeys_FallBackToStoredOrder()
        {
            var first = Row(new RedactionSpanEntity { Confidence = 0.5, Order = 1 });
            var second = Row(new RedactionSpanEntity { Confidence = 0.5, Order = 5 });
            Assert.True(Sorter(Confidence, false).Compare(first, second) < 0); // same confidence -> by Order
        }
    }
}
