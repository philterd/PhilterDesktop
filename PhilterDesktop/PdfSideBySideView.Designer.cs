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

namespace PhilterDesktop
{
    partial class PdfSideBySideView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _beforePicture.Image = null;
                _afterPicture.Image = null;
                _beforeImage?.Dispose();
                _afterImage?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            _nav = new Panel();
            _prev = new Button();
            _next = new Button();
            _pageLabel = new Label();
            _zoomLabel = new Label();
            _fit = new Button();
            _actual = new Button();
            _zoomOut = new Button();
            _zoomIn = new Button();
            _split = new SplitContainer();
            _beforeScroll = new Panel();
            _beforePicture = new PictureBox();
            _beforeTitle = new Label();
            _afterScroll = new Panel();
            _afterPicture = new PictureBox();
            _afterTitle = new Label();
            _nav.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_split).BeginInit();
            _split.Panel1.SuspendLayout();
            _split.Panel2.SuspendLayout();
            _split.SuspendLayout();
            _beforeScroll.SuspendLayout();
            _afterScroll.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_beforePicture).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_afterPicture).BeginInit();
            SuspendLayout();
            //
            // _nav
            //
            _nav.Controls.Add(_prev);
            _nav.Controls.Add(_next);
            _nav.Controls.Add(_pageLabel);
            _nav.Controls.Add(_zoomLabel);
            _nav.Controls.Add(_fit);
            _nav.Controls.Add(_actual);
            _nav.Controls.Add(_zoomOut);
            _nav.Controls.Add(_zoomIn);
            _nav.Dock = DockStyle.Top;
            _nav.Location = new Point(0, 0);
            _nav.Name = "_nav";
            _nav.Size = new Size(900, 46);
            _nav.TabIndex = 0;
            //
            // _prev
            //
            _prev.Location = new Point(8, 6);
            _prev.Name = "_prev";
            _prev.Size = new Size(110, 34);
            _prev.TabIndex = 0;
            _prev.Text = "◀ Previous";
            _prev.UseVisualStyleBackColor = true;
            _prev.Click += OnPrev;
            //
            // _next
            //
            _next.Location = new Point(124, 6);
            _next.Name = "_next";
            _next.Size = new Size(110, 34);
            _next.TabIndex = 1;
            _next.Text = "Next ▶";
            _next.UseVisualStyleBackColor = true;
            _next.Click += OnNext;
            //
            // _pageLabel
            //
            _pageLabel.AutoSize = true;
            _pageLabel.Location = new Point(244, 16);
            _pageLabel.Name = "_pageLabel";
            _pageLabel.Size = new Size(50, 15);
            _pageLabel.TabIndex = 2;
            _pageLabel.Text = "Page 1";
            //
            // _zoomLabel
            //
            _zoomLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _zoomLabel.AutoSize = true;
            _zoomLabel.Location = new Point(478, 16);
            _zoomLabel.Name = "_zoomLabel";
            _zoomLabel.Size = new Size(24, 15);
            _zoomLabel.TabIndex = 3;
            _zoomLabel.Text = "Fit";
            //
            // _fit
            //
            _fit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _fit.Location = new Point(540, 6);
            _fit.Name = "_fit";
            _fit.Size = new Size(60, 34);
            _fit.TabIndex = 4;
            _fit.Text = "Fit";
            _fit.UseVisualStyleBackColor = true;
            _fit.Click += OnFit;
            //
            // _actual
            //
            _actual.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _actual.Location = new Point(606, 6);
            _actual.Name = "_actual";
            _actual.Size = new Size(74, 34);
            _actual.TabIndex = 5;
            _actual.Text = "100%";
            _actual.UseVisualStyleBackColor = true;
            _actual.Click += OnActualSize;
            //
            // _zoomOut
            //
            _zoomOut.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _zoomOut.Location = new Point(686, 6);
            _zoomOut.Name = "_zoomOut";
            _zoomOut.Size = new Size(40, 34);
            _zoomOut.TabIndex = 6;
            _zoomOut.Text = "−";
            _zoomOut.UseVisualStyleBackColor = true;
            _zoomOut.Click += OnZoomOut;
            //
            // _zoomIn
            //
            _zoomIn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _zoomIn.Location = new Point(732, 6);
            _zoomIn.Name = "_zoomIn";
            _zoomIn.Size = new Size(40, 34);
            _zoomIn.TabIndex = 7;
            _zoomIn.Text = "+";
            _zoomIn.UseVisualStyleBackColor = true;
            _zoomIn.Click += OnZoomIn;
            //
            // _split
            //
            _split.Dock = DockStyle.Fill;
            _split.Location = new Point(0, 46);
            _split.Name = "_split";
            _split.Panel1.Controls.Add(_beforeScroll);
            _split.Panel1.Controls.Add(_beforeTitle);
            _split.Panel2.Controls.Add(_afterScroll);
            _split.Panel2.Controls.Add(_afterTitle);
            _split.Size = new Size(900, 554);
            _split.SplitterDistance = 448;
            _split.SplitterWidth = 6;
            _split.TabIndex = 1;
            //
            // _beforeScroll
            //
            _beforeScroll.AutoScroll = true;
            _beforeScroll.BackColor = Color.FromArgb(60, 60, 60);
            _beforeScroll.Controls.Add(_beforePicture);
            _beforeScroll.Dock = DockStyle.Fill;
            _beforeScroll.Location = new Point(0, 26);
            _beforeScroll.Name = "_beforeScroll";
            _beforeScroll.Size = new Size(448, 528);
            _beforeScroll.TabIndex = 0;
            //
            // _beforePicture
            //
            _beforePicture.Location = new Point(0, 0);
            _beforePicture.Name = "_beforePicture";
            _beforePicture.Size = new Size(100, 50);
            _beforePicture.SizeMode = PictureBoxSizeMode.StretchImage;
            _beforePicture.TabIndex = 0;
            _beforePicture.TabStop = false;
            //
            // _beforeTitle
            //
            _beforeTitle.Dock = DockStyle.Top;
            _beforeTitle.Location = new Point(0, 0);
            _beforeTitle.Name = "_beforeTitle";
            _beforeTitle.Size = new Size(448, 26);
            _beforeTitle.TabIndex = 1;
            _beforeTitle.Text = "Before";
            _beforeTitle.TextAlign = ContentAlignment.MiddleCenter;
            //
            // _afterScroll
            //
            _afterScroll.AutoScroll = true;
            _afterScroll.BackColor = Color.FromArgb(60, 60, 60);
            _afterScroll.Controls.Add(_afterPicture);
            _afterScroll.Dock = DockStyle.Fill;
            _afterScroll.Location = new Point(0, 26);
            _afterScroll.Name = "_afterScroll";
            _afterScroll.Size = new Size(446, 528);
            _afterScroll.TabIndex = 0;
            //
            // _afterPicture
            //
            _afterPicture.Location = new Point(0, 0);
            _afterPicture.Name = "_afterPicture";
            _afterPicture.Size = new Size(100, 50);
            _afterPicture.SizeMode = PictureBoxSizeMode.StretchImage;
            _afterPicture.TabIndex = 0;
            _afterPicture.TabStop = false;
            //
            // _afterTitle
            //
            _afterTitle.Dock = DockStyle.Top;
            _afterTitle.Location = new Point(0, 0);
            _afterTitle.Name = "_afterTitle";
            _afterTitle.Size = new Size(446, 26);
            _afterTitle.TabIndex = 1;
            _afterTitle.Text = "After";
            _afterTitle.TextAlign = ContentAlignment.MiddleCenter;
            //
            // PdfSideBySideView
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(_split);
            Controls.Add(_nav);
            Name = "PdfSideBySideView";
            Size = new Size(900, 600);
            _nav.ResumeLayout(false);
            _nav.PerformLayout();
            _split.Panel1.ResumeLayout(false);
            _split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_split).EndInit();
            _split.ResumeLayout(false);
            _beforeScroll.ResumeLayout(false);
            _afterScroll.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_beforePicture).EndInit();
            ((System.ComponentModel.ISupportInitialize)_afterPicture).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel _nav;
        private Button _prev;
        private Button _next;
        private Label _pageLabel;
        private Button _fit;
        private Button _actual;
        private Button _zoomOut;
        private Button _zoomIn;
        private Label _zoomLabel;
        private SplitContainer _split;
        private Panel _beforeScroll;
        private PictureBox _beforePicture;
        private Label _beforeTitle;
        private Panel _afterScroll;
        private PictureBox _afterPicture;
        private Label _afterTitle;
    }
}
