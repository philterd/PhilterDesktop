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

using System.Reflection;
using System.Runtime.ExceptionServices;
using LiteDB;
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// A redacted PDF's pages are flattened images with no text, so a clean leftover check proves nothing:
    /// it's shown as "Not checked (PDF)", and the menu item to check one is disabled. The automatic check
    /// still runs, because finding PII in a redacted PDF means text leaked into it.
    /// </summary>
    public sealed class PdfLeftoverCheckTests : IDisposable
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "pdf-leftover-" + Guid.NewGuid().ToString("N"));

        public PdfLeftoverCheckTests() => Directory.CreateDirectory(_dir);

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { }
        }

        // ---- Which outputs can be checked ----

        [Theory]
        [InlineData(@"C:\a\out.pdf", false)]
        [InlineData(@"C:\a\OUT.PDF", false)]
        [InlineData(@"C:\a\out.docx", true)]
        [InlineData(@"C:\a\out.xlsx", true)]
        [InlineData(@"C:\a\out.eml", true)]
        [InlineData(@"C:\a\out.txt", true)]
        [InlineData(@"C:\a\pdf.txt", true)]
        public void CanCheck_EverythingButPdf(string path, bool expected) =>
            Assert.Equal(expected, RedactionVerifier.CanCheck(path));

        // ---- The problem this guards against ----

        [Fact]
        public async Task ReScanningARedactedPdf_FindsNothing_EvenWithPiiOnThePage()
        {
            // Why PDFs are skipped: the SSN below is left on the page, yet a re-scan reports clean.
            (string input, string output) = await RedactPdfLeavingAnSsn();

            Assert.Equal(VerificationStatus.ResidualsFound, RedactionVerifier.Verify(input, VerificationPolicy.Broad(), "c", new FilterService()).Status);
            Assert.Equal(VerificationStatus.Clean, RedactionVerifier.Verify(output, VerificationPolicy.Broad(), "c", new FilterService()).Status);
        }

        // ---- The automatic check ----

        [Fact]
        public async Task AutomaticCheck_OnACleanPdf_IsShownAsNotChecked_NotNoneFound()
        {
            (_, string output) = await RedactPdfLeavingAnSsn();

            VerificationOutcome? outcome = RedactionVerifier.VerifyIfEnabled(Settings(), output, EmailOnly(), "c", new FilterService(), Array.Empty<RedactionSpanEntity>());

            Assert.Equal(VerificationStatus.Clean, outcome?.Status);
            Assert.Null(RedactionVerifier.WarningFor(outcome)); // nothing to warn about in notifications or logs
            Assert.Equal(RedactionVerifier.CannotCheckDisplay, Display(new RedactionQueueEntity { Name = output, VerificationStatus = "Clean" }));
        }

        [Fact]
        public void AutomaticCheck_CatchesTextLeakedIntoAPdf()
        {
            // A PDF output that still has a text layer carrying an SSN, as a broken redaction would leave.
            string output = Path.Combine(_dir, "leaked.pdf");
            File.WriteAllBytes(output, MinimalPdf.PlainText("SSN 123-45-6789 leaked into the output."));

            VerificationOutcome? outcome = RedactionVerifier.VerifyIfEnabled(Settings(), output, EmailOnly(), "c", new FilterService(), Array.Empty<RedactionSpanEntity>());

            Assert.Equal(VerificationStatus.ResidualsFound, outcome?.Status);
            Assert.Contains(outcome!.Residuals, r => r.Text == "123-45-6789");
            Assert.StartsWith("The leftover check found 1 possible item", RedactionVerifier.WarningFor(outcome));
        }

        [Fact]
        public void LeakedPdf_ShowsItsFindings_NotNotChecked()
        {
            var entity = new RedactionQueueEntity { Name = @"C:.pdf", VerificationStatus = "ResidualsFound", VerificationFindingCount = 1 };

            Assert.Equal("1 found", Display(entity));
            Assert.Contains("1 possible item of leftover PII may remain", Report(@"C:\out.pdf", "ResidualsFound", count: 1).VerificationSummary);
        }

        [Fact]
        public void AutomaticCheck_StillRunsForOtherFormats()
        {
            string output = Path.Combine(_dir, "out.txt");
            File.WriteAllText(output, "SSN 123-45-6789 is still here.");

            VerificationOutcome? outcome = RedactionVerifier.VerifyIfEnabled(Settings(), output, EmailOnly(), "c", new FilterService(), Array.Empty<RedactionSpanEntity>());

            Assert.Equal(VerificationStatus.ResidualsFound, outcome?.Status);
        }

        // ---- The queue column ----

        [Theory]
        [InlineData("Clean", RedactionVerifier.CannotCheckDisplay)]      // a clean PDF proves nothing
        [InlineData("NotRun", RedactionVerifier.CannotCheckDisplay)]
        [InlineData(null, RedactionVerifier.CannotCheckDisplay)]
        [InlineData("NamesNotChecked", "Names not checked")]            // still worth reviewing
        [InlineData("ContentDropped", "Some parts need review")]
        public void Pdf_ShowsNotCheckedInTheQueue(string? status, string shown) =>
            Assert.Equal(shown, Display(new RedactionQueueEntity { Name = @"C:\a\b.pdf", VerificationStatus = status! }));

        [Theory]
        [InlineData("Clean", "None found")]
        [InlineData("NotRun", "Not checked")]
        public void OtherFormats_AreUnchanged(string status, string shown) =>
            Assert.Equal(shown, Display(new RedactionQueueEntity { Name = @"C:\a\b.docx", VerificationStatus = status }));

        [Fact]
        public void NotCheckedText_IsShortAndPlain()
        {
            Assert.Equal("Not checked (PDF)", RedactionVerifier.CannotCheckDisplay);
            Assert.DoesNotContain('\u2014', RedactionReport.CannotCheckPdfLine);
        }

        // ---- The menu ----

        [Theory]
        [InlineData("report.pdf", false)]
        [InlineData("report.PDF", false)]
        [InlineData("report.docx", true)]
        [InlineData("report.txt", true)]
        public void Menu_IsDisabledForACompletedPdf(string file, bool enabled) => WithQueue(file, "Completed", form =>
        {
            Assert.Equal(enabled, OpenMenu(form).Enabled);
        });

        [Fact]
        public void Menu_StaysDisabledForAPdfThatIsntCompleted() => WithQueue("report.pdf", "Pending", form =>
        {
            Assert.False(OpenMenu(form).Enabled);
        });

        // ---- The report ----

        [Theory]
        [InlineData("Clean")]
        [InlineData("NotRun")]
        public void Report_ForAPdf_SaysItWasNotChecked(string status)
        {
            RedactionReportModel model = Report(@"C:\a\out.pdf", status);

            Assert.Equal(RedactionReport.CannotCheckPdfLine, model.VerificationSummary);
            Assert.DoesNotContain("No leftover PII was detected", RedactionReport.ToHtml(model));
        }

        [Fact]
        public void Report_ForAPdfVersionWithNoOutputYet_StillSaysNotChecked()
        {
            RedactionReportModel model = RedactionReport.Build(
                new RedactionVersionEntity { SourcePath = @"C:\in.pdf", OutputPath = string.Empty, FileType = ".pdf" },
                Array.Empty<RedactionSpanEntity>(), "1.0.0", DateTimeOffset.UtcNow, "h1", "h2", new RedactionReportOptions(),
                verificationStatus: "Clean");

            Assert.Equal(RedactionReport.CannotCheckPdfLine, model.VerificationSummary);
        }

        [Fact]
        public void Report_ForAPdf_KeepsAReviewWarning() =>
            Assert.Contains("person names may remain", Report(@"C:\a\out.pdf", "NamesNotChecked").VerificationSummary);

        [Fact]
        public void Report_ForOtherFormats_IsUnchanged()
        {
            Assert.StartsWith("No leftover PII was detected", Report(@"C:\a\out.docx", "Clean").VerificationSummary);
            Assert.Null(Report(@"C:\a\out.docx", "NotRun").VerificationSummary);
        }

        // ---- Helpers ----

        private static PhileasPolicy EmailOnly() => new() { Name = "email", Identifiers = new Identifiers { EmailAddress = new EmailAddress() } };

        private static SettingsEntity Settings() => new() { VerifyAfterRedaction = true, VerificationUseBroadPolicy = true };

        private async Task<(string Input, string Output)> RedactPdfLeavingAnSsn()
        {
            string input = Path.Combine(_dir, "in.pdf");
            string output = Path.Combine(_dir, "out.pdf");
            File.WriteAllBytes(input, MinimalPdf.PlainText("Email jane@example.com and SSN 123-45-6789 here."));
            await RedactionService.RedactFileAsync(input, output, EmailOnly(), "c", new SettingsEntity(), new FilterService());
            return (input, output);
        }

        private static string Display(RedactionQueueEntity entity) =>
            (string)typeof(MainForm).GetMethod("VerificationDisplay", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, new object[] { entity })!;

        private static RedactionReportModel Report(string outputPath, string status, int count = 0) => RedactionReport.Build(
            new RedactionVersionEntity { SourcePath = Path.ChangeExtension(outputPath, ".src"), OutputPath = outputPath, FileType = Path.GetExtension(outputPath) },
            Array.Empty<RedactionSpanEntity>(), "1.0.0", DateTimeOffset.UtcNow, "h1", "h2", new RedactionReportOptions(),
            verificationStatus: status, verificationCount: count);

        private static ToolStripMenuItem OpenMenu(MainForm form)
        {
            ListView list = Field<ListView>(form, "listView1");
            list.Items[0].Selected = true;
            typeof(MainForm).GetMethod("ContextMenuStrip1_Opening", Flags)!
                .Invoke(form, new object?[] { null, new System.ComponentModel.CancelEventArgs() });
            return Field<ToolStripMenuItem>(form, "verifyRedactionToolStripMenuItem");
        }

        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;

        private void WithQueue(string file, string status, Action<MainForm> test)
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using var db = new LiteDatabase(Path.Combine(_dir, Guid.NewGuid().ToString("N") + ".db"));
                    new RedactionQueueRepository(db).Insert(new RedactionQueueEntity
                    {
                        Name = Path.Combine(_dir, file), Status = status, Policy = "default", Context = "default"
                    });
                    using var form = new MainForm(db, startMinimized: false)
                    {
                        StartPosition = FormStartPosition.Manual,
                        Location = new System.Drawing.Point(-32000, -32000),
                        ShowInTaskbar = false
                    };
                    form.Show(); // the list only tracks selection once its window exists
                    Application.DoEvents();
                    typeof(MainForm).GetMethod("LoadRedactionQueue", Flags)!.Invoke(form, null);
                    Assert.Single(Field<ListView>(form, "listView1").Items.Cast<ListViewItem>());
                    test(form);
                }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
            })
            { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            captured?.Throw();
        }
    }
}
