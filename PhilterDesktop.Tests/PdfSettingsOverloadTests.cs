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

using Phileas.Services;
using PDFtoImage;
using PhilterData;
using PhilterDesktop;
using SkiaSharp;
using UglyToad.PdfPig;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The <see cref="SettingsEntity"/> overloads of the in-memory PDF APIs (used by the PDF preview) must
    /// honor every PDF setting, so a preview save matches what the queue produces.
    /// </summary>
    public sealed class PdfSettingsOverloadTests
    {
        // The logo's center, in PDF points from the top-left (= pixels when rendered at 72 DPI).
        private const int LogoX = 150, LogoY = 150;

        private static PhileasPolicy SsnPolicy() => new()
        {
            Name = "ssn",
            Identifiers = new Phileas.Policy.Identifiers { Ssn = new Phileas.Policy.Filters.Ssn() }
        };

        // Two Letter pages, each with the same red 100pt "logo" at the same place, plus an SSN.
        private static byte[] PdfWithRecurringLogo()
        {
            using var logo = new SKBitmap(100, 100);
            logo.Erase(SKColors.Red);
            using var stream = new MemoryStream();
            using (var document = SKDocument.CreatePdf(stream))
            {
                for (int page = 0; page < 2; page++)
                {
                    SKCanvas canvas = document.BeginPage(612, 792);
                    canvas.DrawBitmap(logo, new SKRect(LogoX - 50, LogoY - 50, LogoX + 50, LogoY + 50));
                    using var font = new SKFont(SKTypeface.FromFamilyName("Arial"), 12);
                    using var paint = new SKPaint { Color = SKColors.Black };
                    canvas.DrawText("SSN 123-45-6789", 72, 400, SKTextAlign.Left, font, paint);
                    document.EndPage();
                }
                document.Close();
            }
            return stream.ToArray();
        }

        private static SKColor LogoPixel(byte[] pdf)
        {
            using SKBitmap page = Conversion.ToImage(pdf, page: 0, options: new RenderOptions(Dpi: 72));
            return page.GetPixel(LogoX, LogoY);
        }

        private static bool IsBlack(SKColor c) => c.Red < 40 && c.Green < 40 && c.Blue < 40;
        private static bool IsRed(SKColor c) => c.Red > 200 && c.Green < 60 && c.Blue < 60;

        private static int FirstPageImageWidth(byte[] pdf)
        {
            using PdfDocument doc = PdfDocument.Open(pdf);
            return doc.GetPage(1).GetImages().First().WidthInSamples;
        }

        [Fact]
        public void Fixture_LogoIsDetectedAsRecurring() =>
            Assert.NotEmpty(RecurringImageDetector.Detect(PdfWithRecurringLogo()));

        [Fact]
        public async Task RedactPdfBytes_SettingsOn_CoversRecurringLogo()
        {
            var settings = new SettingsEntity { OcrScannedPdfs = false, RedactRecurringImages = true };
            (byte[] doc, List<RedactionSpanEntity> spans) = await RedactionService.RedactPdfBytesAsync(
                PdfWithRecurringLogo(), SsnPolicy(), "ctx", settings, new FilterService());

            Assert.True(IsBlack(LogoPixel(doc)), LogoPixel(doc).ToString());
            Assert.Equal(2, spans.Count); // text detection is unaffected
        }

        [Fact]
        public async Task RedactPdfBytes_SettingsOff_LeavesLogo()
        {
            var settings = new SettingsEntity { OcrScannedPdfs = false, RedactRecurringImages = false };
            (byte[] doc, _) = await RedactionService.RedactPdfBytesAsync(
                PdfWithRecurringLogo(), SsnPolicy(), "ctx", settings, new FilterService());

            Assert.True(IsRed(LogoPixel(doc)), LogoPixel(doc).ToString());
        }

        [Fact]
        public async Task ApplyPdfSpansToBytes_SettingsOn_CoversRecurringLogo()
        {
            var off = new SettingsEntity { OcrScannedPdfs = false };
            (_, List<RedactionSpanEntity> spans) = await RedactionService.RedactPdfBytesAsync(
                PdfWithRecurringLogo(), SsnPolicy(), "ctx", off, new FilterService());

            var on = new SettingsEntity { OcrScannedPdfs = false, RedactRecurringImages = true };
            byte[] doc = await RedactionService.ApplyPdfSpansToBytesAsync(PdfWithRecurringLogo(), spans, SsnPolicy(), on);

            Assert.True(IsBlack(LogoPixel(doc)), LogoPixel(doc).ToString());
        }

        [Fact]
        public async Task ApplyPdfSpansToBytes_SettingsOff_LeavesLogo()
        {
            var off = new SettingsEntity { OcrScannedPdfs = false };
            (_, List<RedactionSpanEntity> spans) = await RedactionService.RedactPdfBytesAsync(
                PdfWithRecurringLogo(), SsnPolicy(), "ctx", off, new FilterService());

            byte[] doc = await RedactionService.ApplyPdfSpansToBytesAsync(PdfWithRecurringLogo(), spans, SsnPolicy(), off);

            Assert.True(IsRed(LogoPixel(doc)), LogoPixel(doc).ToString());
        }

        [Fact]
        public async Task ApplyPdfSpansToBytes_NullPolicy_StillCoversRecurringLogo()
        {
            var on = new SettingsEntity { OcrScannedPdfs = false, RedactRecurringImages = true };
            byte[] doc = await RedactionService.ApplyPdfSpansToBytesAsync(
                PdfWithRecurringLogo(), Array.Empty<RedactionSpanEntity>(), null, on);

            Assert.True(IsBlack(LogoPixel(doc)), LogoPixel(doc).ToString());
        }

        [Fact]
        public async Task Overloads_ForwardOutputQuality()
        {
            var settings = new SettingsEntity { OcrScannedPdfs = false, PdfOutputQuality = PdfOutputQuality.Smaller };
            (byte[] detected, List<RedactionSpanEntity> spans) = await RedactionService.RedactPdfBytesAsync(
                PdfWithRecurringLogo(), SsnPolicy(), "ctx", settings, new FilterService());
            byte[] reapplied = await RedactionService.ApplyPdfSpansToBytesAsync(PdfWithRecurringLogo(), spans, SsnPolicy(), settings);

            Assert.InRange(FirstPageImageWidth(detected), 1273, 1277);  // 8.5in at 150 DPI
            Assert.InRange(FirstPageImageWidth(reapplied), 1273, 1277);
        }

        [SkippableFact]
        public async Task RedactPdfBytes_ForwardsOcrPageLimit()
        {
            Skip.If(Windows.Media.Ocr.OcrEngine.TryCreateFromUserProfileLanguages() is null,
                "No OCR language is available on this machine.");

            // Two image-only pages both need OCR, so a cap of 1 must fail loud through the overload too.
            using var stream = new MemoryStream();
            using (var document = SKDocument.CreatePdf(stream))
            {
                using var image = new SKBitmap(200, 200);
                image.Erase(SKColors.White);
                for (int i = 0; i < 2; i++)
                {
                    document.BeginPage(612, 792).DrawBitmap(image, new SKRect(0, 0, 612, 792));
                    document.EndPage();
                }
                document.Close();
            }

            var settings = new SettingsEntity { OcrScannedPdfs = true, OcrMaxPages = 1 };
            var ex = await Assert.ThrowsAsync<OcrPageLimitExceededException>(() =>
                RedactionService.RedactPdfBytesAsync(stream.ToArray(), SsnPolicy(), "ctx", settings, new FilterService()));
            Assert.Equal(1, ex.Limit);
        }
    }
}
