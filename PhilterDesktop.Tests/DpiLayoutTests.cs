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

using System.Drawing;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using LiteDB;
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Policy.Filters.Strategies;
using PhilterData;
using PhilterDesktop.PolicyEditing;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Code-built forms must lay out cleanly at the DPI they start at (the real startup path) and after a
    /// move to a 150% monitor, simulated by sending a per-monitor DPI change (96 → 144); that one runs
    /// only on a 100% display.
    /// </summary>
    public sealed class DpiLayoutTests
    {
        private const int WM_DPICHANGED = 0x02E0;
        private static readonly IntPtr PerMonitorAwareV2 = new(-4);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [DllImport("user32.dll")]
        private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref RECT lParam);

        private static readonly string[] CodeBuilt =
        {
            "policy-editor", "policy-wizard", "filter-strategies", "filter-strategies-options", "add-filter-strategy",
            "custom-identifiers", "custom-identifier", "pdf-regions", "add-region", "pdf-region-picker",
            "pheye-model", "ocr-advanced"
        };

        public static TheoryData<string> CodeBuiltForms => new(CodeBuilt);

        // Designer forms are checked at their real startup DPI only: the simulated DPI change rescales them
        // differently from a real high-DPI start, so it reports problems a real 150% display doesn't have.
        public static TheoryData<string> AllForms => new(CodeBuilt.Append("settings").Append("watched-folder"));

        private static Form Create(string name, LiteDatabase db) => name switch
        {
            "policy-editor" => new PolicyEditorForm(Seeded(db), new WatchedFolderRepository(db)),
            "policy-wizard" => new PolicyWizardForm(_ => false),
            "filter-strategies" => new FilterStrategiesForm("SSN", Array.Empty<object>(), typeof(SsnFilterStrategy)),
            "add-filter-strategy" => new AddFilterStrategyForm(new SsnFilterStrategy(), "SSN"),
            "filter-strategies-options" => new FilterStrategiesForm("EIN", Array.Empty<object>(), typeof(SsnFilterStrategy),
                new[] { new FilterOptionToggle { Label = "Only valid prefixes", Description = "Skip numbers whose prefix the IRS never issued.", Value = true } }),
            "custom-identifiers" => new CustomIdentifiersForm(new[] { new Identifier { Classification = "case", Pattern = "CASE-\\d+" } }),
            "custom-identifier" => new CustomIdentifierForm(new Identifier()),
            "pdf-regions" => new PdfRegionsForm(new[] { new BoundingBox { Page = 1, X = 1, Y = 2, W = 3, H = 4 } }),
            "add-region" => new AddRegionForm(),
            "pdf-region-picker" => new PdfRegionPickerForm(MinimalPdf.PlainText("Hello")),
            "pheye-model" => new PhEyeModelForm(PhEyeModel.CreateDefaultFilter()),
            "ocr-advanced" => new OcrAdvancedSettingsForm(0.01, 0.5, 200),
            "settings" => new SettingsForm(new SettingsRepository(db), Seeded(db), SeededContexts(db), new WatchedFolderRepository(db)),
            "watched-folder" => new WatchedFolderForm(Seeded(db), SeededContexts(db),
                new WatchedFolderEntity { FileTypes = new List<string> { ".rtf", ".eml", ".msg" } }),
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };

        private static ContextRepository SeededContexts(LiteDatabase db)
        {
            var contexts = new ContextRepository(db);
            contexts.Insert(new ContextEntity { Name = "default" });
            return contexts;
        }

        private static PolicyRepository Seeded(LiteDatabase db)
        {
            var policies = new PolicyRepository(db);
            policies.Insert(new PolicyEntity { Name = "default", Json = DefaultPolicy.Json() });
            return policies;
        }

        [Theory]
        [MemberData(nameof(AllForms))]
        public void LaysOutCleanly_AtStartupDpi(string name) => WithShownForm(name, perMonitor: false, form =>
        {
            LayoutAudit.AssertClean(AuditAllSteps(form));
        });

        [SkippableTheory]
        [MemberData(nameof(CodeBuiltForms))]
        public void LaysOutCleanly_At150Percent(string name) => WithShownForm(name, perMonitor: true, form =>
        {
            Skip.If(form.DeviceDpi != 96, "test simulates 96 → 144 DPI; this display isn't at 100%");
            Size before = form.ClientSize;
            ChangeDpi(form, 144);

            Assert.Equal(144, form.DeviceDpi);
            Assert.True(form.ClientSize.Width > before.Width * 1.3, $"form didn't scale: {before} → {form.ClientSize}");
            LayoutAudit.AssertClean(AuditAllSteps(form));
        });

        [Fact]
        public void PolicyEditor_ScoreLinkSitsInTheActionsBar_NotOverTheTabs() => WithShownForm("policy-editor", perMonitor: false, form =>
        {
            LinkLabel link = Descendants(form).OfType<LinkLabel>().Single(l => l.Text.StartsWith("Score this policy"));
            Button pdfRegions = Descendants(form).OfType<Button>().Single(b => b.Text == "PDF Regions…");
            TabControl tabs = Descendants(form).OfType<TabControl>().Single();

            Assert.Same(pdfRegions.Parent!.Parent, link.Parent); // same bar as the action buttons
            Rectangle linkOnForm = form.RectangleToClient(link.RectangleToScreen(link.ClientRectangle));
            Rectangle tabsOnForm = form.RectangleToClient(tabs.RectangleToScreen(tabs.ClientRectangle));
            Assert.False(linkOnForm.IntersectsWith(tabsOnForm));
            Assert.True(linkOnForm.Right <= form.ClientSize.Width && linkOnForm.Bottom <= form.ClientSize.Height);
        });

        [Fact]
        public void PolicyEditor_StaysFullyLaidOut_WhenResizedToMinimumAndLarger() => WithShownForm("policy-editor", perMonitor: false, form =>
        {
            form.Size = form.MinimumSize;
            Application.DoEvents();
            LayoutAudit.AssertClean(LayoutAudit.Audit(form));

            form.Size = new Size(1400, 900);
            Application.DoEvents();
            LayoutAudit.AssertClean(LayoutAudit.Audit(form));
        });

        [Fact]
        public void PolicyEditor_FilterRowGrowsForLongText_InsteadOfClipping() => WithShownForm("policy-editor", perMonitor: false, form =>
        {
            CheckBox firstName = Descendants(form).OfType<CheckBox>().First(c => c.Text == "First Name");
            Control row = firstName.Parent!.Parent!;
            Size minimum = row.Size;

            firstName.Text = new string('W', 80);
            Application.DoEvents();

            Assert.True(row.Width > minimum.Width, $"row should widen to fit its text: {minimum} → {row.Size}");
            Assert.True(new Rectangle(Point.Empty, row.Size).Contains(firstName.Parent.Bounds), "text column clipped by its row");
        });

        [Fact]
        public void Settings_WatchedTab_StacksListButtonsConcurrencyAndLink() => WithShownForm("settings", perMonitor: false, form =>
        {
            TabPage tab = Descendants(form).OfType<TabPage>().Single(t => t.Name == "tabWatched");
            form.Controls.OfType<TabControl>().Single().SelectedTab = tab;
            Application.DoEvents();
            Control Find(string name) => Descendants(tab).Single(c => c.Name == name);
            LinkLabel link = Descendants(tab).OfType<LinkLabel>().Single(l => l.Text.StartsWith("Automating redaction"));

            int Bottom(Control c) => tab.RectangleToClient(c.RectangleToScreen(c.ClientRectangle)).Bottom;
            int Top(Control c) => tab.RectangleToClient(c.RectangleToScreen(c.ClientRectangle)).Top;
            Assert.True(Bottom(Find("listWatched")) <= Top(Find("btnAddWatched")));
            Assert.True(Bottom(Find("btnAddWatched")) <= Top(Find("cmbConcurrency")));
            Assert.True(Bottom(Find("cmbConcurrency")) <= Top(link));
            Assert.True(Bottom(link) <= tab.ClientSize.Height, "link must be inside the tab");
            Assert.All(new[] { "btnAddWatched", "btnEditWatched", "btnRemoveWatched", "btnViewLog", "lblConcurrency", "lblStartupHint" },
                n => Find(n)); // every control kept
        });

        [Fact]
        public void Settings_WithoutWatchedRepositories_HidesTheTab_AndStillLaysOut() => Sta(() =>
        {
            string path = Path.Combine(Path.GetTempPath(), "dpi-" + Guid.NewGuid().ToString("N") + ".db");
            try
            {
                using var db = new LiteDatabase(path);
                using var form = new SettingsForm(new SettingsRepository(db));
                form.StartPosition = FormStartPosition.Manual;
                form.ShowInTaskbar = false;
                form.Show();
                Application.DoEvents();
                Assert.DoesNotContain(Descendants(form).OfType<TabPage>(), t => t.Name == "tabWatched");
                LayoutAudit.AssertClean(LayoutAudit.Audit(form));
                form.Close();
            }
            finally { try { File.Delete(path); } catch { /* best effort */ } }
        });

        [Fact]
        public void Settings_EmailTab_KeepsOptionOrderAndIndents() => WithShownForm("settings", perMonitor: false, form =>
        {
            TabPage tab = Descendants(form).OfType<TabPage>().Single(t => t.Name == "tabEmail");
            form.Controls.OfType<TabControl>().Single().SelectedTab = tab;
            Application.DoEvents();
            string[] order =
            {
                "chkScrubEmailHeaders", "lblEmailInfo", "chkRemoveCommonHeaders", "lblCommonHeadersInfo",
                "chkRemoveDateHeader", "lblDateHeaderInfo", "chkRemoveAttachments", "chkRemoveInlineImages", "lblAttachmentsInfo"
            };
            List<Control> controls = order.Select(n => Descendants(tab).Single(c => c.Name == n)).ToList();
            for (int i = 1; i < controls.Count; i++)
            {
                Assert.True(controls[i].Top >= controls[i - 1].Bottom, $"{order[i]} should sit below {order[i - 1]}");
            }
            Control attachments = controls[6], inlineImages = controls[7], hint = controls[8];
            Assert.True(inlineImages.Left > attachments.Left, "dependent option stays indented");
            Assert.True(hint.Left > attachments.Left, "hint stays indented under its checkbox");
        });

        [Fact]
        public void Settings_LoggingGroup_KeepsCheckboxAndButtonsOnOneRow() => WithShownForm("settings", perMonitor: false, form =>
        {
            Control Find(string name) => Descendants(form).Single(c => c.Name == name);
            Control check = Find("chkEnableLogging"), open = Find("btnOpenLog"), clear = Find("btnClearLog");
            Assert.Same(check.Parent, open.Parent);
            Assert.True(check.Right <= open.Left && open.Right <= clear.Left);
            Assert.InRange(check.Top + check.Height / 2, open.Top, open.Bottom); // vertically centered on the buttons
        });

        [Fact]
        public void WatchedFolder_FileTypeGrid_KeepsOrderAndLoadsSavedTypes() => WithShownForm("watched-folder", perMonitor: false, form =>
        {
            string[] names = { "_typePdf", "_typeDocx", "_typeTxt", "_typeRtf", "_typeSpreadsheet", "_typeEmail" };
            List<CheckBox> boxes = names.Select(n => Descendants(form).OfType<CheckBox>().Single(c => c.Name == n)).ToList();
            var grid = (TableLayoutPanel)boxes[0].Parent!;
            for (int i = 0; i < boxes.Count; i++)
            {
                Assert.Same(grid, boxes[i].Parent);
                Assert.Equal(new TableLayoutPanelCellPosition(i % 3, i / 3), grid.GetPositionFromControl(boxes[i]));
            }
            Assert.Equal(new[] { false, false, false, true, false, true }, boxes.Select(b => b.Checked));
            Assert.True(grid.Bottom <= Descendants(form).Single(c => c.Name == "_includeSubfolders").Top,
                "file types must not run into the next option");
        });

        [Fact]
        public void OcrAdvanced_RoundTripsValues_AfterLayoutRewrite() => Sta(() =>
        {
            using var form = new OcrAdvancedSettingsForm(0.025, 0.75, 1234);
            Assert.Equal(0.025, form.TextCoverageThreshold, 6);
            Assert.Equal(0.75, form.ImageCoverageThreshold, 6);
            Assert.Equal(1234, form.MaxPages);
        });

        [Fact]
        public void OcrAdvanced_ClampsOutOfRangeValues() => Sta(() =>
        {
            using var form = new OcrAdvancedSettingsForm(5.0, 0.0, -10);
            Assert.Equal(0.5, form.TextCoverageThreshold, 6); // 50% max
            Assert.Equal(0.05, form.ImageCoverageThreshold, 6); // 5% min
            Assert.Equal(0, form.MaxPages);
        });

        // Walks every wizard step (built on demand) so each one is audited, not just the first.
        private static List<string> AuditAllSteps(Form form)
        {
            var problems = new List<string>(LayoutAudit.Audit(form));
            if (form is PolicyWizardForm)
            {
                Button next = Descendants(form).OfType<Button>().Single(b => b.Text == "Next");
                while (next.Text == "Next")
                {
                    next.PerformClick();
                    Application.DoEvents();
                    problems.AddRange(LayoutAudit.Audit(form).Select(p => $"[step '{next.Text}'] {p}"));
                }
            }
            return problems;
        }

        private static void ChangeDpi(Form form, int dpi)
        {
            Rectangle b = form.Bounds;
            float scale = dpi / (float)form.DeviceDpi;
            var suggested = new RECT
            {
                Left = b.Left,
                Top = b.Top,
                Right = b.Left + (int)(b.Width * scale),
                Bottom = b.Top + (int)(b.Height * scale)
            };
            SendMessage(form.Handle, WM_DPICHANGED, (IntPtr)((dpi << 16) | dpi), ref suggested);
            Application.DoEvents();
        }

        private static void WithShownForm(string name, bool perMonitor, Action<Form> test) => Sta(() =>
        {
            if (perMonitor)
            {
                Skip.If(SetThreadDpiAwarenessContext(PerMonitorAwareV2) == IntPtr.Zero,
                    "this thread can't be made Per-Monitor-v2 DPI aware");
            }
            string path = Path.Combine(Path.GetTempPath(), "dpi-" + Guid.NewGuid().ToString("N") + ".db");
            try
            {
                using var db = new LiteDatabase(path);
                using Form form = Create(name, db);
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(0, 0);
                form.ShowInTaskbar = false;
                form.Show();
                Application.DoEvents();
                test(form);
                form.Close();
            }
            finally
            {
                try { File.Delete(path); } catch { /* best effort */ }
            }
        });

        internal static IEnumerable<Control> Descendants(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (Control grandchild in Descendants(child))
                {
                    yield return grandchild;
                }
            }
        }

        internal static void Sta(Action action)
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                try { action(); }
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
