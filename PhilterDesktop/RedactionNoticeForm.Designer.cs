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
    partial class RedactionNoticeForm
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
            _root = new TableLayoutPanel();
            _heading = new Label();
            _body = new Label();
            _learnMore = new LinkLabel();
            _buttons = new FlowLayoutPanel();
            _ok = new Button();
            _root.SuspendLayout();
            _buttons.SuspendLayout();
            SuspendLayout();
            //
            // _root
            //
            // Text takes the spare height; the button stays pinned to the bottom.
            _root.ColumnCount = 1;
            _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _root.Dock = DockStyle.Fill;
            _root.Location = new Point(0, 0);
            _root.Name = "_root";
            _root.Padding = new Padding(13, 13, 13, 11);
            _root.RowCount = 4;
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.Controls.Add(_heading, 0, 0);
            _root.Controls.Add(_body, 0, 1);
            _root.Controls.Add(_learnMore, 0, 2);
            _root.Controls.Add(_buttons, 0, 3);
            _root.Size = new Size(540, 288);
            _root.TabIndex = 0;
            //
            // _heading
            //
            _heading.AutoSize = true;
            _heading.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _heading.Margin = new Padding(3, 3, 3, 10);
            _heading.Name = "_heading";
            _heading.Size = new Size(238, 19);
            _heading.TabIndex = 0;
            _heading.Text = "Important — reviewing redactions";
            //
            // _body
            //
            _body.Dock = DockStyle.Fill;
            _body.Name = "_body";
            _body.TabIndex = 1;
            //
            // _learnMore
            //
            _learnMore.AutoSize = true;
            _learnMore.Margin = new Padding(3, 5, 3, 3);
            _learnMore.Name = "_learnMore";
            _learnMore.Size = new Size(204, 15);
            _learnMore.TabIndex = 2;
            _learnMore.TabStop = true;
            _learnMore.Text = "Learn more about redaction accuracy";
            _learnMore.LinkClicked += OnLearnMoreClicked;
            //
            // _buttons
            //
            _buttons.AutoSize = true;
            _buttons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _buttons.Dock = DockStyle.Fill;
            _buttons.FlowDirection = FlowDirection.RightToLeft;
            _buttons.Margin = new Padding(0, 14, 0, 0);
            _buttons.Name = "_buttons";
            _buttons.TabIndex = 3;
            _buttons.WrapContents = false;
            _buttons.Controls.Add(_ok);
            //
            // _ok
            //
            _ok.DialogResult = DialogResult.OK;
            _ok.Name = "_ok";
            _ok.Size = new Size(110, 34);
            _ok.TabIndex = 0;
            _ok.Text = "OK";
            _ok.UseVisualStyleBackColor = true;
            //
            // RedactionNoticeForm
            //
            AcceptButton = _ok;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(540, 288);
            Controls.Add(_root);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(460, 340);
            Name = "RedactionNoticeForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Philter Desktop";
            _buttons.ResumeLayout(false);
            _root.ResumeLayout(false);
            _root.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel _root;
        private Label _heading;
        private Label _body;
        private LinkLabel _learnMore;
        private FlowLayoutPanel _buttons;
        private Button _ok;
    }
}
