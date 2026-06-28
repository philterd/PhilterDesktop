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
            linkEula = new LinkLabel();
            lblReview = new Label();
            lblCopyright = new Label();
            lblAlsoFrom = new Label();
            linkPhilter = new LinkLabel();
            linkScope = new LinkLabel();
            linkDiffuse = new LinkLabel();
            linkConsulting = new LinkLabel();
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
            // linkEula
            // 
            linkEula.AutoSize = true;
            linkEula.Location = new Point(264, 210);
            linkEula.Margin = new Padding(2, 0, 2, 0);
            linkEula.Name = "linkEula";
            linkEula.Size = new Size(83, 15);
            linkEula.TabIndex = 7;
            linkEula.TabStop = true;
            linkEula.Text = "View the EULA";
            linkEula.LinkClicked += linkEula_LinkClicked;
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
            lblCopyright.Location = new Point(105, 390);
            lblCopyright.Margin = new Padding(2, 0, 2, 0);
            lblCopyright.Name = "lblCopyright";
            lblCopyright.Size = new Size(186, 15);
            lblCopyright.TabIndex = 8;
            lblCopyright.Text = "Copyright 2024-2026 Philterd, LLC";
            // 
            // lblAlsoFrom
            // 
            lblAlsoFrom.AutoSize = true;
            lblAlsoFrom.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblAlsoFrom.Location = new Point(105, 286);
            lblAlsoFrom.Name = "lblAlsoFrom";
            lblAlsoFrom.Size = new Size(111, 15);
            lblAlsoFrom.TabIndex = 11;
            lblAlsoFrom.Text = "Also from Philterd:";
            // 
            // linkPhilter
            // 
            linkPhilter.AutoSize = true;
            linkPhilter.Location = new Point(105, 305);
            linkPhilter.Name = "linkPhilter";
            linkPhilter.Size = new Size(290, 15);
            linkPhilter.TabIndex = 12;
            linkPhilter.TabStop = true;
            linkPhilter.Text = "Philter — PII redaction for servers, APIs & data pipelines";
            linkPhilter.LinkClicked += linkPhilter_LinkClicked;
            // 
            // linkScope
            // 
            linkScope.AutoSize = true;
            linkScope.Location = new Point(105, 324);
            linkScope.Name = "linkScope";
            linkScope.Size = new Size(284, 15);
            linkScope.TabIndex = 13;
            linkScope.TabStop = true;
            linkScope.Text = "Philter Scope — measure how well a policy performs";
            linkScope.LinkClicked += linkScope_LinkClicked;
            // 
            // linkDiffuse
            // 
            linkDiffuse.AutoSize = true;
            linkDiffuse.Location = new Point(105, 343);
            linkDiffuse.Name = "linkDiffuse";
            linkDiffuse.Size = new Size(331, 15);
            linkDiffuse.TabIndex = 14;
            linkDiffuse.TabStop = true;
            linkDiffuse.Text = "Philter Diffuse — add differential privacy to data and statistics";
            linkDiffuse.LinkClicked += linkDiffuse_LinkClicked;
            // 
            // linkConsulting
            // 
            linkConsulting.AutoSize = true;
            linkConsulting.Location = new Point(105, 362);
            linkConsulting.Name = "linkConsulting";
            linkConsulting.Size = new Size(287, 15);
            linkConsulting.TabIndex = 15;
            linkConsulting.TabStop = true;
            linkConsulting.Text = "Policy consulting — help building & validating policies";
            linkConsulting.LinkClicked += linkConsulting_LinkClicked;
            // 
            // btnOK
            // 
            btnOK.Location = new Point(343, 408);
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
            ClientSize = new Size(452, 443);
            Controls.Add(lblAlsoFrom);
            Controls.Add(linkPhilter);
            Controls.Add(linkScope);
            Controls.Add(linkDiffuse);
            Controls.Add(linkConsulting);
            Controls.Add(pictureBox1);
            Controls.Add(btnOK);
            Controls.Add(lblCopyright);
            Controls.Add(lblReview);
            Controls.Add(linkLabelWebsite);
            Controls.Add(linkEula);
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
        private LinkLabel linkEula;
        private Label lblReview;
        private Label lblCopyright;
        private Label lblAlsoFrom;
        private LinkLabel linkPhilter;
        private LinkLabel linkScope;
        private LinkLabel linkDiffuse;
        private LinkLabel linkConsulting;
        private Button btnOK;
        private PictureBox pictureBox1;
    }
}
