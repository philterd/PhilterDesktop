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

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Phileas.Model;
using PhilterData;

namespace PhilterDesktop
{
    /// <summary>
    /// Redacts Excel (<c>.xlsx</c>) workbooks with the open-source Open XML SDK. Each cell is treated as
    /// an independent unit (so values in different cells are never mis-joined): the cell's text is run
    /// through the filter and, when changed, written back as an <em>inline string</em> — which detaches
    /// it from the shared-string table so redacting one cell can't alter an unrelated cell that happened
    /// to share the same string.
    ///
    /// Cells are enumerated in a stable canonical order — workbook sheet order, then rows, then cells —
    /// and the cell's ordinal is stored in <see cref="RedactionSpanEntity.ParagraphIndex"/> (exactly as
    /// the Word redactor uses a paragraph index), so spans can be re-applied via <see cref="ApplySpans"/>.
    ///
    /// Formula cells are left untouched (their displayed value is computed). Numeric/date cells are only
    /// redacted when their column is explicitly selected for full redaction; otherwise detection runs on
    /// text cells. Columns in <c>fullyRedactedColumns</c> have every non-empty cell replaced outright.
    /// </summary>
    internal static class XlsxRedactor
    {
        private const string DefaultReplacement = "{{{REDACTED-custom}}}";

        /// <summary>The replacement written into cells of a fully-redacted column.</summary>
        public const string ColumnReplacement = "{{{REDACTED}}}";

        /// <summary>The classification recorded for a cell removed because its whole column was selected.</summary>
        public const string ColumnClassification = "full-column";

        public static List<RedactionSpanEntity> Redact(
            string inputPath,
            string outputPath,
            Func<string, TextFilterResult> filter,
            IReadOnlyCollection<int>? fullyRedactedColumns = null)
        {
            File.Copy(inputPath, outputPath, overwrite: true);

            var fullColumns = fullyRedactedColumns is { Count: > 0 }
                ? new HashSet<int>(fullyRedactedColumns)
                : new HashSet<int>();

            var captured = new List<RedactionSpanEntity>();
            int order = 0;
            int cellIndex = 0;

            using SpreadsheetDocument document = SpreadsheetDocument.Open(outputPath, isEditable: true);
            WorkbookPart? workbookPart = document.WorkbookPart;
            if (workbookPart is null)
            {
                return captured;
            }

            foreach ((Cell cell, bool isHeaderRow) in EnumerateCells(workbookPart))
            {
                int currentIndex = cellIndex++;

                if (cell.CellFormula is not null)
                {
                    continue; // computed value — don't touch
                }

                string original = GetCellText(cell, workbookPart);
                if (string.IsNullOrEmpty(original))
                {
                    continue;
                }

                bool fullColumn = fullColumns.Contains(SpreadsheetColumn.LetterToIndex(cell.CellReference));

                if (fullColumn)
                {
                    if (isHeaderRow)
                    {
                        continue; // keep the column's header label; only clear the data cells
                    }
                    SetCellText(cell, ColumnReplacement);
                    var entity = new RedactionSpanEntity
                    {
                        Order = order++,
                        ParagraphIndex = currentIndex,
                        CharacterStart = 0,
                        CharacterEnd = original.Length,
                        Text = original,
                        Replacement = ColumnReplacement,
                        Classification = ColumnClassification
                    };
                    captured.Add(entity);
                    continue;
                }

                if (!IsTextCell(cell))
                {
                    continue; // numbers/dates left as-is unless their column is fully redacted
                }

                TextFilterResult result = filter(original);
                if (string.Equals(result.FilteredText, original, StringComparison.Ordinal))
                {
                    continue;
                }

                SetCellText(cell, result.FilteredText);
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

            return captured;
        }

        /// <summary>
        /// Detects redactions for <paramref name="inputPath"/> using <paramref name="filter"/> without
        /// writing anything, returning the cell-indexed spans the text-cell pass of <see cref="Redact"/>
        /// would apply. Used by the post-redaction verification pass to scan a written output. (Whole-
        /// column redaction isn't reproduced here — verification looks for content the detector flags.)
        /// </summary>
        public static List<RedactionSpanEntity> Detect(string inputPath, Func<string, TextFilterResult> filter)
        {
            using SpreadsheetDocument document = SpreadsheetDocument.Open(inputPath, isEditable: false);
            WorkbookPart? workbookPart = document.WorkbookPart;
            var captured = new List<RedactionSpanEntity>();
            if (workbookPart is null)
            {
                return captured;
            }

            int order = 0;
            int cellIndex = 0;
            foreach ((Cell cell, bool _) in EnumerateCells(workbookPart))
            {
                int currentIndex = cellIndex++;
                if (cell.CellFormula is not null || !IsTextCell(cell))
                {
                    continue;
                }

                string original = GetCellText(cell, workbookPart);
                if (string.IsNullOrEmpty(original))
                {
                    continue;
                }

                foreach (Span s in filter(original).Spans
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

            return captured;
        }

        public static void ApplySpans(string inputPath, string outputPath, IReadOnlyList<RedactionSpanEntity> spans)
        {
            File.Copy(inputPath, outputPath, overwrite: true);

            Dictionary<int, List<RedactionSpanEntity>> byCell = spans
                .GroupBy(s => s.ParagraphIndex)
                .ToDictionary(g => g.Key, g => g.ToList());

            using SpreadsheetDocument document = SpreadsheetDocument.Open(outputPath, isEditable: true);
            WorkbookPart? workbookPart = document.WorkbookPart;
            if (workbookPart is null)
            {
                return;
            }

            int cellIndex = 0;
            foreach ((Cell cell, bool _) in EnumerateCells(workbookPart))
            {
                int currentIndex = cellIndex++;
                if (cell.CellFormula is not null || !byCell.TryGetValue(currentIndex, out List<RedactionSpanEntity>? cellSpans))
                {
                    continue;
                }

                string original = GetCellText(cell, workbookPart);
                if (string.IsNullOrEmpty(original))
                {
                    continue;
                }

                var ranges = new List<ReplacementRange>();
                foreach (RedactionSpanEntity s in cellSpans)
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
                    SetCellText(cell, ApplyRanges(original, resolved));
                }
            }
        }

        /// <summary>
        /// Reads the columns of the first worksheet for the "redact entire column" picker: every column
        /// that has data, with its letter and the header text from the first row (when present).
        /// </summary>
        public static List<SpreadsheetColumn> ReadColumns(string inputPath)
        {
            using SpreadsheetDocument document = SpreadsheetDocument.Open(inputPath, isEditable: false);
            WorkbookPart? workbookPart = document.WorkbookPart;
            WorksheetPart? sheetPart = workbookPart?.WorksheetParts.FirstOrDefault();
            SheetData? sheetData = sheetPart?.Worksheet?.GetFirstChild<SheetData>();
            if (workbookPart is null || sheetData is null)
            {
                return new List<SpreadsheetColumn>();
            }

            var headers = new Dictionary<int, string>();
            int maxColumn = 0;

            List<Row> rows = sheetData.Elements<Row>().ToList();
            Row? headerRow = rows.FirstOrDefault();
            if (headerRow is not null)
            {
                foreach (Cell cell in headerRow.Elements<Cell>())
                {
                    int col = SpreadsheetColumn.LetterToIndex(cell.CellReference);
                    if (col > 0)
                    {
                        headers[col] = GetCellText(cell, workbookPart);
                    }
                }
            }

            // Determine the widest column across the sheet so columns with no header still appear.
            foreach (Row row in rows)
            {
                foreach (Cell cell in row.Elements<Cell>())
                {
                    maxColumn = Math.Max(maxColumn, SpreadsheetColumn.LetterToIndex(cell.CellReference));
                }
            }

            var columns = new List<SpreadsheetColumn>();
            for (int col = 1; col <= maxColumn; col++)
            {
                headers.TryGetValue(col, out string? header);
                columns.Add(new SpreadsheetColumn(col, SpreadsheetColumn.IndexToLetter(col), header ?? string.Empty));
            }
            return columns;
        }

        // --- helpers ----------------------------------------------------------

        // Canonical order: sheets in workbook order, then rows, then cells (document order). The flag
        // marks cells in each sheet's first (header) row, which full-column redaction leaves intact.
        private static IEnumerable<(Cell Cell, bool IsHeaderRow)> EnumerateCells(WorkbookPart workbookPart)
        {
            Sheets? sheets = workbookPart.Workbook?.Sheets;
            if (sheets is null)
            {
                yield break;
            }

            foreach (Sheet sheet in sheets.Elements<Sheet>())
            {
                if (sheet.Id?.Value is not string relationshipId)
                {
                    continue;
                }
                if (workbookPart.GetPartById(relationshipId) is not WorksheetPart worksheetPart)
                {
                    continue;
                }
                SheetData? sheetData = worksheetPart.Worksheet?.GetFirstChild<SheetData>();
                if (sheetData is null)
                {
                    continue;
                }
                bool isHeaderRow = true;
                foreach (Row row in sheetData.Elements<Row>())
                {
                    foreach (Cell cell in row.Elements<Cell>())
                    {
                        yield return (cell, isHeaderRow);
                    }
                    isHeaderRow = false;
                }
            }
        }

        private static bool IsTextCell(Cell cell)
        {
            CellValues? type = cell.DataType?.Value;
            return type == CellValues.SharedString || type == CellValues.InlineString || type == CellValues.String;
        }

        private static string GetCellText(Cell cell, WorkbookPart workbookPart)
        {
            if (cell.DataType?.Value == CellValues.SharedString)
            {
                SharedStringTablePart? sst = workbookPart.SharedStringTablePart;
                if (sst is not null && int.TryParse(cell.CellValue?.InnerText, out int index))
                {
                    SharedStringItem? item = sst.SharedStringTable?.Elements<SharedStringItem>().ElementAtOrDefault(index);
                    return item?.InnerText ?? string.Empty;
                }
                return string.Empty;
            }
            if (cell.DataType?.Value == CellValues.InlineString)
            {
                return cell.InlineString?.Text?.Text ?? cell.InlineString?.InnerText ?? string.Empty;
            }
            return cell.CellValue?.InnerText ?? string.Empty;
        }

        // Writes a plain string into a cell as an inline string (detaching it from the shared-string
        // table), removing any previous value so the redacted text is exactly what's shown.
        private static void SetCellText(Cell cell, string value)
        {
            cell.RemoveAllChildren();
            cell.DataType = new EnumValue<CellValues>(CellValues.InlineString);
            cell.AppendChild(new InlineString(new Text(value) { Space = SpaceProcessingModeValues.Preserve }));
        }

        private static string ApplyRanges(string original, IEnumerable<ReplacementRange> ranges)
        {
            var sb = new System.Text.StringBuilder(original);
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
