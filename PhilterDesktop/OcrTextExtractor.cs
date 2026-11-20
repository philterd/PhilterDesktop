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
    ///   <item>a page whose text layer can't be read (fonts with no Unicode mapping) is OCR'd instead of
    ///   using its text layer.</item>
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
                OcrPageMode mode = DecideMode(SumCharArea(pageLines), area, imageCoverage, _minTextCoverage, _minImageCoverage,
                    IsUnreadable(pageLines));
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
                    : DecideMode(SumCharArea(pageLines), area, imageCoverage, _minTextCoverage, _minImageCoverage,
                        IsUnreadable(pageLines));

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
        /// the fraction of the page covered by embedded images. A text layer that can't be read (see
        /// <see cref="PdfTextLayerQuality"/>) is replaced by OCR whatever its size. Pure (no UI/IO) so it
        /// can be unit-tested.
        /// </summary>
        internal static OcrPageMode DecideMode(double textArea, double pageAreaPts, double imageCoverage,
            double minTextCoverage, double minImageCoverage, bool textUnreadable = false)
        {
            if (textUnreadable)
            {
                return OcrPageMode.Replace; // glyph-ID gibberish: detection would find nothing in it
            }
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

        private static bool IsUnreadable(IReadOnlyList<PdfLine> pageLines) =>
            PdfTextLayerQuality.LooksUnreadable(string.Join("\n", pageLines.Select(l => l.Text)));

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
            byte[] png = RenderPagePng(document, pageNumber, out int widthPx, out int heightPx);
            OcrResult result = RecognizeAsync(engine, png).GetAwaiter().GetResult();

            double ptPerPixel = 72.0 / OcrDpi;
            double pageHeightPts = heightPx * ptPerPixel;
            double textAngle = result.TextAngle ?? 0;
            var lines = new List<PdfLine>();
            foreach (OcrLine ocrLine in result.Lines)
            {
                (string text, IReadOnlyList<CharBox?> boxes) = BuildLine(
                    ocrLine, textAngle, widthPx / 2.0, heightPx / 2.0, ptPerPixel, pageHeightPts);
                if (text.Length > 0)
                {
                    lines.Add(new PdfLine(pageNumber, text, boxes));
                }
            }
            return lines;
        }

        // Rasterizes a page to PNG bytes at the OCR DPI and reports its size in pixels.
        private static byte[] RenderPagePng(byte[] document, int pageNumber, out int widthPx, out int heightPx)
        {
            using SKBitmap bitmap = Conversion.ToImage(document, page: pageNumber - 1,
                options: new RenderOptions(Dpi: OcrDpi));
            widthPx = bitmap.Width;
            heightPx = bitmap.Height;
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

        // Typical advance widths (Arial/Helvetica, 1/1000 em) for ASCII 32-126.
        private static readonly short[] AsciiWidths =
        {
            278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278, // space to /
            556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 278, 278, 584, 584, 584, 556, // 0 to ?
            1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778, // @ to O
            667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556, // P to _
            333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556, // ` to o
            556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584        // p to ~
        };

        private const double DefaultWidth = 556;

        // Share of a character's slot added on each side, kept inside the word, since fonts vary.
        private const double CharMargin = 0.15;

        // OCR word boxes can sit a few pixels inside the ink, so each is widened by these shares of its height.
        private const double WordPadSides = 0.2;
        private const double WordPadEnds = 0.1;

        /// <summary>Widens an OCR word box (pixels, top-left origin) so it covers the word's ink.</summary>
        internal static (double X, double Y, double Width, double Height) PadWord(double x, double y, double width, double height)
        {
            double side = height * WordPadSides, end = height * WordPadEnds;
            return (x - side, y - end, width + 2 * side, height + 2 * end);
        }

        internal static double CharWidth(char c) =>
            c >= ' ' && c <= '~' ? AsciiWidths[c - ' '] : DefaultWidth;

        /// <summary>
        /// Splits a word's horizontal extent into one range per character, sized by typical character
        /// width. An even split leaves part of a number uncovered when narrow punctuation such as ';'
        /// shares its word.
        /// </summary>
        internal static (double Left, double Right)[] SplitWord(string text, double left, double width)
        {
            var ranges = new (double Left, double Right)[text.Length];
            double total = 0;
            foreach (char c in text)
            {
                total += CharWidth(c);
            }
            double right = left + width;
            double x = left;
            for (int i = 0; i < text.Length; i++)
            {
                double slot = width * CharWidth(text[i]) / total;
                double pad = slot * CharMargin;
                ranges[i] = (Math.Max(left, x - pad), Math.Min(right, x + slot + pad));
                x += slot;
            }
            return ranges;
        }

        /// <summary>
        /// Maps a box from Windows OCR's deskewed frame back onto the page image. When OCR detects
        /// skew it reports word boxes rotated by <c>TextAngle</c> about the image centre; used as-is,
        /// they miss the text. Returns the bounding box of the rotated corners (pixels, top-left origin).
        /// </summary>
        internal static (double Left, double Top, double Right, double Bottom) Unrotate(
            double left, double top, double right, double bottom, double angleDegrees, double centerX, double centerY)
        {
            if (angleDegrees == 0)
            {
                return (left, top, right, bottom);
            }
            double a = angleDegrees * Math.PI / 180;
            double cos = Math.Cos(a), sin = Math.Sin(a);
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            foreach ((double x, double y) in new[] { (left, top), (right, top), (left, bottom), (right, bottom) })
            {
                double dx = x - centerX, dy = y - centerY;
                double ux = centerX + dx * cos - dy * sin;
                double uy = centerY + dx * sin + dy * cos;
                minX = Math.Min(minX, ux);
                maxX = Math.Max(maxX, ux);
                minY = Math.Min(minY, uy);
                maxY = Math.Max(maxY, uy);
            }
            return (minX, minY, maxX, maxY);
        }

        // Builds a line's text plus a per-character box. OCR gives word-level boxes, so each word's box
        // is shared out by SplitWord. Image pixels (top-left origin) become PDF points (bottom-left).
        private static (string Text, IReadOnlyList<CharBox?> Boxes) BuildLine(
            OcrLine line, double textAngle, double centerXPx, double centerYPx, double ptPerPixel, double pageHeightPts)
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
                Windows.Foundation.Rect r = word.BoundingRect;
                (double x, double y, double width, double height) = PadWord(r.X, r.Y, r.Width, r.Height);
                (double Left, double Right)[] ranges = SplitWord(value, x, width);

                for (int c = 0; c < value.Length; c++)
                {
                    (double leftPx, double topPx, double rightPx, double bottomPx) = Unrotate(
                        ranges[c].Left, y, ranges[c].Right, y + height, textAngle, centerXPx, centerYPx);

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
