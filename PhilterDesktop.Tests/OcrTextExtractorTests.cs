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

using Phileas.Services.Pdf;
using PhilterDesktop;
using SkiaSharp;
using Windows.Media.Ocr;
using Xunit;

namespace PhilterDesktop.Tests
{
    public class OcrTextExtractorTests
    {
        private const double LetterArea = 612.0 * 792.0; // US Letter, points^2
        private const double MinText = 0.01;             // 1% default
        private const double MinImage = 0.5;             // 50% default
        private const double StrayText = 0.003 * LetterArea;  // ~0.3% coverage
        private const double RealText = 0.13 * LetterArea;    // ~13% coverage

        [Fact]
        public void DecideMode_EmptyPage_Replace() =>
            Assert.Equal(OcrPageMode.Replace, HybridTextExtractor.DecideMode(0, LetterArea, 0, MinText, MinImage));

        [Fact]
        public void DecideMode_StrayText_Replace() =>
            Assert.Equal(OcrPageMode.Replace, HybridTextExtractor.DecideMode(StrayText, LetterArea, 0, MinText, MinImage));

        [Fact]
        public void DecideMode_RealText_NoImage_TextOnly() =>
            Assert.Equal(OcrPageMode.TextOnly, HybridTextExtractor.DecideMode(RealText, LetterArea, 0, MinText, MinImage));

        [Fact]
        public void DecideMode_RealText_SmallImage_TextOnly() =>
            Assert.Equal(OcrPageMode.TextOnly, HybridTextExtractor.DecideMode(RealText, LetterArea, 0.3, MinText, MinImage));

        [Fact]
        public void DecideMode_RealText_LargeImage_Merge() =>
            Assert.Equal(OcrPageMode.Merge, HybridTextExtractor.DecideMode(RealText, LetterArea, 0.6, MinText, MinImage));

        [Fact]
        public void DecideMode_UnknownSize_WithText_TextOnly() =>
            Assert.Equal(OcrPageMode.TextOnly, HybridTextExtractor.DecideMode(5000, 0, 0, MinText, MinImage));

        [Fact]
        public void DecideMode_UnknownSize_NoText_Replace() =>
            Assert.Equal(OcrPageMode.Replace, HybridTextExtractor.DecideMode(0, 0, 0, MinText, MinImage));

        // Builds an image-only (scanned-looking) one-page PDF: text is rasterized onto a bitmap and the
        // bitmap is drawn onto the page, so the PDF has no text layer for PdfPig to read.
        private static byte[] BuildImageOnlyPdf(string text, float skewDegrees = 0)
        {
            const int dpi = 200;
            int widthPx = (int)(8.5 * dpi);
            int heightPx = (int)(11 * dpi);

            using var bitmap = new SKBitmap(widthPx, heightPx);
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.White);
                canvas.RotateDegrees(skewDegrees, widthPx / 2f, heightPx / 2f); // a crooked scan
                using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
                using var font = new SKFont(SKTypeface.FromFamilyName("Arial"), 48);
                canvas.DrawText(text, 120, 300, SKTextAlign.Left, font, paint);
            }

            using var stream = new MemoryStream();
            float widthPts = 8.5f * 72f;
            float heightPts = 11f * 72f;
            using (var document = SKDocument.CreatePdf(stream, new SKDocumentPdfMetadata { RasterDpi = dpi }))
            {
                var canvas = document.BeginPage(widthPts, heightPts);
                canvas.DrawBitmap(bitmap, new SKRect(0, 0, widthPts, heightPts));
                document.EndPage();
                document.Close();
            }
            return stream.ToArray();
        }

        [SkippableFact]
        public void CommittedScannedPdf_HasNoTextLayer_AndOcrRecoversPii()
        {
            Skip.If(OcrEngine.TryCreateFromUserProfileLanguages() is null,
                "No OCR language is available on this machine.");

            string path = Path.Combine(AppContext.BaseDirectory, "test-documents", "scanned-letter.pdf");
            Skip.IfNot(File.Exists(path), "scanned-letter.pdf not found in test-documents.");
            byte[] pdf = File.ReadAllBytes(path);

            // It must genuinely be image-only: the PDF text layer yields nothing, so OCR is required.
            Assert.Empty(new Phileas.Services.Pdf.PdfTextExtractor().GetLines(pdf));

            string all = string.Join(" ", new HybridTextExtractor().GetLines(pdf).Select(l => l.Text));
            Assert.Contains("Smith", all, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Springfield", all, StringComparison.OrdinalIgnoreCase);
        }

        [SkippableFact]
        public async Task EndToEnd_OcrThenDetect_RedactsSsnOnScannedPdf()
        {
            Skip.If(OcrEngine.TryCreateFromUserProfileLanguages() is null,
                "No OCR language is available on this machine.");

            string source = Path.Combine(AppContext.BaseDirectory, "test-documents", "scanned-medical-record.pdf");
            Skip.IfNot(File.Exists(source), "scanned-medical-record.pdf not found in test-documents.");

            // SSN is a regex filter (no on-device model needed), so it isolates the OCR->detect->redact path.
            var policy = new Phileas.Policy.Policy
            {
                Name = "ssn",
                Identifiers = new Phileas.Policy.Identifiers { Ssn = new Phileas.Policy.Filters.Ssn() }
            };

            string output = Path.Combine(Path.GetTempPath(), "ocr-ssn-" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                var spans = await RedactionService.RedactFileAsync(source, output, policy, "ctx", new Phileas.Services.FilterService(),
                    ocrScannedPdfs: true);
                Assert.NotEmpty(spans);                       // the SSN was found via OCR text
                Assert.All(spans, s => Assert.True(s.PageNumber > 0)); // located on a page (so a box is drawn)
            }
            finally
            {
                try { File.Delete(output); } catch { /* best effort */ }
            }
        }

        [SkippableFact]
        public async Task ExceedingOcrPageCap_FailsLoud_RatherThanUnderRedacting()
        {
            Skip.If(OcrEngine.TryCreateFromUserProfileLanguages() is null,
                "No OCR language is available on this machine.");

            string path = Path.Combine(AppContext.BaseDirectory, "test-documents", "scanned-201-pages.pdf");
            Skip.IfNot(File.Exists(path), "scanned-201-pages.pdf not found in test-documents.");
            byte[] pdf = File.ReadAllBytes(path);

            // The pre-flight count must see all 201 image-only pages as needing OCR.
            Assert.Equal(201, new HybridTextExtractor().CountPagesNeedingOcr(pdf));

            // 201 pages all need OCR; with a cap of 200 redaction must fail loud (a clean, caught
            // exception) rather than silently OCR only some pages and leave PII on the rest.
            var policy = new Phileas.Policy.Policy
            {
                Name = "ssn",
                Identifiers = new Phileas.Policy.Identifiers { Ssn = new Phileas.Policy.Filters.Ssn() }
            };
            var ex = await Assert.ThrowsAsync<OcrPageLimitExceededException>(() =>
                RedactionService.RedactPdfBytesAsync(pdf, policy, "ctx", new Phileas.Services.FilterService(),
                    ocrScannedPdfs: true, ocrMaxPages: 200));
            Assert.Equal(201, ex.PagesNeedingOcr);
            Assert.Equal(200, ex.Limit);
        }

        // ---- sharing an OCR word's box across its characters ----

        [Theory]
        [InlineData('0', 556)]
        [InlineData('9', 556)]
        [InlineData(';', 278)]
        [InlineData('.', 278)]
        [InlineData(' ', 278)]
        [InlineData('i', 222)]
        [InlineData('m', 833)]
        [InlineData('W', 944)]
        [InlineData('@', 1015)]
        [InlineData('~', 584)]
        [InlineData('é', 556)] // outside ASCII: average width
        [InlineData('\t', 556)] // control character: average width
        public void CharWidth_UsesTypicalWidths(char c, double expected) =>
            Assert.Equal(expected, HybridTextExtractor.CharWidth(c));

        [Fact]
        public void SplitWord_DigitsBeforeSemicolon_CoverTheirShare()
        {
            // 12 digits (556 each) and ';' (278): the digits take 12*556/(12*556+278) of the width.
            var ranges = HybridTextExtractor.SplitWord("987654321012;", 0, 1000);
            double digitsEnd = 1000.0 * 12 * 556 / (12 * 556 + 278);
            Assert.True(ranges[11].Right >= digitsEnd, $"last digit ends at {ranges[11].Right}, digits end at {digitsEnd}");
            // An even split stops the digits at 12/13 of the width, short of the last digit.
            Assert.True(digitsEnd > 1000.0 * 12 / 13);
        }

        [Fact]
        public void SplitWord_SameWidthCharacters_SplitEvenly_WithMargin()
        {
            var ranges = HybridTextExtractor.SplitWord("1234", 100, 40);
            Assert.Equal(100, ranges[0].Left);    // margin stays inside the word
            Assert.Equal(140, ranges[3].Right);
            Assert.Equal(110 + 1.5, ranges[0].Right, 6); // 10 px slot plus a 15% margin
            Assert.Equal(120 - 1.5, ranges[2].Left, 6);
        }

        [Fact]
        public void SplitWord_StaysInsideTheWord_AndInOrder()
        {
            var ranges = HybridTextExtractor.SplitWord("(617) 555-0142;", 50, 300);
            Assert.All(ranges, r => Assert.InRange(r.Left, 50, 350));
            Assert.All(ranges, r => Assert.InRange(r.Right, 50, 350));
            Assert.All(ranges, r => Assert.True(r.Right > r.Left));
            for (int i = 1; i < ranges.Length; i++)
            {
                Assert.True(ranges[i].Left > ranges[i - 1].Left);
                Assert.True(ranges[i].Left <= ranges[i - 1].Right, "adjacent characters leave no gap");
            }
        }

        [Fact]
        public void SplitWord_SingleCharacter_TakesTheWholeWord()
        {
            var ranges = HybridTextExtractor.SplitWord(";", 10, 5);
            Assert.Equal((10.0, 15.0), Assert.Single(ranges));
        }

        [Fact]
        public void PadWord_WidensByAShareOfTheHeight()
        {
            // 50 px tall: 10 px each side, 5 px above and below.
            Assert.Equal((90.0, 195.0, 220.0, 60.0), HybridTextExtractor.PadWord(100, 200, 200, 50));
        }

        [Fact]
        public void PadWord_ScalesWithTextSize()
        {
            var small = HybridTextExtractor.PadWord(0, 0, 40, 10);
            var large = HybridTextExtractor.PadWord(0, 0, 160, 40);
            Assert.Equal(small.X * 4, large.X, 6);
            Assert.Equal(small.Y * 4, large.Y, 6);
        }

        [Fact]
        public void SplitWord_Empty_ReturnsNothing() =>
            Assert.Empty(HybridTextExtractor.SplitWord(string.Empty, 0, 100));

        [SkippableFact]
        public void OcrBox_CoversANumber_FollowedByPunctuation()
        {
            Skip.If(OcrEngine.TryCreateFromUserProfileLanguages() is null,
                "No OCR language is available on this machine.");

            const string number = "987654321012";
            byte[] pdf = BuildImageOnlyPdf("Account No. " + number + "; Checking");

            // The number's ink, measured on the rendered page: the third word, minus its trailing ';'.
            var words = InkRuns(pdf, minGapPx: 12);
            Assert.Equal(4, words.Count);
            var glyphs = InkRuns(pdf, minGapPx: 2).Where(r => r.Left >= words[2].Left && r.Right <= words[2].Right).ToList();
            double inkLeft = words[2].Left;
            double inkRight = glyphs[^2].Right;

            PdfLine line = new HybridTextExtractor().GetLines(pdf).Single(l => l.Text.Contains(number));
            int start = line.Text.IndexOf(number, StringComparison.Ordinal);
            CharBox first = line.CharBoxes[start]!.Value;
            CharBox last = line.CharBoxes[start + number.Length - 1]!.Value;

            Assert.True(last.Right >= inkRight, $"box ends at {last.Right:F2} pt, last digit ends at {inkRight:F2} pt");
            Assert.True(first.Left <= inkLeft, $"box starts at {first.Left:F2} pt, first digit starts at {inkLeft:F2} pt");
        }

        [SkippableTheory]
        [InlineData(0f)]
        [InlineData(2f)]
        [InlineData(-2f)]
        public void OcrBoxes_CoverTheInk_OnAStraightOrCrookedScan(float skew)
        {
            Skip.If(OcrEngine.TryCreateFromUserProfileLanguages() is null,
                "No OCR language is available on this machine.");

            const string number = "987654321012";
            byte[] pdf = BuildImageOnlyPdf(number, skew);
            var ink = InkBounds(pdf);

            PdfLine line = new HybridTextExtractor().GetLines(pdf).Single(l => l.Text.Contains(number));
            int start = line.Text.IndexOf(number, StringComparison.Ordinal);
            var boxes = Enumerable.Range(start, number.Length).Select(i => line.CharBoxes[i]!.Value).ToList();

            // Tolerance of one 200 DPI pixel, the resolution the ink is measured at.
            const double px = 72.0 / 200;
            Assert.True(boxes.Min(b => b.Left) <= ink.Left + px, $"left {boxes.Min(b => b.Left):F2} vs ink {ink.Left:F2}");
            Assert.True(boxes.Max(b => b.Right) >= ink.Right - px, $"right {boxes.Max(b => b.Right):F2} vs ink {ink.Right:F2}");
            Assert.True(boxes.Min(b => b.Bottom) <= ink.Bottom + px, $"bottom {boxes.Min(b => b.Bottom):F2} vs ink {ink.Bottom:F2}");
            Assert.True(boxes.Max(b => b.Top) >= ink.Top - px, $"top {boxes.Max(b => b.Top):F2} vs ink {ink.Top:F2}");
        }

        // ---- mapping OCR's deskewed boxes back onto the page ----

        [Fact]
        public void Unrotate_NoAngle_ReturnsTheBox() =>
            Assert.Equal((10.0, 20.0, 30.0, 40.0), HybridTextExtractor.Unrotate(10, 20, 30, 40, 0, 500, 500));

        [Fact]
        public void Unrotate_UndoesOcrsDeskew()
        {
            // A box at true (180..200, 400..450) as OCR reports it after deskewing by -1.4 degrees about (1275, 1650).
            const double angle = -1.4, cx = 1275, cy = 1650;
            (double l, double t, double r, double b) = Rotate(180, 400, 200, 450, -angle, cx, cy);
            var back = HybridTextExtractor.Unrotate(l, t, r, b, angle, cx, cy);
            Assert.True(back.Left <= 180 && back.Right >= 200 && back.Top <= 400 && back.Bottom >= 450, back.ToString());
            Assert.True(back.Left > 175 && back.Right < 205 && back.Top > 395 && back.Bottom < 455, back.ToString());
        }

        [Fact]
        public void Unrotate_MeasuredCase_MovesTheBoxBackOntoTheText()
        {
            // Measured: ink starting at x=180 px was reported at x=209 px with TextAngle -1.4 on a 2550x3300 render.
            var back = HybridTextExtractor.Unrotate(209, 405, 471, 470, -1.4, 1275, 1650);
            Assert.InRange(back.Left, 176, 182);
        }

        [Fact]
        public void Unrotate_BoxAtTheCentre_StaysPut()
        {
            var back = HybridTextExtractor.Unrotate(490, 495, 510, 505, 3, 500, 500);
            Assert.InRange(back.Left, 489, 491);
            Assert.InRange(back.Right, 509, 511);
        }

        // Rotates a box's corners about a centre (image coordinates) and returns their bounding box.
        private static (double, double, double, double) Rotate(double l, double t, double r, double b, double deg, double cx, double cy)
        {
            double a = deg * Math.PI / 180, cos = Math.Cos(a), sin = Math.Sin(a);
            var pts = new[] { (l, t), (r, t), (l, b), (r, b) }
                .Select(p => (X: cx + (p.Item1 - cx) * cos - (p.Item2 - cy) * sin, Y: cy + (p.Item1 - cx) * sin + (p.Item2 - cy) * cos))
                .ToList();
            return (pts.Min(p => p.X), pts.Min(p => p.Y), pts.Max(p => p.X), pts.Max(p => p.Y));
        }

        // Bounding box of all ink on page 1 rendered at 200 DPI, in PDF points (bottom-left origin).
        private static (double Left, double Bottom, double Right, double Top) InkBounds(byte[] pdf)
        {
            const int dpi = 200;
            using SKBitmap page = PDFtoImage.Conversion.ToImage(pdf, page: 0, options: new PDFtoImage.RenderOptions(Dpi: dpi));
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
            for (int y = 0; y < page.Height; y++)
            {
                for (int x = 0; x < page.Width; x++)
                {
                    if (page.GetPixel(x, y).Red < 160)
                    {
                        minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                        minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                    }
                }
            }
            double k = 72.0 / dpi, heightPts = page.Height * k;
            return (minX * k, heightPts - (maxY + 1) * k, (maxX + 1) * k, heightPts - minY * k);
        }

        // Horizontal ink runs (in PDF points) on page 1 rendered at 200 DPI, split by blank gaps of at least minGapPx.
        private static List<(double Left, double Right)> InkRuns(byte[] pdf, int minGapPx)
        {
            const int dpi = 200;
            using SKBitmap page = PDFtoImage.Conversion.ToImage(pdf, page: 0, options: new PDFtoImage.RenderOptions(Dpi: dpi));
            var ink = new bool[page.Width];
            for (int x = 0; x < page.Width; x++)
            {
                for (int y = 0; y < page.Height && !ink[x]; y++)
                {
                    ink[x] = page.GetPixel(x, y).Red < 160;
                }
            }

            var runs = new List<(double, double)>();
            int runStart = -1, lastInk = -1;
            for (int x = 0; x <= page.Width; x++)
            {
                bool dark = x < page.Width && ink[x];
                if (dark)
                {
                    if (runStart < 0) runStart = x;
                    else if (x - lastInk > minGapPx) { runs.Add(ToPts(runStart, lastInk)); runStart = x; }
                    lastInk = x;
                }
            }
            if (runStart >= 0) runs.Add(ToPts(runStart, lastInk));
            return runs;

            static (double, double) ToPts(int left, int right) => (left * 72.0 / dpi, (right + 1) * 72.0 / dpi);
        }

        [SkippableFact]
        public void OcrsScannedPage_WhenTextLayerIsAbsent()
        {
            Skip.If(OcrEngine.TryCreateFromUserProfileLanguages() is null,
                "No OCR language is available on this machine.");

            byte[] pdf = BuildImageOnlyPdf("Patient Name Jonathan Smith");

            var lines = new HybridTextExtractor().GetLines(pdf);

            string all = string.Join(" ", lines.Select(l => l.Text));
            Assert.NotEmpty(lines);
            Assert.Contains("Smith", all, StringComparison.OrdinalIgnoreCase);
            // Boxes are positioned on page 1 with positive area (so a span could be located/redacted).
            Assert.All(lines, l => Assert.Equal(1, l.PageNumber));
        }
    }
}
