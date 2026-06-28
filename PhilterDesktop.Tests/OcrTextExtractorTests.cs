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
        private static byte[] BuildImageOnlyPdf(string text)
        {
            const int dpi = 200;
            int widthPx = (int)(8.5 * dpi);
            int heightPx = (int)(11 * dpi);

            using var bitmap = new SKBitmap(widthPx, heightPx);
            using (var canvas = new SKCanvas(bitmap))
            {
                canvas.Clear(SKColors.White);
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
