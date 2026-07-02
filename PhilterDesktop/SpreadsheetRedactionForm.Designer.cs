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
    partial class SpreadsheetRedactionForm
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
            _sourceLabel = new Label();
            _source = new TextBox();
            _browse = new Button();
            _policyLabel = new Label();
            _policy = new ComboBox();
            _contextLabel = new Label();
            _context = new ComboBox();
            _worksheetLabel = new Label();
            _worksheet = new ComboBox();
            _columnsLabel = new Label();
            _selectAll = new LinkLabel();
            _clearAll = new LinkLabel();
            _selectedCount = new Label();
            _columns = new CheckedListBox();
            _redact = new Button();
            _close = new Button();
            SuspendLayout();
            // 
            // _sourceLabel
            // 
            _sourceLabel.AutoSize = true;
            _sourceLabel.Location = new Point(12, 12);
            _sourceLabel.Name = "_sourceLabel";
            _sourceLabel.Size = new Size(124, 15);
            _sourceLabel.TabIndex = 0;
            _sourceLabel.Text = "Spreadsheet to redact:";
            // 
            // _source
            // 
            _source.AccessibleName = "Spreadsheet to redact";
            _source.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _source.Location = new Point(12, 33);
            _source.Name = "_source";
            _source.ReadOnly = true;
            _source.Size = new Size(440, 23);
            _source.TabIndex = 1;
            // 
            // _browse
            // 
            _browse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _browse.Location = new Point(458, 32);
            _browse.Name = "_browse";
            _browse.Size = new Size(90, 26);
            _browse.TabIndex = 2;
            _browse.Text = "&Browse…";
            _browse.UseVisualStyleBackColor = true;
            _browse.Click += OnBrowse;
            // 
            // _policyLabel
            // 
            _policyLabel.AutoSize = true;
            _policyLabel.Location = new Point(12, 73);
            _policyLabel.Name = "_policyLabel";
            _policyLabel.Size = new Size(42, 15);
            _policyLabel.TabIndex = 3;
            _policyLabel.Text = "Policy:";
            // 
            // _policy
            // 
            _policy.DropDownStyle = ComboBoxStyle.DropDownList;
            _policy.Location = new Point(70, 70);
            _policy.Name = "_policy";
            _policy.Size = new Size(180, 23);
            _policy.TabIndex = 4;
            // 
            // _contextLabel
            // 
            _contextLabel.AutoSize = true;
            _contextLabel.Location = new Point(266, 73);
            _contextLabel.Name = "_contextLabel";
            _contextLabel.Size = new Size(51, 15);
            _contextLabel.TabIndex = 5;
            _contextLabel.Text = "Context:";
            // 
            // _context
            // 
            _context.DropDownStyle = ComboBoxStyle.DropDownList;
            _context.Location = new Point(320, 70);
            _context.Name = "_context";
            _context.Size = new Size(180, 23);
            _context.TabIndex = 6;
            // 
            // _worksheetLabel
            // 
            _worksheetLabel.AutoSize = true;
            _worksheetLabel.Location = new Point(12, 108);
            _worksheetLabel.Name = "_worksheetLabel";
            _worksheetLabel.Size = new Size(116, 15);
            _worksheetLabel.TabIndex = 7;
            _worksheetLabel.Text = "Worksheet to redact:";
            // 
            // _worksheet
            // 
            _worksheet.DropDownStyle = ComboBoxStyle.DropDownList;
            _worksheet.Location = new Point(163, 105);
            _worksheet.Name = "_worksheet";
            _worksheet.Size = new Size(250, 23);
            _worksheet.TabIndex = 8;
            _worksheet.SelectedIndexChanged += OnWorksheetChanged;
            // 
            // _columnsLabel
            // 
            _columnsLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _columnsLabel.Location = new Point(12, 140);
            _columnsLabel.Name = "_columnsLabel";
            _columnsLabel.Size = new Size(536, 54);
            _columnsLabel.TabIndex = 9;
            _columnsLabel.Text = "Sensitive information is removed from each text cell (cells stored as numbers are not scanned). Tick a column below to remove its entire contents, useful for name, ID, or number columns:";
            // 
            // _selectAll
            // 
            _selectAll.AutoSize = true;
            _selectAll.Location = new Point(12, 200);
            _selectAll.Name = "_selectAll";
            _selectAll.Size = new Size(53, 15);
            _selectAll.TabIndex = 10;
            _selectAll.TabStop = true;
            _selectAll.Text = "&Select all";
            // 
            // _clearAll
            // 
            _clearAll.AutoSize = true;
            _clearAll.Location = new Point(82, 200);
            _clearAll.Name = "_clearAll";
            _clearAll.Size = new Size(34, 15);
            _clearAll.TabIndex = 11;
            _clearAll.TabStop = true;
            _clearAll.Text = "C&lear";
            // 
            // _selectedCount
            // 
            _selectedCount.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _selectedCount.AutoSize = true;
            _selectedCount.ForeColor = SystemColors.GrayText;
            _selectedCount.Location = new Point(470, 200);
            _selectedCount.Name = "_selectedCount";
            _selectedCount.Size = new Size(59, 15);
            _selectedCount.TabIndex = 12;
            _selectedCount.Text = "0 selected";
            _selectedCount.TextAlign = ContentAlignment.MiddleRight;
            // 
            // _columns
            // 
            _columns.AccessibleName = "Columns to redact entirely";
            _columns.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _columns.CheckOnClick = true;
            _columns.FormattingEnabled = true;
            _columns.IntegralHeight = false;
            _columns.Location = new Point(12, 226);
            _columns.Name = "_columns";
            _columns.Size = new Size(536, 238);
            _columns.TabIndex = 13;
            // 
            // _redact
            // 
            _redact.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _redact.Enabled = false;
            _redact.Location = new Point(362, 476);
            _redact.Name = "_redact";
            _redact.Size = new Size(100, 26);
            _redact.TabIndex = 14;
            _redact.Text = "&Add to Queue";
            _redact.UseVisualStyleBackColor = true;
            _redact.Click += OnRedact;
            // 
            // _close
            // 
            _close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _close.DialogResult = DialogResult.Cancel;
            _close.Location = new Point(468, 476);
            _close.Name = "_close";
            _close.Size = new Size(80, 26);
            _close.TabIndex = 15;
            _close.Text = "&Cancel";
            _close.UseVisualStyleBackColor = true;
            // 
            // SpreadsheetRedactionForm
            // 
            AcceptButton = _redact;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = _close;
            ClientSize = new Size(560, 514);
            Controls.Add(_source);
            Controls.Add(_browse);
            Controls.Add(_sourceLabel);
            Controls.Add(_policyLabel);
            Controls.Add(_policy);
            Controls.Add(_contextLabel);
            Controls.Add(_context);
            Controls.Add(_worksheetLabel);
            Controls.Add(_worksheet);
            Controls.Add(_columnsLabel);
            Controls.Add(_selectAll);
            Controls.Add(_clearAll);
            Controls.Add(_selectedCount);
            Controls.Add(_columns);
            Controls.Add(_redact);
            Controls.Add(_close);
            MinimizeBox = false;
            MinimumSize = new Size(576, 553);
            Name = "SpreadsheetRedactionForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Redact Spreadsheet";
            Load += SpreadsheetRedactionForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label _sourceLabel;
        private TextBox _source;
        private Button _browse;
        private Label _policyLabel;
        private ComboBox _policy;
        private Label _contextLabel;
        private ComboBox _context;
        private Label _worksheetLabel;
        private ComboBox _worksheet;
        private Label _columnsLabel;
        private LinkLabel _selectAll;
        private LinkLabel _clearAll;
        private Label _selectedCount;
        private CheckedListBox _columns;
        private Button _redact;
        private Button _close;
    }
}
