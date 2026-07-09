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

using System.Runtime.InteropServices;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using PhilterData;
using W = DocumentFormat.OpenXml.Wordprocessing;
using X = DocumentFormat.OpenXml.Spreadsheet;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop
{
    /// <summary>
    /// A self-contained smoke test (<c>PhilterDesktop.exe --selftest</c>): generates small documents that
    /// carry a known email and SSN in each supported format (including PDF), runs the real redaction pipeline
    /// over them, and verifies each redacted output is free of residual PII. For every format it first confirms
    /// the planted PII is detectable in the input, so a clean result is meaningful (a redacted PDF is flattened
    /// to an image, so its output would otherwise verify clean even with no redaction). Prints PASS/FAIL per
    /// format and exits 0 (all passed) or 1 (any failed) — a post-install confidence check that needs no user data.
    /// </summary>
    internal static class SelfTest
    {
        private const string Email = "george@fake.com";
        private const string Ssn = "123-45-6789";
        private const string Body = "Contact " + Email + " about SSN " + Ssn + ".";

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int processId);
        private const int AttachParentProcess = -1;

        public static int Run()
        {
            AttachConsole(AttachParentProcess); // a WinExe has no console; attach to the launching one if present
            RegexSafety.InstallDefaultMatchTimeout();

            Console.WriteLine("Philter Desktop self-test");
            Console.WriteLine();

            var policy = new PhileasPolicy
            {
                Name = "selftest",
                Identifiers = new Identifiers { EmailAddress = new EmailAddress(), Ssn = new Ssn() }
            };
            var filterService = new FilterService();

            string dir = Path.Combine(Path.GetTempPath(), "philter-selftest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);

            var cases = new (string Label, Func<string, string> Generate)[]
            {
                ("txt", WriteTxt),
                ("csv", WriteCsv),
                ("rtf", WriteRtf),
                ("eml", WriteEml),
                ("docx", WriteDocx),
                ("xlsx", WriteXlsx),
                ("pdf", WritePdf),
            };

            int passed = 0;
            try
            {
                foreach ((string label, Func<string, string> generate) in cases)
                {
                    if (RunCase(label, generate, dir, policy, filterService))
                    {
                        passed++;
                    }
                }
            }
            finally
            {
                try { Directory.Delete(dir, recursive: true); } catch { /* best effort */ }
            }

            Console.WriteLine();
            bool ok = passed == cases.Length;
            Console.WriteLine($"Result: {(ok ? "PASS" : "FAIL")} ({passed}/{cases.Length})");
            return ok ? 0 : 1;
        }

        private static bool RunCase(string label, Func<string, string> generate, string dir, PhileasPolicy policy, FilterService filterService)
        {
            string input = string.Empty;
            try
            {
                input = generate(Path.Combine(dir, "in." + label));
                string output = Path.Combine(dir, "out." + label);

                // Confirm the planted PII is detectable in the input first, so a clean output means something.
                VerificationOutcome before = RedactionVerifier.Verify(input, policy, "selftest", filterService, sourcePath: null);
                if (before.Status != VerificationStatus.ResidualsFound)
                {
                    Console.WriteLine($"  FAIL  {label} (planted PII not detected in the input)");
                    return false;
                }

                RedactionService.RedactFileAsync(input, output, policy, "selftest", filterService).GetAwaiter().GetResult();

                VerificationOutcome outcome = RedactionVerifier.Verify(output, policy, "selftest", filterService, sourcePath: input);
                if (outcome.Status == VerificationStatus.Clean)
                {
                    Console.WriteLine($"  PASS  {label}");
                    return true;
                }

                string detail = outcome.Status == VerificationStatus.Error
                    ? outcome.Error ?? "error"
                    : "residual: " + string.Join(", ", outcome.Residuals.Select(r => r.Text).Distinct().Take(3));
                Console.WriteLine($"  FAIL  {label} ({detail})");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  FAIL  {label} ({ex.Message})");
                return false;
            }
        }

        // --- input generators (each carries the email + SSN markers) ------------------

        private static string WriteTxt(string path)
        {
            File.WriteAllText(path, Body);
            return path;
        }

        private static string WriteCsv(string path)
        {
            File.WriteAllText(path, "name,email,ssn\r\nBob," + Email + "," + Ssn + "\r\n");
            return path;
        }

        private static string WriteRtf(string path)
        {
            File.WriteAllText(path, @"{\rtf1\ansi\ansicpg1252 " + Body + @"\par}");
            return path;
        }

        private static string WriteEml(string path)
        {
            string eml =
                "From: sender@example.com\r\n" +
                "To: recipient@example.com\r\n" +
                "Subject: Self-test\r\n" +
                "MIME-Version: 1.0\r\n" +
                "Content-Type: text/plain; charset=utf-8\r\n" +
                "\r\n" + Body + "\r\n";
            File.WriteAllText(path, eml);
            return path;
        }

        private static string WriteDocx(string path)
        {
            using (WordprocessingDocument doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
            {
                MainDocumentPart main = doc.AddMainDocumentPart();
                main.Document = new W.Document(new W.Body(new W.Paragraph(new W.Run(new W.Text(Body)))));
            }
            return path;
        }

        private static string WriteXlsx(string path)
        {
            using (SpreadsheetDocument doc = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart wbPart = doc.AddWorkbookPart();
                wbPart.Workbook = new X.Workbook();
                WorksheetPart wsPart = wbPart.AddNewPart<WorksheetPart>();
                var sheetData = new X.SheetData();
                wsPart.Worksheet = new X.Worksheet(sheetData);
                X.Sheets sheets = wbPart.Workbook.AppendChild(new X.Sheets());
                sheets.Append(new X.Sheet { Id = wbPart.GetIdOfPart(wsPart), SheetId = 1, Name = "Sheet1" });
                var cell = new X.Cell
                {
                    CellReference = "A1",
                    DataType = X.CellValues.InlineString,
                    InlineString = new X.InlineString(new X.Text(Body))
                };
                sheetData.Append(new X.Row(cell));
                wbPart.Workbook.Save();
            }
            return path;
        }

        private static string WritePdf(string path)
        {
            // Author a one-page PDF with a real, extractable text layer (Standard-14 Helvetica) carrying the
            // email + SSN, so the redactor detects and redacts them like any text PDF.
            var builder = new UglyToad.PdfPig.Writer.PdfDocumentBuilder();
            var font = builder.AddStandard14Font(UglyToad.PdfPig.Fonts.Standard14Fonts.Standard14Font.Helvetica);
            var page = builder.AddPage(595, 842); // A4 in points
            page.AddText(Body, 12, new UglyToad.PdfPig.Core.PdfPoint(50, 750), font);
            File.WriteAllBytes(path, builder.Build());
            return path;
        }
    }
}
