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

using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Phileas.Model;
using PhilterData;

namespace PhilterDesktop
{
    /// <summary>
    /// Redacts CSV files. Each field is treated as an independent cell (so values in different columns
    /// are never mis-joined): the field's text is run through the filter and, when changed, written
    /// back. Parsing and writing go through CsvHelper, which is quote- and delimiter-aware, so embedded
    /// commas, quotes, and newlines round-trip correctly. The output preserves the source's structure:
    /// the detected delimiter, the line-ending style (LF / CRLF / CR), and whether the file ended with a
    /// trailing newline.
    ///
    /// Processing is fully streamed — each row is read, redacted, written, and discarded — so the whole
    /// table is never materialized in memory, regardless of how many rows the file has. The output is
    /// written to a temporary file and moved into place only after the whole file is written, so the
    /// destination is never left as a partial file on failure.
    ///
    /// Fields are enumerated in a stable canonical order — row by row, then field by field — and the
    /// field's ordinal is stored in <see cref="RedactionSpanEntity.ParagraphIndex"/>, so spans can be
    /// re-applied via <see cref="ApplySpans"/>. Columns in <c>fullyRedactedColumns</c> have every
    /// non-empty field replaced outright.
    /// </summary>
    internal static class CsvRedactor
    {
        private const string DefaultReplacement = "{{{REDACTED-custom}}}";

        public static List<RedactionSpanEntity> Redact(
            string inputPath,
            string outputPath,
            Func<string, TextFilterResult> filter,
            IReadOnlyCollection<int>? fullyRedactedColumns = null)
        {
            var fullColumns = fullyRedactedColumns is { Count: > 0 }
                ? new HashSet<int>(fullyRedactedColumns)
                : new HashSet<int>();

            var captured = new List<RedactionSpanEntity>();
            int order = 0;
            int fieldIndex = 0;

            // Stream the table: read a row, redact it in place, write it, discard it. Whole-column
            // redaction only needs the (per-field) column index and the header check only needs the row
            // number — both are available in a single forward pass, so no second pass is required.
            StreamTransform(inputPath, outputPath, (row, rowNumber) =>
            {
                for (int column = 0; column < row.Length; column++)
                {
                    int currentIndex = fieldIndex++;
                    string original = row[column];
                    if (string.IsNullOrEmpty(original))
                    {
                        continue;
                    }

                    if (fullColumns.Contains(column + 1)) // columns are 1-based for the picker
                    {
                        if (rowNumber == 0)
                        {
                            continue; // keep the column's header label; only clear the data cells
                        }
                        row[column] = XlsxRedactor.ColumnReplacement;
                        captured.Add(new RedactionSpanEntity
                        {
                            Order = order++,
                            ParagraphIndex = currentIndex,
                            CharacterStart = 0,
                            CharacterEnd = original.Length,
                            Text = original,
                            Replacement = XlsxRedactor.ColumnReplacement,
                            Classification = XlsxRedactor.ColumnClassification
                        });
                        continue;
                    }

                    TextFilterResult result = filter(original);
                    if (string.Equals(result.FilteredText, original, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    row[column] = result.FilteredText;
                    foreach (Span s in result.Spans
                        .Where(s => s.CharacterStart >= 0 && s.CharacterEnd <= original.Length && s.CharacterEnd > s.CharacterStart)
                        .OrderBy(s => s.CharacterStart))
                    {
                        var entity = new RedactionSpanEntity
                        {
                            Order = order++,
                            ParagraphIndex = currentIndex,
                            CharacterStart = s.CharacterStart,
                            CharacterEnd = s.CharacterEnd,
                            Text = original.Substring(s.CharacterStart, s.CharacterEnd - s.CharacterStart),
                            Replacement = s.Replacement ?? string.Empty,
                            Classification = s.Classification ?? string.Empty
                        };
                        SpanExplanation.Populate(entity, s);
                        captured.Add(entity);
                    }
                }

                return row;
            });

            return captured;
        }

        public static void ApplySpans(string inputPath, string outputPath, IReadOnlyList<RedactionSpanEntity> spans)
        {
            Dictionary<int, List<RedactionSpanEntity>> byField = spans
                .GroupBy(s => s.ParagraphIndex)
                .ToDictionary(g => g.Key, g => g.ToList());

            int fieldIndex = 0;
            StreamTransform(inputPath, outputPath, (row, _) =>
            {
                for (int column = 0; column < row.Length; column++)
                {
                    int currentIndex = fieldIndex++;
                    string original = row[column];
                    if (string.IsNullOrEmpty(original) || !byField.TryGetValue(currentIndex, out List<RedactionSpanEntity>? fieldSpans))
                    {
                        continue;
                    }

                    var ranges = new List<ReplacementRange>();
                    foreach (RedactionSpanEntity s in fieldSpans)
                    {
                        if (s.CharacterStart >= 0 && s.CharacterEnd <= original.Length && s.CharacterEnd > s.CharacterStart)
                        {
                            string repl = string.IsNullOrEmpty(s.Replacement) ? DefaultReplacement : s.Replacement;
                            ranges.Add(new ReplacementRange(s.CharacterStart, s.CharacterEnd, repl));
                        }
                    }

                    List<ReplacementRange> resolved = RedactionSpanMath.ResolveNonOverlapping(ranges);
                    if (resolved.Count > 0)
                    {
                        row[column] = ApplyRanges(original, resolved);
                    }
                }

                return row;
            });
        }

        /// <summary>Reads the header (first) row as the column list for the "redact entire column" picker.</summary>
        public static List<SpreadsheetColumn> ReadColumns(string inputPath)
        {
            var columns = new List<SpreadsheetColumn>();

            string[]? header = null;
            int maxColumns = 0;

            // Stream the file once, keeping only the header row and the widest row's column count — never
            // the whole table.
            using (var reader = new StreamReader(inputPath))
            using (var parser = new CsvParser(reader, ReadConfig()))
            {
                while (parser.Read())
                {
                    string[] record = parser.Record ?? Array.Empty<string>();
                    header ??= record;
                    if (record.Length > maxColumns)
                    {
                        maxColumns = record.Length;
                    }
                }
            }

            if (header is null)
            {
                return columns;
            }

            for (int column = 0; column < maxColumns; column++)
            {
                string headerText = column < header.Length ? header[column] : string.Empty;
                columns.Add(new SpreadsheetColumn(column + 1, SpreadsheetColumn.IndexToLetter(column + 1), headerText));
            }
            return columns;
        }

        // --- CsvHelper streaming round-trip ----------------------------------

        private static CsvConfiguration ReadConfig() => new(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,    // treat every row as data; the first row is just row 0
            DetectDelimiter = true,
            BadDataFound = null         // tolerate quirky quoting rather than throwing
        };

        /// <summary>
        /// Reads each record, hands it to <paramref name="transform"/> (which may edit the field array
        /// in place and returns the row to write), writes it, then discards it — so only one row is held
        /// at a time. The result is written to a temporary file and moved into place only once the whole
        /// file is written, so a mid-stream failure never leaves a partial or corrupt file at
        /// <paramref name="outputPath"/> (and never touches the original).
        /// </summary>
        private static void StreamTransform(
            string inputPath,
            string outputPath,
            Func<string[], int, string[]> transform)
        {
            Encoding encoding = DetectEncoding(inputPath);
            // Preserve the source's line-ending style and trailing-newline (CsvHelper defaults to CRLF + trailing).
            string newLine = DetectLineEnding(inputPath, encoding) ?? "\r\n";
            bool sourceEndsWithNewline = EndsWithNewline(inputPath, encoding);
            string tempPath = outputPath + ".redacting-" + Guid.NewGuid().ToString("N") + ".tmp";
            bool committed = false;
            bool wroteRecords = false;

            try
            {
                using (var reader = new StreamReader(inputPath))
                using (var parser = new CsvParser(reader, ReadConfig()))
                {
                    if (!parser.Read())
                    {
                        // Empty input -> empty output (matches writing an empty set of rows).
                        using (File.Create(tempPath)) { }
                    }
                    else
                    {
                        wroteRecords = true;

                        // The delimiter is detected on the first read; reuse it so the output keeps the
                        // same delimiter as the source.
                        string delimiter = string.IsNullOrEmpty(parser.Delimiter) ? "," : parser.Delimiter;
                        var writeConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                        {
                            HasHeaderRecord = false,
                            Delimiter = delimiter,
                            NewLine = newLine, // keep the source's line-ending style (LF / CRLF / CR)
                            // Guard against CSV/formula injection: a field beginning with = + - @ (or a
                            // tab/CR) is executable when the redacted file is opened in Excel/Sheets.
                            // Escape prefixes such a field with an apostrophe so it's treated as text.
                            InjectionOptions = InjectionOptions.Escape
                        };

                        using var output = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
                        using var writer = new StreamWriter(output, encoding);
                        using var csv = new CsvWriter(writer, writeConfig);

                        int rowNumber = 0;
                        do
                        {
                            string[] row = parser.Record ?? Array.Empty<string>();
                            string[] outRow = transform(row, rowNumber);
                            foreach (string field in outRow)
                            {
                                csv.WriteField(field);
                            }
                            csv.NextRecord();
                            rowNumber++;
                        }
                        while (parser.Read());
                    }
                }

                // CsvWriter terminates every record; drop the last terminator if the source had none.
                if (wroteRecords && !sourceEndsWithNewline)
                {
                    TrimFinalNewline(tempPath, encoding, newLine);
                }

                // Atomically swap the completed file into place. Done only after every row is written and
                // all writers are flushed/closed, so the destination only ever appears complete.
                File.Move(tempPath, outputPath, overwrite: true);
                committed = true;
            }
            finally
            {
                if (!committed)
                {
                    TryDelete(tempPath);
                }
            }
        }

        // Chooses the output encoding to match the source so the redacted CSV keeps the same byte-order
        // mark and character encoding (otherwise non-ASCII — e.g. accented names — can be mangled when
        // the file is reopened, especially in Excel). Falls back to UTF-8 without a BOM.
        private static Encoding DetectEncoding(string path)
        {
            try
            {
                using FileStream fs = File.OpenRead(path);
                Span<byte> bom = stackalloc byte[3];
                int read = fs.Read(bom);
                if (read >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                {
                    return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);    // UTF-8 with BOM
                }
                if (read >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)
                {
                    return new UnicodeEncoding(bigEndian: false, byteOrderMark: true); // UTF-16 LE
                }
                if (read >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)
                {
                    return new UnicodeEncoding(bigEndian: true, byteOrderMark: true);  // UTF-16 BE
                }
            }
            catch
            {
                // unreadable preamble — fall through to the default
            }
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);           // UTF-8, no BOM
        }

        // The source's first line-ending ("\r\n", "\n", or "\r"), or null if it has no line break.
        private static string? DetectLineEnding(string path, Encoding encoding)
        {
            try
            {
                using var reader = new StreamReader(path, encoding);
                int ch;
                while ((ch = reader.Read()) != -1)
                {
                    if (ch == '\r')
                    {
                        return reader.Peek() == '\n' ? "\r\n" : "\r";
                    }
                    if (ch == '\n')
                    {
                        return "\n";
                    }
                }
            }
            catch
            {
                // unreadable — fall back to the caller's default
            }
            return null;
        }

        // True when the file's final code unit is a line terminator (LF or CR), in the given encoding.
        private static bool EndsWithNewline(string path, Encoding encoding)
        {
            try
            {
                byte[] lf = encoding.GetBytes("\n");
                byte[] cr = encoding.GetBytes("\r");
                int unit = lf.Length; // bytes per code unit (1 for UTF-8, 2 for UTF-16)
                using FileStream fs = File.OpenRead(path);
                if (fs.Length < unit)
                {
                    return false;
                }
                fs.Seek(-unit, SeekOrigin.End);
                var tail = new byte[unit];
                if (fs.Read(tail, 0, unit) != unit)
                {
                    return false;
                }
                return tail.AsSpan().SequenceEqual(lf) || tail.AsSpan().SequenceEqual(cr);
            }
            catch
            {
                return true; // if unsure, assume trailing newline so we never strip real content
            }
        }

        // Removes the terminator CsvWriter appended after the last record (one newLine's worth of bytes).
        private static void TrimFinalNewline(string path, Encoding encoding, string newLine)
        {
            int terminatorBytes = encoding.GetByteCount(newLine);
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None);
            if (fs.Length >= terminatorBytes)
            {
                fs.SetLength(fs.Length - terminatorBytes);
            }
        }

        private static string ApplyRanges(string original, IEnumerable<ReplacementRange> ranges)
        {
            var sb = new StringBuilder(original);
            foreach (ReplacementRange r in ranges.OrderByDescending(r => r.Start))
            {
                if (r.Start < 0 || r.End > original.Length || r.End <= r.Start)
                {
                    continue;
                }
                sb.Remove(r.Start, r.End - r.Start);
                sb.Insert(r.Start, r.Replacement ?? string.Empty);
            }
            return sb.ToString();
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // best effort
            }
        }
    }
}
