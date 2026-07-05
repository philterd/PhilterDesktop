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
            _licenseHeading = new Label();
            _licenseBody = new TextBox();
            _eulaHeading = new Label();
            _eulaBody = new TextBox();
            _agree = new Button();
            _disagree = new Button();
            SuspendLayout();
            // 
            // _licenseHeading
            // 
            _licenseHeading.AutoSize = true;
            _licenseHeading.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _licenseHeading.Location = new Point(14, 12);
            _licenseHeading.Name = "_licenseHeading";
            _licenseHeading.Size = new Size(57, 19);
            _licenseHeading.TabIndex = 0;
            _licenseHeading.Text = "License";
            // 
            // _licenseBody
            // 
            _licenseBody.Location = new Point(14, 34);
            _licenseBody.Multiline = true;
            _licenseBody.Name = "_licenseBody";
            _licenseBody.ReadOnly = true;
            _licenseBody.ScrollBars = ScrollBars.Vertical;
            _licenseBody.Size = new Size(592, 232);
            _licenseBody.TabIndex = 1;
            _licenseBody.TabStop = false;
            // 
            // _eulaHeading
            // 
            _eulaHeading.AutoSize = true;
            _eulaHeading.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _eulaHeading.Location = new Point(14, 276);
            _eulaHeading.Name = "_eulaHeading";
            _eulaHeading.Size = new Size(198, 19);
            _eulaHeading.TabIndex = 2;
            _eulaHeading.Text = "End User License Agreement";
            // 
            // _eulaBody
            // 
            _eulaBody.Location = new Point(14, 298);
            _eulaBody.Multiline = true;
            _eulaBody.Name = "_eulaBody";
            _eulaBody.ReadOnly = true;
            _eulaBody.ScrollBars = ScrollBars.Vertical;
            _eulaBody.Size = new Size(592, 232);
            _eulaBody.TabIndex = 3;
            _eulaBody.TabStop = false;
            // 
            // _agree
            // 
            _agree.DialogResult = DialogResult.OK;
            _agree.Location = new Point(380, 542);
            _agree.Name = "_agree";
            _agree.Size = new Size(110, 34);
            _agree.TabIndex = 4;
            _agree.Text = "I &Agree";
            _agree.UseVisualStyleBackColor = true;
            // 
            // _disagree
            // 
            _disagree.DialogResult = DialogResult.Cancel;
            _disagree.Location = new Point(496, 542);
            _disagree.Name = "_disagree";
            _disagree.Size = new Size(110, 34);
            _disagree.TabIndex = 5;
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
            Controls.Add(_licenseHeading);
            Controls.Add(_licenseBody);
            Controls.Add(_eulaHeading);
            Controls.Add(_eulaBody);
            Controls.Add(_agree);
            Controls.Add(_disagree);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "LicenseForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Philter Desktop License";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label _licenseHeading;
        private TextBox _licenseBody;
        private Label _eulaHeading;
        private TextBox _eulaBody;
        private Button _agree;
        private Button _disagree;
    }
}
