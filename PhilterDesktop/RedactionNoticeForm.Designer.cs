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
            _heading = new Label();
            _body = new Label();
            _learnMore = new LinkLabel();
            _ok = new Button();
            SuspendLayout();
            // 
            // _heading
            // 
            _heading.AutoSize = true;
            _heading.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _heading.Location = new Point(16, 16);
            _heading.Name = "_heading";
            _heading.Size = new Size(238, 19);
            _heading.TabIndex = 0;
            _heading.Text = "Important — reviewing redactions";
            // 
            // _body
            // 
            _body.Location = new Point(16, 48);
            _body.Name = "_body";
            _body.Size = new Size(508, 148);
            _body.TabIndex = 1;
            // 
            // _learnMore
            // 
            _learnMore.AutoSize = true;
            _learnMore.Location = new Point(16, 204);
            _learnMore.Name = "_learnMore";
            _learnMore.Size = new Size(204, 15);
            _learnMore.TabIndex = 2;
            _learnMore.TabStop = true;
            _learnMore.Text = "Learn more about redaction accuracy";
            _learnMore.LinkClicked += OnLearnMoreClicked;
            // 
            // _ok
            // 
            _ok.DialogResult = DialogResult.OK;
            _ok.Location = new Point(414, 240);
            _ok.Name = "_ok";
            _ok.Size = new Size(110, 34);
            _ok.TabIndex = 3;
            _ok.Text = "OK";
            _ok.UseVisualStyleBackColor = true;
            // 
            // RedactionNoticeForm
            // 
            AcceptButton = _ok;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(540, 288);
            Controls.Add(_heading);
            Controls.Add(_body);
            Controls.Add(_learnMore);
            Controls.Add(_ok);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "RedactionNoticeForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Philter Desktop";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label _heading;
        private Label _body;
        private LinkLabel _learnMore;
        private Button _ok;
    }
}
