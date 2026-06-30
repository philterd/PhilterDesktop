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
    partial class UpdateAvailableForm
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
            _message = new Label();
            _linkCaption = new Label();
            _link = new LinkLabel();
            _close = new Button();
            SuspendLayout();
            //
            // _message
            //
            _message.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _message.Location = new Point(12, 14);
            _message.Name = "_message";
            _message.Size = new Size(436, 70);
            _message.TabIndex = 0;
            _message.Text = "A new version of Philter Desktop is available.";
            //
            // _linkCaption
            //
            _linkCaption.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _linkCaption.AutoSize = true;
            _linkCaption.Location = new Point(12, 92);
            _linkCaption.Name = "_linkCaption";
            _linkCaption.Size = new Size(0, 15);
            _linkCaption.TabIndex = 1;
            _linkCaption.Text = "Subscribers can download the new version at:";
            //
            // _link
            //
            _link.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _link.AutoEllipsis = true;
            _link.Location = new Point(12, 112);
            _link.Name = "_link";
            _link.Size = new Size(436, 20);
            _link.TabIndex = 2;
            _link.TabStop = true;
            _link.Text = "https://account.philterd.ai";
            _link.LinkClicked += Link_LinkClicked;
            //
            // _close
            //
            _close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _close.DialogResult = DialogResult.OK;
            _close.Location = new Point(338, 150);
            _close.Name = "_close";
            _close.Size = new Size(110, 34);
            _close.TabIndex = 3;
            _close.Text = "&Close";
            _close.UseVisualStyleBackColor = true;
            //
            // UpdateAvailableForm
            //
            AcceptButton = _close;
            CancelButton = _close;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(460, 196);
            Controls.Add(_close);
            Controls.Add(_link);
            Controls.Add(_linkCaption);
            Controls.Add(_message);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "UpdateAvailableForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Update Available";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label _message;
        private Label _linkCaption;
        private LinkLabel _link;
        private Button _close;
    }
}
