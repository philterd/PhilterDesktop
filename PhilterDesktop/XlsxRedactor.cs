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
using DocumentFormat.OpenXml.Office2019.Excel.ThreadedComments;
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
    /// to share the same string. After redaction, any shared-string entry no longer referenced by a cell
    /// is blanked, so an original value can't be recovered from <c>xl/sharedStrings.xml</c>.
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
            IReadOnlyCollection<int>? fullyRedactedColumns = null,
            string? worksheet = null,
            bool redactHeadersFooters = true,
            bool redactCharts = true,
            bool redactFormulaValues = true)
        {
            var fullColumns = fullyRedactedColumns is { Count: > 0 }
                ? new HashSet<int>(fullyRedactedColumns)
                : new HashSet<int>();

            var captured = new List<RedactionSpanEntity>();
            int order = 0;
            int cellIndex = 0;
            bool anyCellRedacted = false;

            // Redact in memory, then write once so a failure never leaves the original or a partial file.
            using MemoryStream buffer = SafeOutput.ReadToEditableStream(inputPath);
            using SpreadsheetDocument document = SpreadsheetDocument.Open(buffer, isEditable: true);
            WorkbookPart? workbookPart = document.WorkbookPart;
            if (workbookPart is null)
            {
                document.Save();
                SafeOutput.Write(outputPath, buffer.ToArray());
                return captured;
            }

            foreach ((Cell cell, bool isHeaderRow, int column) in EnumerateCells(workbookPart, worksheet))
            {
                int currentIndex = cellIndex++;

                // A formula cell stores a cached copy of its last computed value, which can duplicate PII
                // from a now-redacted cell. When the option is off, leave formula cells untouched; when on,
                // scan (and, below, staticize) the cached value like any other cell.
                if (cell.CellFormula is not null && !redactFormulaValues)
                {
                    continue;
                }

                string original = GetCellText(cell, workbookPart);
                if (string.IsNullOrEmpty(original))
                {
                    continue;
                }

                bool fullColumn = fullColumns.Contains(column);

                if (fullColumn)
                {
                    if (isHeaderRow)
                    {
                        continue; // keep the column's header label; only clear the data cells
                    }
                    SetCellText(cell, ColumnReplacement); // also drops any formula (RemoveAllChildren)
                    anyCellRedacted = true;
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

                if (!IsScannableCell(cell))
                {
                    continue; // boolean/error cells carry no free-text PII
                }

                TextFilterResult result = filter(original);
                if (string.Equals(result.FilteredText, original, StringComparison.Ordinal))
                {
                    continue;
                }

                SetCellText(cell, result.FilteredText); // for a formula cell this drops the formula too
                anyCellRedacted = true;
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

            // Print headers/footers are free text that prints on every page (e.g. "Confidential — John
            // Doe") but aren't cells, so they need a separate pass.
            if (redactHeadersFooters)
            {
                ScanHeadersFooters(workbookPart, worksheet, filter, write: true, captured, ref order);
            }

            // Cell comments (legacy and threaded) carry free text that isn't in any cell — redact it too.
            ScanComments(workbookPart, worksheet, filter, write: true, captured, ref order);

            // Embedded charts cache the plotted cell values (and carry title/label text) — redact those.
            if (redactCharts)
            {
                ScanCharts(workbookPart, worksheet, filter, write: true, captured, ref order);
            }

            // A formula whose cached result was detected as PII was already staticized above. Any remaining
            // formula cache could still hold a stale copy derived from a redacted cell, so clear the caches
            // and make Excel recalculate on open. Only when a cell was actually redacted (else leave intact).
            if (redactFormulaValues && anyCellRedacted)
            {
                ClearFormulaCachesAndForceRecalc(workbookPart, worksheet);
            }

            // Redacted cells were converted to inline strings; blank any shared-string entries they left
            // orphaned so the original text can't be recovered from xl/sharedStrings.xml.
            PruneUnusedSharedStrings(workbookPart);

            document.Save(); // flush into the buffer
            SafeOutput.Write(outputPath, buffer.ToArray());
            return captured;
        }

        /// <summary>
        /// Detects redactions for <paramref name="inputPath"/> using <paramref name="filter"/> without
        /// writing anything, returning the cell-indexed spans the text-cell pass of <see cref="Redact"/>
        /// would apply. Used by the post-redaction verification pass to scan a written output. (Whole-
        /// column redaction isn't reproduced here — verification looks for content the detector flags.)
        /// </summary>
        public static List<RedactionSpanEntity> Detect(string inputPath, Func<string, TextFilterResult> filter, string? worksheet = null, bool redactHeadersFooters = true, bool redactCharts = true)
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
            foreach ((Cell cell, bool _, int _) in EnumerateCells(workbookPart, worksheet))
            {
                int currentIndex = cellIndex++;
                // Scan formula cells' cached results too — regardless of the redaction option — so a
                // residual PII value left in a formula cache is still caught by verification.
                if (!IsScannableCell(cell))
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

            // Scan the print headers/footers too, so verification catches PII left in them.
            if (redactHeadersFooters)
            {
                ScanHeadersFooters(workbookPart, worksheet, filter, write: false, captured, ref order);
            }
            // Scan cell comments too, so verification catches PII left in them.
            ScanComments(workbookPart, worksheet, filter, write: false, captured, ref order);
            // Scan embedded charts too.
            if (redactCharts)
            {
                ScanCharts(workbookPart, worksheet, filter, write: false, captured, ref order);
            }

            return captured;
        }

        public static void ApplySpans(string inputPath, string outputPath, IReadOnlyList<RedactionSpanEntity> spans, string? worksheet = null,
            Func<string, TextFilterResult>? policyFilter = null, bool redactHeadersFooters = true, bool redactCharts = true, bool redactFormulaValues = true)
        {
            Dictionary<int, List<RedactionSpanEntity>> byCell = spans
                .GroupBy(s => s.ParagraphIndex)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Apply in memory, then write once (see Redact).
            using MemoryStream buffer = SafeOutput.ReadToEditableStream(inputPath);
            using SpreadsheetDocument document = SpreadsheetDocument.Open(buffer, isEditable: true);
            WorkbookPart? workbookPart = document.WorkbookPart;
            if (workbookPart is null)
            {
                document.Save();
                SafeOutput.Write(outputPath, buffer.ToArray());
                return;
            }

            int cellIndex = 0;
            bool anyCellRedacted = false;
            foreach ((Cell cell, bool _, int _) in EnumerateCells(workbookPart, worksheet))
            {
                int currentIndex = cellIndex++;
                // Formula cells are re-redacted (their cached value staticized) only when the option is on.
                if ((cell.CellFormula is not null && !redactFormulaValues) ||
                    !byCell.TryGetValue(currentIndex, out List<RedactionSpanEntity>? cellSpans))
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
                    SetCellText(cell, ApplyRanges(original, resolved)); // drops the formula for a formula cell
                    anyCellRedacted = true;
                }
            }

            if (redactFormulaValues && anyCellRedacted)
            {
                ClearFormulaCachesAndForceRecalc(workbookPart, worksheet);
            }

            // Header/footer text and comments aren't stored by position (they're not cells), so re-redact
            // them via the filter when one is supplied (parity with how Word drawings re-render in Modify).
            if (policyFilter is not null)
            {
                int order = 0;
                if (redactHeadersFooters)
                {
                    ScanHeadersFooters(workbookPart, worksheet, policyFilter, write: true, captured: null, ref order);
                }
                ScanComments(workbookPart, worksheet, policyFilter, write: true, captured: null, ref order);
                if (redactCharts)
                {
                    ScanCharts(workbookPart, worksheet, policyFilter, write: true, captured: null, ref order);
                }
            }

            PruneUnusedSharedStrings(workbookPart);

            document.Save(); // flush into the buffer
            SafeOutput.Write(outputPath, buffer.ToArray());
        }

        /// <summary>
        /// Reads the columns of the first worksheet for the "redact entire column" picker: every column
        /// that has data, with its letter and the header text from the first row (when present).
        /// </summary>
        /// <summary>The worksheet names in workbook order (for the single-worksheet picker).</summary>
        public static List<string> ReadSheetNames(string inputPath)
        {
            using SpreadsheetDocument document = SpreadsheetDocument.Open(inputPath, isEditable: false);
            Sheets? sheets = document.WorkbookPart?.Workbook?.Sheets;
            if (sheets is null)
            {
                return new List<string>();
            }
            return sheets.Elements<Sheet>()
                .Select(s => s.Name?.Value ?? string.Empty)
                .Where(n => n.Length > 0)
                .ToList();
        }

        // Resolves a worksheet by name (or the first sheet when the name is empty) to its part.
        private static WorksheetPart? FindWorksheetPart(WorkbookPart? workbookPart, string? sheetName)
        {
            if (workbookPart is null)
            {
                return null;
            }
            Sheets? sheets = workbookPart.Workbook?.Sheets;
            if (sheets is null)
            {
                return workbookPart.WorksheetParts.FirstOrDefault();
            }
            Sheet? sheet = string.IsNullOrEmpty(sheetName)
                ? sheets.Elements<Sheet>().FirstOrDefault()
                : sheets.Elements<Sheet>().FirstOrDefault(s => string.Equals(s.Name?.Value, sheetName, StringComparison.Ordinal));
            return sheet?.Id?.Value is string relId && workbookPart.GetPartById(relId) is WorksheetPart part ? part : null;
        }

        public static List<SpreadsheetColumn> ReadColumns(string inputPath, string? sheetName = null)
        {
            using SpreadsheetDocument document = SpreadsheetDocument.Open(inputPath, isEditable: false);
            WorkbookPart? workbookPart = document.WorkbookPart;
            WorksheetPart? sheetPart = FindWorksheetPart(workbookPart, sheetName);
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
        // marks cells in each sheet's first (header) row, which full-column redaction leaves intact. The
        // 1-based Column comes from the cell's reference (e.g. "B3" -> 2); when a cell omits its reference
        // (some generators do), it's the previous cell's column + 1, so it still maps to a column.
        private static IEnumerable<(Cell Cell, bool IsHeaderRow, int Column)> EnumerateCells(
            WorkbookPart workbookPart, string? worksheet = null)
        {
            Sheets? sheets = workbookPart.Workbook?.Sheets;
            if (sheets is null)
            {
                yield break;
            }

            foreach (Sheet sheet in sheets.Elements<Sheet>())
            {
                // A named worksheet limits redaction to that single sheet; empty means the whole workbook.
                if (!string.IsNullOrEmpty(worksheet) && !string.Equals(sheet.Name?.Value, worksheet, StringComparison.Ordinal))
                {
                    continue;
                }
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
                    int column = 0;
                    foreach (Cell cell in row.Elements<Cell>())
                    {
                        int referenced = SpreadsheetColumn.LetterToIndex(cell.CellReference);
                        column = referenced > 0 ? referenced : column + 1;
                        yield return (cell, isHeaderRow, column);
                    }
                    isHeaderRow = false;
                }
            }
        }

        // The worksheet parts redaction processes: a single named sheet, or all of them when the name is
        // empty. Mirrors the sheet selection in EnumerateCells.
        private static IEnumerable<WorksheetPart> ProcessedWorksheetParts(WorkbookPart workbookPart, string? worksheet)
        {
            Sheets? sheets = workbookPart.Workbook?.Sheets;
            if (sheets is null)
            {
                yield break;
            }
            foreach (Sheet sheet in sheets.Elements<Sheet>())
            {
                if (!string.IsNullOrEmpty(worksheet) && !string.Equals(sheet.Name?.Value, worksheet, StringComparison.Ordinal))
                {
                    continue;
                }
                if (sheet.Id?.Value is string relationshipId && workbookPart.GetPartById(relationshipId) is WorksheetPart part)
                {
                    yield return part;
                }
            }
        }

        // Redacts (write: true) or detects (write: false) PII in the print header/footer strings of each
        // processed worksheet. Excel field codes (&P page, &D date, &"font,style", &L/&C/&R section markers)
        // aren't PII and pass through the filter untouched. Header/footer text isn't a cell, so captured
        // spans use ParagraphIndex -1 (report-only); Modify re-redacts these via the filter, not by position.
        private static void ScanHeadersFooters(
            WorkbookPart workbookPart, string? worksheet, Func<string, TextFilterResult> filter,
            bool write, List<RedactionSpanEntity>? captured, ref int order)
        {
            foreach (WorksheetPart part in ProcessedWorksheetParts(workbookPart, worksheet))
            {
                HeaderFooter? headerFooter = part.Worksheet?.GetFirstChild<HeaderFooter>();
                if (headerFooter is null)
                {
                    continue;
                }

                // The six children (odd/even/first × header/footer) are all leaf text elements.
                foreach (OpenXmlLeafTextElement element in headerFooter.Elements<OpenXmlLeafTextElement>().ToList())
                {
                    string original = element.Text ?? string.Empty;
                    if (string.IsNullOrEmpty(original))
                    {
                        continue;
                    }

                    TextFilterResult result = filter(original);
                    if (string.Equals(result.FilteredText, original, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (write)
                    {
                        element.Text = result.FilteredText;
                    }

                    if (captured is null)
                    {
                        continue;
                    }
                    foreach (Span s in result.Spans
                        .Where(s => s.CharacterStart >= 0 && s.CharacterEnd <= original.Length && s.CharacterEnd > s.CharacterStart)
                        .OrderBy(s => s.CharacterStart))
                    {
                        var entity = new RedactionSpanEntity
                        {
                            Order = order++,
                            ParagraphIndex = -1, // header/footer, not a cell
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
        }

        // Redacts (write: true) or detects (write: false) PII in cell comments: the legacy comment store
        // (xl/comments*.xml) and the modern threaded comments (xl/threadedComments/*.xml), plus the
        // workbook's threaded-comment author list (xl/persons/*.xml). Comment text isn't a cell, so it's
        // run through the policy filter here (Word redacts comment text the same way) and captured with
        // ParagraphIndex -1. Author/person names are filtered too, so policy-detected identity PII goes.
        private static void ScanComments(
            WorkbookPart workbookPart, string? worksheet, Func<string, TextFilterResult> filter,
            bool write, List<RedactionSpanEntity>? captured, ref int order)
        {
            foreach (WorksheetPart part in ProcessedWorksheetParts(workbookPart, worksheet))
            {
                // Legacy cell comments: comment body runs (<t>) and the per-sheet author list.
                if (part.WorksheetCommentsPart?.Comments is Comments comments)
                {
                    foreach (Text text in comments.Descendants<Text>().ToList())
                    {
                        RedactCommentLeaf(text, filter, write, captured, ref order);
                    }
                    foreach (Author author in comments.Descendants<Author>().ToList())
                    {
                        RedactCommentLeaf(author, filter, write, captured, ref order);
                    }
                }

                // Modern threaded comments (the source of truth in current Excel).
                foreach (WorksheetThreadedCommentsPart threadedPart in part.WorksheetThreadedCommentsParts)
                {
                    if (threadedPart.ThreadedComments is null)
                    {
                        continue;
                    }
                    foreach (ThreadedCommentText text in threadedPart.ThreadedComments.Descendants<ThreadedCommentText>().ToList())
                    {
                        RedactCommentLeaf(text, filter, write, captured, ref order);
                    }
                }
            }

            // Threaded-comment authors are stored once per workbook (xl/persons/*.xml), shared across sheets.
            foreach (WorkbookPersonPart personPart in workbookPart.WorkbookPersonParts)
            {
                if (personPart.PersonList is not PersonList persons)
                {
                    continue;
                }
                foreach (Person person in persons.Elements<Person>().ToList())
                {
                    string original = person.DisplayName?.Value ?? string.Empty;
                    if (string.IsNullOrEmpty(original))
                    {
                        continue;
                    }
                    TextFilterResult result = filter(original);
                    if (string.Equals(result.FilteredText, original, StringComparison.Ordinal))
                    {
                        continue;
                    }
                    if (write)
                    {
                        person.DisplayName = result.FilteredText;
                    }
                    CaptureCommentSpans(result, original, captured, ref order);
                }
            }
        }

        // Redacts (write: true) or detects (write: false) PII in embedded charts of the processed
        // worksheets: chart title/label text and the cached series/category values that copy the cells.
        private static void ScanCharts(
            WorkbookPart workbookPart, string? worksheet, Func<string, TextFilterResult> filter,
            bool write, List<RedactionSpanEntity>? captured, ref int order)
        {
            foreach (WorksheetPart part in ProcessedWorksheetParts(workbookPart, worksheet))
            {
                DrawingsPart? drawings = part.DrawingsPart;
                if (drawings is null)
                {
                    continue;
                }
                foreach (ChartPart chartPart in drawings.ChartParts)
                {
                    ChartRedactor.RedactChartPart(chartPart, filter, write, captured, ref order);
                }
            }
        }

        // Clears the cached result (<v>) of every formula cell in the processed worksheets and sets the
        // workbook to fully recalculate on open. A formula's cache can duplicate a value from a cell that
        // was just redacted, and that plaintext ships until recalculation — so drop the caches and let Excel
        // recompute (correct, redacted) values when the file is opened.
        private static void ClearFormulaCachesAndForceRecalc(WorkbookPart workbookPart, string? worksheet)
        {
            foreach ((Cell cell, bool _, int _) in EnumerateCells(workbookPart, worksheet))
            {
                if (cell.CellFormula is not null)
                {
                    cell.CellValue?.Remove();
                }
            }

            Workbook? workbook = workbookPart.Workbook;
            if (workbook is null)
            {
                return;
            }
            CalculationProperties? calcPr = workbook.GetFirstChild<CalculationProperties>();
            if (calcPr is null)
            {
                calcPr = new CalculationProperties();
                // calcPr follows definedNames (or, absent that, the sheets) in the workbook schema.
                OpenXmlElement? anchor = workbook.GetFirstChild<DefinedNames>() ?? (OpenXmlElement?)workbook.GetFirstChild<Sheets>();
                if (anchor is not null)
                {
                    workbook.InsertAfter(calcPr, anchor);
                }
                else
                {
                    workbook.AppendChild(calcPr);
                }
            }
            calcPr.FullCalculationOnLoad = true;
        }

        // Runs the filter over one comment leaf text element (a comment run, an author, or a threaded-
        // comment text), replacing detected PII in place when writing and recording the spans.
        private static void RedactCommentLeaf(
            OpenXmlLeafTextElement element, Func<string, TextFilterResult> filter,
            bool write, List<RedactionSpanEntity>? captured, ref int order)
        {
            string original = element.Text ?? string.Empty;
            if (string.IsNullOrEmpty(original))
            {
                return;
            }
            TextFilterResult result = filter(original);
            if (string.Equals(result.FilteredText, original, StringComparison.Ordinal))
            {
                return;
            }
            if (write)
            {
                element.Text = result.FilteredText;
            }
            CaptureCommentSpans(result, original, captured, ref order);
        }

        // Records each detection from a comment/author/person redaction (ParagraphIndex -1: not a cell).
        private static void CaptureCommentSpans(
            TextFilterResult result, string original, List<RedactionSpanEntity>? captured, ref int order)
        {
            if (captured is null)
            {
                return;
            }
            foreach (Span s in result.Spans
                .Where(s => s.CharacterStart >= 0 && s.CharacterEnd <= original.Length && s.CharacterEnd > s.CharacterStart)
                .OrderBy(s => s.CharacterStart))
            {
                var entity = new RedactionSpanEntity
                {
                    Order = order++,
                    ParagraphIndex = -1,
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

        // Cells whose stored value should be run through the detector. This includes numeric- and
        // date-typed cells: PII such as an SSN, phone, or account number is commonly typed as a bare
        // number (e.g. 123456789 with a custom "000-00-0000" display format), and that raw value is the
        // sensitive data actually stored in the file. A cell with no DataType defaults to number.
        // Boolean and error cells are skipped (no free-text PII, and they shouldn't become strings);
        // formula cells are skipped by the callers (their value is computed).
        private static bool IsScannableCell(Cell cell)
        {
            CellValues? type = cell.DataType?.Value;
            return type is null
                || type == CellValues.Number
                || type == CellValues.SharedString
                || type == CellValues.InlineString
                || type == CellValues.String
                || type == CellValues.Date;
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

        // Blanks shared-string entries that no cell references anymore. Redacting a shared-string cell
        // detaches it (it becomes an inline string), but the original text stays in the shared-string
        // table and is recoverable by unzipping the workbook. We clear the text of every now-orphaned
        // entry, blanking in place (rather than removing items) so the indices that surviving cells still
        // reference don't shift.
        private static void PruneUnusedSharedStrings(WorkbookPart workbookPart)
        {
            SharedStringTable? table = workbookPart.SharedStringTablePart?.SharedStringTable;
            if (table is null)
            {
                return;
            }

            var referenced = new HashSet<int>();
            foreach ((Cell cell, bool _, int _) in EnumerateCells(workbookPart))
            {
                if (cell.DataType?.Value == CellValues.SharedString &&
                    int.TryParse(cell.CellValue?.InnerText, out int index))
                {
                    referenced.Add(index);
                }
            }

            int i = 0;
            foreach (SharedStringItem item in table.Elements<SharedStringItem>())
            {
                if (!referenced.Contains(i))
                {
                    // No cell points here anymore — drop any residual (possibly sensitive) text, leaving
                    // an empty placeholder so later entries keep their indices.
                    item.RemoveAllChildren();
                    item.AppendChild(new Text(string.Empty));
                }
                i++;
            }
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
