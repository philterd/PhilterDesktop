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
    public sealed class DiffViewGateTests
    {
        [Theory]
        [InlineData("report.txt", true)]
        [InlineData("memo.docx", true)]
        [InlineData("scan.pdf", true)]
        [InlineData("list.csv", true)]
        [InlineData("message.eml", true)]
        [InlineData("book.xlsx", false)]  // binary spreadsheet — no text diff
        [InlineData("message.msg", false)] // output is .eml, source is binary — excluded
        [InlineData("note.rtf", false)]   // raw markup would be noisy
        public void IsDiffableType_AllowsTextAndPdfOnly(string name, bool expected)
        {
            Assert.Equal(expected, DiffViewGate.IsDiffableType(name));
        }

        [Theory]
        [InlineData(0, true)]
        [InlineData(5_000_000, true)]
        [InlineData(10L * 1024 * 1024, true)]       // exactly at the cap
        [InlineData(10L * 1024 * 1024 + 1, false)]  // just over
        [InlineData(50_000_000, false)]
        public void IsWithinSizeLimit_EnforcesCap(long bytes, bool expected)
        {
            Assert.Equal(expected, DiffViewGate.IsWithinSizeLimit(bytes));
        }
    }
}
