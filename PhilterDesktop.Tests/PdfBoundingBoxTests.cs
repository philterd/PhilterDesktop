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

using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Phileas.Policy;
using PhilterDesktop;
using PhilterDesktop.PolicyEditing;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Fixed PDF redaction regions (bounding boxes): a policy can carry rectangles that are always painted
    /// over, regardless of content. The engine already applies them; these cover the desktop side —
    /// persistence through the policy serializer, the authoring form's parsing, and end-to-end redaction.
    /// </summary>
    public sealed class PdfBoundingBoxTests : IDisposable
    {
        private static string SamplesDir => Path.Combine(AppContext.BaseDirectory, "test-documents");

        private readonly string _dir;

        public PdfBoundingBoxTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-bbox-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        [Fact]
        public void BoundingBoxes_RoundTripThroughPolicySerializer()
        {
            var policy = new Policy { Name = "p" };
            policy.Graphical.BoundingBoxes.Add(new BoundingBox { Page = 2, X = 10.5f, Y = 20, W = 100, H = 40, Color = "#000000" });

            string json = PolicySerializer.SerializeToJson(policy);
            Policy back = PolicySerializer.DeserializeFromJson(json);

            BoundingBox b = Assert.Single(back.Graphical.BoundingBoxes);
            Assert.Equal(2, b.Page);
            Assert.Equal(10.5f, b.X);
            Assert.Equal(100, b.W);
            Assert.Equal("#000000", b.Color);
            Assert.Contains("boundingBoxes", json); // stored under the schema's graphical key
        }

        [Fact]
        public void TryParse_SinglePage_ProducesOneEntry()
        {
            bool ok = AddRegionForm.TryParse("3", "12", "34", "56", "78", "#ff0000", out PdfRegionEntry? entry, out string? error);

            Assert.True(ok);
            Assert.Null(error);
            Assert.Equal("3", entry!.PageSpec);
            Assert.Equal("#ff0000", entry.Color);
            BoundingBox b = Assert.Single(entry.ToBoundingBoxes());
            Assert.Equal(3, b.Page);
            Assert.Equal(12, b.X);
            Assert.Equal(56, b.W);
            Assert.True(b.Enabled);
        }

        [Fact]
        public void TryParse_BlankColor_DefaultsToBlack()
        {
            Assert.True(AddRegionForm.TryParse("1", "0", "0", "10", "10", "  ", out PdfRegionEntry? entry, out _));
            Assert.Equal("Black", entry!.Color);
        }

        // A range or list stays a single entry (one list row), preserving the pages as entered.
        [Fact]
        public void TryParse_Range_IsOneEntry_KeptAsEntered()
        {
            Assert.True(AddRegionForm.TryParse("2-5", "1", "1", "10", "10", "", out PdfRegionEntry? entry, out _));
            Assert.Equal("2-5", entry!.PageSpec);
            Assert.Equal("2-5", entry.PageDisplay);
            Assert.Equal(new[] { 2, 3, 4, 5 }, entry.ToBoundingBoxes().Select(b => b.Page).ToArray()); // expands on save
        }

        [Fact]
        public void TryParse_List_IsOneEntry_ExpandsDistinctSorted()
        {
            Assert.True(AddRegionForm.TryParse("5,1,2,5", "1", "1", "10", "10", "", out PdfRegionEntry? entry, out _));
            Assert.Equal("5,1,2,5", entry!.PageSpec); // shown as entered
            Assert.Equal(new[] { 1, 2, 5 }, entry.ToBoundingBoxes().Select(b => b.Page).ToArray());
        }

        [Fact]
        public void TryParse_PageZero_ShowsAsAll_ExpandsToOneBox()
        {
            Assert.True(AddRegionForm.TryParse("0", "1", "2", "3", "4", "", out PdfRegionEntry? entry, out _));
            Assert.Equal("All", entry!.PageDisplay);
            Assert.Equal(0, Assert.Single(entry.ToBoundingBoxes()).Page); // 0 = all pages, expanded at redaction time
        }

        [Fact]
        public void TryParse_ToleratesWhitespaceInList()
        {
            Assert.True(AddRegionForm.TryParse(" 1 , 2 , 5 ", "1", "1", "10", "10", "", out PdfRegionEntry? entry, out _));
            Assert.Equal(new[] { 1, 2, 5 }, entry!.ToBoundingBoxes().Select(b => b.Page).ToArray());
        }

        // Round-trip: a "1,2" entry expands to two boxes on save, and reloading regroups them into one row.
        [Fact]
        public void FromBoxes_RegroupsSameRectPages_IntoOneEntry()
        {
            AddRegionForm.TryParse("1,2", "10", "20", "30", "40", "Black", out PdfRegionEntry? entry, out _);
            List<BoundingBox> saved = entry!.ToBoundingBoxes().ToList(); // what the policy stores
            Assert.Equal(2, saved.Count);

            List<PdfRegionEntry> reloaded = PdfRegionEntry.FromBoxes(saved);

            PdfRegionEntry back = Assert.Single(reloaded);
            Assert.Equal("1,2", back.PageSpec);
            Assert.Equal(new[] { 1, 2 }, back.ToBoundingBoxes().Select(b => b.Page).ToArray());
        }

        [Fact]
        public void FromBoxes_DifferentRectangles_StaySeparateEntries()
        {
            var boxes = new List<BoundingBox>
            {
                new() { Page = 1, X = 10, Y = 10, W = 5, H = 5, Color = "Black" },
                new() { Page = 2, X = 10, Y = 10, W = 5, H = 5, Color = "Black" }, // same rect as above -> merges
                new() { Page = 1, X = 99, Y = 99, W = 5, H = 5, Color = "Black" }  // different rect -> separate
            };

            List<PdfRegionEntry> entries = PdfRegionEntry.FromBoxes(boxes);

            Assert.Equal(2, entries.Count);
            Assert.Contains(entries, e => e.PageSpec == "1,2");
            Assert.Contains(entries, e => e.PageSpec == "1" && e.X == 99);
        }

        [Theory]
        [InlineData(new[] { 1, 2 }, "1,2")]         // a pair stays a list
        [InlineData(new[] { 1, 2, 3 }, "1-3")]      // 3+ consecutive collapse to a range
        [InlineData(new[] { 2, 3, 4, 8 }, "2-4,8")] // run + single
        [InlineData(new[] { 1, 2, 5, 6 }, "1,2,5,6")]
        [InlineData(new[] { 5 }, "5")]
        public void FormatPageSpec_CompactsRuns(int[] pages, string expected)
        {
            Assert.Equal(expected, PdfRegionEntry.FormatPageSpec(pages.ToList()));
        }

        [Theory]
        [InlineData("0,3", "0", "0", "10", "10", "")]   // 0 (all) can't combine with specific pages
        [InlineData("-1", "0", "0", "10", "10", "")]    // negative page
        [InlineData("5-2", "0", "0", "10", "10", "")]   // reversed range
        [InlineData("x", "0", "0", "10", "10", "")]     // page not a number
        [InlineData("1", "a", "0", "10", "10", "")]     // x not a number
        [InlineData("1", "0", "0", "0", "10", "")]      // width must be > 0
        [InlineData("1", "0", "0", "10", "-5", "")]     // height must be > 0
        public void TryParse_Invalid_FailsWithMessage(string page, string x, string y, string w, string h, string color)
        {
            Assert.False(AddRegionForm.TryParse(page, x, y, w, h, color, out PdfRegionEntry? entry, out string? error));
            Assert.Null(entry);
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Theory]
        [InlineData("0", 1, new int[0])]           // 0 = all pages (open from 1)
        [InlineData("2-", 2, new int[0])]          // open-ended: all but the first page
        [InlineData("3-", 3, new int[0])]          // open-ended: from page 3 to the end
        [InlineData("3", 0, new[] { 3 })]
        [InlineData("2-5", 0, new[] { 2, 3, 4, 5 })]
        [InlineData("1,2,5", 0, new[] { 1, 2, 5 })]
        [InlineData("2-4,8", 0, new[] { 2, 3, 4, 8 })]
        public void ParsePages_ResolvesSpec(string spec, int expectOpenFrom, int[] expectPages)
        {
            Assert.True(AddRegionForm.ParsePages(spec, out List<int> pages, out int openFrom, out _));
            Assert.Equal(expectOpenFrom, openFrom);
            Assert.Equal(expectPages, pages.ToArray());
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("0,3")]
        [InlineData("2-,8")]  // an open-ended range can't be combined with other pages
        [InlineData("5-2")]
        [InlineData("-")]     // open-ended range needs a start page
        [InlineData("abc")]
        public void ParsePages_Invalid_ReturnsError(string spec)
        {
            Assert.False(AddRegionForm.ParsePages(spec, out _, out _, out string? error));
            Assert.False(string.IsNullOrEmpty(error));
        }

        [Fact]
        public void TryParse_AllButFirst_ShowsFriendlyLabel_AndYieldsOneDeferredBox()
        {
            Assert.True(AddRegionForm.TryParse("2-", "1", "2", "3", "4", "", out PdfRegionEntry? entry, out _));
            Assert.Equal("2-", entry!.PageSpec);
            Assert.Equal("All but first", entry.PageDisplay);
            // Stays one deferred box (page -2 = "from page 2 to the end"), expanded at redaction time.
            Assert.Equal(-2, Assert.Single(entry.ToBoundingBoxes()).Page);
        }

        [Fact]
        public void FromBox_NegativePage_RoundTripsToOpenEndedSpec()
        {
            PdfRegionEntry entry = PdfRegionEntry.FromBox(new BoundingBox { Page = -2, X = 1, Y = 2, W = 3, H = 4 });
            Assert.Equal("2-", entry.PageSpec);
            Assert.Equal("All but first", entry.PageDisplay);
        }

        // The dialog validates on OK: valid input populates Entry (the invalid path shows a modal message
        // and keeps the dialog open, which is exercised by the ParsePages/TryParse tests above).
        [Fact]
        public void AddRegionForm_OnOk_ValidRange_PopulatesEntry()
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using var form = new AddRegionForm();
                    form.CreateControl();
                    SetField(form, "_page", "2-3");
                    SetField(form, "_x", "10");
                    SetField(form, "_y", "20");
                    SetField(form, "_w", "30");
                    SetField(form, "_h", "40");

                    typeof(AddRegionForm)
                        .GetMethod("OnOk", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                        .Invoke(form, new object?[] { null, EventArgs.Empty });

                    Assert.Equal("2-3", form.Entry!.PageSpec); // one entry, kept as entered
                    Assert.Equal(new[] { 2, 3 }, form.Entry.ToBoundingBoxes().Select(b => b.Page).ToArray());
                    Assert.Equal("Black", form.Entry.Color); // default color
                }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
            })
            { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            captured?.Throw();
        }

        private static void SetField(object form, string name, string text)
        {
            var box = (TextBox)form.GetType()
                .GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .GetValue(form)!;
            box.Text = text;
        }

        [Theory]
        [InlineData("0,3", "0", "0", "10", "10", "")]   // 0 (all) can't combine with specific pages
        [InlineData("-1", "0", "0", "10", "10", "")]    // page can't be negative
        [InlineData("5-2", "0", "0", "10", "10", "")]   // reversed range
        [InlineData("x", "0", "0", "10", "10", "")]     // page not a number
        [InlineData("1", "a", "0", "10", "10", "")]     // x not a number
        [InlineData("1", "0", "0", "0", "10", "")]      // width must be > 0
        [InlineData("1", "0", "0", "10", "-5", "")]     // height must be > 0
        public void TryParse_InvalidValues_FailWithMessage(string page, string x, string y, string w, string h, string color)
        {
            bool ok = AddRegionForm.TryParse(page, x, y, w, h, color, out _, out string? error);

            Assert.False(ok);
            Assert.False(string.IsNullOrEmpty(error));
        }


        [Fact]
        public async Task RedactPdf_WithAllPagesBox_RedactsMultiPageDocument()
        {
            string input = Path.Combine(SamplesDir, "scanned-two-page.pdf");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_dir, "allpages.pdf");

            var policy = new Policy { Name = "p" };
            policy.Graphical.BoundingBoxes.Add(new BoundingBox { Page = 0, X = 40, Y = 40, W = 150, H = 100 }); // all pages

            await RedactionService.RedactFileAsync(input, output, policy, "ctx");

            byte[] bytes = await File.ReadAllBytesAsync(output);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
        }

        [Fact]
        public async Task RedactPdf_WithBoundingBoxOnly_ProducesValidRasterizedPdf()
        {
            // A policy with no identifiers, just a fixed region — the box alone should still redact.
            string input = Path.Combine(SamplesDir, "test1.pdf");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_dir, "boxed.pdf");

            var policy = new Policy { Name = "p" };
            policy.Graphical.BoundingBoxes.Add(new BoundingBox { Page = 1, X = 50, Y = 50, W = 200, H = 120 });

            await RedactionService.RedactFileAsync(input, output, policy, "ctx");

            byte[] bytes = await File.ReadAllBytesAsync(output);
            Assert.True(bytes.Length > 0);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
        }

        [Fact]
        public void PdfRegionsForm_Constructs_AndExposesExistingBoxes()
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using var form = new PdfRegionsForm(new[] { new BoundingBox { Page = 1, X = 1, Y = 2, W = 3, H = 4 } });
                    form.CreateControl();
                    Assert.NotNull(form.Boxes); // default empty until OK
                }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
            })
            { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            captured?.Throw();
        }

        [Fact]
        public void PickerToBox_MapsDrawnRegionToBoundingBox()
        {
            // A region drawn on the preview arrives in PDF page space (lower-left + upper-right).
            var e = new PdfRegionDrawnEventArgs(2, 10, 20, 60, 80);

            BoundingBox b = PdfRegionPickerForm.ToBox(e);

            Assert.Equal(2, b.Page);
            Assert.Equal(10, b.X);
            Assert.Equal(20, b.Y);
            Assert.Equal(50, b.W); // 60 - 10
            Assert.Equal(60, b.H); // 80 - 20
            Assert.Equal("Black", b.Color); // drawn regions default to Black
        }

        [Fact]
        public void MapPageRegion_RoundTripsWithMapPictureRect()
        {
            // The overlay mapping (page space -> pixels) inverts the draw mapping (pixels -> page space).
            var rect = System.Drawing.Rectangle.FromLTRB(30, 40, 130, 100);
            const int pictureWidth = 800, imageWidth = 1200, imageHeight = 1500, dpi = 150;

            PdfRegionDrawnEventArgs? region = PdfSideBySideView.MapPictureRectToPage(rect, pictureWidth, imageWidth, imageHeight, 1, dpi);
            Assert.NotNull(region);
            System.Drawing.Rectangle back = PdfSideBySideView.MapPageRegionToPicture(region!, pictureWidth, imageWidth, imageHeight, dpi);

            Assert.InRange(back.Left, rect.Left - 1, rect.Left + 1);
            Assert.InRange(back.Top, rect.Top - 1, rect.Top + 1);
            Assert.InRange(back.Right, rect.Right - 1, rect.Right + 1);
            Assert.InRange(back.Bottom, rect.Bottom - 1, rect.Bottom + 1);
        }

        [Fact]
        public void PdfPageView_Constructs()
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using var view = new PdfPageView();
                    view.CreateControl();
                    Assert.Empty(view.OverlayRegions);
                }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
            })
            { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            captured?.Throw();
        }

        [Fact]
        public void AddRegionForm_Constructs_WithPrefilledBox()
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using var form = new AddRegionForm(new PdfRegionEntry("2-5", 1, 2, 3, 4, "Black"));
                    form.CreateControl();
                }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
            })
            { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            captured?.Throw();
        }

        [Fact]
        public void PdfRegionsForm_RemoveButton_EnabledOnlyWhenRowSelected()
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using var form = new PdfRegionsForm(new[] { new BoundingBox { Page = 1, X = 1, Y = 2, W = 3, H = 4 } });
                    form.Show(); // realize the ListView so its selection events fire
                    try
                    {
                        var list = (ListView)typeof(PdfRegionsForm)
                            .GetField("_list", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                            .GetValue(form)!;
                        Button remove = FindButton(form, "Remove") ?? throw new InvalidOperationException("Remove button not found");

                        Assert.False(remove.Enabled); // nothing selected

                        list.Items[0].Selected = true;
                        Application.DoEvents();
                        Assert.True(remove.Enabled); // a row is selected

                        list.Items[0].Selected = false;
                        Application.DoEvents();
                        Assert.False(remove.Enabled); // selection cleared
                    }
                    finally
                    {
                        form.Close();
                    }
                }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
            })
            { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            captured?.Throw();
        }

        [Fact]
        public void PdfRegionsForm_ContextMenu_EnablesActionsWithSelection_AndDuplicates()
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using var form = new PdfRegionsForm(new[] { new BoundingBox { Page = 1, X = 1, Y = 2, W = 3, H = 4 } });
                    form.Show();
                    try
                    {
                        var list = (ListView)typeof(PdfRegionsForm)
                            .GetField("_list", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                            .GetValue(form)!;
                        ContextMenuStrip menu = list.ContextMenuStrip!;
                        ToolStripMenuItem add = MenuItem(menu, "Add"), modify = MenuItem(menu, "Modify"),
                            duplicate = MenuItem(menu, "Duplicate"), remove = MenuItem(menu, "Remove");

                        // Nothing selected: only Add is enabled.
                        Assert.True(add.Enabled);
                        Assert.False(modify.Enabled);
                        Assert.False(duplicate.Enabled);
                        Assert.False(remove.Enabled);

                        // A row selected: Modify/Duplicate/Remove become enabled.
                        list.Items[0].Selected = true;
                        Application.DoEvents();
                        Assert.True(add.Enabled);
                        Assert.True(modify.Enabled);
                        Assert.True(duplicate.Enabled);
                        Assert.True(remove.Enabled);

                        // Duplicate copies the selected region.
                        int before = list.Items.Count;
                        duplicate.PerformClick();
                        Assert.Equal(before + 1, list.Items.Count);
                    }
                    finally { form.Close(); }
                }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
            })
            { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            captured?.Throw();
        }

        private static ToolStripMenuItem MenuItem(ContextMenuStrip menu, string contains) =>
            menu.Items.OfType<ToolStripMenuItem>()
                .First(i => i.Text!.Replace("&", "").Contains(contains, StringComparison.Ordinal)); // ignore the mnemonic

        private static Button? FindButton(Control root, string textContains)
        {
            foreach (Control c in root.Controls)
            {
                if (c is Button b && b.Text.Contains(textContains, StringComparison.Ordinal))
                {
                    return b;
                }
                Button? nested = FindButton(c, textContains);
                if (nested is not null)
                {
                    return nested;
                }
            }
            return null;
        }
    }
}
