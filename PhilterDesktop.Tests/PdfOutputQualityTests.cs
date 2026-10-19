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
using LiteDB;
using Phileas.Services;
using PhilterData;
using PhilterDesktop;
using UglyToad.PdfPig;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    public sealed class PdfOutputQualityTests : IDisposable
    {
        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "philter-pdfq-" + Guid.NewGuid().ToString("N"));

        public PdfOutputQualityTests() => Directory.CreateDirectory(_tempDir);

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        }

        private static PhileasPolicy SsnPolicy() => new()
        {
            Name = "ssn",
            Identifiers = new Phileas.Policy.Identifiers { Ssn = new Phileas.Policy.Filters.Ssn() }
        };

        private static byte[] SourcePdf() =>
            MinimalPdf.Pages((PdfTextLayerQualityTests.Prose, false), (PdfTextLayerQualityTests.Prose, false));

        // Pixel width of the first page's image: the rendered DPI times 8.5in for US Letter.
        private static int FirstPageImageWidth(byte[] pdf)
        {
            using PdfDocument doc = PdfDocument.Open(pdf);
            return doc.GetPage(1).GetImages().First().WidthInSamples;
        }

        // The engine's rasterizer can round a pixel either way.
        private static void AssertWidth(int expected, byte[] pdf) =>
            Assert.InRange(FirstPageImageWidth(pdf), expected - 2, expected + 2);

        // --- Profile mapping ---

        [Theory]
        [InlineData(PdfOutputQuality.Smaller, 150, 0.75f)]
        [InlineData(PdfOutputQuality.Balanced, 200, 0.85f)]
        [InlineData(PdfOutputQuality.Best, 200, 1.0f)]
        public void Profile_MapsEachPreset(PdfOutputQuality quality, int dpi, float jpeg)
        {
            (int d, float q) = RedactionService.PdfOutputProfile(quality);
            Assert.Equal(dpi, d);
            Assert.Equal(jpeg, q);
        }

        [Fact]
        public void Profile_UnknownValue_FallsBackToBalanced() =>
            Assert.Equal(RedactionService.PdfOutputProfile(PdfOutputQuality.Balanced),
                RedactionService.PdfOutputProfile((PdfOutputQuality)42));

        [Fact]
        public void Default_IsBalanced() =>
            Assert.Equal(PdfOutputQuality.Balanced, new SettingsEntity().PdfOutputQuality);

        // --- PreparePdfPolicy ---

        [Theory]
        [InlineData(PdfOutputQuality.Smaller, 150, 0.75f)]
        [InlineData(PdfOutputQuality.Balanced, 200, 0.85f)]
        [InlineData(PdfOutputQuality.Best, 200, 1.0f)]
        public void Prepare_AppliesPresetAndFullScale(PdfOutputQuality quality, int dpi, float jpeg)
        {
            PhileasPolicy policy = SsnPolicy();
            RedactionService.PreparePdfPolicy(policy, SourcePdf(), pdfOutputQuality: quality);
            Assert.Equal(1.0f, policy.Config.Pdf.Scale);
            Assert.Equal(dpi, policy.Config.Pdf.Dpi);
            Assert.Equal(jpeg, policy.Config.Pdf.CompressionQuality);
        }

        [Fact]
        public void Prepare_KeepsHigherPolicyDpi()
        {
            PhileasPolicy policy = SsnPolicy();
            policy.Config.Pdf.Dpi = 300;
            RedactionService.PreparePdfPolicy(policy, SourcePdf(), pdfOutputQuality: PdfOutputQuality.Smaller);
            Assert.Equal(300, policy.Config.Pdf.Dpi);
            Assert.Equal(0.75f, policy.Config.Pdf.CompressionQuality);
        }

        [Fact]
        public void Prepare_DefaultsToBalanced()
        {
            PhileasPolicy policy = SsnPolicy();
            RedactionService.PreparePdfPolicy(policy, SourcePdf());
            Assert.Equal(200, policy.Config.Pdf.Dpi);
            Assert.Equal(0.85f, policy.Config.Pdf.CompressionQuality);
        }

        // --- Rendered output ---

        [Fact]
        public async Task RedactPdfBytes_RendersAtPresetDpi()
        {
            (byte[] smaller, _) = await RedactionService.RedactPdfBytesAsync(SourcePdf(), SsnPolicy(), "ctx", new FilterService(),
                ocrScannedPdfs: false, pdfOutputQuality: PdfOutputQuality.Smaller);
            (byte[] balanced, _) = await RedactionService.RedactPdfBytesAsync(SourcePdf(), SsnPolicy(), "ctx", new FilterService(),
                ocrScannedPdfs: false, pdfOutputQuality: PdfOutputQuality.Balanced);

            AssertWidth(1275, smaller);  // 8.5in at 150 DPI
            AssertWidth(1700, balanced); // 8.5in at 200 DPI
        }

        [Fact]
        public async Task RedactPdfBytes_SizeShrinksWithEachPreset_AndStillRedacts()
        {
            var sizes = new Dictionary<PdfOutputQuality, int>();
            foreach (PdfOutputQuality q in Enum.GetValues<PdfOutputQuality>())
            {
                (byte[] doc, List<RedactionSpanEntity> spans) = await RedactionService.RedactPdfBytesAsync(
                    SourcePdf(), SsnPolicy(), "ctx", new FilterService(), ocrScannedPdfs: false, pdfOutputQuality: q);
                Assert.Equal(2, spans.Count); // the SSN on each page, whatever the quality
                sizes[q] = doc.Length;
            }

            Assert.True(sizes[PdfOutputQuality.Smaller] < sizes[PdfOutputQuality.Balanced], string.Join(", ", sizes));
            Assert.True(sizes[PdfOutputQuality.Balanced] < sizes[PdfOutputQuality.Best], string.Join(", ", sizes));
        }

        [Fact]
        public async Task ApplyPdfSpansToBytes_HonorsPreset()
        {
            (_, List<RedactionSpanEntity> spans) = await RedactionService.RedactPdfBytesAsync(
                SourcePdf(), SsnPolicy(), "ctx", new FilterService(), ocrScannedPdfs: false);

            byte[] reapplied = await RedactionService.ApplyPdfSpansToBytesAsync(SourcePdf(), spans,
                pdfOutputQuality: PdfOutputQuality.Smaller);

            AssertWidth(1275, reapplied);
        }

        [Fact]
        public async Task SettingsOverload_ForwardsPreset()
        {
            string source = Path.Combine(_tempDir, "in.pdf");
            File.WriteAllBytes(source, SourcePdf());
            string output = Path.Combine(_tempDir, "out.pdf");

            var settings = new SettingsEntity { OcrScannedPdfs = false, PdfOutputQuality = PdfOutputQuality.Smaller };
            await RedactionService.RedactFileAsync(source, output, SsnPolicy(), "ctx", settings, new FilterService());

            AssertWidth(1275, File.ReadAllBytes(output));
        }

        [Fact]
        public async Task ApplySpans_PdfForwardsPreset()
        {
            string source = Path.Combine(_tempDir, "in.pdf");
            File.WriteAllBytes(source, SourcePdf());
            string first = Path.Combine(_tempDir, "first.pdf");
            List<RedactionSpanEntity> spans = await RedactionService.RedactFileAsync(
                source, first, SsnPolicy(), "ctx", new FilterService(), ocrScannedPdfs: false);

            string output = Path.Combine(_tempDir, "modified.pdf");
            await RedactionService.ApplySpansAsync(source, output, ".pdf", highlight: false, spans, SsnPolicy(),
                pdfOutputQuality: PdfOutputQuality.Smaller);

            AssertWidth(1275, File.ReadAllBytes(output));
        }

        // --- Persistence and the Settings dialog ---

        [Fact]
        public void Setting_RoundTripsThroughDatabase()
        {
            using var db = new LiteDatabase(new MemoryStream());
            var repo = new SettingsRepository(db);
            SettingsEntity s = repo.GetSettings();
            s.PdfOutputQuality = PdfOutputQuality.Smaller;
            repo.SaveSettings(s);

            Assert.Equal(PdfOutputQuality.Smaller, new SettingsRepository(db).GetSettings().PdfOutputQuality);
        }

        [Fact]
        public void Setting_MissingFromOlderDatabase_DefaultsToBalanced()
        {
            using var db = new LiteDatabase(new MemoryStream());
            // A settings record written before the field existed.
            db.GetCollection("settings").Insert(new BsonDocument { ["_id"] = 1, ["OcrScannedPdfs"] = true });

            Assert.Equal(PdfOutputQuality.Balanced, new SettingsRepository(db).GetSettings().PdfOutputQuality);
        }

        private static void Sta(Action action)
        {
            ExceptionDispatchInfo? error = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { error = ExceptionDispatchInfo.Capture(ex); }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            error?.Throw();
        }

        private static IEnumerable<Control> Descendants(Control root) =>
            root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));

        [Theory]
        [InlineData(PdfOutputQuality.Smaller, PdfOutputQuality.Best)]
        [InlineData(PdfOutputQuality.Best, PdfOutputQuality.Smaller)]
        [InlineData(PdfOutputQuality.Balanced, PdfOutputQuality.Smaller)]
        public void SettingsDialog_LoadsAndSavesQuality(PdfOutputQuality stored, PdfOutputQuality chosen) => Sta(() =>
        {
            using var db = new LiteDatabase(new MemoryStream());
            var repo = new SettingsRepository(db);
            SettingsEntity s = repo.GetSettings();
            s.PdfOutputQuality = stored;
            repo.SaveSettings(s);

            using var form = new SettingsForm(repo);
            form.StartPosition = FormStartPosition.Manual;
            form.ShowInTaskbar = false;
            form.Show();
            Application.DoEvents();

            ComboBox combo = Descendants(form).OfType<ComboBox>().Single(c => c.Name == "cmbPdfQuality");
            Assert.Equal(3, combo.Items.Count);
            Assert.Equal((int)stored, combo.SelectedIndex);
            Assert.DoesNotContain(combo.Items.Cast<string>(), i => i.Contains('—'));

            combo.SelectedIndex = (int)chosen;
            Descendants(form).OfType<Button>().Single(b => b.Name == "btnSave").PerformClick();

            Assert.Equal(chosen, repo.GetSettings().PdfOutputQuality);
        });

        [Fact]
        public void SettingsDialog_UnknownStoredValue_ShowsBalanced() => Sta(() =>
        {
            using var db = new LiteDatabase(new MemoryStream());
            var repo = new SettingsRepository(db);
            SettingsEntity s = repo.GetSettings();
            s.PdfOutputQuality = (PdfOutputQuality)42;
            repo.SaveSettings(s);

            using var form = new SettingsForm(repo);
            form.StartPosition = FormStartPosition.Manual;
            form.ShowInTaskbar = false;
            form.Show();
            Application.DoEvents();

            ComboBox combo = Descendants(form).OfType<ComboBox>().Single(c => c.Name == "cmbPdfQuality");
            Assert.Equal((int)PdfOutputQuality.Balanced, combo.SelectedIndex);
        });
    }
}
