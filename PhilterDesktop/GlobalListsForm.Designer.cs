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
    partial class GlobalListsForm
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
            tabControl = new TabControl();
            btnImport = new Button();
            tabRedact = new TabPage();
            txtRedact = new TextBox();
            lblRedact = new Label();
            tabIgnore = new TabPage();
            txtIgnore = new TextBox();
            lblIgnore = new Label();
            btnOk = new Button();
            btnCancel = new Button();
            tabControl.SuspendLayout();
            tabRedact.SuspendLayout();
            tabIgnore.SuspendLayout();
            SuspendLayout();
            //
            // tabControl
            //
            tabControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl.Controls.Add(tabRedact);
            tabControl.Controls.Add(tabIgnore);
            tabControl.Location = new Point(12, 12);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(456, 376);
            tabControl.TabIndex = 0;
            //
            // tabRedact
            //
            tabRedact.Controls.Add(txtRedact);
            tabRedact.Controls.Add(lblRedact);
            tabRedact.Location = new Point(4, 24);
            tabRedact.Name = "tabRedact";
            tabRedact.Padding = new Padding(3);
            tabRedact.Size = new Size(448, 348);
            tabRedact.TabIndex = 0;
            tabRedact.Text = "Always Redact";
            tabRedact.UseVisualStyleBackColor = true;
            //
            // lblRedact
            //
            lblRedact.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblRedact.Location = new Point(6, 8);
            lblRedact.Name = "lblRedact";
            lblRedact.Size = new Size(436, 34);
            lblRedact.TabIndex = 0;
            lblRedact.Text = "These terms are always redacted, no matter which policy is used. One per line.";
            //
            // txtRedact
            //
            txtRedact.AcceptsReturn = true;
            txtRedact.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtRedact.Location = new Point(6, 45);
            txtRedact.Multiline = true;
            txtRedact.Name = "txtRedact";
            txtRedact.ScrollBars = ScrollBars.Both;
            txtRedact.Size = new Size(436, 297);
            txtRedact.TabIndex = 1;
            txtRedact.WordWrap = false;
            //
            // tabIgnore
            //
            tabIgnore.Controls.Add(txtIgnore);
            tabIgnore.Controls.Add(lblIgnore);
            tabIgnore.Location = new Point(4, 24);
            tabIgnore.Name = "tabIgnore";
            tabIgnore.Padding = new Padding(3);
            tabIgnore.Size = new Size(448, 348);
            tabIgnore.TabIndex = 1;
            tabIgnore.Text = "Always Ignore";
            tabIgnore.UseVisualStyleBackColor = true;
            //
            // lblIgnore
            //
            lblIgnore.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblIgnore.Location = new Point(6, 8);
            lblIgnore.Name = "lblIgnore";
            lblIgnore.Size = new Size(436, 34);
            lblIgnore.TabIndex = 0;
            lblIgnore.Text = "These terms are never redacted, no matter which policy is used. One per line.";
            //
            // txtIgnore
            //
            txtIgnore.AcceptsReturn = true;
            txtIgnore.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtIgnore.Location = new Point(6, 45);
            txtIgnore.Multiline = true;
            txtIgnore.Name = "txtIgnore";
            txtIgnore.ScrollBars = ScrollBars.Both;
            txtIgnore.Size = new Size(436, 297);
            txtIgnore.TabIndex = 1;
            txtIgnore.WordWrap = false;
            //
            // btnOk
            //
            btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(300, 402);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(80, 26);
            btnOk.TabIndex = 1;
            btnOk.Text = "OK";
            btnOk.UseVisualStyleBackColor = true;
            //
            // btnCancel
            //
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(388, 402);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(80, 26);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            //
            // btnImport
            //
            btnImport.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnImport.AutoSize = true;
            btnImport.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnImport.Location = new Point(12, 403);
            btnImport.MinimumSize = new Size(0, 26);
            btnImport.Name = "btnImport";
            btnImport.Padding = new Padding(10, 0, 10, 0);
            btnImport.TabIndex = 3;
            btnImport.Text = "Import from file…";
            btnImport.UseVisualStyleBackColor = true;
            btnImport.Click += btnImport_Click;
            //
            // GlobalListsForm
            //
            AcceptButton = btnOk;
            CancelButton = btnCancel;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(480, 440);
            Controls.Add(tabControl);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            Controls.Add(btnImport);
            MinimizeBox = false;
            MinimumSize = new Size(396, 359);
            Name = "GlobalListsForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Always Redact / Always Ignore Lists";
            tabControl.ResumeLayout(false);
            tabRedact.ResumeLayout(false);
            tabRedact.PerformLayout();
            tabIgnore.ResumeLayout(false);
            tabIgnore.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControl;
        private TabPage tabRedact;
        private TabPage tabIgnore;
        private TextBox txtRedact;
        private TextBox txtIgnore;
        private Label lblRedact;
        private Label lblIgnore;
        private Button btnOk;
        private Button btnCancel;
        private Button btnImport;
    }
}
