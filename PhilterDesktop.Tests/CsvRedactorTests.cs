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

        // --- Streaming refactor ---------------------------------------

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

        // --- Line-ending & trailing-newline fidelity ------------

        // No-op filter (no PII) so the output structure equals the input structure exactly.
        private static Phileas.Model.TextFilterResult NoOp(string text) =>
            new(text, new List<Phileas.Model.Span>());

        private string WriteRaw(string content, string name) => Write(content, name); // verbatim UTF-8, no BOM

        [Theory]
        // (input, expected output) — output must be byte-identical for a no-op filter.
        [InlineData("a,b\nc,d\n", "a,b\nc,d\n")]          // LF, trailing LF
        [InlineData("a,b\nc,d", "a,b\nc,d")]              // LF, NO trailing newline
        [InlineData("a,b\r\nc,d\r\n", "a,b\r\nc,d\r\n")]  // CRLF, trailing CRLF
        [InlineData("a,b\r\nc,d", "a,b\r\nc,d")]          // CRLF, NO trailing newline
        [InlineData("a,b\rc,d\r", "a,b\rc,d\r")]          // CR-only (old Mac), trailing CR
        [InlineData("a,b\rc,d", "a,b\rc,d")]              // CR-only, NO trailing newline
        [InlineData("a,b", "a,b")]                        // single record, no newline at all
        [InlineData("a,b\n", "a,b\n")]                    // single record, trailing LF
        [InlineData("a,b\r\n", "a,b\r\n")]                // single record, trailing CRLF
        public void Redact_PreservesLineEndingStyleAndTrailingNewline(string input, string expected)
        {
            string inPath = WriteRaw(input, "le-in.csv");
            string outPath = Path.Combine(_tempDir, "le-out.csv");

            CsvRedactor.Redact(inPath, outPath, NoOp);

            Assert.Equal(expected, File.ReadAllText(outPath));
        }

        [Fact]
        public void Redact_MixedLineEndings_NormalizeToTheFirstStyle()
        {
            // First terminator is CRLF, so the whole output uses CRLF; source has no trailing newline.
            string inPath = WriteRaw("a,b\r\nc,d\ne,f", "mixed.csv");
            string outPath = Path.Combine(_tempDir, "mixed-out.csv");

            CsvRedactor.Redact(inPath, outPath, NoOp);

            Assert.Equal("a,b\r\nc,d\r\ne,f", File.ReadAllText(outPath));
        }

        [Fact]
        public async Task RedactFileAsync_Csv_LfSource_StaysLf_AndStillRedacts()
        {
            // LF line endings, no trailing newline, with PII to redact.
            string inPath = WriteRaw("Name,Email\nAlice,alice@example.com\nBob,bob@example.com", "lf-pii.csv");
            string outPath = Path.Combine(_tempDir, "lf-pii-out.csv");

            await RedactionService.RedactFileAsync(inPath, outPath, EmailAndSsnPolicy(), "ctx");

            string result = await File.ReadAllTextAsync(outPath);
            Assert.DoesNotContain("alice@example.com", result);
            Assert.DoesNotContain("bob@example.com", result);
            Assert.DoesNotContain("\r", result);                 // never introduced CRLF
            Assert.False(result.EndsWith("\n"), "must not append a trailing newline the source lacked");
        }

        [Fact]
        public void Redact_EmptyFile_ProducesEmptyOutput()
        {
            string inPath = WriteRaw(string.Empty, "empty.csv");
            string outPath = Path.Combine(_tempDir, "empty-out.csv");

            CsvRedactor.Redact(inPath, outPath, NoOp);

            Assert.True(File.Exists(outPath));
            Assert.Equal(0, new FileInfo(outPath).Length);
        }

        [Fact]
        public void Redact_Utf8Bom_Lf_NoTrailingNewline_PreservedExactly()
        {
            // BOM + LF + no trailing newline: the trailing-newline trim must remove only the 1-byte LF
            // terminator, never the BOM at the start.
            var enc = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            string inPath = Path.Combine(_tempDir, "bom-lf.csv");
            byte[] inputBytes = enc.GetPreamble().Concat(Encoding.UTF8.GetBytes("a,b\nc,d")).ToArray();
            File.WriteAllBytes(inPath, inputBytes);
            string outPath = Path.Combine(_tempDir, "bom-lf-out.csv");

            CsvRedactor.Redact(inPath, outPath, NoOp);

            byte[] expected = enc.GetPreamble().Concat(Encoding.UTF8.GetBytes("a,b\nc,d")).ToArray();
            Assert.Equal(expected, File.ReadAllBytes(outPath));
        }

        [Fact]
        public void Redact_Utf16Le_Lf_NoTrailingNewline_PreservedExactly()
        {
            // UTF-16: a code unit is 2 bytes, so the line-ending detection and the trailing-newline trim
            // must both account for the encoding (LF = 0x0A 0x00, trim 2 bytes — not 1).
            var enc = new UnicodeEncoding(bigEndian: false, byteOrderMark: true);
            string inPath = Path.Combine(_tempDir, "u16-lf.csv");
            byte[] inputBytes = enc.GetPreamble().Concat(enc.GetBytes("a,b\nc,d")).ToArray();
            File.WriteAllBytes(inPath, inputBytes);
            string outPath = Path.Combine(_tempDir, "u16-lf-out.csv");

            CsvRedactor.Redact(inPath, outPath, NoOp);

            byte[] expected = enc.GetPreamble().Concat(enc.GetBytes("a,b\nc,d")).ToArray();
            Assert.Equal(expected, File.ReadAllBytes(outPath));
        }

        [Fact]
        public void Redact_Utf16Le_Crlf_TrailingNewline_Preserved()
        {
            var enc = new UnicodeEncoding(bigEndian: false, byteOrderMark: true);
            string inPath = Path.Combine(_tempDir, "u16-crlf.csv");
            File.WriteAllBytes(inPath, enc.GetPreamble().Concat(enc.GetBytes("a,b\r\nc,d\r\n")).ToArray());
            string outPath = Path.Combine(_tempDir, "u16-crlf-out.csv");

            CsvRedactor.Redact(inPath, outPath, NoOp);

            byte[] expected = enc.GetPreamble().Concat(enc.GetBytes("a,b\r\nc,d\r\n")).ToArray();
            Assert.Equal(expected, File.ReadAllBytes(outPath));
        }

        [Fact]
        public void Redact_LfSource_WithRedaction_KeepsLfAndNoTrailingNewline()
        {
            // Redaction changes a cell; the surrounding structure (LF, no trailing newline) must survive.
            string inPath = WriteRaw("Email,Note\nalice@example.com,hi\nbob@example.com,yo", "lf-redact.csv");
            string outPath = Path.Combine(_tempDir, "lf-redact-out.csv");
            var fs = new Phileas.Services.FilterService();
            var policy = EmailAndSsnPolicy();

            CsvRedactor.Redact(inPath, outPath, t => fs.Filter(policy, "ctx", 0, t));

            string result = File.ReadAllText(outPath);
            Assert.DoesNotContain("alice@example.com", result);
            Assert.Equal(2, result.Count(ch => ch == '\n')); // exactly the two interior LFs, none added
            Assert.DoesNotContain("\r", result);
            Assert.False(result.EndsWith("\n"));
        }

        // --- Per-field verification detection --------------------------

        [Fact]
        public void Detect_FindsResidual_AtFieldOrdinalAndPerCellOffset()
        {
            // header: fields 0,1; row1: Bob (field 2), "SSN 078-05-1120" (field 3).
            string input = Write("Name,Notes\r\nBob,SSN 078-05-1120\r\n", "detect.csv");
            var fs = new Phileas.Services.FilterService();
            var policy = EmailAndSsnPolicy();

            List<RedactionSpanEntity> residuals = CsvRedactor.Detect(input, t => fs.Filter(policy, "ctx", 0, t));

            RedactionSpanEntity ssn = Assert.Single(residuals);
            Assert.Equal(3, ssn.ParagraphIndex);     // 4th field (row-major), not a whole-file marker (-1)
            Assert.Equal(4, ssn.CharacterStart);     // offset within the cell "SSN 078-05-1120", not the file
            Assert.Equal("078-05-1120", ssn.Text);
        }

        [Fact]
        public void Detect_NoPii_ReturnsEmpty()
        {
            string input = Write("Name,Notes\r\nBob,just a normal note\r\n", "detect-clean.csv");
            var fs = new Phileas.Services.FilterService();
            var policy = EmailAndSsnPolicy();

            Assert.Empty(CsvRedactor.Detect(input, t => fs.Filter(policy, "ctx", 0, t)));
        }

        [Fact]
        public void Detect_MultipleResiduals_HaveRowMajorOrdinals()
        {
            string input = Write("Name,Email\r\nAlice,a@example.com\r\nBob,b@example.com\r\n", "detect-multi.csv");
            var fs = new Phileas.Services.FilterService();
            var policy = EmailAndSsnPolicy();

            List<RedactionSpanEntity> residuals = CsvRedactor.Detect(input, t => fs.Filter(policy, "ctx", 0, t));

            Assert.Equal(new[] { 3, 5 }, residuals.Select(r => r.ParagraphIndex).OrderBy(x => x).ToArray());
        }

        [Fact]
        public void Detect_SkipsEmptyCells_ButKeepsOrdinalAlignment()
        {
            // row1: empty (field 2), email (field 3).
            string input = Write("Name,Email\r\n,c@example.com\r\n", "detect-empty.csv");
            var fs = new Phileas.Services.FilterService();
            var policy = EmailAndSsnPolicy();

            RedactionSpanEntity span = Assert.Single(CsvRedactor.Detect(input, t => fs.Filter(policy, "ctx", 0, t)));
            Assert.Equal(3, span.ParagraphIndex);
        }

        [Fact]
        public void Detect_OnRedactedOutput_IsClean()
        {
            // Redacting then detecting the output must find nothing — verification matches redaction.
            string input = Write("Name,Email\r\nAlice,alice@example.com\r\nBob,bob@example.com\r\n", "detect-rt.csv");
            string output = Path.Combine(_tempDir, "detect-rt-out.csv");
            var fs = new Phileas.Services.FilterService();
            var policy = EmailAndSsnPolicy();
            CsvRedactor.Redact(input, output, t => fs.Filter(policy, "ctx", 0, t));

            Assert.Empty(CsvRedactor.Detect(output, t => fs.Filter(policy, "ctx", 0, t)));
        }

        [Fact]
        public void Detect_DoesNotMatchAcrossCellBoundaries()
        {
            // The two cells would form an SSN only if joined ("078-05" + "1120"); per-field scanning must
            // not detect one. A whole-file blob scan is the behavior this fix replaces.
            string input = Write("078-05,1120\r\n", "detect-split.csv");
            var fs = new Phileas.Services.FilterService();
            var policy = EmailAndSsnPolicy();

            Assert.Empty(CsvRedactor.Detect(input, t => fs.Filter(policy, "ctx", 0, t)));
        }

        // --- Blank-line preservation ----------------------------------

        [Theory]
        [InlineData("a,b\nx,y\n\np,q\n")]        // blank line in the middle (LF, trailing)
        [InlineData("a,b\n\n\nc,d\n")]           // multiple consecutive blank lines
        [InlineData("\na,b\n")]                  // leading blank line
        [InlineData("a,b\n\n")]                  // trailing blank line
        [InlineData("a,b\n\nc,d")]               // blank line + no trailing newline
        [InlineData("a,b\r\n\r\nc,d\r\n")]       // CRLF blank line
        public void Redact_PreservesBlankLines_Exactly(string input)
        {
            string inPath = WriteRaw(input, "blank-in.csv");
            string outPath = Path.Combine(_tempDir, "blank-out.csv");

            CsvRedactor.Redact(inPath, outPath, NoOp);

            Assert.Equal(input, File.ReadAllText(outPath)); // blank rows neither dropped nor added
        }

        [Fact]
        public void Redact_BlankLines_RowCountPreserved()
        {
            string inPath = WriteRaw("a,b\n\nc,d\n\n\ne,f\n", "blank-count.csv");
            string outPath = Path.Combine(_tempDir, "blank-count-out.csv");

            CsvRedactor.Redact(inPath, outPath, NoOp);

            Assert.Equal(6, File.ReadAllText(outPath).Count(ch => ch == '\n')); // same 6 line terminators as input
        }

        [Fact]
        public async Task RedactFileAsync_Csv_PreservesBlankLines_AndStillRedacts()
        {
            string inPath = WriteRaw("Email\nalice@example.com\n\nbob@example.com\n", "blank-pii.csv");
            string outPath = Path.Combine(_tempDir, "blank-pii-out.csv");

            await RedactionService.RedactFileAsync(inPath, outPath, EmailAndSsnPolicy(), "ctx");

            string result = await File.ReadAllTextAsync(outPath);
            Assert.DoesNotContain("alice@example.com", result);
            Assert.DoesNotContain("bob@example.com", result);
            Assert.Equal(4, result.Count(ch => ch == '\n')); // header + 2 data rows + 1 blank, all preserved
            Assert.Contains("\n\n", result);                  // the blank separator survives
        }

        // --- Column header as detection context -----------------------

        // A context-dependent detector: only flags the 9-digit token when an 'SSN' label is in the text,
        // so it fires only when the column header supplies that context. Mirrors how a context-sensitive
        // detector (e.g. the name model) behaves, deterministically.
        private static Phileas.Model.TextFilterResult LabelGated(string input)
        {
            const string token = "999999999";
            int idx = input.IndexOf(token, StringComparison.Ordinal);
            if (idx < 0 || input.IndexOf("SSN", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return new Phileas.Model.TextFilterResult(input, new List<Phileas.Model.Span>());
            }
            var span = new Phileas.Model.Span
            {
                CharacterStart = idx,
                CharacterEnd = idx + token.Length,
                Text = token,
                Replacement = "{{{REDACTED-ssn}}}",
                Classification = "SSN"
            };
            string filtered = input[..idx] + span.Replacement + input[(idx + token.Length)..];
            return new Phileas.Model.TextFilterResult(filtered, new List<Phileas.Model.Span> { span });
        }

        [Fact]
        public void Redact_UsesColumnHeaderAsContext_EnablingContextDependentDetection()
        {
            // Header "SSN" (field 0); data "999999999" (field 1). The value alone carries no label, so the
            // detector only fires because the header is supplied as context.
            string input = Write("SSN\r\n999999999\r\n", "hdr-ctx.csv");
            string output = Path.Combine(_tempDir, "hdr-ctx-out.csv");

            List<RedactionSpanEntity> spans = CsvRedactor.Redact(input, output, LabelGated);

            string result = File.ReadAllText(output);
            Assert.DoesNotContain("999999999", result); // value redacted thanks to header context
            Assert.Contains("SSN", result);             // header label preserved (never redacted)
            RedactionSpanEntity span = Assert.Single(spans);
            Assert.Equal(1, span.ParagraphIndex);       // the data cell (row-major field ordinal)
            Assert.Equal(0, span.CharacterStart);       // offset within the value, not the combined string
            Assert.Equal("999999999", span.Text);
        }

        [Fact]
        public void Redact_WithoutLabelHeader_ContextDependentDetectionDoesNotFire()
        {
            // Same value, but the header ("Number") gives no SSN context, so nothing is detected.
            string input = Write("Number\r\n999999999\r\n", "no-ctx.csv");
            string output = Path.Combine(_tempDir, "no-ctx-out.csv");

            List<RedactionSpanEntity> spans = CsvRedactor.Redact(input, output, LabelGated);

            Assert.Contains("999999999", File.ReadAllText(output));
            Assert.Empty(spans);
        }

        [Fact]
        public void Redact_HeaderContext_DropsMatchesInTheHeaderRegion_NeverRedactingIntoTheCell()
        {
            // Header label literally contains the token. When used as context for the data cell, that
            // match lands in the header region and must be dropped (never redacted into the data value).
            // The header row's own cell is still scanned and redacted on its own.
            string input = Write("SSN 999999999\r\nplain\r\n", "hdr-region.csv");
            string output = Path.Combine(_tempDir, "hdr-region-out.csv");

            List<RedactionSpanEntity> spans = CsvRedactor.Redact(input, output, LabelGated);

            string result = File.ReadAllText(output);
            Assert.Contains("plain", result);                          // data cell untouched
            Assert.DoesNotContain(spans, s => s.ParagraphIndex == 1);  // no detection mapped into the data cell
            RedactionSpanEntity headerSpan = Assert.Single(spans);     // only the header row's own value was redacted
            Assert.Equal(0, headerSpan.ParagraphIndex);
        }

        [Fact]
        public void Detect_UsesColumnHeaderAsContext_MatchingRedaction()
        {
            // Verification must apply the same header context so it stays symmetric with redaction.
            string input = Write("SSN\r\n999999999\r\n", "detect-ctx.csv");

            List<RedactionSpanEntity> residuals = CsvRedactor.Detect(input, LabelGated);

            RedactionSpanEntity d = Assert.Single(residuals);
            Assert.Equal(1, d.ParagraphIndex);
            Assert.Equal(0, d.CharacterStart);
            Assert.Equal("999999999", d.Text);
        }

        [Fact]
        public void Redact_HeaderContext_RoundTripsThroughApplySpans()
        {
            // The captured header-context spans re-apply correctly (offsets are value-relative).
            string input = Write("SSN\r\n999999999\r\n", "hdr-apply.csv");
            string firstPass = Path.Combine(_tempDir, "hdr-apply-1.csv");
            List<RedactionSpanEntity> spans = CsvRedactor.Redact(input, firstPass, LabelGated);

            string reapplied = Path.Combine(_tempDir, "hdr-apply-2.csv");
            CsvRedactor.ApplySpans(input, reapplied, spans);

            Assert.DoesNotContain("999999999", File.ReadAllText(reapplied));
            Assert.Contains("SSN", File.ReadAllText(reapplied)); // header preserved
        }
    }
}
