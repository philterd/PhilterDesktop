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
    /// Reusable side-by-side PDF page viewer: renders two PDFs a page at a time with Fit / Actual /
    /// zoom, the two panes scrolling together. Used by both the PDF compare and PDF preview forms.
    /// Call <see cref="SetDocuments"/> to load (or clear) the two documents.
    /// </summary>
    public partial class PdfSideBySideView : UserControl
    {
        private const int RenderDpi = 150;

        private byte[]? _before;
        private byte[]? _after;
        private int _page;
        private int _beforeCount;
        private int _afterCount;

        private Bitmap? _beforeImage;
        private Bitmap? _afterImage;
        private bool _fitMode = true;
        private double _zoom = 1.0;
        private bool _syncing;
        private int _renderToken;
        private readonly SemaphoreSlim _renderGate = new(1, 1);

        public PdfSideBySideView()
        {
            InitializeComponent();
            _beforeScroll.Scroll += OnPaneScroll;
            _afterScroll.Scroll += OnPaneScroll;
            _beforeScroll.Resize += (_, _) => RelayoutIfFit(_beforeScroll, _beforePicture, _beforeImage);
            _afterScroll.Resize += (_, _) => RelayoutIfFit(_afterScroll, _afterPicture, _afterImage);
        }

        /// <summary>Loads two PDFs (any may be empty) and shows the first page; pass titles for each pane.</summary>
        public void SetDocuments(byte[]? before, byte[]? after, string beforeTitle, string afterTitle)
        {
            _before = before;
            _after = after;
            _beforeTitle.Text = beforeTitle;
            _afterTitle.Text = afterTitle;
            try
            {
                _beforeCount = PageCount(before);
                _afterCount = PageCount(after);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not read the PDF(s): {ex.Message}",
                    "PDF", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _beforeCount = _afterCount = 0;
            }
            ShowPage(0);
        }

        // Renders only the current page, lazily and off the UI thread, so large PDFs stay responsive.
        private async void ShowPage(int page)
        {
            int pageCount = Math.Max(_beforeCount, _afterCount);
            if (pageCount == 0)
            {
                _pageLabel.Text = "No pages";
                SwapImage(ref _beforeImage, null);
                SwapImage(ref _afterImage, null);
                ApplyZoom();
                _prev.Enabled = _next.Enabled = false;
                return;
            }

            _page = Math.Clamp(page, 0, pageCount - 1);
            _prev.Enabled = _page > 0;
            _next.Enabled = _page < pageCount - 1;

            // Drop the previous page's bitmaps right away (only the current page is kept in memory)
            // and show a rendering indicator while the new page renders in the background.
            SwapImage(ref _beforeImage, null);
            SwapImage(ref _afterImage, null);
            ApplyZoom();
            _pageLabel.Text = $"Rendering page {_page + 1} / {pageCount}…";

            int token = ++_renderToken;
            byte[]? before = _before;
            byte[]? after = _after;
            int p = _page;
            int bc = _beforeCount;
            int ac = _afterCount;

            Bitmap? renderedBefore = null;
            Bitmap? renderedAfter = null;
            await _renderGate.WaitAsync(); // one PDFium render at a time
            try
            {
                if (token != _renderToken)
                {
                    return; // a newer page was requested while we waited
                }
                (renderedBefore, renderedAfter) = await Task.Run(() => (Render(before, p, bc), Render(after, p, ac)));
            }
            finally
            {
                _renderGate.Release();
            }

            if (token != _renderToken)
            {
                renderedBefore?.Dispose();
                renderedAfter?.Dispose();
                return; // superseded by a later navigation; discard
            }

            SwapImage(ref _beforeImage, renderedBefore);
            SwapImage(ref _afterImage, renderedAfter);
            _beforeScroll.AutoScrollPosition = Point.Empty;
            _afterScroll.AutoScrollPosition = Point.Empty;
            _pageLabel.Text = $"Page {_page + 1} / {pageCount}";
            ApplyZoom();
        }

        private void OnPrev(object? sender, EventArgs e) => ShowPage(_page - 1);

        private void OnNext(object? sender, EventArgs e) => ShowPage(_page + 1);

        // --- Zoom -------------------------------------------------------------

        private void OnFit(object? sender, EventArgs e)
        {
            _fitMode = true;
            ApplyZoom();
        }

        private void OnActualSize(object? sender, EventArgs e) => SetZoom(1.0);

        private void OnZoomIn(object? sender, EventArgs e) => SetZoom(CurrentScale() * 1.25);

        private void OnZoomOut(object? sender, EventArgs e) => SetZoom(CurrentScale() * 0.8);

        private void SetZoom(double zoom)
        {
            _fitMode = false;
            _zoom = Math.Clamp(zoom, 0.1, 8.0);
            ApplyZoom();
        }

        private double CurrentScale() =>
            _fitMode && _beforeImage is not null ? FitScale(_beforeScroll, _beforeImage) : _zoom;

        private void ApplyZoom()
        {
            LayoutSide(_beforeScroll, _beforePicture, _beforeImage);
            LayoutSide(_afterScroll, _afterPicture, _afterImage);
            _zoomLabel.Text = _fitMode ? "Fit" : $"{(int)Math.Round(_zoom * 100)}%";
        }

        private void RelayoutIfFit(Panel scroll, PictureBox box, Image? image)
        {
            if (_fitMode)
            {
                LayoutSide(scroll, box, image);
            }
        }

        private void LayoutSide(Panel scroll, PictureBox box, Image? image)
        {
            if (image is null)
            {
                box.Image = null;
                box.Size = Size.Empty;
                return;
            }
            box.Image = image;
            double scale = _fitMode ? FitScale(scroll, image) : _zoom;
            box.Location = Point.Empty;
            box.Size = new Size(
                Math.Max(1, (int)Math.Round(image.Width * scale)),
                Math.Max(1, (int)Math.Round(image.Height * scale)));
        }

        private static double FitScale(Panel scroll, Image image)
        {
            int w = scroll.ClientSize.Width;
            int h = scroll.ClientSize.Height;
            if (w <= 0 || h <= 0 || image.Width == 0 || image.Height == 0)
            {
                return 1.0;
            }
            return Math.Min((double)w / image.Width, (double)h / image.Height);
        }

        // --- Synchronized scrolling ------------------------------------------

        private void OnPaneScroll(object? sender, ScrollEventArgs e)
        {
            if (sender == _beforeScroll)
            {
                Mirror(_beforeScroll, _afterScroll);
            }
            else
            {
                Mirror(_afterScroll, _beforeScroll);
            }
        }

        private void Mirror(Panel from, Panel to)
        {
            if (_syncing)
            {
                return;
            }
            _syncing = true;
            try
            {
                to.AutoScrollPosition = new Point(-from.AutoScrollPosition.X, -from.AutoScrollPosition.Y);
            }
            finally
            {
                _syncing = false;
            }
        }

        // --- Rendering --------------------------------------------------------

        private static int PageCount(byte[]? pdf) =>
            pdf is { Length: > 0 } ? Conversion.GetPageCount(pdf) : 0;

        private static void SwapImage(ref Bitmap? field, Bitmap? value)
        {
            field?.Dispose();
            field = value;
        }

        private static Bitmap? Render(byte[]? pdf, int page, int count)
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
    }
}
