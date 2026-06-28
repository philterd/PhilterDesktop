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

using System.Drawing;
using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Tests the pixel-to-PDF-point mapping behind reviewer-drawn PDF redactions
    /// (<see cref="PdfSideBySideView.MapPictureRectToPage"/>).
    /// </summary>
    public class PdfRegionMappingTests
    {
        // Rendered at 150 DPI: 1200 x 1600 px maps to 576 x 768 points (1 px = 72/150 = 0.48 pt).
        private const int Dpi = 150;
        private const int ImageWidth = 1200;
        private const int ImageHeight = 1600;
        private const double PageHeightPts = 768; // 1600 * 72 / 150

        [Fact]
        public void MapsPixelsToPoints_AtActualSize_WithYFlip()
        {
            // Picture shown at actual size (display scale 1): 1 px = 0.48 points.
            var region = PdfSideBySideView.MapPictureRectToPage(
                Rectangle.FromLTRB(150, 150, 300, 300), pictureWidth: ImageWidth,
                ImageWidth, ImageHeight, pageNumber: 3, Dpi);

            Assert.NotNull(region);
            Assert.Equal(3, region!.PageNumber);
            Assert.Equal(72, region.LowerLeftX, 3);    // 150 * 0.48
            Assert.Equal(144, region.UpperRightX, 3);  // 300 * 0.48
            // Y is flipped about the page height (top pixel -> larger Y).
            Assert.Equal(PageHeightPts - 72, region.UpperRightY, 3);   // 768 - 150*0.48
            Assert.Equal(PageHeightPts - 144, region.LowerLeftY, 3);   // 768 - 300*0.48
        }

        [Fact]
        public void DisplayScaleIsCompensated_SameRegionAtHalfSize()
        {
            // Same page region must result regardless of zoom: at half display size, pixels halve.
            var region = PdfSideBySideView.MapPictureRectToPage(
                Rectangle.FromLTRB(75, 75, 150, 150), pictureWidth: ImageWidth / 2,
                ImageWidth, ImageHeight, pageNumber: 1, Dpi);

            Assert.NotNull(region);
            Assert.Equal(72, region!.LowerLeftX, 3);
            Assert.Equal(144, region.UpperRightX, 3);
            Assert.Equal(PageHeightPts - 72, region.UpperRightY, 3);
            Assert.Equal(PageHeightPts - 144, region.LowerLeftY, 3);
        }

        [Fact]
        public void ReturnsNull_OnDegenerateInputs()
        {
            Assert.Null(PdfSideBySideView.MapPictureRectToPage(new Rectangle(0, 0, 10, 10), 0, ImageWidth, ImageHeight, 1, Dpi));
            Assert.Null(PdfSideBySideView.MapPictureRectToPage(new Rectangle(0, 0, 10, 10), ImageWidth, 0, ImageHeight, 1, Dpi));
            Assert.Null(PdfSideBySideView.MapPictureRectToPage(new Rectangle(0, 0, 10, 10), ImageWidth, ImageWidth, ImageHeight, 1, 0));
        }
    }
}
