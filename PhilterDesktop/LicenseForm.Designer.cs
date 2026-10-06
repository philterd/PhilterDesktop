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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            _root = new TableLayoutPanel();
            _licenseHeading = new Label();
            _licenseBody = new TextBox();
            _eulaHeading = new Label();
            _eulaBody = new TextBox();
            _buttons = new FlowLayoutPanel();
            _agree = new Button();
            _disagree = new Button();
            _root.SuspendLayout();
            _buttons.SuspendLayout();
            SuspendLayout();
            //
            // _root
            //
            // Texts share the height; buttons stay pinned to the bottom.
            _root.ColumnCount = 1;
            _root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            _root.Dock = DockStyle.Fill;
            _root.Location = new Point(0, 0);
            _root.Name = "_root";
            _root.Padding = new Padding(11, 9, 11, 11);
            _root.RowCount = 5;
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.Controls.Add(_licenseHeading, 0, 0);
            _root.Controls.Add(_licenseBody, 0, 1);
            _root.Controls.Add(_eulaHeading, 0, 2);
            _root.Controls.Add(_eulaBody, 0, 3);
            _root.Controls.Add(_buttons, 0, 4);
            _root.Size = new Size(620, 590);
            _root.TabIndex = 0;
            //
            // _licenseHeading
            //
            _licenseHeading.AutoSize = true;
            _licenseHeading.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _licenseHeading.Margin = new Padding(3, 3, 3, 3);
            _licenseHeading.Name = "_licenseHeading";
            _licenseHeading.Size = new Size(57, 19);
            _licenseHeading.TabIndex = 0;
            _licenseHeading.Text = "License";
            //
            // _licenseBody
            //
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
            _eulaHeading.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _eulaHeading.Margin = new Padding(3, 10, 3, 3);
            _eulaHeading.Name = "_eulaHeading";
            _eulaHeading.Size = new Size(198, 19);
            _eulaHeading.TabIndex = 2;
            _eulaHeading.Text = "End User License Agreement";
            //
            // _eulaBody
            //
            _eulaBody.Dock = DockStyle.Fill;
            _eulaBody.Multiline = true;
            _eulaBody.Name = "_eulaBody";
            _eulaBody.ReadOnly = true;
            _eulaBody.ScrollBars = ScrollBars.Vertical;
            _eulaBody.TabIndex = 3;
            _eulaBody.TabStop = false;
            //
            // _buttons
            //
            _buttons.AutoSize = true;
            _buttons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _buttons.Dock = DockStyle.Fill;
            _buttons.FlowDirection = FlowDirection.RightToLeft;
            _buttons.Margin = new Padding(0, 9, 0, 0);
            _buttons.Name = "_buttons";
            _buttons.TabIndex = 4;
            _buttons.WrapContents = false;
            _buttons.Controls.Add(_disagree);
            _buttons.Controls.Add(_agree);
            //
            // _agree
            //
            _agree.DialogResult = DialogResult.OK;
            _agree.Name = "_agree";
            _agree.Size = new Size(110, 34);
            _agree.TabIndex = 0;
            _agree.Text = "I &Agree";
            _agree.UseVisualStyleBackColor = true;
            //
            // _disagree
            //
            _disagree.DialogResult = DialogResult.Cancel;
            _disagree.Margin = new Padding(6, 3, 3, 3);
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
            ClientSize = new Size(620, 590);
            Controls.Add(_root);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MinimumSize = new Size(440, 380);
            Name = "LicenseForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Philter Desktop License";
            _buttons.ResumeLayout(false);
            _root.ResumeLayout(false);
            _root.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel _root;
        private Label _licenseHeading;
        private TextBox _licenseBody;
        private Label _eulaHeading;
        private TextBox _eulaBody;
        private FlowLayoutPanel _buttons;
        private Button _agree;
        private Button _disagree;
    }
}
