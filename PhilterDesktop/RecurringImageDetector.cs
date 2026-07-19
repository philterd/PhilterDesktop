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

using System.Security.Cryptography;
using Phileas.Policy;
using UglyToad.PdfPig;

namespace PhilterDesktop
{
    /// <summary>
    /// Finds raster images that repeat across a PDF's pages — logos, watermarks, stamps — and turns each
    /// occurrence into a fixed redaction region (a <see cref="BoundingBox"/>), so they can be blacked out
    /// at any position without a template or fixed coordinates. An image counts as recurring when the same
    /// content appears on a majority of pages (at least 2) and each occurrence is within a size band — above
    /// a minimum (so decorative icons aren't caught) and below a maximum (so a full-page background or
    /// template isn't treated as a logo, which would black out the whole page).
    ///
    /// A logo is matched by repeated content <b>or</b> by a repeated layout position, so a per-page colour
    /// variant (e.g. a light logo on a dark page) is still caught because it sits in the same slot.
    ///
    /// Raster images only: a logo drawn as vector paths has no image object to find and is not detected.
    /// The recurrence test may also cover other images that genuinely repeat across pages.
    /// </summary>
    internal static class RecurringImageDetector
    {
        /// <summary>One image found on a page: its content identity, placement (PDF points, lower-left
        /// origin — matching how the redactor paints a box), and the fraction of the page it covers.</summary>
        internal readonly record struct ImageOccurrence(
            int Page, string ContentKey, float X, float Y, float W, float H, double AreaFraction);

        /// <summary>Occurrences smaller than this fraction of the page are ignored (bullets, tiny icons).</summary>
        internal const double MinAreaFraction = 0.0015; // 0.15%

        /// <summary>
        /// Occurrences larger than this fraction of the page are ignored. A logo or watermark is small
        /// relative to the page; an image that covers most of it is a full-page background or template,
        /// and redacting it would black out the whole page.
        /// </summary>
        internal const double MaxAreaFraction = 0.5; // 50%

        /// <summary>
        /// Detects recurring images in <paramref name="pdf"/> and returns one box per occurrence of a
        /// flagged image. Best-effort: any failure (unreadable PDF, images without bounds) yields an empty
        /// list rather than blocking redaction.
        /// </summary>
        public static List<BoundingBox> Detect(byte[] pdf)
        {
            try
            {
                IReadOnlyList<ImageOccurrence> occurrences = ReadOccurrences(pdf, out int pageCount);
                return SelectRecurringBoxes(occurrences, pageCount);
            }
            catch
            {
                return new List<BoundingBox>();
            }
        }

        /// <summary>
        /// Pure selection: keeps occurrences whose content recurs on at least a majority of pages
        /// (<c>ceil(pageCount / 2)</c>, minimum 2) and that clear the minimum-size filter, and emits one
        /// box per kept occurrence. Recurrence counts <b>distinct pages</b>, so an image repeated twice on
        /// one page still counts once toward the threshold.
        /// </summary>
        internal static List<BoundingBox> SelectRecurringBoxes(IReadOnlyList<ImageOccurrence> occurrences, int pageCount)
        {
            if (pageCount <= 1 || occurrences.Count == 0)
            {
                return new List<BoundingBox>();
            }

            int threshold = Math.Max(2, (pageCount + 1) / 2); // majority: ceil(pageCount / 2), at least 2

            // Two recurrence signals, so a logo is caught whether its bytes are identical across pages (a
            // shared image) or its appearance changes per page but it stays in the same layout slot — e.g. a
            // light-on-dark variant for a page with a different background colour. Count distinct pages each.
            var pagesByContent = new Dictionary<string, HashSet<int>>();
            var pagesByPosition = new Dictionary<string, HashSet<int>>();
            foreach (ImageOccurrence o in occurrences)
            {
                AddPage(pagesByContent, o.ContentKey, o.Page);
                AddPage(pagesByPosition, PositionKey(o), o.Page);
            }

            var boxes = new List<BoundingBox>();
            foreach (ImageOccurrence o in occurrences)
            {
                // Skip images that are too small (icons/bullets) or too large (a full-page background or
                // template — redacting it would black out the whole page).
                if (o.AreaFraction < MinAreaFraction || o.AreaFraction > MaxAreaFraction)
                {
                    continue;
                }
                bool recurs = pagesByContent[o.ContentKey].Count >= threshold
                    || pagesByPosition[PositionKey(o)].Count >= threshold;
                if (!recurs)
                {
                    continue;
                }
                boxes.Add(new BoundingBox { Page = o.Page, X = o.X, Y = o.Y, W = o.W, H = o.H, Enabled = true });
            }
            return boxes;
        }

        private static void AddPage(Dictionary<string, HashSet<int>> pagesByKey, string key, int page)
        {
            if (!pagesByKey.TryGetValue(key, out HashSet<int>? pages))
            {
                pages = new HashSet<int>();
                pagesByKey[key] = pages;
            }
            pages.Add(page);
        }

        // A logo/watermark occupies the same layout slot on each page, so quantize its box to a coarse grid:
        // occurrences at roughly the same position and size group together even when their image bytes
        // differ per page (colour/background variants, which content hashing alone would miss) or their box
        // varies by a point or two. The grid is deliberately coarse so those small differences don't split
        // one logo slot into separate groups.
        private static string PositionKey(ImageOccurrence o)
        {
            const double bucket = 18.0; // PDF points (~0.25")
            return $"{Math.Round(o.X / bucket)}:{Math.Round(o.Y / bucket)}:{Math.Round(o.W / bucket)}:{Math.Round(o.H / bucket)}";
        }

        private static IReadOnlyList<ImageOccurrence> ReadOccurrences(byte[] pdf, out int pageCount)
        {
            var occurrences = new List<ImageOccurrence>();
            using PdfDocument document = PdfDocument.Open(pdf);
            pageCount = document.NumberOfPages;

            foreach (var page in document.GetPages())
            {
                double pageArea = page.Width * page.Height;
                if (pageArea <= 0)
                {
                    continue;
                }

                IEnumerable<UglyToad.PdfPig.Content.IPdfImage> images;
                try
                {
                    images = page.GetImages();
                }
                catch
                {
                    continue; // a page whose images can't be enumerated contributes nothing
                }

                foreach (var image in images)
                {
                    double w, h, left, bottom;
                    try
                    {
                        var bounds = image.BoundingBox;
                        w = bounds.Width;
                        h = bounds.Height;
                        left = bounds.Left;
                        bottom = bounds.Bottom;
                    }
                    catch
                    {
                        continue; // some images don't expose usable bounds
                    }
                    if (w <= 0 || h <= 0)
                    {
                        continue;
                    }

                    occurrences.Add(new ImageOccurrence(
                        page.Number, ContentKey(image),
                        (float)left, (float)bottom, (float)w, (float)h, (w * h) / pageArea));
                }
            }
            return occurrences;
        }

        // Identity of an image's content: a hash of its raw bytes, so the same logo (a shared XObject, or
        // identical bytes) hashes the same across pages. Falls back to sample dimensions when bytes aren't
        // available (an image the same size on every page is still a plausible watermark).
        private static string ContentKey(UglyToad.PdfPig.Content.IPdfImage image)
        {
            try
            {
                ReadOnlySpan<byte> raw = image.RawBytes;
                if (raw.Length > 0)
                {
                    return Convert.ToHexString(SHA256.HashData(raw));
                }
            }
            catch
            {
                // fall through to the size-based key
            }
            return $"size:{image.WidthInSamples}x{image.HeightInSamples}";
        }
    }
}
