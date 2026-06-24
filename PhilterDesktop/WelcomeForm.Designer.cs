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
    partial class WelcomeForm
    {
        /// <summary>Required designer variable.</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>Clean up any resources being used.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            _heading = new Label();
            _body = new TextBox();
            _eulaLink = new LinkLabel();
            _doNotShowAgain = new CheckBox();
            _agree = new Button();
            _disagree = new Button();
            SuspendLayout();
            //
            // _heading
            //
            _heading.AutoSize = true;
            _heading.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
            _heading.Location = new Point(14, 14);
            _heading.Name = "_heading";
            _heading.Size = new Size(290, 25);
            _heading.TabIndex = 0;
            _heading.Text = "Welcome to Philter Desktop";
            //
            // _body
            //
            _body.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _body.Location = new Point(14, 50);
            _body.Multiline = true;
            _body.Name = "_body";
            _body.ReadOnly = true;
            _body.ScrollBars = ScrollBars.Vertical;
            _body.Size = new Size(552, 348);
            _body.TabIndex = 1;
            _body.TabStop = false;
            //
            // _eulaLink
            //
            _eulaLink.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _eulaLink.AutoSize = true;
            _eulaLink.Location = new Point(14, 410);
            _eulaLink.Name = "_eulaLink";
            _eulaLink.Size = new Size(280, 15);
            _eulaLink.TabIndex = 2;
            _eulaLink.TabStop = true;
            _eulaLink.Text = "View the Philterd Commercial License Agreement";
            _eulaLink.LinkClicked += OnEulaLinkClicked;
            //
            // _doNotShowAgain
            //
            _doNotShowAgain.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _doNotShowAgain.AutoSize = true;
            _doNotShowAgain.Checked = true;
            _doNotShowAgain.CheckState = CheckState.Checked;
            _doNotShowAgain.Location = new Point(14, 444);
            _doNotShowAgain.Name = "_doNotShowAgain";
            _doNotShowAgain.Size = new Size(146, 19);
            _doNotShowAgain.TabIndex = 3;
            _doNotShowAgain.Text = "Don't show this again";
            _doNotShowAgain.UseVisualStyleBackColor = true;
            //
            // _agree
            //
            _agree.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _agree.DialogResult = DialogResult.OK;
            _agree.Location = new Point(340, 440);
            _agree.Name = "_agree";
            _agree.Size = new Size(110, 34);
            _agree.TabIndex = 4;
            _agree.Text = "I Agree";
            _agree.UseVisualStyleBackColor = true;
            //
            // _disagree
            //
            _disagree.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _disagree.DialogResult = DialogResult.Cancel;
            _disagree.Location = new Point(456, 440);
            _disagree.Name = "_disagree";
            _disagree.Size = new Size(110, 34);
            _disagree.TabIndex = 5;
            _disagree.Text = "I Disagree";
            _disagree.UseVisualStyleBackColor = true;
            //
            // WelcomeForm
            //
            AcceptButton = _agree;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = _disagree;
            ClientSize = new Size(580, 488);
            Controls.Add(_heading);
            Controls.Add(_body);
            Controls.Add(_eulaLink);
            Controls.Add(_doNotShowAgain);
            Controls.Add(_agree);
            Controls.Add(_disagree);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(520, 420);
            Name = "WelcomeForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Philter Desktop";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label _heading;
        private TextBox _body;
        private LinkLabel _eulaLink;
        private CheckBox _doNotShowAgain;
        private Button _agree;
        private Button _disagree;
    }
}
