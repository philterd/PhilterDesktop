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

using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace PhilterDesktop
{
    /// <summary>
    /// A dashed "Or drag a file here" frame that accepts one dropped file. An alternative to the Browse
    /// dialog, which some machines' shell add-ins crash.
    /// </summary>
    internal sealed class FileDropZone : Control
    {
        public const string DefaultPrompt = "Or drag a file here";

        /// <summary>Which files the zone takes; null accepts any existing file.</summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Func<string, bool>? Accepts { get; set; }

        /// <summary>Raised with the chosen file's full path.</summary>
        public event Action<string>? FileDropped;

        /// <summary>True while an acceptable file is dragged over the zone.</summary>
        internal bool IsDragHover { get; private set; }

        public FileDropZone()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            AllowDrop = true;
            TabStop = false;
            Text = DefaultPrompt;
            AccessibleName = DefaultPrompt;
            AccessibleRole = AccessibleRole.Grouping;
        }

        /// <summary>
        /// The first dropped path that is an existing file the zone accepts, or null. Folders, missing
        /// files and non-file data are ignored.
        /// </summary>
        internal static string? PickFile(IDataObject? data, Func<string, bool>? accepts)
        {
            if (data?.GetDataPresent(DataFormats.FileDrop) != true ||
                data.GetData(DataFormats.FileDrop) is not string[] paths)
            {
                return null;
            }
            return paths.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p) && (accepts?.Invoke(p) ?? true));
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);
            HandleDragEnter(e);
        }

        protected override void OnDragLeave(EventArgs e)
        {
            base.OnDragLeave(e);
            HandleDragLeave();
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            base.OnDragDrop(e);
            HandleDragDrop(e);
        }

        internal void HandleDragEnter(DragEventArgs e)
        {
            bool ok = Enabled && PickFile(e.Data, Accepts) is not null;
            e.Effect = ok ? DragDropEffects.Copy : DragDropEffects.None;
            SetHover(ok);
        }

        internal void HandleDragLeave() => SetHover(false);

        internal void HandleDragDrop(DragEventArgs e)
        {
            SetHover(false);
            if (Enabled && PickFile(e.Data, Accepts) is { } path)
            {
                FileDropped?.Invoke(path);
            }
        }

        private void SetHover(bool hover)
        {
            if (IsDragHover != hover)
            {
                IsDragHover = hover;
                Invalidate();
            }
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Parent?.BackColor ?? ModernTheme.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float scale = DeviceDpi / 96f;
            float inset = Math.Max(1f, scale);
            var frame = new RectangleF(inset, inset, Width - 2 * inset - 1, Height - 2 * inset - 1);
            if (frame.Width <= 0 || frame.Height <= 0)
            {
                return;
            }

            using GraphicsPath shape = RoundedRect(frame, 6 * scale);
            using (var fill = new SolidBrush(IsDragHover ? ModernTheme.SelectionBack : ModernTheme.Surface))
            {
                g.FillPath(fill, shape);
            }
            using (var pen = new Pen(IsDragHover ? ModernTheme.Accent : ModernTheme.SubtleText, Math.Max(1f, 1.5f * scale)))
            {
                if (!IsDragHover)
                {
                    pen.DashStyle = DashStyle.Dash;
                }
                g.DrawPath(pen, shape);
            }

            TextRenderer.DrawText(g, Text, Font, ClientRectangle,
                IsDragHover ? ModernTheme.Text : ModernTheme.SubtleText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis);
        }

        private static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            float d = Math.Min(2 * radius, Math.Min(r.Width, r.Height));
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
