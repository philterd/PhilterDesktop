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
    partial class ContextMenuRedactForm
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
            _filesLabel = new Label();
            _fileList = new ListView();
            _fileColumn = new ColumnHeader();
            _policyLabel = new Label();
            _policyCombo = new ComboBox();
            _contextLabel = new Label();
            _contextCombo = new ComboBox();
            _highlight = new CheckBox();
            _redact = new Button();
            _cancel = new Button();
            SuspendLayout();
            //
            // _filesLabel
            //
            _filesLabel.AutoSize = true;
            _filesLabel.Location = new Point(14, 12);
            _filesLabel.Name = "_filesLabel";
            _filesLabel.Size = new Size(260, 15);
            _filesLabel.TabIndex = 0;
            _filesLabel.Text = "These files will be added to the redaction queue:";
            //
            // _fileList
            //
            _fileList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _fileList.Columns.AddRange(new ColumnHeader[] { _fileColumn });
            _fileList.FullRowSelect = true;
            _fileList.HeaderStyle = ColumnHeaderStyle.None;
            _fileList.Location = new Point(14, 33);
            _fileList.MultiSelect = false;
            _fileList.Name = "_fileList";
            _fileList.Size = new Size(532, 250);
            _fileList.TabIndex = 1;
            _fileList.UseCompatibleStateImageBehavior = false;
            _fileList.View = View.Details;
            //
            // _fileColumn
            //
            _fileColumn.Text = "File";
            _fileColumn.Width = 510;
            //
            // _policyLabel
            //
            _policyLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _policyLabel.AutoSize = true;
            _policyLabel.Location = new Point(14, 300);
            _policyLabel.Name = "_policyLabel";
            _policyLabel.Size = new Size(45, 15);
            _policyLabel.TabIndex = 2;
            _policyLabel.Text = "Policy:";
            //
            // _policyCombo
            //
            _policyCombo.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _policyCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _policyCombo.Location = new Point(110, 297);
            _policyCombo.Name = "_policyCombo";
            _policyCombo.Size = new Size(220, 23);
            _policyCombo.TabIndex = 3;
            //
            // _contextLabel
            //
            _contextLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _contextLabel.AutoSize = true;
            _contextLabel.Location = new Point(14, 336);
            _contextLabel.Name = "_contextLabel";
            _contextLabel.Size = new Size(53, 15);
            _contextLabel.TabIndex = 4;
            _contextLabel.Text = "Context:";
            //
            // _contextCombo
            //
            _contextCombo.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _contextCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _contextCombo.Location = new Point(110, 333);
            _contextCombo.Name = "_contextCombo";
            _contextCombo.Size = new Size(220, 23);
            _contextCombo.TabIndex = 5;
            //
            // _highlight
            //
            _highlight.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _highlight.AutoSize = true;
            _highlight.Location = new Point(14, 372);
            _highlight.Name = "_highlight";
            _highlight.Size = new Size(305, 19);
            _highlight.TabIndex = 6;
            _highlight.Text = "Highlight redactions in Word (.docx) documents";
            _highlight.UseVisualStyleBackColor = true;
            //
            // _redact
            //
            _redact.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _redact.DialogResult = DialogResult.OK;
            _redact.Location = new Point(326, 410);
            _redact.Name = "_redact";
            _redact.Size = new Size(110, 34);
            _redact.TabIndex = 7;
            _redact.Text = "&Add to Queue";
            _redact.UseVisualStyleBackColor = true;
            _redact.Click += OnRedact;
            //
            // _cancel
            //
            _cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _cancel.DialogResult = DialogResult.Cancel;
            _cancel.Location = new Point(442, 410);
            _cancel.Name = "_cancel";
            _cancel.Size = new Size(110, 34);
            _cancel.TabIndex = 8;
            _cancel.Text = "&Cancel";
            _cancel.UseVisualStyleBackColor = true;
            //
            // ContextMenuRedactForm
            //
            AcceptButton = _redact;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = _cancel;
            ClientSize = new Size(560, 458);
            Controls.Add(_filesLabel);
            Controls.Add(_fileList);
            Controls.Add(_policyLabel);
            Controls.Add(_policyCombo);
            Controls.Add(_contextLabel);
            Controls.Add(_contextCombo);
            Controls.Add(_highlight);
            Controls.Add(_redact);
            Controls.Add(_cancel);
            MinimizeBox = false;
            MinimumSize = new Size(480, 360);
            Name = "ContextMenuRedactForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Redact with Philter Desktop";
            Load += ContextMenuRedactForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label _filesLabel;
        private ListView _fileList;
        private ColumnHeader _fileColumn;
        private Label _policyLabel;
        private ComboBox _policyCombo;
        private Label _contextLabel;
        private ComboBox _contextCombo;
        private CheckBox _highlight;
        private Button _redact;
        private Button _cancel;
    }
}
