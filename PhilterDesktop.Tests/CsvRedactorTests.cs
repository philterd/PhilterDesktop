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

using System.Text;
using Phileas.Policy;
using Phileas.Policy.Filters;
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>Tests for <see cref="CsvRedactor"/> and the .csv path through <see cref="RedactionService"/>.</summary>
    public sealed class CsvRedactorTests : IDisposable
    {
        private readonly string _tempDir;

        public CsvRedactorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "philter-csv-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        }

        private static PhileasPolicy EmailAndSsnPolicy() => new()
        {
            Name = "csv",
            Identifiers = new Identifiers { EmailAddress = new EmailAddress(), Ssn = new Ssn() }
        };

        private string Write(string content, string name = "in.csv")
        {
            string path = Path.Combine(_tempDir, name);
            File.WriteAllText(path, content);
            return path;
        }

        [Fact]
        public async Task RedactFileAsync_Csv_RedactsFields_PreservesStructure()
        {
            string input = Write(
                "Name,Email,Notes\r\n" +
                "Alice,alice@example.com,VIP\r\n" +
                "Bob,bob@example.com,SSN 123-45-6789\r\n");
            string output = Path.Combine(_tempDir, "out.csv");

            await RedactionService.RedactFileAsync(input, output, EmailAndSsnPolicy(), "ctx");

            string result = await File.ReadAllTextAsync(output);
            Assert.DoesNotContain("alice@example.com", result);
            Assert.DoesNotContain("bob@example.com", result);
            Assert.DoesNotContain("123-45-6789", result);
            Assert.Contains("VIP", result);
            // Three columns are preserved on each data row.
            string[] lines = result.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
            Assert.All(lines, line => Assert.Equal(2, line.Count(ch => ch == ',')));
        }

        [Fact]
        public async Task RedactFileAsync_Csv_PreservesQuotedFieldsWithCommas()
        {
            // The address field contains a comma, so it must stay quoted on the way out.
            string input = Write(
                "Name,Address,Email\r\n" +
                "Alice,\"123 Main St, Springfield\",alice@example.com\r\n");
            string output = Path.Combine(_tempDir, "out.csv");

            await RedactionService.RedactFileAsync(input, output, EmailAndSsnPolicy(), "ctx");

            string result = await File.ReadAllTextAsync(output);
            Assert.DoesNotContain("alice@example.com", result);
            // The comma-bearing field is still a single quoted field (round-trip preserved).
            Assert.Contains("\"123 Main St, Springfield\"", result);
        }

        [Fact]
        public async Task RedactFileAsync_Csv_FullColumn_RedactsEveryField()
        {
            string input = Write(
                "Name,Email\r\n" +
                "Alice,a@example.com\r\n" +
                "Bob,b@example.com\r\n");
            string output = Path.Combine(_tempDir, "out.csv");

            await RedactionService.RedactFileAsync(input, output, EmailAndSsnPolicy(), "ctx",
                fullyRedactedColumns: new[] { 1 }); // column A (Name)

            string result = await File.ReadAllTextAsync(output);
            Assert.DoesNotContain("Alice", result);
            Assert.DoesNotContain("Bob", result);
            Assert.Contains(XlsxRedactor.ColumnReplacement, result);
            // The column's header label is preserved (only the data cells are cleared).
            Assert.Contains("Name", result);
        }

        [Fact]
        public async Task RedactFileAsync_Csv_CapturesExplanationSpans()
        {
            string input = Write("Email\r\nalice@example.com\r\n");
            string output = Path.Combine(_tempDir, "out.csv");

            List<RedactionSpanEntity> spans = await RedactionService.RedactFileAsync(input, output, EmailAndSsnPolicy(), "ctx");

            Assert.Contains(spans, s => s.FilterType == "EmailAddress");
        }

        [Fact]
        public void ApplySpans_Csv_ReappliesEditedSpanSet()
        {
            string input = Write("Email\r\nalice@example.com\r\n");
            string firstPass = Path.Combine(_tempDir, "first.csv");
            var fs = new Phileas.Services.FilterService();
            var policy = EmailAndSsnPolicy();
            List<RedactionSpanEntity> spans = CsvRedactor.Redact(
                input, firstPass, text => fs.Filter(policy, "ctx", 0, text));

            Assert.NotEmpty(spans);

            string reapplied = Path.Combine(_tempDir, "reapplied.csv");
            CsvRedactor.ApplySpans(input, reapplied, spans);

            Assert.DoesNotContain("alice@example.com", File.ReadAllText(reapplied));
        }

        [Fact]
        public async Task RedactFileAsync_Csv_PreservesUtf8Bom_AndNonAscii()
        {
            // "Jose Nunez" with accents, built from char codes so the test source stays pure ASCII.
            string name = "Jos" + (char)0x00E9 + " N" + (char)0x00FA + (char)0x00F1 + "ez";
            string input = Path.Combine(_tempDir, "bom.csv");
            File.WriteAllText(input, $"Name,Email\r\n{name},jose@example.com\r\n",
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)); // UTF-8 WITH BOM
            string output = Path.Combine(_tempDir, "out.csv");

            await RedactionService.RedactFileAsync(input, output, EmailAndSsnPolicy(), "ctx");

            byte[] bytes = await File.ReadAllBytesAsync(output);
            Assert.True(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
                "the UTF-8 BOM should be preserved");
            string text = await File.ReadAllTextAsync(output);
            Assert.Contains(name, text);                       // non-ASCII survives intact
            Assert.DoesNotContain("jose@example.com", text);   // and the email is still redacted
        }

        [Fact]
        public async Task RedactFileAsync_Csv_NoBomSource_StaysWithoutBom()
        {
            string input = Path.Combine(_tempDir, "nobom.csv");
            File.WriteAllText(input, "Name,Email\r\nAlice,alice@example.com\r\n",
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)); // no BOM
            string output = Path.Combine(_tempDir, "out.csv");

            await RedactionService.RedactFileAsync(input, output, EmailAndSsnPolicy(), "ctx");

            byte[] bytes = await File.ReadAllBytesAsync(output);
            bool hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
            Assert.False(hasBom, "a source without a BOM should not gain one");
        }

        [Fact]
        public async Task RedactFileAsync_Csv_EscapesFormulaInjection()
        {
            // Non-PII cells that begin with a formula character must be neutralized so they can't execute
            // when the redacted CSV is opened in Excel/Sheets (CSV/formula injection).
            string input = Write(
                "Name,Note\r\n" +
                "Alice,=1+2\r\n" +
                "Bob,@SUM(A1)\r\n");
            string output = Path.Combine(_tempDir, "out.csv");

            await RedactionService.RedactFileAsync(input, output, EmailAndSsnPolicy(), "ctx");

            string result = await File.ReadAllTextAsync(output);
            // Each risky field is prefixed with an apostrophe (escaped), not left as a raw leading = / @.
            Assert.Contains("'=1+2", result);
            Assert.Contains("'@SUM(A1)", result);
            Assert.DoesNotContain(",=1+2", result);     // not a bare, executable leading formula
            Assert.DoesNotContain(",@SUM(A1)", result);
        }

        [Fact]
        public void ReadColumns_ReturnsHeaderRow()
        {
            string input = Write("Name,Email,Notes\r\nAlice,a@example.com,x\r\n");

            List<SpreadsheetColumn> columns = CsvRedactor.ReadColumns(input);

            Assert.Equal(3, columns.Count);
            Assert.Equal("Name", columns[0].Header);
            Assert.Equal("Email", columns[1].Header);
            Assert.Equal("Notes", columns[2].Header);
        }

        // --- Streaming refactor (#476) ---------------------------------------

        // Byte-level guard: with a no-op filter the streamed writer must emit canonical CSV exactly,
        // proving the streaming write path produces the same bytes as before.
        [Fact]
        public void Redact_NoOpFilter_ProducesCanonicalCsvExactly()
        {
            string input = Write("a,b\r\nc,d\r\n", "plain.csv");
            string output = Path.Combine(_tempDir, "plain-out.csv");

            CsvRedactor.Redact(input, output,
                text => new Phileas.Model.TextFilterResult(text, new List<Phileas.Model.Span>()));

            Assert.Equal("a,b\r\nc,d\r\n", File.ReadAllText(output));
        }

        // The detected (non-comma) delimiter is preserved across the streamed read -> write.
        [Fact]
        public async Task RedactFileAsync_Csv_PreservesSemicolonDelimiter()
        {
            string input = Write("Name;Email\r\nAlice;alice@example.com\r\nBob;bob@example.com\r\n", "semi.csv");
            string output = Path.Combine(_tempDir, "semi-out.csv");

            await RedactionService.RedactFileAsync(input, output, EmailAndSsnPolicy(), "ctx");

            string result = await File.ReadAllTextAsync(output);
            Assert.Contains("Name;Email", result);            // delimiter preserved, not switched to comma
            Assert.DoesNotContain("alice@example.com", result);
            Assert.DoesNotContain("bob@example.com", result);
        }

        // Captured spans are unchanged by streaming: one span per email, with row-major field ordinals
        // (header=0,1; row1=2,3; row2=4,5 -> emails at 3 and 5) and sequential Order values.
        [Fact]
        public void Redact_MultiRow_CapturesSpansWithRowMajorFieldOrdinals()
        {
            string input = Write("Name,Email\r\nAlice,a@example.com\r\nBob,b@example.com\r\n", "ords.csv");
            string output = Path.Combine(_tempDir, "ords-out.csv");
            var fs = new Phileas.Services.FilterService();
            var policy = EmailAndSsnPolicy();

            List<RedactionSpanEntity> spans = CsvRedactor.Redact(
                input, output, text => fs.Filter(policy, "ctx", 0, text));

            Assert.Equal(2, spans.Count);
            Assert.All(spans, s => Assert.Equal("EmailAddress", s.FilterType));
            Assert.Equal(new[] { 3, 5 }, spans.Select(s => s.ParagraphIndex).OrderBy(x => x).ToArray());
            Assert.Equal(new[] { 0, 1 }, spans.Select(s => s.Order).OrderBy(x => x).ToArray());
        }

        // Whole-column redaction still works through the single streaming pass: the header label is kept
        // and every data cell in the column is cleared, with one captured span per cleared cell.
        [Fact]
        public void Redact_FullColumn_Streamed_KeepsHeaderAndClearsDataCells()
        {
            string input = Write("Name,Email\r\nAlice,a@example.com\r\nBob,b@example.com\r\n", "col.csv");
            string output = Path.Combine(_tempDir, "col-out.csv");
            var fs = new Phileas.Services.FilterService();
            var policy = EmailAndSsnPolicy();

            List<RedactionSpanEntity> spans = CsvRedactor.Redact(
                input, output, text => fs.Filter(policy, "ctx", 0, text), fullyRedactedColumns: new[] { 1 });

            string result = File.ReadAllText(output);
            Assert.Contains("Name", result);     // header label preserved
            Assert.DoesNotContain("Alice", result);
            Assert.DoesNotContain("Bob", result);
            // Two data cells cleared in column 1 -> two column-redaction spans captured.
            Assert.Equal(2, spans.Count(s => s.Classification == XlsxRedactor.ColumnClassification));
        }

        // A failure mid-stream must not leave a partial or corrupt file at the destination (the streamed
        // writer writes to a temp file and only moves it into place on success).
        [Fact]
        public void Redact_FilterThrowsMidStream_LeavesNoOutputFile()
        {
            string input = Write("Email\r\nfirst@example.com\r\nboom@example.com\r\n", "fail.csv");
            string output = Path.Combine(_tempDir, "fail-out.csv");

            Assert.Throws<InvalidOperationException>(() =>
                CsvRedactor.Redact(input, output, text =>
                    text.Contains("boom")
                        ? throw new InvalidOperationException("simulated failure")
                        : new Phileas.Model.TextFilterResult(text, new List<Phileas.Model.Span>())));

            Assert.False(File.Exists(output), "no output file should remain after a mid-stream failure");
            // And no leftover temp files in the output directory.
            Assert.Empty(Directory.GetFiles(_tempDir, "*.tmp"));
            Assert.Empty(Directory.GetFiles(_tempDir, "*.redacting-*"));
        }

        // The streaming pipeline must not hold the whole table in memory. Redacting a large CSV with no
        // PII (so the captured-spans list stays empty) should retain only a tiny amount of managed memory
        // afterward — far less than a materialized List<string[]> of the file would require.
        [Fact]
        public void Redact_LargeCsv_DoesNotMaterializeAllRowsInMemory()
        {
            const int rows = 150_000;
            string input = Path.Combine(_tempDir, "large.csv");
            using (var w = new StreamWriter(input, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                w.Write("Col1,Col2,Col3\r\n");
                for (int i = 0; i < rows; i++)
                {
                    w.Write($"value-{i}-aaaaaaaaaa,value-{i}-bbbbbbbbbb,value-{i}-cccccccccc\r\n");
                }
            }
            long fileSize = new FileInfo(input).Length;
            string output = Path.Combine(_tempDir, "large-out.csv");

            // No-op filter: never matches, so no spans are captured and nothing per-row is retained.
            static Phileas.Model.TextFilterResult NoOp(string text) =>
                new(text, new List<Phileas.Model.Span>());

            GC.Collect();
            GC.WaitForPendingFinalizers();
            long before = GC.GetTotalMemory(forceFullCollection: true);

            List<RedactionSpanEntity> spans = CsvRedactor.Redact(input, output, NoOp);

            long retained = GC.GetTotalMemory(forceFullCollection: true) - before;

            Assert.Empty(spans);
            Assert.Equal(rows + 1, File.ReadLines(output).Count()); // every row written
            Assert.True(retained < fileSize / 4,
                $"retained {retained:N0} bytes after streaming a {fileSize:N0}-byte CSV; the table appears to be materialized in memory");
        }
    }
}
