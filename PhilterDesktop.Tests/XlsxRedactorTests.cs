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

using Phileas.Policy;
using Phileas.Policy.Filters;
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>Tests for <see cref="XlsxRedactor"/> and the .xlsx path through <see cref="RedactionService"/>.</summary>
    public sealed class XlsxRedactorTests : IDisposable
    {
        private readonly string _tempDir;

        public XlsxRedactorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "philter-xlsx-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        }

        private static PhileasPolicy EmailAndSsnPolicy() => new()
        {
            Name = "xlsx",
            Identifiers = new Identifiers { EmailAddress = new EmailAddress(), Ssn = new Ssn() }
        };

        private string Make(IReadOnlyList<string?[]> rows, string name = "in.xlsx")
        {
            string path = Path.Combine(_tempDir, name);
            SpreadsheetTestHelper.CreateXlsx(path, rows);
            return path;
        }

        [Fact]
        public async Task RedactFileAsync_Xlsx_RedactsTextCells_PreservesOthers()
        {
            string input = Make(new[]
            {
                new string?[] { "Name", "Email", "Notes" },
                new string?[] { "Alice", "alice@example.com", "VIP client" },
                new string?[] { "Bob", "bob@example.com", "SSN 123-45-6789" }
            });
            string output = Path.Combine(_tempDir, "out.xlsx");

            await RedactionService.RedactFileAsync(input, output, EmailAndSsnPolicy(), "ctx");

            string text = SpreadsheetTestHelper.AllText(output);
            Assert.DoesNotContain("alice@example.com", text);
            Assert.DoesNotContain("bob@example.com", text);
            Assert.DoesNotContain("123-45-6789", text);
            // Non-sensitive cells are untouched.
            Assert.Contains("VIP client", text);
            Assert.Contains("Name", text);
        }

        [Fact]
        public async Task RedactFileAsync_Xlsx_LeavesNumberCellsAlone()
        {
            string input = Make(new[]
            {
                new string?[] { "Account", "Balance" },
                new string?[] { "alice@example.com", "12345" } // 12345 is a number cell
            });
            string output = Path.Combine(_tempDir, "out.xlsx");

            await RedactionService.RedactFileAsync(input, output, EmailAndSsnPolicy(), "ctx");

            string text = SpreadsheetTestHelper.AllText(output);
            Assert.DoesNotContain("alice@example.com", text);
            Assert.Contains("12345", text); // numeric cell preserved (not a selected full column)
        }

        [Fact]
        public async Task RedactFileAsync_Xlsx_FullColumn_RedactsEveryCellInColumn()
        {
            // Column A holds names the model may not detect in isolation; full-column removes them all.
            string input = Make(new[]
            {
                new string?[] { "Name", "Email" },
                new string?[] { "Alice", "a@example.com" },
                new string?[] { "Bob", "b@example.com" },
                new string?[] { "Carol", "c@example.com" }
            });
            string output = Path.Combine(_tempDir, "out.xlsx");

            await RedactionService.RedactFileAsync(input, output, EmailAndSsnPolicy(), "ctx",
                fullyRedactedColumns: new[] { 1 }); // column A

            string text = SpreadsheetTestHelper.AllText(output);
            Assert.DoesNotContain("Alice", text);
            Assert.DoesNotContain("Bob", text);
            Assert.DoesNotContain("Carol", text);
            Assert.Contains(XlsxRedactor.ColumnReplacement, text);
            // The column's header label is preserved (only the data cells are cleared).
            Assert.Contains("Name", text);
        }

        [Fact]
        public async Task RedactFileAsync_Xlsx_RedactsAcrossMultipleSheets()
        {
            string input = Make(new[]
            {
                new string?[] { "Email" },
                new string?[] { "sheet1@example.com" }
            });
            SpreadsheetTestHelper.AppendSheet(input, new[]
            {
                new string?[] { "Email" },
                new string?[] { "sheet2@example.com" }
            }, "Sheet2");
            string output = Path.Combine(_tempDir, "out.xlsx");

            await RedactionService.RedactFileAsync(input, output, EmailAndSsnPolicy(), "ctx");

            string text = SpreadsheetTestHelper.AllText(output);
            Assert.DoesNotContain("sheet1@example.com", text);
            Assert.DoesNotContain("sheet2@example.com", text);
        }

        [Fact]
        public async Task RedactFileAsync_Xlsx_CapturesExplanationSpans()
        {
            string input = Make(new[]
            {
                new string?[] { "Email" },
                new string?[] { "alice@example.com" }
            });
            string output = Path.Combine(_tempDir, "out.xlsx");

            List<RedactionSpanEntity> spans = await RedactionService.RedactFileAsync(input, output, EmailAndSsnPolicy(), "ctx");

            Assert.Contains(spans, s => s.FilterType == "EmailAddress");
        }

        [Fact]
        public void ApplySpans_Xlsx_ReappliesEditedSpanSet()
        {
            string input = Make(new[]
            {
                new string?[] { "Email" },
                new string?[] { "alice@example.com" }
            });
            string firstPass = Path.Combine(_tempDir, "first.xlsx");
            var fs = new Phileas.Services.FilterService();
            var policy = EmailAndSsnPolicy();
            List<RedactionSpanEntity> spans = XlsxRedactor.Redact(
                input, firstPass, text => fs.Filter(policy, "ctx", 0, text));

            Assert.NotEmpty(spans);

            string reapplied = Path.Combine(_tempDir, "reapplied.xlsx");
            XlsxRedactor.ApplySpans(input, reapplied, spans);

            Assert.DoesNotContain("alice@example.com", SpreadsheetTestHelper.AllText(reapplied));
        }

        [Fact]
        public void ReadColumns_ReturnsLetterAndHeader()
        {
            string input = Make(new[]
            {
                new string?[] { "Name", "Email", "Notes" },
                new string?[] { "Alice", "a@example.com", "x" }
            });

            List<SpreadsheetColumn> columns = XlsxRedactor.ReadColumns(input);

            Assert.Equal(3, columns.Count);
            Assert.Equal("A", columns[0].Letter);
            Assert.Equal("Name", columns[0].Header);
            Assert.Equal("Email", columns[1].Header);
        }
    }
}
