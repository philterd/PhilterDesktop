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
    partial class PdfCompareForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            _nav = new Panel();
            _prev = new Button();
            _next = new Button();
            _pageLabel = new Label();
            _close = new Button();
            _split = new SplitContainer();
            _beforePicture = new PictureBox();
            _beforeTitle = new Label();
            _afterPicture = new PictureBox();
            _afterTitle = new Label();
            _nav.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_split).BeginInit();
            _split.Panel1.SuspendLayout();
            _split.Panel2.SuspendLayout();
            _split.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_beforePicture).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_afterPicture).BeginInit();
            SuspendLayout();
            //
            // _nav
            //
            _nav.Controls.Add(_prev);
            _nav.Controls.Add(_next);
            _nav.Controls.Add(_pageLabel);
            _nav.Controls.Add(_close);
            _nav.Dock = DockStyle.Top;
            _nav.Location = new Point(0, 0);
            _nav.Name = "_nav";
            _nav.Padding = new Padding(8, 6, 8, 6);
            _nav.Size = new Size(1000, 46);
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
            _pageLabel.Location = new Point(248, 16);
            _pageLabel.Name = "_pageLabel";
            _pageLabel.Size = new Size(50, 15);
            _pageLabel.TabIndex = 2;
            _pageLabel.Text = "Page 1";
            //
            // _close
            //
            _close.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _close.DialogResult = DialogResult.OK;
            _close.Location = new Point(882, 6);
            _close.Name = "_close";
            _close.Size = new Size(110, 34);
            _close.TabIndex = 3;
            _close.Text = "Close";
            _close.UseVisualStyleBackColor = true;
            //
            // _split
            //
            _split.Dock = DockStyle.Fill;
            _split.Location = new Point(0, 46);
            _split.Name = "_split";
            //
            // _split.Panel1
            //
            _split.Panel1.Controls.Add(_beforePicture);
            _split.Panel1.Controls.Add(_beforeTitle);
            //
            // _split.Panel2
            //
            _split.Panel2.Controls.Add(_afterPicture);
            _split.Panel2.Controls.Add(_afterTitle);
            _split.Size = new Size(1000, 608);
            _split.SplitterDistance = 498;
            _split.SplitterWidth = 6;
            _split.TabIndex = 1;
            //
            // _beforePicture
            //
            _beforePicture.BackColor = Color.FromArgb(60, 60, 60);
            _beforePicture.Dock = DockStyle.Fill;
            _beforePicture.Location = new Point(0, 26);
            _beforePicture.Name = "_beforePicture";
            _beforePicture.Size = new Size(498, 582);
            _beforePicture.SizeMode = PictureBoxSizeMode.Zoom;
            _beforePicture.TabIndex = 0;
            _beforePicture.TabStop = false;
            //
            // _beforeTitle
            //
            _beforeTitle.Dock = DockStyle.Top;
            _beforeTitle.Location = new Point(0, 0);
            _beforeTitle.Name = "_beforeTitle";
            _beforeTitle.Size = new Size(498, 26);
            _beforeTitle.TabIndex = 1;
            _beforeTitle.Text = "Before";
            _beforeTitle.TextAlign = ContentAlignment.MiddleCenter;
            //
            // _afterPicture
            //
            _afterPicture.BackColor = Color.FromArgb(60, 60, 60);
            _afterPicture.Dock = DockStyle.Fill;
            _afterPicture.Location = new Point(0, 26);
            _afterPicture.Name = "_afterPicture";
            _afterPicture.Size = new Size(496, 582);
            _afterPicture.SizeMode = PictureBoxSizeMode.Zoom;
            _afterPicture.TabIndex = 0;
            _afterPicture.TabStop = false;
            //
            // _afterTitle
            //
            _afterTitle.Dock = DockStyle.Top;
            _afterTitle.Location = new Point(0, 0);
            _afterTitle.Name = "_afterTitle";
            _afterTitle.Size = new Size(496, 26);
            _afterTitle.TabIndex = 1;
            _afterTitle.Text = "After";
            _afterTitle.TextAlign = ContentAlignment.MiddleCenter;
            //
            // PdfCompareForm
            //
            AcceptButton = _close;
            CancelButton = _close;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1000, 654);
            Controls.Add(_split);
            Controls.Add(_nav);
            MinimizeBox = false;
            MinimumSize = new Size(640, 420);
            Name = "PdfCompareForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Compare PDF";
            _nav.ResumeLayout(false);
            _nav.PerformLayout();
            _split.Panel1.ResumeLayout(false);
            _split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_split).EndInit();
            _split.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_beforePicture).EndInit();
            ((System.ComponentModel.ISupportInitialize)_afterPicture).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel _nav;
        private Button _prev;
        private Button _next;
        private Label _pageLabel;
        private Button _close;
        private SplitContainer _split;
        private PictureBox _beforePicture;
        private Label _beforeTitle;
        private PictureBox _afterPicture;
        private Label _afterTitle;
    }
}
