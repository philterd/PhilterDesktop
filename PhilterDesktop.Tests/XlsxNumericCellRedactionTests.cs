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
using Phileas.Services;
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// PII is commonly typed into Excel as a bare number (SSN 123456789, phone 5551234567, account /
    /// card numbers), which is stored as a numeric cell. Those were skipped by detection
    ///. These pin that numeric-typed cells are now scanned and redacted,
    /// while genuine non-PII numbers are left alone.
    /// </summary>
    public sealed class XlsxNumericCellRedactionTests : IDisposable
    {
        private readonly string _dir;

        public XlsxNumericCellRedactionTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-xlsxnum-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private static PhileasPolicy Policy() => new()
        {
            Name = "num",
            Identifiers = new Identifiers
            {
                EmailAddress = new EmailAddress(),
                Ssn = new Ssn(),
                PhoneNumber = new PhoneNumber(),
                CreditCard = new CreditCard()
            }
        };

        private string Make(IReadOnlyList<string?[]> rows, string name = "in.xlsx")
        {
            string path = Path.Combine(_dir, name);
            SpreadsheetTestHelper.CreateXlsx(path, rows);
            return path;
        }

        private string Redact(string input, PhileasPolicy? policy = null)
        {
            string output = Path.Combine(_dir, "out_" + Guid.NewGuid().ToString("N") + ".xlsx");
            var fs = new FilterService();
            PhileasPolicy p = policy ?? Policy();
            XlsxRedactor.Redact(input, output, text => fs.Filter(p, "ctx", 0, text));
            return output;
        }

        [Theory]
        [InlineData("123456789")]    // SSN as a bare number
        [InlineData("5551234567")]   // phone as a bare number
        [InlineData("4111111111111111")] // credit card as a bare number
        public void NumericPiiCell_IsRedacted(string numericPii)
        {
            string input = Make(new[]
            {
                new string?[] { "Value" },
                new string?[] { numericPii }
            });
            Assert.True(SpreadsheetTestHelper.IsNumberCell(input, "A2"), "fixture must store the PII as a numeric cell");

            string output = Redact(input);

            Assert.DoesNotContain(numericPii, SpreadsheetTestHelper.AllText(output));
        }

        [Theory]
        [InlineData("12345")]   // small number — not PII
        [InlineData("42")]      // quantity
        [InlineData("2024")]    // year
        [InlineData("3.14159")] // decimal
        public void NonPiiNumber_IsPreserved(string number)
        {
            string input = Make(new[]
            {
                new string?[] { "Value" },
                new string?[] { number }
            });

            string output = Redact(input);

            Assert.Contains(number, SpreadsheetTestHelper.AllText(output));
        }

        [Fact]
        public void RedactedNumericCell_BecomesInlineString()
        {
            string input = Make(new[]
            {
                new string?[] { "SSN" },
                new string?[] { "123456789" }
            });
            Assert.True(SpreadsheetTestHelper.IsNumberCell(input, "A2"));

            string output = Redact(input);

            // After redaction the cell holds replacement text, so it is now an inline string, not numeric.
            Assert.True(SpreadsheetTestHelper.IsInlineStringCell(output, "A2"));
        }

        [Fact]
        public void MixedRow_TextAndNumericPii_BothRedacted_OtherNumbersKept()
        {
            string input = Make(new[]
            {
                new string?[] { "Email", "SSN", "Age" },
                new string?[] { "alice@example.com", "123456789", "37" }
            });
            Assert.True(SpreadsheetTestHelper.IsNumberCell(input, "B2"), "SSN must be a numeric cell");
            Assert.True(SpreadsheetTestHelper.IsNumberCell(input, "C2"), "Age must be a numeric cell");

            string output = Redact(input);

            string text = SpreadsheetTestHelper.AllText(output);
            Assert.DoesNotContain("alice@example.com", text);
            Assert.DoesNotContain("123456789", text);
            Assert.Contains("37", text); // non-PII number preserved
        }

        [Fact]
        public void MultipleNumericPiiCells_EachRedactedIndependently()
        {
            string input = Make(new[]
            {
                new string?[] { "SSN", "Phone" },
                new string?[] { "123456789", "5551234567" },
                new string?[] { "078051120", "5559876543" } // 078 is a valid SSN area (987 is not)
            });
            string output = Redact(input);

            string text = SpreadsheetTestHelper.AllText(output);
            foreach (string pii in new[] { "123456789", "5551234567", "078051120", "5559876543" })
            {
                Assert.DoesNotContain(pii, text);
            }
        }

        [Fact]
        public void Detect_FindsNumericPii()
        {
            string input = Make(new[]
            {
                new string?[] { "SSN" },
                new string?[] { "123456789" }
            });
            var fs = new FilterService();
            PhileasPolicy p = Policy();

            List<RedactionSpanEntity> spans = XlsxRedactor.Detect(input, text => fs.Filter(p, "ctx", 0, text));

            Assert.Contains(spans, s => s.Text == "123456789");
        }

        [Fact]
        public void ApplySpans_ReappliesNumericRedaction()
        {
            string input = Make(new[]
            {
                new string?[] { "SSN" },
                new string?[] { "123456789" }
            });
            var fs = new FilterService();
            PhileasPolicy p = Policy();
            List<RedactionSpanEntity> spans = XlsxRedactor.Detect(input, text => fs.Filter(p, "ctx", 0, text));
            Assert.NotEmpty(spans);

            string output = Path.Combine(_dir, "applied.xlsx");
            XlsxRedactor.ApplySpans(input, output, spans);

            Assert.DoesNotContain("123456789", SpreadsheetTestHelper.AllText(output));
        }

        [Fact]
        public async Task RedactFileAsync_NumericPii_EndToEnd()
        {
            string input = Make(new[]
            {
                new string?[] { "Name", "SSN", "Phone", "Balance" },
                new string?[] { "Acme Corp", "123456789", "5551234567", "1000" }
            });
            string output = Path.Combine(_dir, "svc_out.xlsx");

            await RedactionService.RedactFileAsync(input, output, Policy(), "ctx");

            string text = SpreadsheetTestHelper.AllText(output);
            Assert.DoesNotContain("123456789", text);
            Assert.DoesNotContain("5551234567", text);
            Assert.Contains("1000", text); // balance is not PII
        }

        [Fact]
        public void IntegerIdColumn_NotMangled()
        {
            // A column of plain integers (ids) must be left intact — no detector matches them.
            string input = Make(new[]
            {
                new string?[] { "Id" },
                new string?[] { "1" },
                new string?[] { "2" },
                new string?[] { "1001" }
            });
            string output = Redact(input);

            string text = SpreadsheetTestHelper.AllText(output);
            Assert.Contains("1001", text);
            Assert.DoesNotContain("REDACTED", text);
        }

        [Fact]
        public void NegativeAndDecimalNumbers_Preserved()
        {
            string input = Make(new[]
            {
                new string?[] { "Amount" },
                new string?[] { "-1000.50" },
                new string?[] { "0" }
            });
            string output = Redact(input);

            string text = SpreadsheetTestHelper.AllText(output);
            Assert.Contains("-1000.50", text);
            Assert.Contains("0", text);
        }

        [Fact]
        public void RedactedNumericCell_LeavesNoResidueInWorksheetXml()
        {
            string input = Make(new[]
            {
                new string?[] { "SSN" },
                new string?[] { "123456789" }
            });
            string output = Redact(input);

            // The original number must be gone from the raw worksheet XML, not just from the visible text.
            Assert.DoesNotContain("123456789", SpreadsheetTestHelper.WorksheetXml(output));
        }

        [Fact]
        public void FormulaCell_WithPiiCachedValue_IsRedacted_AndStaticized()
        {
            // A formula cell's cached result is scanned too: a PII-shaped cached value is redacted and the
            // formula dropped so recalculation can't restore it. (Governed by the "Redact cached formula
            // values" option, default on — see XlsxFormulaCacheTests for the on/off coverage.)
            string path = Path.Combine(_dir, "formula.xlsx");
            SpreadsheetTestHelper.CreateTyped(path, new SpreadsheetTestHelper.CellSpec?[][]
            {
                new SpreadsheetTestHelper.CellSpec?[] { new("SSN") },
                new SpreadsheetTestHelper.CellSpec?[] { new("123456789", SpreadsheetTestHelper.CellKind.Formula, Formula: "A1") }
            });

            string output = Redact(path);

            Assert.DoesNotContain("123456789", SpreadsheetTestHelper.AllText(output)); // cached PII redacted
            Assert.False(SpreadsheetTestHelper.IsFormulaCell(output, "A2"));           // formula dropped -> static
        }

        // --- Edge cases: cell kinds that must NOT be scanned ----------------------------------------

        [Fact]
        public void BooleanCell_IsLeftUntouched()
        {
            string path = Path.Combine(_dir, "bool.xlsx");
            SpreadsheetTestHelper.CreateTyped(path, new SpreadsheetTestHelper.CellSpec?[][]
            {
                new SpreadsheetTestHelper.CellSpec?[] { new("Flag") },
                new SpreadsheetTestHelper.CellSpec?[] { new("1", SpreadsheetTestHelper.CellKind.Boolean) }
            });

            string output = Redact(path);

            Assert.True(SpreadsheetTestHelper.IsBooleanCell(output, "A2"), "a boolean cell must stay boolean");
            Assert.DoesNotContain("REDACTED", SpreadsheetTestHelper.AllText(output));
        }

        [Fact]
        public void DateTypedCell_IsoValue_NotContentRedacted_ButFullColumnClearsIt()
        {
            // Date-typed cells store an ISO value (e.g. 1972-05-20) that the date detector doesn't match,
            // so content detection leaves it — whole-column redaction is the dependable tool (documented).
            string path = Path.Combine(_dir, "date.xlsx");
            SpreadsheetTestHelper.CreateTyped(path, new SpreadsheetTestHelper.CellSpec?[][]
            {
                new SpreadsheetTestHelper.CellSpec?[] { new("DOB") },
                new SpreadsheetTestHelper.CellSpec?[] { new("1972-05-20", SpreadsheetTestHelper.CellKind.Date) }
            });
            var policy = new PhileasPolicy { Name = "date", Identifiers = new Identifiers { Date = new Date() } };

            string contentOnly = Redact(path, policy);
            Assert.Contains("1972-05-20", SpreadsheetTestHelper.AllText(contentOnly)); // not caught by content scan

            // Whole-column removal clears it regardless of detection.
            string fullColumn = Path.Combine(_dir, "date_fc.xlsx");
            var fs = new FilterService();
            XlsxRedactor.Redact(path, fullColumn, text => fs.Filter(policy, "ctx", 0, text), fullyRedactedColumns: new[] { 1 });
            Assert.DoesNotContain("1972-05-20", SpreadsheetTestHelper.AllText(fullColumn));
        }

        [Fact]
        public void ErrorCell_IsLeftUntouched()
        {
            // Error cells (e.g. #N/A) carry no free-text PII and must not be scanned or converted.
            string path = Path.Combine(_dir, "err.xlsx");
            SpreadsheetTestHelper.CreateTyped(path, new SpreadsheetTestHelper.CellSpec?[][]
            {
                new SpreadsheetTestHelper.CellSpec?[] { new("Result") },
                new SpreadsheetTestHelper.CellSpec?[] { new("#N/A", SpreadsheetTestHelper.CellKind.Error) }
            });

            string output = Redact(path);

            Assert.DoesNotContain("REDACTED", SpreadsheetTestHelper.AllText(output));
            Assert.False(SpreadsheetTestHelper.IsInlineStringCell(output, "A2"), "an error cell must not become a string");
        }

        [Fact]
        public async Task NumericPii_AcrossMultipleSheets_AllRedacted()
        {
            string input = Make(new[]
            {
                new string?[] { "SSN" },
                new string?[] { "123456789" }
            });
            SpreadsheetTestHelper.AppendSheet(input, new[]
            {
                new string?[] { "Phone" },
                new string?[] { "5551234567" }
            }, "Sheet2");
            string output = Path.Combine(_dir, "ms_out.xlsx");

            await RedactionService.RedactFileAsync(input, output, Policy(), "ctx");

            string text = SpreadsheetTestHelper.AllText(output);
            Assert.DoesNotContain("123456789", text);
            Assert.DoesNotContain("5551234567", text);
        }

        [Fact]
        public void FullColumn_OnNumericColumn_ClearsEveryDataCell_KeepsHeader()
        {
            string input = Make(new[]
            {
                new string?[] { "Account" },
                new string?[] { "100200300" },
                new string?[] { "400500600" }
            });
            Assert.True(SpreadsheetTestHelper.IsNumberCell(input, "A2"));

            string output = Path.Combine(_dir, "fc_out.xlsx");
            var fs = new FilterService();
            XlsxRedactor.Redact(input, output, text => fs.Filter(Policy(), "ctx", 0, text), fullyRedactedColumns: new[] { 1 });

            string text = SpreadsheetTestHelper.AllText(output);
            Assert.DoesNotContain("100200300", text);
            Assert.DoesNotContain("400500600", text);
            Assert.Contains(XlsxRedactor.ColumnReplacement, text);
            Assert.Contains("Account", text); // header kept
        }
    }
}
