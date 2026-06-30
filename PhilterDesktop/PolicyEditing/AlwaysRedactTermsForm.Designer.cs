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

namespace PhilterDesktop.PolicyEditing
{
    partial class AlwaysRedactTermsForm
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
            instructionsLabel = new Label();
            termsTextBox = new TextBox();
            bottomPanel = new Panel();
            okButton = new Button();
            cancelButton = new Button();
            importButton = new Button();
            bottomPanel.SuspendLayout();
            SuspendLayout();
            // 
            // instructionsLabel
            // 
            instructionsLabel.AutoSize = true;
            instructionsLabel.Dock = DockStyle.Top;
            instructionsLabel.Location = new Point(12, 12);
            instructionsLabel.Name = "instructionsLabel";
            instructionsLabel.Padding = new Padding(0, 0, 0, 8);
            instructionsLabel.Size = new Size(585, 23);
            instructionsLabel.TabIndex = 2;
            instructionsLabel.Text = "Terms entered here are always redacted, even if no filter would otherwise detect them. Enter one term per line.";
            // 
            // termsTextBox
            // 
            termsTextBox.AcceptsReturn = true;
            termsTextBox.Dock = DockStyle.Fill;
            termsTextBox.Location = new Point(12, 35);
            termsTextBox.Multiline = true;
            termsTextBox.Name = "termsTextBox";
            termsTextBox.ScrollBars = ScrollBars.Vertical;
            termsTextBox.Size = new Size(601, 195);
            termsTextBox.TabIndex = 0;
            termsTextBox.WordWrap = false;
            // 
            // bottomPanel
            // 
            bottomPanel.Controls.Add(okButton);
            bottomPanel.Controls.Add(cancelButton);
            bottomPanel.Controls.Add(importButton);
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Location = new Point(12, 230);
            bottomPanel.Name = "bottomPanel";
            bottomPanel.Size = new Size(601, 52);
            bottomPanel.TabIndex = 1;
            // 
            // okButton
            // 
            okButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new Point(375, 13);
            okButton.Name = "okButton";
            okButton.Size = new Size(110, 34);
            okButton.TabIndex = 0;
            okButton.Text = "&OK";
            okButton.UseVisualStyleBackColor = true;
            okButton.Click += OkButton_Click;
            // 
            // cancelButton
            // 
            cancelButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(491, 13);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(110, 34);
            cancelButton.TabIndex = 1;
            cancelButton.Text = "&Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            //
            // importButton
            //
            importButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            importButton.AutoSize = true;
            importButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            importButton.Location = new Point(0, 13);
            importButton.MinimumSize = new Size(0, 34);
            importButton.Name = "importButton";
            importButton.Padding = new Padding(10, 0, 10, 0);
            importButton.TabIndex = 2;
            importButton.Text = "Import from file…";
            importButton.UseVisualStyleBackColor = true;
            importButton.Click += ImportButton_Click;
            //
            // AlwaysRedactTermsForm
            //
            AcceptButton = okButton;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(625, 294);
            Controls.Add(termsTextBox);
            Controls.Add(bottomPanel);
            Controls.Add(instructionsLabel);
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(641, 333);
            Name = "AlwaysRedactTermsForm";
            Padding = new Padding(12);
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Always Redact Terms";
            bottomPanel.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label instructionsLabel;
        private TextBox termsTextBox;
        private Panel bottomPanel;
        private Button okButton;
        private Button cancelButton;
        private Button importButton;
    }
}
