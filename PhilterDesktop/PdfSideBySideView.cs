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

        // Reviewer "draw a box to redact" mode, active on the before (original) pane only.
        private const int MinDragPx = 4;
        private bool _drawMode;
        private bool _dragging;
        private Point _dragStart;
        private Point _dragCurrent;

        /// <summary>
        /// When true, the reviewer can drag a rectangle on the original (left) page to mark a region to
        /// redact; each completed rectangle raises <see cref="RegionDrawn"/> with page-space coordinates.
        /// </summary>
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool DrawToRedact
        {
            get => _drawMode;
            set
            {
                _drawMode = value;
                _beforePicture.Cursor = value ? Cursors.Cross : Cursors.Default;
                if (!value)
                {
                    _dragging = false;
                    _beforePicture.Invalidate();
                }
            }
        }

        /// <summary>Raised when the reviewer finishes drawing a redaction rectangle on the original page.</summary>
        public event EventHandler<PdfRegionDrawnEventArgs>? RegionDrawn;

        public PdfSideBySideView()
        {
            InitializeComponent();
            _beforeScroll.Scroll += OnPaneScroll;
            _afterScroll.Scroll += OnPaneScroll;
            _beforeScroll.Resize += (_, _) => RelayoutIfFit(_beforeScroll, _beforePicture, _beforeImage);
            _afterScroll.Resize += (_, _) => RelayoutIfFit(_afterScroll, _afterPicture, _afterImage);
            _beforePicture.MouseDown += BeforePicture_MouseDown;
            _beforePicture.MouseMove += BeforePicture_MouseMove;
            _beforePicture.MouseUp += BeforePicture_MouseUp;
            _beforePicture.Paint += BeforePicture_Paint;
        }

        /// <summary>
        /// Replaces only the after (redacted) document and re-renders the current page, preserving the
        /// page the reviewer is on (unlike <see cref="SetDocuments"/>, which resets to the first page).
        /// </summary>
        public void UpdateAfter(byte[]? after)
        {
            _after = after;
            try
            {
                _afterCount = PageCount(after);
            }
            catch
            {
                _afterCount = 0;
            }
            ShowPage(_page);
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

        // --- Draw-to-redact (before pane) ------------------------------------

        private void BeforePicture_MouseDown(object? sender, MouseEventArgs e)
        {
            if (!_drawMode || e.Button != MouseButtons.Left || _beforeImage is null)
            {
                return;
            }
            _dragging = true;
            _dragStart = e.Location;
            _dragCurrent = e.Location;
        }

        private void BeforePicture_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_dragging)
            {
                return;
            }
            _dragCurrent = e.Location;
            _beforePicture.Invalidate();
        }

        private void BeforePicture_MouseUp(object? sender, MouseEventArgs e)
        {
            if (!_dragging)
            {
                return;
            }
            _dragging = false;
            _beforePicture.Invalidate();

            if (_beforeImage is null)
            {
                return;
            }

            Rectangle rect = NormalizedRect(_dragStart, _dragCurrent);
            rect.Intersect(new Rectangle(Point.Empty, _beforePicture.ClientSize));
            if (rect.Width < MinDragPx || rect.Height < MinDragPx)
            {
                return; // ignore stray clicks / tiny drags
            }

            PdfRegionDrawnEventArgs? region = ToPageRegion(rect);
            if (region is not null)
            {
                RegionDrawn?.Invoke(this, region);
            }
        }

        private void BeforePicture_Paint(object? sender, PaintEventArgs e)
        {
            if (!_dragging)
            {
                return;
            }
            Rectangle rect = NormalizedRect(_dragStart, _dragCurrent);
            using var fill = new SolidBrush(Color.FromArgb(70, 0, 0, 0));
            using var pen = new Pen(Color.Black, 1);
            e.Graphics.FillRectangle(fill, rect);
            e.Graphics.DrawRectangle(pen, rect);
        }

        private static Rectangle NormalizedRect(Point a, Point b) =>
            Rectangle.FromLTRB(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));

        // Maps a rectangle in displayed-picture pixels to PDF user-space points (bottom-left origin) on
        // the current page. The before page is rendered at RenderDpi and stretched to the picture box, so
        // points = pixels / displayScale * 72 / RenderDpi, with the Y axis flipped about the page height.
        private PdfRegionDrawnEventArgs? ToPageRegion(Rectangle pictureRect)
        {
            if (_beforeImage is null)
            {
                return null;
            }
            return MapPictureRectToPage(pictureRect, _beforePicture.ClientSize.Width,
                _beforeImage.Width, _beforeImage.Height, _page + 1, RenderDpi);
        }

        /// <summary>
        /// Maps a rectangle in displayed-picture pixels to a redaction region in PDF user-space points
        /// (bottom-left origin). The page is rendered at <paramref name="renderDpi" /> and stretched to
        /// <paramref name="pictureWidth" />, so points = pixels / displayScale * 72 / dpi, with the Y axis
        /// flipped about the page height. Pure (no UI state) so it can be unit-tested.
        /// </summary>
        internal static PdfRegionDrawnEventArgs? MapPictureRectToPage(Rectangle pictureRect,
            int pictureWidth, int imageWidth, int imageHeight, int pageNumber, int renderDpi)
        {
            if (pictureWidth <= 0 || imageWidth <= 0 || renderDpi <= 0)
            {
                return null;
            }
            double displayScale = (double)pictureWidth / imageWidth;
            double ptPerPixel = 72.0 / renderDpi / displayScale;
            double pageHeightPts = imageHeight * 72.0 / renderDpi;

            double lowerLeftX = pictureRect.Left * ptPerPixel;
            double upperRightX = pictureRect.Right * ptPerPixel;
            double upperRightY = pageHeightPts - pictureRect.Top * ptPerPixel;
            double lowerLeftY = pageHeightPts - pictureRect.Bottom * ptPerPixel;

            return new PdfRegionDrawnEventArgs(pageNumber, lowerLeftX, lowerLeftY, upperRightX, upperRightY);
        }

        /// <summary>
        /// Inverse of <see cref="MapPictureRectToPage" />: a page-space region back to displayed-picture
        /// pixels, for painting an overlay of an already-drawn box. Pure so it can be unit-tested.
        /// </summary>
        internal static Rectangle MapPageRegionToPicture(PdfRegionDrawnEventArgs region,
            int pictureWidth, int imageWidth, int imageHeight, int renderDpi)
        {
            if (pictureWidth <= 0 || imageWidth <= 0 || renderDpi <= 0)
            {
                return Rectangle.Empty;
            }
            double displayScale = (double)pictureWidth / imageWidth;
            double pxPerPt = renderDpi * displayScale / 72.0;
            double pageHeightPts = imageHeight * 72.0 / renderDpi;

            int left = (int)Math.Round(region.LowerLeftX * pxPerPt);
            int right = (int)Math.Round(region.UpperRightX * pxPerPt);
            int top = (int)Math.Round((pageHeightPts - region.UpperRightY) * pxPerPt);
            int bottom = (int)Math.Round((pageHeightPts - region.LowerLeftY) * pxPerPt);
            return Rectangle.FromLTRB(left, top, right, bottom);
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

    /// <summary>
    /// A reviewer-drawn redaction rectangle, in PDF user-space points (1/72 inch, bottom-left origin)
    /// on a specific 1-based page. These map directly onto a span's page and bounding-box fields.
    /// </summary>
    public sealed class PdfRegionDrawnEventArgs : EventArgs
    {
        public PdfRegionDrawnEventArgs(int pageNumber, double lowerLeftX, double lowerLeftY,
            double upperRightX, double upperRightY)
        {
            PageNumber = pageNumber;
            LowerLeftX = lowerLeftX;
            LowerLeftY = lowerLeftY;
            UpperRightX = upperRightX;
            UpperRightY = upperRightY;
        }

        public int PageNumber { get; }
        public double LowerLeftX { get; }
        public double LowerLeftY { get; }
        public double UpperRightX { get; }
        public double UpperRightY { get; }
    }
}
