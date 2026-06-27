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
using PhilterDesktop;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Test-only helpers to build .xlsx fixtures (using the realistic shared-string format) and read
    /// them back, so the tests don't depend on a committed binary.
    /// </summary>
    internal static class SpreadsheetTestHelper
    {
        /// <summary>Creates a single-sheet workbook; every text value goes through the shared-string table.</summary>
        public static void CreateXlsx(string path, IReadOnlyList<string?[]> rows, string sheetName = "Sheet1")
        {
            using SpreadsheetDocument doc = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook);
            WorkbookPart wbPart = doc.AddWorkbookPart();
            wbPart.Workbook = new Workbook();
            WorksheetPart wsPart = wbPart.AddNewPart<WorksheetPart>();
            SharedStringTablePart sstPart = wbPart.AddNewPart<SharedStringTablePart>();
            sstPart.SharedStringTable = new SharedStringTable();

            var sharedIndex = new Dictionary<string, int>();
            int Intern(string value)
            {
                if (sharedIndex.TryGetValue(value, out int existing))
                {
                    return existing;
                }
                int index = sharedIndex.Count;
                sharedIndex[value] = index;
                sstPart.SharedStringTable.AppendChild(new SharedStringItem(new Text(value)));
                return index;
            }

            var sheetData = new SheetData();
            for (int r = 0; r < rows.Count; r++)
            {
                var rowElement = new Row { RowIndex = (uint)(r + 1) };
                string?[] row = rows[r];
                for (int c = 0; c < row.Length; c++)
                {
                    string? value = row[c];
                    if (value is null)
                    {
                        continue;
                    }
                    string reference = SpreadsheetColumn.IndexToLetter(c + 1) + (r + 1);

                    Cell cell;
                    if (double.TryParse(value, out _))
                    {
                        // store as a number cell (no DataType)
                        cell = new Cell { CellReference = reference, CellValue = new CellValue(value) };
                    }
                    else
                    {
                        cell = new Cell
                        {
                            CellReference = reference,
                            DataType = new EnumValue<CellValues>(CellValues.SharedString),
                            CellValue = new CellValue(Intern(value).ToString())
                        };
                    }
                    rowElement.AppendChild(cell);
                }
                sheetData.AppendChild(rowElement);
            }

            wsPart.Worksheet = new Worksheet(sheetData);
            Sheets sheets = wbPart.Workbook.AppendChild(new Sheets());
            sheets.AppendChild(new Sheet { Id = wbPart.GetIdOfPart(wsPart), SheetId = 1, Name = sheetName });
            wbPart.Workbook.Save();
        }

        /// <summary>Adds a second sheet to an existing workbook (text values as inline strings).</summary>
        public static void AppendSheet(string path, IReadOnlyList<string?[]> rows, string sheetName)
        {
            using SpreadsheetDocument doc = SpreadsheetDocument.Open(path, isEditable: true);
            WorkbookPart wbPart = doc.WorkbookPart!;
            WorksheetPart wsPart = wbPart.AddNewPart<WorksheetPart>();

            var sheetData = new SheetData();
            for (int r = 0; r < rows.Count; r++)
            {
                var rowElement = new Row { RowIndex = (uint)(r + 1) };
                string?[] row = rows[r];
                for (int c = 0; c < row.Length; c++)
                {
                    if (row[c] is not string value)
                    {
                        continue;
                    }
                    rowElement.AppendChild(new Cell
                    {
                        CellReference = SpreadsheetColumn.IndexToLetter(c + 1) + (r + 1),
                        DataType = new EnumValue<CellValues>(CellValues.InlineString),
                        InlineString = new InlineString(new Text(value))
                    });
                }
                sheetData.AppendChild(rowElement);
            }
            wsPart.Worksheet = new Worksheet(sheetData);

            Workbook workbook = wbPart.Workbook!;
            Sheets sheets = workbook.GetFirstChild<Sheets>() ?? workbook.AppendChild(new Sheets());
            uint sheetId = (uint)(sheets.Elements<Sheet>().Count() + 1);
            sheets.AppendChild(new Sheet { Id = wbPart.GetIdOfPart(wsPart), SheetId = sheetId, Name = sheetName });
            workbook.Save();
        }

        /// <summary>All cell text in the workbook (across sheets), concatenated with newlines.</summary>
        public static string AllText(string path)
        {
            using SpreadsheetDocument doc = SpreadsheetDocument.Open(path, isEditable: false);
            WorkbookPart wbPart = doc.WorkbookPart!;
            var lines = new List<string>();
            foreach (WorksheetPart wsPart in wbPart.WorksheetParts)
            {
                SheetData? sheetData = wsPart.Worksheet?.GetFirstChild<SheetData>();
                if (sheetData is null)
                {
                    continue;
                }
                foreach (Cell cell in sheetData.Descendants<Cell>())
                {
                    lines.Add(ReadCell(cell, wbPart));
                }
            }
            return string.Join("\n", lines);
        }

        private static string ReadCell(Cell cell, WorkbookPart wbPart)
        {
            if (cell.DataType?.Value == CellValues.SharedString)
            {
                SharedStringTablePart? sst = wbPart.SharedStringTablePart;
                if (sst is not null && int.TryParse(cell.CellValue?.InnerText, out int index))
                {
                    return sst.SharedStringTable?.Elements<SharedStringItem>().ElementAtOrDefault(index)?.InnerText ?? string.Empty;
                }
                return string.Empty;
            }
            if (cell.DataType?.Value == CellValues.InlineString)
            {
                return cell.InlineString?.InnerText ?? string.Empty;
            }
            return cell.CellValue?.InnerText ?? string.Empty;
        }
    }
}
