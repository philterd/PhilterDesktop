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
using Phileas.Services.Office;
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

        // Separator placed between a column header and a cell value when the header is supplied as
        // detection context (e.g. "Email: jane@x.com").
        private const string HeaderContextSeparator = ": ";

        // A detection within a single cell, with value-relative offsets and its source engine span.
        private readonly record struct CellDetection(int Start, int End, Span Source);

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
            string[]? headerRow = null;

            // Stream the table: read a row, redact it in place, write it, discard it. Whole-column
            // redaction only needs the (per-field) column index and the header check only needs the row
            // number — both are available in a single forward pass, so no second pass is required.
            StreamTransform(inputPath, outputPath, (row, rowNumber) =>
            {
                if (rowNumber == 0)
                {
                    headerRow = (string[])row.Clone(); // original header labels, used as detection context below
                }

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

                    List<CellDetection> dets = DetectInCell(filter, HeaderFor(headerRow, rowNumber, column), original);
                    if (dets.Count == 0)
                    {
                        continue;
                    }

                    var ranges = dets
                        .Select(d => new ReplacementRange(d.Start, d.End,
                            string.IsNullOrEmpty(d.Source.Replacement) ? DefaultReplacement : d.Source.Replacement))
                        .ToList();
                    List<ReplacementRange> resolved = RedactionSpanMath.ResolveNonOverlapping(ranges);
                    string redacted = ApplyRanges(original, resolved);
                    if (string.Equals(redacted, original, StringComparison.Ordinal))
                    {
                        continue;
                    }
                    row[column] = redacted;

                    foreach (CellDetection d in dets.OrderBy(d => d.Start))
                    {
                        var entity = new RedactionSpanEntity
                        {
                            Order = order++,
                            ParagraphIndex = currentIndex,
                            CharacterStart = d.Start,
                            CharacterEnd = d.End,
                            Text = original.Substring(d.Start, d.End - d.Start),
                            Replacement = d.Source.Replacement ?? string.Empty,
                            Classification = d.Source.Classification ?? string.Empty
                        };
                        SpanExplanation.Populate(entity, d.Source);
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

        /// <summary>
        /// Scans each field independently (exactly as <see cref="Redact"/> does) and returns residual
        /// detections — used by post-redaction verification so it matches per-field redaction instead of
        /// scanning the serialized file as one blob. Streams row by row; field ordinals match Redact's.
        /// </summary>
        public static List<RedactionSpanEntity> Detect(string inputPath, Func<string, TextFilterResult> filter)
        {
            var captured = new List<RedactionSpanEntity>();
            int order = 0;
            int fieldIndex = 0;
            string[]? headerRow = null;
            int rowNumber = 0;

            using var reader = new StreamReader(inputPath);
            using var parser = new CsvParser(reader, ReadConfig());
            while (parser.Read())
            {
                string[] row = parser.Record ?? Array.Empty<string>();
                if (rowNumber == 0)
                {
                    headerRow = (string[])row.Clone();
                }

                for (int column = 0; column < row.Length; column++)
                {
                    int currentIndex = fieldIndex++;
                    string original = row[column];
                    if (string.IsNullOrEmpty(original))
                    {
                        continue;
                    }

                    foreach (CellDetection d in DetectInCell(filter, HeaderFor(headerRow, rowNumber, column), original)
                        .OrderBy(d => d.Start))
                    {
                        var entity = new RedactionSpanEntity
                        {
                            Order = order++,
                            ParagraphIndex = currentIndex,
                            CharacterStart = d.Start,
                            CharacterEnd = d.End,
                            Text = original.Substring(d.Start, d.End - d.Start),
                            Replacement = d.Source.Replacement ?? string.Empty,
                            Classification = d.Source.Classification ?? string.Empty
                        };
                        SpanExplanation.Populate(entity, d.Source);
                        captured.Add(entity);
                    }
                }

                rowNumber++;
            }

            return captured;
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
            IgnoreBlankLines = false,   // keep blank/separator lines so the output has the same rows
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
        // the file is reopened, especially in Excel). Falls back to UTF-8 without a BOM. Shared with the
        // plain-text path via TextEncodingDetector so both formats preserve encoding the same way.
        private static Encoding DetectEncoding(string path) => TextEncodingDetector.Detect(path);

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

        // The column header to use as detection context for a data cell: the header row's label for this
        // column. Null for the header row itself or when there's no usable label, so those cells are
        // scanned on their own.
        private static string? HeaderFor(string[]? headerRow, int rowNumber, int column)
        {
            if (rowNumber == 0 || headerRow is null || column >= headerRow.Length)
            {
                return null;
            }
            string header = headerRow[column];
            return string.IsNullOrWhiteSpace(header) ? null : header;
        }

        // Detects PII in a cell. When a header is supplied it is prepended as leading context so
        // context-sensitive detectors (e.g. the on-device name model) can use it; the header is never
        // redacted because only detections lying entirely within the value are kept and their offsets are
        // mapped back to be value-relative.
        private static List<CellDetection> DetectInCell(Func<string, TextFilterResult> filter, string? header, string value)
        {
            int offset = 0;
            string input = value;
            if (!string.IsNullOrEmpty(header))
            {
                string prefix = header + HeaderContextSeparator;
                offset = prefix.Length;
                input = prefix + value;
            }

            var detections = new List<CellDetection>();
            foreach (Span s in filter(input).Spans)
            {
                int start = s.CharacterStart - offset;
                int end = s.CharacterEnd - offset;
                if (start >= 0 && end <= value.Length && end > start)
                {
                    detections.Add(new CellDetection(start, end, s));
                }
            }
            return detections;
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
