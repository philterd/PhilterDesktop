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
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            lblTitle = new Label();
            lblVersion = new Label();
            lblDescription = new Label();
            lblCopyright = new Label();
            linkLabelWebsite = new LinkLabel();
            btnOK = new Button();
            pictureBox1 = new PictureBox();
            icons8LinkLabel = new LinkLabel();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTitle.Location = new Point(150, 20);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(221, 38);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Philter Desktop";
            // 
            // lblVersion
            // 
            lblVersion.AutoSize = true;
            lblVersion.Location = new Point(150, 65);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new Size(99, 25);
            lblVersion.TabIndex = 1;
            lblVersion.Text = "Version 1.0";
            // 
            // lblDescription
            // 
            lblDescription.Location = new Point(150, 100);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new Size(450, 60);
            lblDescription.TabIndex = 2;
            lblDescription.Text = "Philter Desktop is a Windows application for redacting sensitive information from documents.";
            // 
            // lblCopyright
            // 
            lblCopyright.AutoSize = true;
            lblCopyright.Location = new Point(150, 170);
            lblCopyright.Name = "lblCopyright";
            lblCopyright.Size = new Size(220, 25);
            lblCopyright.TabIndex = 3;
            lblCopyright.Text = "� 2024-2026 Philterd, LLC";
            // 
            // linkLabelWebsite
            // 
            linkLabelWebsite.AutoSize = true;
            linkLabelWebsite.Location = new Point(150, 210);
            linkLabelWebsite.Name = "linkLabelWebsite";
            linkLabelWebsite.Size = new Size(192, 25);
            linkLabelWebsite.TabIndex = 4;
            linkLabelWebsite.TabStop = true;
            linkLabelWebsite.Text = "https://www.philterd.ai";
            linkLabelWebsite.LinkClicked += linkLabelWebsite_LinkClicked;
            // 
            // btnOK
            // 
            btnOK.Location = new Point(478, 286);
            btnOK.Margin = new Padding(4, 5, 4, 5);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(110, 34);
            btnOK.TabIndex = 6;
            btnOK.Text = "OK";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.InitialImage = null;
            pictureBox1.Location = new Point(20, 20);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(100, 100);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 7;
            pictureBox1.TabStop = false;
            // 
            // icons8LinkLabel
            // 
            icons8LinkLabel.AutoSize = true;
            icons8LinkLabel.Location = new Point(20, 297);
            icons8LinkLabel.Name = "icons8LinkLabel";
            icons8LinkLabel.Size = new Size(136, 25);
            icons8LinkLabel.TabIndex = 8;
            icons8LinkLabel.TabStop = true;
            icons8LinkLabel.Text = "Icons by Icons8";
            icons8LinkLabel.LinkClicked += icons8LinkLabel_LinkClicked;
            // 
            // AboutForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(620, 350);
            Controls.Add(icons8LinkLabel);
            Controls.Add(pictureBox1);
            Controls.Add(btnOK);
            Controls.Add(linkLabelWebsite);
            Controls.Add(lblCopyright);
            Controls.Add(lblDescription);
            Controls.Add(lblVersion);
            Controls.Add(lblTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
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
        private Label lblCopyright;
        private LinkLabel linkLabelWebsite;
        private Button btnOK;
        private PictureBox pictureBox1;
        private LinkLabel icons8LinkLabel;
    }
}