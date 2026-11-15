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
    partial class LicenseForm
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
        /// Layout: a header band (icon, title, subtitle), the license text in framed reading panes, and a
        /// footer band (the Apache note and the buttons), separated by hairlines.
        /// </summary>
        private void InitializeComponent()
        {
            _root = new TableLayoutPanel();
            _header = new TableLayoutPanel();
            _headerIcon = new PictureBox();
            _title = new Label();
            _subtitle = new Label();
            _headerDivider = new Panel();
            _body = new TableLayoutPanel();
            _licenseHeading = new Label();
            _licenseFrame = new Panel();
            _licenseInner = new Panel();
            _licenseBody = new TextBox();
            _eulaHeading = new Label();
            _eulaFrame = new Panel();
            _eulaInner = new Panel();
            _eulaBody = new TextBox();
            _footerDivider = new Panel();
            _footer = new TableLayoutPanel();
            _apacheLink = new LinkLabel();
            _buttons = new FlowLayoutPanel();
            _agree = new Button();
            _disagree = new Button();
            _root.SuspendLayout();
            _header.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_headerIcon).BeginInit();
            _body.SuspendLayout();
            _licenseFrame.SuspendLayout();
            _licenseInner.SuspendLayout();
            _eulaFrame.SuspendLayout();
            _eulaInner.SuspendLayout();
            _footer.SuspendLayout();
            _buttons.SuspendLayout();
            SuspendLayout();
            //
            // _root
            //
            _root.ColumnCount = 1;
            _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _root.Dock = DockStyle.Fill;
            _root.Margin = new Padding(0);
            _root.Name = "_root";
            _root.RowCount = 5;
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 1F));
            _root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _root.RowStyles.Add(new RowStyle(SizeType.Absolute, 1F));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.Controls.Add(_header, 0, 0);
            _root.Controls.Add(_headerDivider, 0, 1);
            _root.Controls.Add(_body, 0, 2);
            _root.Controls.Add(_footerDivider, 0, 3);
            _root.Controls.Add(_footer, 0, 4);
            _root.TabIndex = 0;
            //
            // _header
            //
            _header.AutoSize = true;
            _header.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _header.ColumnCount = 2;
            _header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _header.Dock = DockStyle.Fill;
            _header.Margin = new Padding(0);
            _header.Name = "_header";
            _header.Padding = new Padding(20, 18, 20, 16);
            _header.RowCount = 2;
            _header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _header.Controls.Add(_headerIcon, 0, 0);
            _header.SetRowSpan(_headerIcon, 2);
            _header.Controls.Add(_title, 1, 0);
            _header.Controls.Add(_subtitle, 1, 1);
            //
            // _headerIcon
            //
            _headerIcon.Margin = new Padding(0, 2, 14, 0);
            _headerIcon.Name = "_headerIcon";
            _headerIcon.Size = new Size(40, 40);
            _headerIcon.SizeMode = PictureBoxSizeMode.Zoom;
            _headerIcon.TabStop = false;
            //
            // _title
            //
            _title.AutoSize = true;
            _title.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
            _title.Margin = new Padding(0);
            _title.Name = "_title";
            _title.Text = "License Agreement";
            //
            // _subtitle
            //
            _subtitle.AutoSize = true;
            _subtitle.Margin = new Padding(0, 4, 0, 0);
            _subtitle.MaximumSize = new Size(540, 0);
            _subtitle.Name = "_subtitle";
            //
            // _headerDivider
            //
            _headerDivider.Dock = DockStyle.Fill;
            _headerDivider.Margin = new Padding(0);
            _headerDivider.Name = "_headerDivider";
            //
            // _body
            //
            _body.ColumnCount = 1;
            _body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _body.Dock = DockStyle.Fill;
            _body.Margin = new Padding(0);
            _body.Name = "_body";
            _body.Padding = new Padding(20, 16, 20, 16);
            _body.RowCount = 4;
            _body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _body.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            _body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _body.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            _body.Controls.Add(_licenseHeading, 0, 0);
            _body.Controls.Add(_licenseFrame, 0, 1);
            _body.Controls.Add(_eulaHeading, 0, 2);
            _body.Controls.Add(_eulaFrame, 0, 3);
            //
            // _licenseHeading
            //
            _licenseHeading.AutoSize = true;
            _licenseHeading.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            _licenseHeading.Margin = new Padding(0, 0, 0, 6);
            _licenseHeading.Name = "_licenseHeading";
            _licenseHeading.Text = "Apache License 2.0 (source code)";
            //
            // _licenseFrame: a 1px border (its back colour) around the reading pane
            //
            _licenseFrame.Controls.Add(_licenseInner);
            _licenseFrame.Dock = DockStyle.Fill;
            _licenseFrame.Margin = new Padding(0, 0, 0, 16);
            _licenseFrame.Name = "_licenseFrame";
            _licenseFrame.Padding = new Padding(1);
            //
            // _licenseInner
            //
            _licenseInner.Controls.Add(_licenseBody);
            _licenseInner.Dock = DockStyle.Fill;
            _licenseInner.Name = "_licenseInner";
            _licenseInner.Padding = new Padding(14, 12, 4, 12);
            //
            // _licenseBody
            //
            _licenseBody.BorderStyle = BorderStyle.None;
            _licenseBody.Dock = DockStyle.Fill;
            _licenseBody.Multiline = true;
            _licenseBody.Name = "_licenseBody";
            _licenseBody.ReadOnly = true;
            _licenseBody.ScrollBars = ScrollBars.Vertical;
            _licenseBody.TabIndex = 1;
            _licenseBody.TabStop = false;
            //
            // _eulaHeading
            //
            _eulaHeading.AutoSize = true;
            _eulaHeading.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            _eulaHeading.Margin = new Padding(0, 0, 0, 6);
            _eulaHeading.Name = "_eulaHeading";
            _eulaHeading.Text = "Philterd Commercial License Agreement";
            //
            // _eulaFrame
            //
            _eulaFrame.Controls.Add(_eulaInner);
            _eulaFrame.Dock = DockStyle.Fill;
            _eulaFrame.Margin = new Padding(0);
            _eulaFrame.Name = "_eulaFrame";
            _eulaFrame.Padding = new Padding(1);
            //
            // _eulaInner
            //
            _eulaInner.Controls.Add(_eulaBody);
            _eulaInner.Dock = DockStyle.Fill;
            _eulaInner.Name = "_eulaInner";
            _eulaInner.Padding = new Padding(14, 12, 4, 12);
            //
            // _eulaBody
            //
            _eulaBody.BorderStyle = BorderStyle.None;
            _eulaBody.Dock = DockStyle.Fill;
            _eulaBody.Multiline = true;
            _eulaBody.Name = "_eulaBody";
            _eulaBody.ReadOnly = true;
            _eulaBody.ScrollBars = ScrollBars.Vertical;
            _eulaBody.TabIndex = 2;
            _eulaBody.TabStop = false;
            //
            // _footerDivider
            //
            _footerDivider.Dock = DockStyle.Fill;
            _footerDivider.Margin = new Padding(0);
            _footerDivider.Name = "_footerDivider";
            //
            // _footer
            //
            _footer.AutoSize = true;
            _footer.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _footer.ColumnCount = 2;
            _footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _footer.Dock = DockStyle.Fill;
            _footer.Margin = new Padding(0);
            _footer.Name = "_footer";
            _footer.Padding = new Padding(20, 12, 20, 12);
            _footer.RowCount = 1;
            _footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _footer.Controls.Add(_apacheLink, 0, 0);
            _footer.Controls.Add(_buttons, 1, 0);
            //
            // _apacheLink
            //
            _apacheLink.Anchor = AnchorStyles.Left;
            _apacheLink.AutoSize = true;
            _apacheLink.Margin = new Padding(0, 0, 16, 0);
            _apacheLink.Name = "_apacheLink";
            _apacheLink.TabIndex = 5;
            _apacheLink.TabStop = true;
            _apacheLink.LinkClicked += ApacheLink_LinkClicked;
            //
            // _buttons
            //
            _buttons.Anchor = AnchorStyles.Right;
            _buttons.AutoSize = true;
            _buttons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _buttons.Controls.Add(_agree);
            _buttons.Controls.Add(_disagree);
            _buttons.Margin = new Padding(0);
            _buttons.Name = "_buttons";
            _buttons.TabIndex = 3;
            _buttons.WrapContents = false;
            //
            // _agree
            //
            _agree.DialogResult = DialogResult.OK;
            _agree.Margin = new Padding(0, 0, 8, 0);
            _agree.Name = "_agree";
            _agree.Size = new Size(110, 34);
            _agree.TabIndex = 0;
            _agree.Text = "I &Agree";
            _agree.UseVisualStyleBackColor = true;
            //
            // _disagree
            //
            _disagree.DialogResult = DialogResult.Cancel;
            _disagree.Margin = new Padding(0);
            _disagree.Name = "_disagree";
            _disagree.Size = new Size(110, 34);
            _disagree.TabIndex = 1;
            _disagree.Text = "I &Disagree";
            _disagree.UseVisualStyleBackColor = true;
            //
            // LicenseForm
            //
            AcceptButton = _agree;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = _disagree;
            ClientSize = new Size(660, 500);
            Controls.Add(_root);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(500, 400);
            Name = "LicenseForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Philter Desktop";
            _buttons.ResumeLayout(false);
            _footer.ResumeLayout(false);
            _footer.PerformLayout();
            _eulaInner.ResumeLayout(false);
            _eulaInner.PerformLayout();
            _eulaFrame.ResumeLayout(false);
            _licenseInner.ResumeLayout(false);
            _licenseInner.PerformLayout();
            _licenseFrame.ResumeLayout(false);
            _body.ResumeLayout(false);
            _body.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_headerIcon).EndInit();
            _header.ResumeLayout(false);
            _header.PerformLayout();
            _root.ResumeLayout(false);
            _root.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel _root;
        private TableLayoutPanel _header;
        private PictureBox _headerIcon;
        private Label _title;
        private Label _subtitle;
        private Panel _headerDivider;
        private TableLayoutPanel _body;
        private Label _licenseHeading;
        private Panel _licenseFrame;
        private Panel _licenseInner;
        private TextBox _licenseBody;
        private Label _eulaHeading;
        private Panel _eulaFrame;
        private Panel _eulaInner;
        private TextBox _eulaBody;
        private Panel _footerDivider;
        private TableLayoutPanel _footer;
        private LinkLabel _apacheLink;
        private FlowLayoutPanel _buttons;
        private Button _agree;
        private Button _disagree;
    }
}
