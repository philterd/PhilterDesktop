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
    partial class PdfRedactionPreviewForm
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
            _topPanel = new Panel();
            _fileLabel = new Label();
            _policyLabel = new Label();
            _policyCombo = new ComboBox();
            _contextLabel = new Label();
            _contextCombo = new ComboBox();
            _view = new PdfSideBySideView();
            _bottomPanel = new Panel();
            _openAfter = new CheckBox();
            _save = new Button();
            _cancel = new Button();
            _topPanel.SuspendLayout();
            _bottomPanel.SuspendLayout();
            SuspendLayout();
            // 
            // _topPanel
            // 
            _topPanel.Controls.Add(_fileLabel);
            _topPanel.Controls.Add(_policyLabel);
            _topPanel.Controls.Add(_policyCombo);
            _topPanel.Controls.Add(_contextLabel);
            _topPanel.Controls.Add(_contextCombo);
            _topPanel.Dock = DockStyle.Top;
            _topPanel.Location = new Point(0, 0);
            _topPanel.Name = "_topPanel";
            _topPanel.Size = new Size(1000, 72);
            _topPanel.TabIndex = 0;
            // 
            // _fileLabel
            // 
            _fileLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _fileLabel.AutoEllipsis = true;
            _fileLabel.Location = new Point(12, 10);
            _fileLabel.Name = "_fileLabel";
            _fileLabel.Size = new Size(976, 20);
            _fileLabel.TabIndex = 0;
            _fileLabel.Text = "(file)";
            // 
            // _policyLabel
            // 
            _policyLabel.AutoSize = true;
            _policyLabel.Location = new Point(12, 41);
            _policyLabel.Name = "_policyLabel";
            _policyLabel.Size = new Size(42, 15);
            _policyLabel.TabIndex = 1;
            _policyLabel.Text = "Policy:";
            // 
            // _policyCombo
            // 
            _policyCombo.AccessibleName = "Policy";
            _policyCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _policyCombo.Location = new Point(72, 38);
            _policyCombo.Name = "_policyCombo";
            _policyCombo.Size = new Size(200, 23);
            _policyCombo.TabIndex = 2;
            _policyCombo.SelectedIndexChanged += Selection_Changed;
            // 
            // _contextLabel
            // 
            _contextLabel.AutoSize = true;
            _contextLabel.Location = new Point(290, 41);
            _contextLabel.Name = "_contextLabel";
            _contextLabel.Size = new Size(51, 15);
            _contextLabel.TabIndex = 3;
            _contextLabel.Text = "Context:";
            // 
            // _contextCombo
            // 
            _contextCombo.AccessibleName = "Context";
            _contextCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _contextCombo.Location = new Point(349, 38);
            _contextCombo.Name = "_contextCombo";
            _contextCombo.Size = new Size(200, 23);
            _contextCombo.TabIndex = 4;
            _contextCombo.SelectedIndexChanged += Selection_Changed;
            // 
            // _view
            // 
            _view.Dock = DockStyle.Fill;
            _view.Location = new Point(0, 72);
            _view.Name = "_view";
            _view.Size = new Size(1000, 556);
            _view.TabIndex = 1;
            // 
            // _bottomPanel
            // 
            _bottomPanel.Controls.Add(_openAfter);
            _bottomPanel.Controls.Add(_save);
            _bottomPanel.Controls.Add(_cancel);
            _bottomPanel.Dock = DockStyle.Bottom;
            _bottomPanel.Location = new Point(0, 628);
            _bottomPanel.Name = "_bottomPanel";
            _bottomPanel.Size = new Size(1000, 52);
            _bottomPanel.TabIndex = 2;
            // 
            // _openAfter
            // 
            _openAfter.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _openAfter.AutoSize = true;
            _openAfter.Location = new Point(12, 16);
            _openAfter.Name = "_openAfter";
            _openAfter.Size = new Size(193, 19);
            _openAfter.TabIndex = 0;
            _openAfter.Text = "Open document after redaction";
            _openAfter.UseVisualStyleBackColor = true;
            // 
            // _save
            // 
            _save.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _save.Location = new Point(764, 9);
            _save.Name = "_save";
            _save.Size = new Size(160, 34);
            _save.TabIndex = 0;
            _save.Text = "&Save Redacted File";
            _save.UseVisualStyleBackColor = true;
            _save.Click += OnSave;
            // 
            // _cancel
            // 
            _cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _cancel.DialogResult = DialogResult.Cancel;
            _cancel.Location = new Point(930, 9);
            _cancel.Name = "_cancel";
            _cancel.Size = new Size(58, 34);
            _cancel.TabIndex = 1;
            _cancel.Text = "&Cancel";
            _cancel.UseVisualStyleBackColor = true;
            // 
            // PdfRedactionPreviewForm
            // 
            AcceptButton = _save;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = _cancel;
            ClientSize = new Size(1000, 680);
            Controls.Add(_view);
            Controls.Add(_topPanel);
            Controls.Add(_bottomPanel);
            MinimizeBox = false;
            MinimumSize = new Size(820, 460);
            Name = "PdfRedactionPreviewForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Redact with Preview";
            Load += PdfRedactionPreviewForm_Load;
            _topPanel.ResumeLayout(false);
            _topPanel.PerformLayout();
            _bottomPanel.ResumeLayout(false);
            _bottomPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Panel _topPanel;
        private Label _fileLabel;
        private Label _policyLabel;
        private ComboBox _policyCombo;
        private Label _contextLabel;
        private ComboBox _contextCombo;
        private PdfSideBySideView _view;
        private Panel _bottomPanel;
        private CheckBox _openAfter;
        private Button _save;
        private Button _cancel;
    }
}
