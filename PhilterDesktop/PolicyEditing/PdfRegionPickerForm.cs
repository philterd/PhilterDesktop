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

using Phileas.Policy;

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// Draws a policy's fixed PDF redaction regions by dragging rectangles on a sample PDF, instead of
    /// typing point coordinates. Uses the single-pane <see cref="PdfPageView"/>, whose drawn rectangles
    /// arrive in PDF page space, and turns each into a <see cref="BoundingBox"/>.
    /// </summary>
    internal sealed class PdfRegionPickerForm : Form
    {
        private readonly byte[] _pdf;
        private readonly PdfPageView _view = new() { Dock = DockStyle.Fill };
        private readonly Label _status = new() { Dock = DockStyle.Bottom, Height = 24, Padding = new Padding(6, 4, 6, 0) };

        /// <summary>The regions drawn during this session (valid on <see cref="DialogResult.OK"/>).</summary>
        public List<BoundingBox> Boxes { get; private set; } = new();

        public PdfRegionPickerForm(byte[] pdfBytes)
        {
            _pdf = pdfBytes;
            Text = "Draw PDF Regions";
            StartPosition = FormStartPosition.CenterParent;
            Size = new System.Drawing.Size(1000, 720);

            // The view holds the drawn regions (so a right-click "Remove Region" stays in sync); the box
            // list is computed from them when the user is done.
            _view.RegionDrawn += (_, e) =>
            {
                _view.AddOverlayRegion(e); // keep the drawn rectangle visible on the page
                UpdateStatus();
            };
            _view.OverlayRegionRemoved += (_, _) => UpdateStatus();

            var done = new Button { Text = "&Done", DialogResult = DialogResult.OK, AutoSize = true };
            done.Click += (_, _) => Boxes = _view.OverlayRegions.Select(ToBox).ToList();
            var cancel = new Button { Text = "&Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 40, Padding = new Padding(6) };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(done);

            Controls.Add(_view);
            Controls.Add(_status);
            Controls.Add(buttons);
            AcceptButton = done;
            CancelButton = cancel;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _view.DrawToRedact = true;
            _view.SetDocument(_pdf);
            UpdateStatus();
        }

        private void UpdateStatus() =>
            _status.Text = $"Drag to draw a region; right-click a region to remove it (use the page arrows for other pages). " +
                           $"{_view.OverlayRegions.Count} drawn.";

        /// <summary>Converts a drawn page-space region into a bounding box (X/Y = lower-left, plus width/height).</summary>
        internal static BoundingBox ToBox(PdfRegionDrawnEventArgs e) => new()
        {
            Page = e.PageNumber,
            X = (float)e.LowerLeftX,
            Y = (float)e.LowerLeftY,
            W = (float)(e.UpperRightX - e.LowerLeftX),
            H = (float)(e.UpperRightY - e.LowerLeftY),
            Color = DefaultRegionColor,
            Enabled = true
        };

        /// <summary>The default fill for a region when none is specified.</summary>
        internal const string DefaultRegionColor = "Black";
    }
}
