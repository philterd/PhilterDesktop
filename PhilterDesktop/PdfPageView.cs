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
    /// A single-pane PDF page viewer: renders one page at a time, navigates and zooms, and lets the user
    /// drag rectangles to mark regions (raising <see cref="RegionDrawn"/> in PDF page space). Drawn
    /// regions can be overlaid so they stay visible, and removed via a right-click menu. Used where only
    /// one document is shown (drawing policy regions); the side-by-side preview uses its own control.
    /// </summary>
    internal sealed class PdfPageView : UserControl
    {
        private const int RenderDpi = 150;
        private const int MinDragPx = 4;

        private byte[]? _pdf;
        private int _page;
        private int _pageCount;
        private Bitmap? _image;
        private bool _fitMode = true;
        private double _zoom = 1.0;
        private int _renderToken;
        private readonly SemaphoreSlim _renderGate = new(1, 1);

        private bool _drawMode;
        private bool _dragging;
        private Point _dragStart;
        private Point _dragCurrent;

        private readonly List<PdfRegionDrawnEventArgs> _overlays = new();
        private readonly ContextMenuStrip _overlayMenu = new();
        private PdfRegionDrawnEventArgs? _overlayUnderCursor;

        private readonly Panel _scroll = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(60, 60, 60) };
        private readonly PictureBox _picture = new() { SizeMode = PictureBoxSizeMode.StretchImage, Location = Point.Empty };
        private readonly Button _prev = new() { Text = "◀ Previous", AutoSize = true };
        private readonly Button _next = new() { Text = "Next ▶", AutoSize = true };
        private readonly Button _fit = new() { Text = "Fit", AutoSize = true };
        private readonly Button _zoomIn = new() { Text = "+", AutoSize = true };
        private readonly Button _zoomOut = new() { Text = "−", AutoSize = true };
        private readonly Label _pageLabel = new() { AutoSize = true, Margin = new Padding(10, 8, 10, 0) };
        private readonly Label _zoomLabel = new() { AutoSize = true, Margin = new Padding(10, 8, 10, 0) };

        /// <summary>When true, dragging on the page marks a region and raises <see cref="RegionDrawn"/>.</summary>
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool DrawToRedact
        {
            get => _drawMode;
            set
            {
                _drawMode = value;
                _picture.Cursor = value ? Cursors.Cross : Cursors.Default;
            }
        }

        /// <summary>Raised when the user finishes dragging a rectangle on the page.</summary>
        public event EventHandler<PdfRegionDrawnEventArgs>? RegionDrawn;

        /// <summary>Raised when an overlay region is removed via its right-click menu.</summary>
        public event EventHandler<PdfRegionDrawnEventArgs>? OverlayRegionRemoved;

        /// <summary>The regions currently overlaid, in draw order.</summary>
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public IReadOnlyList<PdfRegionDrawnEventArgs> OverlayRegions => _overlays;

        public PdfPageView()
        {
            var nav = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(6, 4, 6, 4) };
            _prev.Click += (_, _) => ShowPage(_page - 1);
            _next.Click += (_, _) => ShowPage(_page + 1);
            _fit.Click += (_, _) => { _fitMode = true; ApplyZoom(); };
            _zoomIn.Click += (_, _) => SetZoom(CurrentScale() * 1.25);
            _zoomOut.Click += (_, _) => SetZoom(CurrentScale() * 0.8);
            nav.Controls.AddRange(new Control[] { _prev, _next, _pageLabel, _fit, _zoomOut, _zoomIn, _zoomLabel });

            _scroll.Controls.Add(_picture);
            _scroll.Resize += (_, _) => { if (_fitMode) { LayoutPicture(); } };
            _picture.MouseDown += Picture_MouseDown;
            _picture.MouseMove += Picture_MouseMove;
            _picture.MouseUp += Picture_MouseUp;
            _picture.Paint += Picture_Paint;

            var removeItem = new ToolStripMenuItem("&Remove Region");
            removeItem.Click += (_, _) =>
            {
                if (_overlayUnderCursor is null)
                {
                    return;
                }
                PdfRegionDrawnEventArgs removed = _overlayUnderCursor;
                _overlays.Remove(removed);
                _overlayUnderCursor = null;
                _picture.Invalidate();
                OverlayRegionRemoved?.Invoke(this, removed);
            };
            _overlayMenu.Items.Add(removeItem);

            Controls.Add(_scroll);
            Controls.Add(nav);
        }

        /// <summary>Loads a PDF and shows its first page.</summary>
        public void SetDocument(byte[] pdf)
        {
            _pdf = pdf;
            try
            {
                _pageCount = pdf is { Length: > 0 } ? Conversion.GetPageCount(pdf) : 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not read the PDF: {ex.Message}", "PDF", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _pageCount = 0;
            }
            ShowPage(0);
        }

        /// <summary>Adds a region overlay so it stays visible, and repaints.</summary>
        public void AddOverlayRegion(PdfRegionDrawnEventArgs region)
        {
            _overlays.Add(region);
            _picture.Invalidate();
        }

        /// <summary>Removes all overlay regions.</summary>
        public void ClearOverlayRegions()
        {
            _overlays.Clear();
            _picture.Invalidate();
        }

        // --- Rendering / navigation -------------------------------------------

        private async void ShowPage(int page)
        {
            if (_pageCount == 0)
            {
                _pageLabel.Text = "No pages";
                SwapImage(null);
                LayoutPicture();
                _prev.Enabled = _next.Enabled = false;
                return;
            }

            _page = Math.Clamp(page, 0, _pageCount - 1);
            _prev.Enabled = _page > 0;
            _next.Enabled = _page < _pageCount - 1;

            SwapImage(null);
            LayoutPicture();
            _pageLabel.Text = $"Rendering page {_page + 1} / {_pageCount}…";

            int token = ++_renderToken;
            byte[]? pdf = _pdf;
            int p = _page;
            Bitmap? rendered = null;
            await _renderGate.WaitAsync();
            try
            {
                if (token != _renderToken)
                {
                    return;
                }
                rendered = await Task.Run(() => Render(pdf, p, _pageCount));
            }
            finally
            {
                _renderGate.Release();
            }

            if (token != _renderToken)
            {
                rendered?.Dispose();
                return;
            }
            SwapImage(rendered);
            _scroll.AutoScrollPosition = Point.Empty;
            _pageLabel.Text = $"Page {_page + 1} / {_pageCount}";
            LayoutPicture();
        }

        private void SetZoom(double zoom)
        {
            _fitMode = false;
            _zoom = Math.Clamp(zoom, 0.1, 8.0);
            ApplyZoom();
        }

        private double CurrentScale() => _fitMode && _image is not null ? FitScale() : _zoom;

        private void ApplyZoom()
        {
            LayoutPicture();
            _zoomLabel.Text = _fitMode ? "Fit" : $"{(int)Math.Round(_zoom * 100)}%";
        }

        private void LayoutPicture()
        {
            if (_image is null)
            {
                _picture.Image = null;
                _picture.Size = Size.Empty;
                return;
            }
            _picture.Image = _image;
            double scale = _fitMode ? FitScale() : _zoom;
            _picture.Location = Point.Empty;
            _picture.Size = new Size(
                Math.Max(1, (int)Math.Round(_image.Width * scale)),
                Math.Max(1, (int)Math.Round(_image.Height * scale)));
        }

        private double FitScale()
        {
            int w = _scroll.ClientSize.Width;
            int h = _scroll.ClientSize.Height;
            if (w <= 0 || h <= 0 || _image is null || _image.Width == 0 || _image.Height == 0)
            {
                return 1.0;
            }
            return Math.Min((double)w / _image.Width, (double)h / _image.Height);
        }

        private void SwapImage(Bitmap? value)
        {
            _image?.Dispose();
            _image = value;
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
                return new Bitmap(loaded);
            }
            catch
            {
                return null;
            }
        }

        // --- Draw / overlay ---------------------------------------------------

        private void Picture_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _overlayUnderCursor = OverlayRegionAt(e.Location);
                if (_overlayUnderCursor is not null)
                {
                    _overlayMenu.Show(_picture, e.Location);
                }
                return;
            }
            if (!_drawMode || e.Button != MouseButtons.Left || _image is null)
            {
                return;
            }
            _dragging = true;
            _dragStart = e.Location;
            _dragCurrent = e.Location;
        }

        private void Picture_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_dragging)
            {
                return;
            }
            _dragCurrent = e.Location;
            _picture.Invalidate();
        }

        private void Picture_MouseUp(object? sender, MouseEventArgs e)
        {
            if (!_dragging)
            {
                return;
            }
            _dragging = false;
            _picture.Invalidate();
            if (_image is null)
            {
                return;
            }

            Rectangle rect = NormalizedRect(_dragStart, _dragCurrent);
            rect.Intersect(new Rectangle(Point.Empty, _picture.ClientSize));
            if (rect.Width < MinDragPx || rect.Height < MinDragPx)
            {
                return;
            }

            PdfRegionDrawnEventArgs? region = PdfSideBySideView.MapPictureRectToPage(
                rect, _picture.ClientSize.Width, _image.Width, _image.Height, _page + 1, RenderDpi);
            if (region is not null)
            {
                RegionDrawn?.Invoke(this, region);
            }
        }

        private void Picture_Paint(object? sender, PaintEventArgs e)
        {
            if (_image is not null && _overlays.Count > 0)
            {
                using var fill = new SolidBrush(Color.FromArgb(60, 200, 0, 0));
                using var pen = new Pen(Color.FromArgb(200, 0, 0), 1);
                foreach (PdfRegionDrawnEventArgs region in _overlays)
                {
                    if (region.PageNumber >= 1 && region.PageNumber != _page + 1)
                    {
                        continue;
                    }
                    Rectangle r = PdfSideBySideView.MapPageRegionToPicture(
                        region, _picture.ClientSize.Width, _image.Width, _image.Height, RenderDpi);
                    e.Graphics.FillRectangle(fill, r);
                    e.Graphics.DrawRectangle(pen, r);
                }
            }

            if (_dragging)
            {
                Rectangle rect = NormalizedRect(_dragStart, _dragCurrent);
                using var fill = new SolidBrush(Color.FromArgb(70, 0, 0, 0));
                using var pen = new Pen(Color.Black, 1);
                e.Graphics.FillRectangle(fill, rect);
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        private PdfRegionDrawnEventArgs? OverlayRegionAt(Point point)
        {
            if (_image is null)
            {
                return null;
            }
            for (int i = _overlays.Count - 1; i >= 0; i--)
            {
                PdfRegionDrawnEventArgs region = _overlays[i];
                if (region.PageNumber >= 1 && region.PageNumber != _page + 1)
                {
                    continue;
                }
                Rectangle r = PdfSideBySideView.MapPageRegionToPicture(
                    region, _picture.ClientSize.Width, _image.Width, _image.Height, RenderDpi);
                if (r.Contains(point))
                {
                    return region;
                }
            }
            return null;
        }

        private static Rectangle NormalizedRect(Point a, Point b) =>
            Rectangle.FromLTRB(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
    }
}
