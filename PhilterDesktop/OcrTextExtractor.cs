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

// On-device OCR of scanned PDFs via Windows.Media.Ocr. Produces the engine's CharBox-based PdfLine
// (Phileas .NET 1.4.0+) so OCR-recognized text flows through the normal PDF redaction pipeline.
using System.Drawing;
using System.Text;
using PDFtoImage;
using Phileas.Services.Pdf;
using SkiaSharp;
using UglyToad.PdfPig;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace PhilterDesktop
{
    /// <summary>
    /// Thrown when a PDF needs OCR on more pages than the configured safety cap. Surfaced to the user
    /// (rather than silently OCR'ing only some pages) so a partially-processed document is never mistaken
    /// for a fully-redacted one.
    /// </summary>
    public sealed class OcrPageLimitExceededException : Exception
    {
        public int PagesNeedingOcr { get; }
        public int Limit { get; }

        public OcrPageLimitExceededException(int pagesNeedingOcr, int limit)
            : base($"This PDF needs OCR on {pagesNeedingOcr} pages, which exceeds the limit of {limit}. " +
                   "Increase \"Maximum pages to OCR\" in Settings → PDF → Advanced, or split the document into smaller files.")
        {
            PagesNeedingOcr = pagesNeedingOcr;
            Limit = limit;
        }
    }

    /// <summary>How a page's text should be sourced for detection.</summary>
    internal enum OcrPageMode
    {
        /// <summary>Use the PDF text layer only (a normal digital page).</summary>
        TextOnly,

        /// <summary>OCR the page and use only the OCR text (a scanned/text-less page).</summary>
        Replace,

        /// <summary>Use the text layer <b>and</b> OCR (a page with real text over a large scanned image).</summary>
        Merge
    }

    /// <summary>
    /// An <see cref="ITextExtractor"/> that reads a PDF's text layer first and adds on-device OCR (via
    /// <see cref="OcrEngine"/>) where it's needed:
    /// <list type="bullet">
    ///   <item>a page whose text is too <b>sparse</b> to be its real content (scanned, or only a stray
    ///   header of real text) is OCR'd instead of using its text layer;</item>
    ///   <item>a page that has real text <b>and</b> a large embedded image (a scan with a digital
    ///   header/letterhead) is OCR'd <b>in addition to</b> its text layer, so PII baked into the image is
    ///   still found while the accurate digital text is kept.</item>
    /// </list>
    /// OCR-recognized words are mapped to per-character bounding boxes in PDF user-space points so the
    /// rest of the redaction pipeline (detect, locate, draw) is unchanged. OCR is best-effort: it can
    /// miss low-quality scans, unusual fonts, and all handwriting, so the output must still be reviewed.
    /// </summary>
    internal sealed class HybridTextExtractor : ITextExtractor
    {
        // Render scanned pages at a high DPI for OCR quality (independent of the redaction render DPI).
        private const int OcrDpi = 300;

        private readonly PdfTextExtractor _textLayer = new();
        private readonly double _minTextCoverage;
        private readonly double _minImageCoverage;

        /// <param name="minTextCoverage">
        ///     A page whose text-layer glyphs cover less than this fraction of the page is treated as
        ///     scanned and OCR'd (replacing its text). Biased low: over-OCR costs time, under-OCR misses PII.
        /// </param>
        /// <param name="minImageCoverage">
        ///     A page that still has real text but whose embedded images cover at least this fraction of
        ///     the page is also OCR'd (merged with its text), to catch PII inside a large scanned image.
        /// </param>
        public HybridTextExtractor(double minTextCoverage = 0.01, double minImageCoverage = 0.5)
        {
            _minTextCoverage = minTextCoverage;
            _minImageCoverage = minImageCoverage;
        }

        /// <summary>
        /// Counts how many pages would be OCR'd (Replace or Merge), without running any OCR. Used for a
        /// cheap pre-flight so a caller can enforce a page cap and fail loudly before doing OCR work.
        /// Returns 0 when no OCR engine is available (nothing would be OCR'd).
        /// </summary>
        public int CountPagesNeedingOcr(byte[] document)
        {
            if (OcrEngine.TryCreateFromUserProfileLanguages() is null)
            {
                return 0;
            }

            Dictionary<int, List<PdfLine>> byPage = _textLayer.GetLines(document)
                .GroupBy(l => l.PageNumber)
                .ToDictionary(g => g.Key, g => g.ToList());
            IReadOnlyList<(double Area, double ImageCoverage)> geometry = ReadPageGeometry(document);

            int count = 0;
            for (int page = 1; page <= geometry.Count; page++)
            {
                List<PdfLine> pageLines = byPage.TryGetValue(page, out List<PdfLine>? l) ? l : new List<PdfLine>();
                (double area, double imageCoverage) = geometry[page - 1];
                OcrPageMode mode = DecideMode(SumCharArea(pageLines), area, imageCoverage, _minTextCoverage, _minImageCoverage);
                if (mode is OcrPageMode.Replace or OcrPageMode.Merge)
                {
                    count++;
                }
            }
            return count;
        }

        public IReadOnlyList<PdfLine> GetLines(byte[] document)
        {
            Dictionary<int, List<PdfLine>> byPage = _textLayer.GetLines(document)
                .GroupBy(l => l.PageNumber)
                .ToDictionary(g => g.Key, g => g.ToList());

            OcrEngine? engine = OcrEngine.TryCreateFromUserProfileLanguages();
            IReadOnlyList<(double Area, double ImageCoverage)> geometry = ReadPageGeometry(document);

            var result = new List<PdfLine>();
            for (int page = 1; page <= geometry.Count; page++)
            {
                List<PdfLine> pageLines = byPage.TryGetValue(page, out List<PdfLine>? l) ? l : new List<PdfLine>();
                (double area, double imageCoverage) = geometry[page - 1];

                OcrPageMode mode = engine is null
                    ? OcrPageMode.TextOnly
                    : DecideMode(SumCharArea(pageLines), area, imageCoverage, _minTextCoverage, _minImageCoverage);

                switch (mode)
                {
                    case OcrPageMode.Replace:
                        result.AddRange(OcrPage(document, page, engine!));
                        break;
                    case OcrPageMode.Merge:
                        result.AddRange(pageLines);                  // keep accurate digital text
                        result.AddRange(OcrPage(document, page, engine!)); // + OCR to catch image PII
                        break;
                    default:
                        result.AddRange(pageLines);
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// Decides how a page's text should be sourced, given its text-layer glyph area, page area, and
        /// the fraction of the page covered by embedded images. Pure (no UI/IO) so it can be unit-tested.
        /// </summary>
        internal static OcrPageMode DecideMode(double textArea, double pageAreaPts, double imageCoverage,
            double minTextCoverage, double minImageCoverage)
        {
            if (pageAreaPts <= 0)
            {
                // Unknown geometry: only OCR when there is no text at all (can't measure coverage).
                return textArea <= 0 ? OcrPageMode.Replace : OcrPageMode.TextOnly;
            }

            double textCoverage = textArea / pageAreaPts;
            if (textCoverage < minTextCoverage)
            {
                return OcrPageMode.Replace; // scanned / essentially empty of real text
            }
            if (imageCoverage >= minImageCoverage)
            {
                return OcrPageMode.Merge; // real text plus a large scanned image
            }
            return OcrPageMode.TextOnly;
        }

        private static double SumCharArea(IReadOnlyList<PdfLine> pageLines)
        {
            double area = 0;
            foreach (PdfLine line in pageLines)
            {
                foreach (CharBox? box in line.CharBoxes)
                {
                    if (box is { } b)
                    {
                        area += Math.Max(0, b.Right - b.Left) * Math.Max(0, b.Top - b.Bottom);
                    }
                }
            }
            return area;
        }

        // Per-page area (PDF points^2) and the fraction of the page covered by embedded raster images
        // (summed and clamped to 1, so tiled scans count too). Uses PdfPig; on failure, falls back to
        // page sizes from PDFtoImage with no image information.
        private static IReadOnlyList<(double Area, double ImageCoverage)> ReadPageGeometry(byte[] document)
        {
            try
            {
                var list = new List<(double, double)>();
                using PdfDocument pdf = PdfDocument.Open(document);
                foreach (var page in pdf.GetPages())
                {
                    double area = page.Width * page.Height;
                    double imageArea = 0;
                    try
                    {
                        foreach (var image in page.GetImages())
                        {
                            imageArea += Math.Max(0, image.BoundingBox.Width) * Math.Max(0, image.BoundingBox.Height);
                        }
                    }
                    catch
                    {
                        // Some images fail to expose bounds; ignore and treat as no image coverage.
                    }
                    double coverage = area > 0 ? Math.Min(1.0, imageArea / area) : 0;
                    list.Add((area, coverage));
                }
                return list;
            }
            catch
            {
                return SafeGetPageSizes(document)
                    .Select(s => ((double)s.Width * s.Height, 0.0))
                    .ToList();
            }
        }

        private static IReadOnlyList<SizeF> SafeGetPageSizes(byte[] document)
        {
            try
            {
                return Conversion.GetPageSizes(document).ToList();
            }
            catch
            {
                return Array.Empty<SizeF>();
            }
        }

        private static IReadOnlyList<PdfLine> OcrPage(byte[] document, int pageNumber, OcrEngine engine)
        {
            byte[] png = RenderPagePng(document, pageNumber, out double pageHeightPts);
            OcrResult result = RecognizeAsync(engine, png).GetAwaiter().GetResult();

            double ptPerPixel = 72.0 / OcrDpi;
            var lines = new List<PdfLine>();
            foreach (OcrLine ocrLine in result.Lines)
            {
                (string text, IReadOnlyList<CharBox?> boxes) = BuildLine(ocrLine, ptPerPixel, pageHeightPts);
                if (text.Length > 0)
                {
                    lines.Add(new PdfLine(pageNumber, text, boxes));
                }
            }
            return lines;
        }

        // Rasterizes a page to PNG bytes at the OCR DPI and reports the page height in PDF points.
        private static byte[] RenderPagePng(byte[] document, int pageNumber, out double pageHeightPts)
        {
            using SKBitmap bitmap = Conversion.ToImage(document, page: pageNumber - 1,
                options: new RenderOptions(Dpi: OcrDpi));
            pageHeightPts = bitmap.Height * 72.0 / OcrDpi;
            using SKImage image = SKImage.FromBitmap(bitmap);
            using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        private static async Task<OcrResult> RecognizeAsync(OcrEngine engine, byte[] png)
        {
            using var stream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(stream))
            {
                writer.WriteBytes(png);
                await writer.StoreAsync();
                await writer.FlushAsync();
                writer.DetachStream();
            }
            stream.Seek(0);

            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            using SoftwareBitmap decoded = await decoder.GetSoftwareBitmapAsync();
            using SoftwareBitmap bgra = SoftwareBitmap.Convert(decoded, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            return await engine.RecognizeAsync(bgra);
        }

        // Builds a line's text plus a per-character box. OCR gives word-level boxes, so each character's
        // box is interpolated across its word's width (coarse but adequate for drawing redaction boxes).
        // Image pixels (top-left origin) are converted to PDF points (bottom-left origin) with a Y-flip.
        private static (string Text, IReadOnlyList<CharBox?> Boxes) BuildLine(
            OcrLine line, double ptPerPixel, double pageHeightPts)
        {
            var text = new StringBuilder();
            var boxes = new List<CharBox?>();

            for (int w = 0; w < line.Words.Count; w++)
            {
                if (w > 0)
                {
                    text.Append(' ');
                    boxes.Add(null); // word separator has no glyph
                }

                OcrWord word = line.Words[w];
                string value = word.Text ?? string.Empty;
                Windows.Foundation.Rect rect = word.BoundingRect;
                int n = Math.Max(value.Length, 1);

                for (int c = 0; c < value.Length; c++)
                {
                    double leftPx = rect.X + rect.Width * c / n;
                    double rightPx = rect.X + rect.Width * (c + 1) / n;
                    double topPx = rect.Y;
                    double bottomPx = rect.Y + rect.Height;

                    double left = leftPx * ptPerPixel;
                    double right = rightPx * ptPerPixel;
                    double top = pageHeightPts - topPx * ptPerPixel;       // image top -> larger PDF Y
                    double bottom = pageHeightPts - bottomPx * ptPerPixel; // image bottom -> smaller PDF Y

                    text.Append(value[c]);
                    boxes.Add(new CharBox(left, bottom, right, top));
                }
            }

            return (text.ToString(), boxes);
        }
    }
}
