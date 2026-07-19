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

using System.Text;
using Phileas.Policy;
using PhilterData;
using Xunit;
using Occ = PhilterDesktop.RecurringImageDetector.ImageOccurrence;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Recurring-image detection turns logos/watermarks (raster images that repeat across a PDF's pages)
    /// into per-occurrence redaction boxes. These cover the pure selection logic — the threshold, the
    /// minimum-size filter, distinct-page counting, and coordinate pass-through — without needing a PDF.
    /// </summary>
    public sealed class RecurringImageDetectorTests
    {
        private const double Big = 0.05;   // clears the min-size filter
        private const double Tiny = 0.0005; // below RecurringImageDetector.MinAreaFraction (0.0015)

        private static Occ Img(int page, string key, double area = Big, float x = 10, float y = 20, float w = 30, float h = 40)
            => new(page, key, x, y, w, h, area);

        [Fact]
        public void RecurringImage_OnAtLeastHalfThePages_ProducesOneBoxPerOccurrence()
        {
            // 4 pages, a logo on all 4 (threshold = 2). One box per occurrence, at the image's position.
            var boxes = RecurringImageDetector.SelectRecurringBoxes(
                new[] { Img(1, "logo", x: 5, y: 6, w: 7, h: 8), Img(2, "logo"), Img(3, "logo"), Img(4, "logo") }, pageCount: 4);

            Assert.Equal(new[] { 1, 2, 3, 4 }, boxes.Select(b => b.Page).OrderBy(p => p).ToArray());
            BoundingBox first = boxes.Single(b => b.Page == 1);
            Assert.Equal(5, first.X);
            Assert.Equal(6, first.Y);
            Assert.Equal(7, first.W);
            Assert.Equal(8, first.H);
            Assert.True(first.Enabled);
        }

        [Fact]
        public void ImageOnFewerThanHalfThePages_IsNotFlagged()
        {
            // 4 pages, image on only 1 -> below the threshold of 2.
            var boxes = RecurringImageDetector.SelectRecurringBoxes(new[] { Img(1, "once") }, pageCount: 4);
            Assert.Empty(boxes);
        }

        [Fact]
        public void OnlyRecurringImages_AreFlagged_OneOffsAreLeft()
        {
            // "logo" recurs on all 4 pages (same slot); "photo" appears once, elsewhere on the page.
            var boxes = RecurringImageDetector.SelectRecurringBoxes(
                new[]
                {
                    Img(1, "logo"), Img(2, "logo"), Img(3, "logo"), Img(4, "logo"),
                    Img(2, "photo", x: 300, y: 400), // one-off, a different position from the logo
                },
                pageCount: 4);

            Assert.Equal(4, boxes.Count); // the four logo occurrences only
        }

        [Fact]
        public void TinyRecurringImages_AreIgnored()
        {
            // A small icon on every page still shouldn't be redacted.
            var boxes = RecurringImageDetector.SelectRecurringBoxes(
                new[] { Img(1, "icon", Tiny), Img(2, "icon", Tiny), Img(3, "icon", Tiny) }, pageCount: 3);
            Assert.Empty(boxes);
        }

        [Fact]
        public void FullPageRecurringImage_IsIgnored_SoWholePagesAreNotBlackedOut()
        {
            // A full-page background/template image repeats on every page; redacting it would black out the
            // whole page, so anything above the max-size cap is left alone.
            const double fullPage = 0.98;
            var boxes = RecurringImageDetector.SelectRecurringBoxes(
                new[] { Img(1, "bg", fullPage), Img(2, "bg", fullPage), Img(3, "bg", fullPage), Img(4, "bg", fullPage) },
                pageCount: 4);
            Assert.Empty(boxes);
        }

        [Fact]
        public void ColourVariantAtSamePosition_IsCaught_ViaLayoutPosition()
        {
            // 4 pages: the logo is one image on pages 1-3, but a different image (a light-on-dark variant)
            // on page 4 — same bottom-left slot. Content hashing splits them, but the shared position keeps
            // all four flagged, so page 4 isn't left unredacted.
            var boxes = RecurringImageDetector.SelectRecurringBoxes(
                new[]
                {
                    Img(1, "darkLogo", x: 20, y: 20, w: 80, h: 30),
                    Img(2, "darkLogo", x: 20, y: 20, w: 80, h: 30),
                    Img(3, "darkLogo", x: 20, y: 20, w: 80, h: 30),
                    Img(4, "lightLogo", x: 21, y: 20, w: 80, h: 30), // different bytes, same slot (±1pt)
                },
                pageCount: 4);

            Assert.Equal(new[] { 1, 2, 3, 4 }, boxes.Select(b => b.Page).OrderBy(p => p).ToArray());
        }

        [Fact]
        public void RecurrenceCountsDistinctPages_NotOccurrences()
        {
            // Same image twice on page 1 but nowhere else, in a 2-page doc -> one distinct page, below threshold 2.
            var boxes = RecurringImageDetector.SelectRecurringBoxes(
                new[] { Img(1, "logo"), Img(1, "logo", x: 99) }, pageCount: 2);
            Assert.Empty(boxes);
        }

        [Fact]
        public void SmallDocument_RequiresImageOnEveryPage()
        {
            // 2 pages: threshold clamps to 2, so the image must be on both.
            Assert.Single(RecurringImageDetector.SelectRecurringBoxes(new[] { Img(1, "l"), Img(2, "l") }, pageCount: 2), b => b.Page == 1);
            Assert.Empty(RecurringImageDetector.SelectRecurringBoxes(new[] { Img(1, "l") }, pageCount: 2));
        }

        [Fact]
        public void SinglePageOrNoImages_ProducesNothing()
        {
            Assert.Empty(RecurringImageDetector.SelectRecurringBoxes(new[] { Img(1, "l") }, pageCount: 1));
            Assert.Empty(RecurringImageDetector.SelectRecurringBoxes(Array.Empty<Occ>(), pageCount: 10));
        }

        [Fact]
        public void Detect_OnNonPdfBytes_ReturnsEmpty_DoesNotThrow()
        {
            // Best-effort: a bad/unreadable document must never block redaction.
            Assert.Empty(RecurringImageDetector.Detect(new byte[] { 1, 2, 3, 4 }));
        }

        private static string SamplesDir => Path.Combine(AppContext.BaseDirectory, "test-documents");

        [SkippableFact]
        public void Detect_OnRealMultiPagePdf_ExercisesImageApi_WithoutThrowing()
        {
            string pdf = Path.Combine(SamplesDir, "scanned-two-page.pdf");
            Skip.IfNot(File.Exists(pdf), "sample PDF not present");
            // Exercises the real PdfPig path (GetImages / BoundingBox / RawBytes). Each scanned page is a
            // distinct image, so nothing recurs — the point is that it runs cleanly on a real document.
            Assert.NotNull(RecurringImageDetector.Detect(File.ReadAllBytes(pdf)));
        }

        [SkippableFact]
        public async Task RedactFileAsync_WithRecurringImagesSetting_ProducesValidPdf()
        {
            string input = Path.Combine(SamplesDir, "test1.pdf");
            Skip.IfNot(File.Exists(input), "sample PDF not present");
            string output = Path.Combine(Path.GetTempPath(), "recimg-" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                // The setting flows through RedactFileAsync -> RedactPdfBytesAsync -> PreparePdfPolicy.
                var settings = new SettingsEntity { RedactRecurringImages = true, OcrScannedPdfs = false };
                await RedactionService.RedactFileAsync(input, output, new Policy { Name = "p" }, "ctx", settings);

                byte[] bytes = await File.ReadAllBytesAsync(output);
                Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
            }
            finally { try { File.Delete(output); } catch { /* best effort */ } }
        }
    }
}
