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
    /// carry a known email, SSN, and person name in each supported format (including PDF), runs the real
    /// redaction pipeline over them, and checks each redacted output for leftover PII. For every format it
    /// first confirms the planted PII is detectable in the input, so a clean result is meaningful. A redacted
    /// PDF is flattened to images with no text, so finding nothing in it proves nothing: PDF is reported NOT
    /// CHECKED rather than passed, but still fails if PII is found (a leaked text layer). When the bundled
    /// on-device name model is present it also exercises name
    /// detection through the ONNX runtime, proving a packaged build's model and native runtime actually load
    /// and redact; on a dev build (no bundled model) that is reported as skipped rather than silently passing.
    /// Exits 0 when nothing failed and 1 otherwise: a post-install confidence check that needs no user data.
    /// </summary>
    internal static class SelfTest
    {
        private const string Email = "george@fake.com";
        private const string Ssn = "123-45-6789";
        private const string Name = "George Washington";
        private const string Surname = "Washington"; // unique to Name; not in the email or SSN
        private const string Body = "Contact " + Name + " at " + Email + " about SSN " + Ssn + ".";

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int processId);
        private const int AttachParentProcess = -1;

        public static int Run()
        {
            AttachConsole(AttachParentProcess); // a WinExe has no console; attach to the launching one if present
            RegexSafety.InstallDefaultMatchTimeout();

            Console.WriteLine("Philter Desktop self-test");
            Console.WriteLine();

            // ONNX name detection runs only when the model is bundled (dev builds aren't); report which.
            bool nameModel = PhEyeModel.IsAvailable;
            if (nameModel)
            {
                Console.WriteLine("On-device name detection (ONNX): ENABLED");
            }
            else
            {
                Console.WriteLine("On-device name detection (ONNX): UNAVAILABLE");
                Console.WriteLine("  WARNING: the bundled name model was not found, so person-name redaction is NOT");
                Console.WriteLine("  exercised. Expected on a dev build; a packaged install must include the model.");
            }
            Console.WriteLine();

            var policy = new PhileasPolicy
            {
                Name = "selftest",
                Identifiers = new Identifiers
                {
                    EmailAddress = new EmailAddress(),
                    Ssn = new Ssn(),
                    PhEyes = nameModel ? new List<PhEye> { PhEyeModel.CreateDefaultFilter() } : null
                }
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

            var results = new List<(string Label, CaseResult Result)>();
            try
            {
                foreach ((string label, Func<string, string> generate) in cases)
                {
                    results.Add((label, RunCase(label, generate, dir, policy, filterService, nameModel ? Surname : null)));
                }
            }
            finally
            {
                try { Directory.Delete(dir, recursive: true); } catch { /* best effort */ }
            }

            Console.WriteLine();
            Console.WriteLine(ResultLine(results, nameModel));
            return results.Any(r => r.Result == CaseResult.Failed) ? 1 : 0;
        }

        internal enum CaseResult { Passed, Failed, NotChecked }

        /// <summary>
        /// The summary line, e.g. "Result: PASS (6/6, pdf not checked) incl. name detection". Not-checked
        /// formats don't count toward the total, and don't fail the run.
        /// </summary>
        internal static string ResultLine(IReadOnlyList<(string Label, CaseResult Result)> results, bool nameModel)
        {
            int checkedCount = results.Count(r => r.Result != CaseResult.NotChecked);
            int passed = results.Count(r => r.Result == CaseResult.Passed);
            bool ok = results.All(r => r.Result != CaseResult.Failed);
            string notChecked = string.Join(", ", results.Where(r => r.Result == CaseResult.NotChecked).Select(r => r.Label));
            return $"Result: {(ok ? "PASS" : "FAIL")} ({passed}/{checkedCount}"
                + (notChecked.Length > 0 ? $", {notChecked} not checked)" : ")")
                + (nameModel ? " incl. name detection" : ", name detection NOT tested");
        }

        internal static CaseResult RunCase(string label, Func<string, string> generate, string dir, PhileasPolicy policy, FilterService filterService, string? requireInputName)
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
                    return CaseResult.Failed;
                }

                // Confirm the model detected the name: one it silently missed would also verify "clean".
                if (requireInputName != null
                    && !before.Residuals.Any(r => r.Text != null && r.Text.Contains(requireInputName, StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine($"  FAIL  {label} (name model did not detect the planted name in the input)");
                    return CaseResult.Failed;
                }

                RedactionService.RedactFileAsync(input, output, policy, "selftest", filterService).GetAwaiter().GetResult();

                bool canCheck = RedactionVerifier.CanCheck(output);
                if (!canCheck && !IsPdf(output))
                {
                    Console.WriteLine($"  FAIL  {label} (no PDF was written)");
                    return CaseResult.Failed;
                }

                VerificationOutcome outcome = RedactionVerifier.Verify(output, policy, "selftest", filterService, sourcePath: input);
                CaseResult result = Classify(outcome, canCheck);
                switch (result)
                {
                    case CaseResult.Passed:
                        Console.WriteLine($"  PASS  {label}");
                        break;
                    case CaseResult.NotChecked:
                        Console.WriteLine($"  NOT CHECKED  {label} (redacted; a redacted PDF has no text to re-scan, so review one by eye)");
                        break;
                    default:
                        string detail = outcome.Status == VerificationStatus.Error
                            ? outcome.Error ?? "error"
                            : "residual: " + string.Join(", ", outcome.Residuals.Select(r => r.Text).Distinct().Take(3));
                        Console.WriteLine($"  FAIL  {label} ({detail})");
                        break;
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  FAIL  {label} ({ex.Message})");
                return CaseResult.Failed;
            }
        }

        /// <summary>
        /// How a re-scan of the output counts. Anything found, or an error, fails. Finding nothing passes only
        /// if the output has text to scan: a redacted PDF has none, so a clean re-scan proves nothing. A PDF is
        /// still re-scanned, though, because finding PII there means a text layer leaked into the output.
        /// </summary>
        internal static CaseResult Classify(VerificationOutcome outcome, bool canCheck) => outcome.Status switch
        {
            VerificationStatus.Clean => canCheck ? CaseResult.Passed : CaseResult.NotChecked,
            _ => CaseResult.Failed
        };

        // A file that starts with the PDF header.
        private static bool IsPdf(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }
            byte[] head = new byte[5];
            using FileStream stream = File.OpenRead(path);
            return stream.Read(head, 0, head.Length) == head.Length && Encoding.ASCII.GetString(head) == "%PDF-";
        }

        // --- input generators (each carries the email + SSN markers) ------------------

        private static string WriteTxt(string path)
        {
            File.WriteAllText(path, Body);
            return path;
        }

        private static string WriteCsv(string path)
        {
            File.WriteAllText(path, "name,email,ssn\r\n" + Name + "," + Email + "," + Ssn + "\r\n");
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
