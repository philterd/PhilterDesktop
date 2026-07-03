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

using PDFtoImage;
using SkiaSharp;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Confirms the PDF rendering path used by the PDF compare viewer works end-to-end against a real
    /// sample (PDFium native + SkiaSharp).
    /// </summary>
    public sealed class PdfRenderingTests
    {
        [Fact]
        public void PdfToImage_RendersSamplePdf()
        {
            string input = Path.Combine(AppContext.BaseDirectory, "test-documents", "test1.pdf");
            Assert.True(File.Exists(input), $"Sample not found: {input}");

            byte[] bytes = File.ReadAllBytes(input);
            Assert.True(Conversion.GetPageCount(bytes) >= 1);

            using SKBitmap bitmap = Conversion.ToImage(bytes, page: 0, options: new RenderOptions(Dpi: 96));
            Assert.True(bitmap.Width > 0 && bitmap.Height > 0);
        }
    }
}
