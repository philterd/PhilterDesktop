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
    partial class AboutForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            lblTitle = new Label();
            lblVersion = new Label();
            lblDescription = new Label();
            lblLicense = new Label();
            linkLicense = new LinkLabel();
            linkGitHub = new LinkLabel();
            linkLabelWebsite = new LinkLabel();
            lblReview = new Label();
            lblCopyright = new Label();
            icons8LinkLabel = new LinkLabel();
            btnOK = new Button();
            pictureBox1 = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTitle.Location = new Point(105, 12);
            lblTitle.Margin = new Padding(2, 0, 2, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(149, 25);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Philter Desktop";
            // 
            // lblVersion
            // 
            lblVersion.AutoSize = true;
            lblVersion.Location = new Point(105, 37);
            lblVersion.Margin = new Padding(2, 0, 2, 0);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new Size(63, 15);
            lblVersion.TabIndex = 1;
            lblVersion.Text = "Version 1.0";
            // 
            // lblDescription
            // 
            lblDescription.Location = new Point(105, 59);
            lblDescription.Margin = new Padding(2, 0, 2, 0);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new Size(315, 51);
            lblDescription.TabIndex = 2;
            lblDescription.Text = "Redacts personally identifiable information (PII) from text, Microsoft Word, and PDF documents entirely on your own computer. Powered by the Phileas redaction engine.";
            // 
            // lblLicense
            // 
            lblLicense.Location = new Point(105, 113);
            lblLicense.Margin = new Padding(2, 0, 2, 0);
            lblLicense.Name = "lblLicense";
            lblLicense.Size = new Size(318, 62);
            lblLicense.TabIndex = 3;
            lblLicense.Text = "Philter Desktop is open source under the Apache License 2.0. The official, signed build and support are a per-user subscription from Philterd. 'Philter' is a trademark of Philterd, LLC.";
            // 
            // linkLicense
            // 
            linkLicense.AutoSize = true;
            linkLicense.Location = new Point(105, 186);
            linkLicense.Margin = new Padding(2, 0, 2, 0);
            linkLicense.Name = "linkLicense";
            linkLicense.Size = new Size(155, 15);
            linkLicense.TabIndex = 4;
            linkLicense.TabStop = true;
            linkLicense.Text = "View the Apache License 2.0";
            linkLicense.LinkClicked += linkLicense_LinkClicked;
            // 
            // linkGitHub
            // 
            linkGitHub.AutoSize = true;
            linkGitHub.Location = new Point(264, 186);
            linkGitHub.Margin = new Padding(2, 0, 2, 0);
            linkGitHub.Name = "linkGitHub";
            linkGitHub.Size = new Size(130, 15);
            linkGitHub.TabIndex = 5;
            linkGitHub.TabStop = true;
            linkGitHub.Text = "Source code on GitHub";
            linkGitHub.LinkClicked += linkGitHub_LinkClicked;
            // 
            // linkLabelWebsite
            // 
            linkLabelWebsite.AutoSize = true;
            linkLabelWebsite.Location = new Point(105, 210);
            linkLabelWebsite.Margin = new Padding(2, 0, 2, 0);
            linkLabelWebsite.Name = "linkLabelWebsite";
            linkLabelWebsite.Size = new Size(130, 15);
            linkLabelWebsite.TabIndex = 6;
            linkLabelWebsite.TabStop = true;
            linkLabelWebsite.Text = "https://www.philterd.ai";
            linkLabelWebsite.LinkClicked += linkLabelWebsite_LinkClicked;
            // 
            // lblReview
            // 
            lblReview.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblReview.Location = new Point(105, 242);
            lblReview.Margin = new Padding(2, 0, 2, 0);
            lblReview.Name = "lblReview";
            lblReview.Size = new Size(315, 36);
            lblReview.TabIndex = 7;
            lblReview.Text = "Automated redaction can miss things — always review each redacted document before sharing it.";
            // 
            // lblCopyright
            // 
            lblCopyright.AutoSize = true;
            lblCopyright.Location = new Point(105, 297);
            lblCopyright.Margin = new Padding(2, 0, 2, 0);
            lblCopyright.Name = "lblCopyright";
            lblCopyright.Size = new Size(186, 15);
            lblCopyright.TabIndex = 8;
            lblCopyright.Text = "Copyright 2024-2026 Philterd, LLC";
            // 
            // icons8LinkLabel
            // 
            icons8LinkLabel.AutoSize = true;
            icons8LinkLabel.Location = new Point(105, 320);
            icons8LinkLabel.Margin = new Padding(2, 0, 2, 0);
            icons8LinkLabel.Name = "icons8LinkLabel";
            icons8LinkLabel.Size = new Size(88, 15);
            icons8LinkLabel.TabIndex = 10;
            icons8LinkLabel.TabStop = true;
            icons8LinkLabel.Text = "Icons by Icons8";
            icons8LinkLabel.LinkClicked += icons8LinkLabel_LinkClicked;
            // 
            // btnOK
            // 
            btnOK.Location = new Point(343, 315);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(77, 20);
            btnOK.TabIndex = 9;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.InitialImage = null;
            pictureBox1.Location = new Point(14, 12);
            pictureBox1.Margin = new Padding(2);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(70, 60);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 11;
            pictureBox1.TabStop = false;
            // 
            // AboutForm
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(434, 349);
            Controls.Add(icons8LinkLabel);
            Controls.Add(pictureBox1);
            Controls.Add(btnOK);
            Controls.Add(lblCopyright);
            Controls.Add(lblReview);
            Controls.Add(linkLabelWebsite);
            Controls.Add(linkGitHub);
            Controls.Add(linkLicense);
            Controls.Add(lblLicense);
            Controls.Add(lblDescription);
            Controls.Add(lblVersion);
            Controls.Add(lblTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(2);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AboutForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "About Philter Desktop";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblTitle;
        private Label lblVersion;
        private Label lblDescription;
        private Label lblLicense;
        private LinkLabel linkLicense;
        private LinkLabel linkGitHub;
        private LinkLabel linkLabelWebsite;
        private Label lblReview;
        private Label lblCopyright;
        private LinkLabel icons8LinkLabel;
        private Button btnOK;
        private PictureBox pictureBox1;
    }
}
