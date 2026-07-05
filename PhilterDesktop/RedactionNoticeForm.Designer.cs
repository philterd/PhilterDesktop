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
            _agree = new Button();
            _disagree = new Button();
            SuspendLayout();
            //
            // _heading
            //
            _heading.AutoSize = true;
            _heading.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _heading.Location = new Point(16, 16);
            _heading.Name = "_heading";
            _heading.Size = new Size(230, 19);
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
            _learnMore.Size = new Size(230, 15);
            _learnMore.TabIndex = 2;
            _learnMore.TabStop = true;
            _learnMore.Text = "Learn more about redaction accuracy";
            _learnMore.LinkClicked += OnLearnMoreClicked;
            //
            // _agree
            //
            _agree.DialogResult = DialogResult.OK;
            _agree.Location = new Point(298, 240);
            _agree.Name = "_agree";
            _agree.Size = new Size(110, 34);
            _agree.TabIndex = 3;
            _agree.Text = "I &Agree";
            _agree.UseVisualStyleBackColor = true;
            //
            // _disagree
            //
            _disagree.DialogResult = DialogResult.Cancel;
            _disagree.Location = new Point(414, 240);
            _disagree.Name = "_disagree";
            _disagree.Size = new Size(110, 34);
            _disagree.TabIndex = 4;
            _disagree.Text = "I &Disagree";
            _disagree.UseVisualStyleBackColor = true;
            //
            // RedactionNoticeForm
            //
            AcceptButton = _agree;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = _disagree;
            ClientSize = new Size(540, 288);
            Controls.Add(_heading);
            Controls.Add(_body);
            Controls.Add(_learnMore);
            Controls.Add(_agree);
            Controls.Add(_disagree);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "RedactionNoticeForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Philter Desktop";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label _heading;
        private Label _body;
        private LinkLabel _learnMore;
        private Button _agree;
        private Button _disagree;
    }
}
