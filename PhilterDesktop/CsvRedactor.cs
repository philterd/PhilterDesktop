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
    /// commas, quotes, and newlines round-trip correctly (and the detected delimiter is preserved).
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

            List<string[]> rows = ReadRows(inputPath, out string delimiter);

            var captured = new List<RedactionSpanEntity>();
            int order = 0;
            int fieldIndex = 0;

            for (int rowNumber = 0; rowNumber < rows.Count; rowNumber++)
            {
                string[] row = rows[rowNumber];
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
            }

            WriteRows(outputPath, rows, delimiter, DetectEncoding(inputPath));
            return captured;
        }

        public static void ApplySpans(string inputPath, string outputPath, IReadOnlyList<RedactionSpanEntity> spans)
        {
            Dictionary<int, List<RedactionSpanEntity>> byField = spans
                .GroupBy(s => s.ParagraphIndex)
                .ToDictionary(g => g.Key, g => g.ToList());

            List<string[]> rows = ReadRows(inputPath, out string delimiter);

            int fieldIndex = 0;
            foreach (string[] row in rows)
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
            }

            WriteRows(outputPath, rows, delimiter, DetectEncoding(inputPath));
        }

        /// <summary>Reads the header (first) row as the column list for the "redact entire column" picker.</summary>
        public static List<SpreadsheetColumn> ReadColumns(string inputPath)
        {
            List<string[]> rows = ReadRows(inputPath, out _);
            var columns = new List<SpreadsheetColumn>();
            if (rows.Count == 0)
            {
                return columns;
            }

            int maxColumns = rows.Max(r => r.Length);
            string[] header = rows[0];
            for (int column = 0; column < maxColumns; column++)
            {
                string headerText = column < header.Length ? header[column] : string.Empty;
                columns.Add(new SpreadsheetColumn(column + 1, SpreadsheetColumn.IndexToLetter(column + 1), headerText));
            }
            return columns;
        }

        // --- CsvHelper round-trip --------------------------------------------

        private static List<string[]> ReadRows(string inputPath, out string delimiter)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,    // treat every row as data; the first row is just row 0
                DetectDelimiter = true,
                BadDataFound = null         // tolerate quirky quoting rather than throwing
            };

            using var reader = new StreamReader(inputPath);
            using var parser = new CsvParser(reader, config);

            var rows = new List<string[]>();
            string detected = ",";
            while (parser.Read())
            {
                detected = parser.Delimiter;
                rows.Add(parser.Record ?? Array.Empty<string>());
            }
            delimiter = string.IsNullOrEmpty(detected) ? "," : detected;
            return rows;
        }

        private static void WriteRows(string outputPath, List<string[]> rows, string delimiter, Encoding encoding)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
                Delimiter = delimiter,
                // Guard against CSV/formula injection: a field beginning with = + - @ (or a tab/CR) is
                // executable when the redacted file is opened in Excel/Sheets. Escape prefixes such a
                // field with an apostrophe so the recipient's spreadsheet treats it as plain text.
                InjectionOptions = InjectionOptions.Escape
            };

            // Build in memory, then write once so a failure never leaves the original or a partial file (issue #483).
            using var buffer = new MemoryStream();
            using (var writer = new StreamWriter(buffer, encoding, leaveOpen: true))
            using (var csv = new CsvWriter(writer, config))
            {
                foreach (string[] row in rows)
                {
                    foreach (string field in row)
                    {
                        csv.WriteField(field);
                    }
                    csv.NextRecord();
                }
            }

            SafeOutput.Write(outputPath, buffer.ToArray());
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
    }
}
