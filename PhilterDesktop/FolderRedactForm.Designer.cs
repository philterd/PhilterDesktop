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
    partial class FolderRedactForm
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
            _folderLabel = new Label();
            _folderBox = new TextBox();
            _browse = new Button();
            _recurse = new CheckBox();
            _policyLabel = new Label();
            _policyCombo = new ComboBox();
            _contextLabel = new Label();
            _contextCombo = new ComboBox();
            _highlight = new CheckBox();
            _summaryLabel = new Label();
            _redact = new Button();
            _cancel = new Button();
            SuspendLayout();
            //
            // _folderLabel
            //
            _folderLabel.AutoSize = true;
            _folderLabel.Location = new Point(14, 15);
            _folderLabel.Name = "_folderLabel";
            _folderLabel.Size = new Size(45, 15);
            _folderLabel.TabIndex = 0;
            _folderLabel.Text = "Folder:";
            //
            // _folderBox
            //
            _folderBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _folderBox.Location = new Point(110, 12);
            _folderBox.Name = "_folderBox";
            _folderBox.Size = new Size(330, 23);
            _folderBox.TabIndex = 1;
            _folderBox.TextChanged += OnFolderChanged;
            //
            // _browse
            //
            _browse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _browse.Location = new Point(446, 11);
            _browse.Name = "_browse";
            _browse.Size = new Size(100, 26);
            _browse.TabIndex = 2;
            _browse.Text = "Browse...";
            _browse.UseVisualStyleBackColor = true;
            _browse.Click += OnBrowse;
            //
            // _recurse
            //
            _recurse.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _recurse.AutoSize = true;
            _recurse.Location = new Point(110, 44);
            _recurse.Name = "_recurse";
            _recurse.Size = new Size(180, 19);
            _recurse.TabIndex = 3;
            _recurse.Text = "Include files in subfolders";
            _recurse.UseVisualStyleBackColor = true;
            _recurse.CheckedChanged += OnRecurseChanged;
            //
            // _policyLabel
            //
            _policyLabel.AutoSize = true;
            _policyLabel.Location = new Point(14, 80);
            _policyLabel.Name = "_policyLabel";
            _policyLabel.Size = new Size(45, 15);
            _policyLabel.TabIndex = 4;
            _policyLabel.Text = "Policy:";
            //
            // _policyCombo
            //
            _policyCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _policyCombo.Location = new Point(110, 77);
            _policyCombo.Name = "_policyCombo";
            _policyCombo.Size = new Size(330, 23);
            _policyCombo.TabIndex = 5;
            //
            // _contextLabel
            //
            _contextLabel.AutoSize = true;
            _contextLabel.Location = new Point(14, 116);
            _contextLabel.Name = "_contextLabel";
            _contextLabel.Size = new Size(53, 15);
            _contextLabel.TabIndex = 6;
            _contextLabel.Text = "Context:";
            //
            // _contextCombo
            //
            _contextCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _contextCombo.Location = new Point(110, 113);
            _contextCombo.Name = "_contextCombo";
            _contextCombo.Size = new Size(330, 23);
            _contextCombo.TabIndex = 7;
            //
            // _highlight
            //
            _highlight.AutoSize = true;
            _highlight.Location = new Point(110, 149);
            _highlight.Name = "_highlight";
            _highlight.Size = new Size(305, 19);
            _highlight.TabIndex = 8;
            _highlight.Text = "Highlight redactions in Word (.docx) documents";
            _highlight.UseVisualStyleBackColor = true;
            //
            // _summaryLabel
            //
            _summaryLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _summaryLabel.Location = new Point(14, 185);
            _summaryLabel.Name = "_summaryLabel";
            _summaryLabel.Size = new Size(532, 40);
            _summaryLabel.TabIndex = 9;
            _summaryLabel.Text = "Choose a folder to see how many files will be redacted.";
            //
            // _redact
            //
            _redact.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _redact.DialogResult = DialogResult.OK;
            _redact.Location = new Point(326, 235);
            _redact.Name = "_redact";
            _redact.Size = new Size(110, 34);
            _redact.TabIndex = 10;
            _redact.Text = "Add to Queue";
            _redact.UseVisualStyleBackColor = true;
            _redact.Click += OnRedact;
            //
            // _cancel
            //
            _cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _cancel.DialogResult = DialogResult.Cancel;
            _cancel.Location = new Point(442, 235);
            _cancel.Name = "_cancel";
            _cancel.Size = new Size(110, 34);
            _cancel.TabIndex = 11;
            _cancel.Text = "Cancel";
            _cancel.UseVisualStyleBackColor = true;
            //
            // FolderRedactForm
            //
            AcceptButton = _redact;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = _cancel;
            ClientSize = new Size(560, 283);
            Controls.Add(_folderLabel);
            Controls.Add(_folderBox);
            Controls.Add(_browse);
            Controls.Add(_recurse);
            Controls.Add(_policyLabel);
            Controls.Add(_policyCombo);
            Controls.Add(_contextLabel);
            Controls.Add(_contextCombo);
            Controls.Add(_highlight);
            Controls.Add(_summaryLabel);
            Controls.Add(_redact);
            Controls.Add(_cancel);
            MinimizeBox = false;
            MinimumSize = new Size(480, 322);
            Name = "FolderRedactForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Redact Folder";
            Load += FolderRedactForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label _folderLabel;
        private TextBox _folderBox;
        private Button _browse;
        private CheckBox _recurse;
        private Label _policyLabel;
        private ComboBox _policyCombo;
        private Label _contextLabel;
        private ComboBox _contextCombo;
        private CheckBox _highlight;
        private Label _summaryLabel;
        private Button _redact;
        private Button _cancel;
    }
}
