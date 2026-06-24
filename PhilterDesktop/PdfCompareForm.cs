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

namespace PhilterDesktop
{
    /// <summary>
    /// Side-by-side visual comparison of two PDFs (original vs. redacted), rendered a page at a time.
    /// Both panes show the same page; navigate with Previous/Next. Because redacted PDFs are
    /// image-based, this is a visual comparison rather than a text/pixel diff.
    /// </summary>
    public partial class PdfCompareForm : Form
    {
        private const int RenderDpi = 150;

        private readonly byte[] _before;
        private readonly byte[] _after;
        private int _page;
        private int _beforeCount;
        private int _afterCount;

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public PdfCompareForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
        }

        public PdfCompareForm(byte[] beforePdf, byte[] afterPdf, string beforeName, string afterName)
            : this()
        {
            _before = beforePdf;
            _after = afterPdf;
            _beforeTitle.Text = $"Before — {beforeName}";
            _afterTitle.Text = $"After — {afterName}";
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            try
            {
                _beforeCount = PageCount(_before);
                _afterCount = PageCount(_after);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not read the PDF(s): {ex.Message}",
                    "Compare PDF", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            ShowPage(0);
        }

        private void ShowPage(int page)
        {
            int pageCount = Math.Max(_beforeCount, _afterCount);
            if (pageCount == 0)
            {
                _pageLabel.Text = "No pages";
                SetImage(_beforePicture, null);
                SetImage(_afterPicture, null);
                _prev.Enabled = _next.Enabled = false;
                return;
            }

            _page = Math.Clamp(page, 0, pageCount - 1);
            SetImage(_beforePicture, Render(_before, _page, _beforeCount));
            SetImage(_afterPicture, Render(_after, _page, _afterCount));
            _pageLabel.Text = $"Page {_page + 1} / {pageCount}";
            _prev.Enabled = _page > 0;
            _next.Enabled = _page < pageCount - 1;
        }

        private static int PageCount(byte[]? pdf) =>
            pdf is { Length: > 0 } ? Conversion.GetPageCount(pdf) : 0;

        private static void SetImage(PictureBox box, Image? image)
        {
            box.Image?.Dispose();
            box.Image = image;
        }

        private static Image? Render(byte[]? pdf, int page, int count)
        {
            if (pdf is null || pdf.Length == 0 || page >= count)
            {
                return null;
            }
            try
            {
                using SKBitmap bitmap = Conversion.ToImage(pdf, page: page, options: new RenderOptions(Dpi: RenderDpi));
                using SKImage image = SKImage.FromBitmap(bitmap);
                using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = new MemoryStream(data.ToArray());
                using var loaded = new Bitmap(stream);
                return new Bitmap(loaded); // independent copy, safe after the stream is disposed
            }
            catch
            {
                return null;
            }
        }

        private void OnPrev(object? sender, EventArgs e) => ShowPage(_page - 1);

        private void OnNext(object? sender, EventArgs e) => ShowPage(_page + 1);
    }
}
