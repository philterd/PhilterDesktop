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
using PDFtoImage;
using PhilterData;
using SkiaSharp;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// In the PDF Redact with Preview window, Remove Selected works on any redaction, detected or drawn,
    /// as in the other previews, and a Source column says which each is. Driven through the real window
    /// against a real PDF: detection, removal, and the rebuilt page checked by its pixels.
    /// </summary>
    public sealed class PdfPreviewRemoveTests : IDisposable
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private const string Email = "jane@example.com";
        private const string Ssn = "123-45-6789";
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "pdf-preview-" + Guid.NewGuid().ToString("N"));

        public PdfPreviewRemoveTests() => Directory.CreateDirectory(_dir);

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { }
        }

        // ---- The button ----

        [Fact]
        public void NothingSelected_RemoveIsDisabled() => WithPreview(form =>
        {
            Assert.Equal(2, List(form).Items.Count);
            Assert.False(Remove(form).Enabled);
        });

        [Fact]
        public void DetectedRowSelected_RemoveIsEnabled() => WithPreview(form =>
        {
            Select(form, Email);
            Assert.True(Remove(form).Enabled);
        });

        [Fact]
        public void AddedRowSelected_RemoveIsEnabled() => WithPreview(form =>
        {
            DrawRegion(form);
            Select(form, "Region");
            Assert.True(Remove(form).Enabled);
        });

        // ---- The Source column ----

        [Fact]
        public void SourceColumn_SaysDetectedOrAdded() => WithPreview(form =>
        {
            DrawRegion(form);
            ListView list = List(form);

            Assert.Equal(new[] { "Source", "Type", "Location" }, list.Columns.Cast<ColumnHeader>().Select(c => c.Text));
            var rows = list.Items.Cast<ListViewItem>().Select(i => (Source: i.Text, Type: i.SubItems[1].Text)).ToList();
            Assert.Contains(("Detected", "Email Address"), rows);
            Assert.Contains(("Detected", "SSN"), rows);
            Assert.Contains(("Added", "Region"), rows);
        });

        // ---- Removing ----

        [Fact]
        public void RemovingADetectedItem_LeavesThatTextVisible_AndTheRestRedacted() => WithPreview(form =>
        {
            RedactionSpanEntity email = Span(form, Email);
            RedactionSpanEntity ssn = Span(form, Ssn);
            Assert.True(DarkFraction(Redacted(form), email) > 0.8, "the email should start out blacked out");

            Select(form, Email);
            Click(form);

            Assert.DoesNotContain(Spans(form), s => s.Text == Email);
            Assert.Single(List(form).Items.Cast<ListViewItem>());
            Assert.True(DarkFraction(Redacted(form), email) < 0.5, "the removed email should be readable again");
            Assert.True(DarkFraction(Redacted(form), ssn) > 0.8, "the SSN should stay blacked out");
        });

        [Fact]
        public void RemovingADrawnRegion_StillWorks() => WithPreview(form =>
        {
            DrawRegion(form);
            Select(form, "Region");
            Click(form);

            Assert.DoesNotContain(Spans(form), s => s.UserAdded);
            Assert.Equal(2, Spans(form).Count);
        });

        [Fact]
        public void AfterRemoving_NothingIsSelected_SoRemoveIsDisabled() => WithPreview(form =>
        {
            Select(form, Email);
            Click(form);

            Assert.Empty(List(form).SelectedItems);
            Assert.False(Remove(form).Enabled);
        });

        [Fact]
        public void AfterRedetecting_TheSelectionIsCleared_SoRemoveIsDisabled() => WithPreview(form =>
        {
            Select(form, Email);
            Assert.True(Remove(form).Enabled);

            Invoke(form, "DetectAsync");
            WaitForIdle(form);

            Assert.Empty(List(form).SelectedItems);
            Assert.False(Remove(form).Enabled);
            Assert.Contains(Spans(form), s => s.Text == Email); // re-detecting finds it again
        });

        // ---- Save never writes a PDF that doesn't match the list ----

        [Fact]
        public void SaveIsDisabled_WhileThePdfIsBeingRebuilt() => WithPreview(form =>
        {
            Assert.True(SaveButton(form).Enabled);

            DrawRegion(form, Box1, wait: false); // returns at its first await, mid-rebuild

            Assert.False(SaveButton(form).Enabled);
            Assert.False(Field<ComboBox>(form, "_policyCombo").Enabled); // no re-detect mid-rebuild either
            WaitForIdle(form);
            Assert.True(SaveButton(form).Enabled);
        });

        [Fact]
        public void TwoBoxesDrawnQuickly_AreBothApplied() => WithPreview(form =>
        {
            DrawRegion(form, Box1, wait: false);
            DrawRegion(form, Box2, wait: false); // arrives while the first rebuild is running
            WaitForIdle(form);

            List<RedactionSpanEntity> added = Spans(form).Where(s => s.UserAdded).ToList();
            Assert.Equal(2, added.Count);
            Assert.All(added, box => Assert.True(DarkFraction(Redacted(form), box) > 0.8, "every drawn box must be in the PDF"));
            Assert.True(SaveButton(form).Enabled);
        });

        [Fact]
        public void RemovingWhileRebuilding_IsApplied() => WithPreview(form =>
        {
            RedactionSpanEntity email = Span(form, Email);

            DrawRegion(form, Box1, wait: false);
            Select(form, Email);
            Invoke(form, "RemoveButton_Click", null, EventArgs.Empty); // arrives while the box is being applied
            WaitForIdle(form);

            Assert.True(DarkFraction(Redacted(form), email) < 0.5, "the removed email should be readable");
            Assert.True(DarkFraction(Redacted(form), Spans(form).Single(s => s.UserAdded)) > 0.8, "the box should be applied");
        });

        [Fact]
        public void ABoxDrawnDuringDetection_IsKeptAndApplied() => WithPreview(form =>
        {
            Invoke(form, "DetectAsync"); // re-detect, not awaited
            DrawRegion(form, Box1, wait: false);
            WaitForIdle(form);

            RedactionSpanEntity box = Assert.Single(Spans(form), s => s.UserAdded);
            Assert.True(DarkFraction(Redacted(form), box) > 0.8, "the box drawn during detection must be in the PDF");
            Assert.Equal(3, List(form).Items.Count);
            Assert.True(SaveButton(form).Enabled);
        });

        [Fact]
        public void TheOldOnlyAddedMessage_IsGone()
        {
            string source = File.ReadAllText(RepoFile("PhilterDesktop", "PdfRedactionPreviewForm.cs"));
            Assert.DoesNotContain("Only redactions you added here can be removed", source);
        }

        // ---- Helpers ----

        private void WithPreview(Action<PdfRedactionPreviewForm> test)
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                try
                {
                    string pdf = Path.Combine(_dir, "in.pdf");
                    File.WriteAllBytes(pdf, MinimalPdf.PlainText($"Email {Email} and SSN {Ssn} here."));
                    using var db = new LiteDatabase(Path.Combine(_dir, Guid.NewGuid().ToString("N") + ".db"));
                    var policies = new PolicyRepository(db);
                    policies.Insert(new PolicyEntity
                    {
                        Name = "email-ssn",
                        Json = PolicySerializer.SerializeToJson(new PhileasPolicy
                        {
                            Name = "email-ssn",
                            Identifiers = new Identifiers { EmailAddress = new EmailAddress(), Ssn = new Phileas.Policy.Filters.Ssn() }
                        })
                    });
                    var contexts = new ContextRepository(db);
                    contexts.Insert(new ContextEntity { Name = "default" });
                    var settings = new SettingsEntity { LastPolicy = "email-ssn", LastContext = "default" };

                    using var form = new PdfRedactionPreviewForm(pdf, policies, contexts, settings)
                    {
                        StartPosition = FormStartPosition.Manual,
                        Location = new Point(-32000, -32000),
                        ShowInTaskbar = false
                    };
                    form.Show(); // Load reads the PDF and detects
                    WaitForIdle(form);
                    Assert.Equal(2, Spans(form).Count);
                    test(form);
                    form.Close();
                }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
            })
            { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            captured?.Throw();
        }

        // Pumps messages until detection or re-apply has finished.
        private static void WaitForIdle(PdfRedactionPreviewForm form)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(60);
            do
            {
                Application.DoEvents();
                Thread.Sleep(20);
            }
            while ((Field<bool>(form, "_busy") || Field<byte[]>(form, "_redactedBytes").Length == 0) && DateTime.UtcNow < deadline);
            Application.DoEvents();
            Assert.False(Field<bool>(form, "_busy"), "the preview didn't finish in time");
        }

        // Blank areas of the page, so a drawn box shows as solid black.
        private static readonly PdfRegionDrawnEventArgs Box1 = new(1, 72, 600, 200, 640);
        private static readonly PdfRegionDrawnEventArgs Box2 = new(1, 300, 500, 420, 540);

        private static void DrawRegion(PdfRedactionPreviewForm form) => DrawRegion(form, Box1, wait: true);

        private static void DrawRegion(PdfRedactionPreviewForm form, PdfRegionDrawnEventArgs box, bool wait)
        {
            typeof(PdfRedactionPreviewForm).GetMethod("View_RegionDrawn", Flags)!.Invoke(form, new object?[] { null, box });
            if (wait)
            {
                WaitForIdle(form);
            }
        }

        private static Button SaveButton(PdfRedactionPreviewForm form) => Field<Button>(form, "_save");

        private static void Click(PdfRedactionPreviewForm form)
        {
            Invoke(form, "RemoveButton_Click", null, EventArgs.Empty);
            WaitForIdle(form);
        }

        private static void Invoke(PdfRedactionPreviewForm form, string method, params object?[] args) =>
            typeof(PdfRedactionPreviewForm).GetMethod(method, Flags)!.Invoke(form, args.Length == 0 ? null : args);

        // Selects the row for a detected value, or the drawn region when given "Region".
        private static void Select(PdfRedactionPreviewForm form, string valueOrRegion)
        {
            ListViewItem item = List(form).Items.Cast<ListViewItem>().Single(i =>
                i.Tag is RedactionSpanEntity s && (valueOrRegion == "Region" ? s.UserAdded : s.Text == valueOrRegion));
            item.Selected = true;
            Application.DoEvents();
        }

        private static RedactionSpanEntity Span(PdfRedactionPreviewForm form, string text) => Spans(form).Single(s => s.Text == text);

        private static List<RedactionSpanEntity> Spans(PdfRedactionPreviewForm form) => Field<List<RedactionSpanEntity>>(form, "_spans");

        private static byte[] Redacted(PdfRedactionPreviewForm form) => Field<byte[]>(form, "_redactedBytes");

        private static ListView List(PdfRedactionPreviewForm form) => Field<ListView>(form, "_redactionList");

        private static Button Remove(PdfRedactionPreviewForm form) => Field<Button>(form, "_removeButton");

        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;

        // The share of dark pixels inside a span's box on the rendered page: near 1 when blacked out.
        private static double DarkFraction(byte[] pdf, RedactionSpanEntity span)
        {
            const int dpi = 100;
            const double pageHeight = 792; // MinimalPdf's US Letter media box, in points
            using SKBitmap page = Conversion.ToImage(pdf, page: span.PageNumber - 1, options: new RenderOptions(Dpi: dpi));
            double scale = dpi / 72.0;
            int left = (int)Math.Ceiling(span.LowerLeftX * scale) + 1;
            int right = (int)Math.Floor(span.UpperRightX * scale) - 1;
            int top = (int)Math.Ceiling((pageHeight - span.UpperRightY) * scale) + 1;
            int bottom = (int)Math.Floor((pageHeight - span.LowerLeftY) * scale) - 1;

            int dark = 0, total = 0;
            for (int y = top; y <= bottom; y++)
            {
                for (int x = left; x <= right; x++)
                {
                    SKColor c = page.GetPixel(x, y);
                    if (c.Red + c.Green + c.Blue < 3 * 80)
                    {
                        dark++;
                    }
                    total++;
                }
            }
            return total == 0 ? 0 : (double)dark / total;
        }

        private static string RepoFile(params string[] parts)
        {
            for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            {
                string candidate = Path.Combine([dir.FullName, .. parts]);
                if (File.Exists(candidate) && File.Exists(Path.Combine(dir.FullName, "PhilterDesktop.slnx")))
                {
                    return candidate;
                }
            }
            throw new FileNotFoundException("Could not locate " + Path.Combine(parts));
        }
    }
}
