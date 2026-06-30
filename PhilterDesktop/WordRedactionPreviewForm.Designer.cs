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
    partial class WordRedactionPreviewForm
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
            _highlight = new CheckBox();
            _split = new SplitContainer();
            _leftTabs = new TabControl();
            _compareTab = new TabPage();
            _preview = new DataGridView();
            _previewBefore = new DataGridViewTextBoxColumn();
            _previewAfter = new DataGridViewTextBoxColumn();
            _selectTab = new TabPage();
            _originalBox = new TextBox();
            _spanList = new ListView();
            _colType = new ColumnHeader();
            _colText = new ColumnHeader();
            _colReplacement = new ColumnHeader();
            _colPara = new ColumnHeader();
            _colStart = new ColumnHeader();
            _colStop = new ColumnHeader();
            _spanButtons = new FlowLayoutPanel();
            _redactSelection = new Button();
            _add = new Button();
            _edit = new Button();
            _remove = new Button();
            _bottomPanel = new Panel();
            _openAfter = new CheckBox();
            _save = new Button();
            _cancel = new Button();
            _topPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_split).BeginInit();
            _split.Panel1.SuspendLayout();
            _split.Panel2.SuspendLayout();
            _split.SuspendLayout();
            _leftTabs.SuspendLayout();
            _compareTab.SuspendLayout();
            _selectTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_preview).BeginInit();
            _spanButtons.SuspendLayout();
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
            _topPanel.Controls.Add(_highlight);
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
            _policyLabel.Size = new Size(45, 15);
            _policyLabel.TabIndex = 1;
            _policyLabel.Text = "Policy:";
            //
            // _policyCombo
            //
            _policyCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _policyCombo.Location = new Point(72, 38);
            _policyCombo.Name = "_policyCombo";
            _policyCombo.AccessibleName = "Policy";
            _policyCombo.Size = new Size(200, 23);
            _policyCombo.TabIndex = 2;
            _policyCombo.SelectedIndexChanged += Selection_Changed;
            //
            // _contextLabel
            //
            _contextLabel.AutoSize = true;
            _contextLabel.Location = new Point(290, 41);
            _contextLabel.Name = "_contextLabel";
            _contextLabel.Size = new Size(53, 15);
            _contextLabel.TabIndex = 3;
            _contextLabel.Text = "Context:";
            //
            // _contextCombo
            //
            _contextCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            _contextCombo.Location = new Point(349, 38);
            _contextCombo.Name = "_contextCombo";
            _contextCombo.AccessibleName = "Context";
            _contextCombo.Size = new Size(200, 23);
            _contextCombo.TabIndex = 4;
            _contextCombo.SelectedIndexChanged += Selection_Changed;
            //
            // _highlight
            //
            _highlight.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _highlight.AutoSize = true;
            _highlight.Location = new Point(580, 40);
            _highlight.Name = "_highlight";
            _highlight.Size = new Size(305, 19);
            _highlight.TabIndex = 5;
            _highlight.Text = "Highlight redactions in the Word document";
            _highlight.UseVisualStyleBackColor = true;
            //
            // _split
            //
            _split.Dock = DockStyle.Fill;
            _split.Location = new Point(0, 72);
            _split.Name = "_split";
            _split.Panel1.Controls.Add(_leftTabs);
            _split.Panel2.Controls.Add(_spanList);
            _split.Panel2.Controls.Add(_spanButtons);
            _split.Size = new Size(1000, 556);
            _split.SplitterDistance = 600;
            _split.SplitterWidth = 6;
            _split.TabIndex = 1;
            //
            // _leftTabs
            //
            _leftTabs.Controls.Add(_compareTab);
            _leftTabs.Controls.Add(_selectTab);
            _leftTabs.Dock = DockStyle.Fill;
            _leftTabs.Location = new Point(0, 0);
            _leftTabs.Name = "_leftTabs";
            _leftTabs.SelectedIndex = 0;
            _leftTabs.Size = new Size(600, 556);
            _leftTabs.TabIndex = 0;
            //
            // _compareTab
            //
            _compareTab.Controls.Add(_preview);
            _compareTab.Location = new Point(4, 24);
            _compareTab.Name = "_compareTab";
            _compareTab.Padding = new Padding(3);
            _compareTab.Size = new Size(592, 528);
            _compareTab.TabIndex = 0;
            _compareTab.Text = "Compare";
            _compareTab.UseVisualStyleBackColor = true;
            //
            // _preview
            //
            _preview.AllowUserToAddRows = false;
            _preview.AllowUserToDeleteRows = false;
            _preview.AllowUserToResizeRows = false;
            _preview.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _preview.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            _preview.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _preview.Columns.AddRange(new DataGridViewColumn[] { _previewBefore, _previewAfter });
            _preview.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            _preview.Dock = DockStyle.Fill;
            _preview.EditMode = DataGridViewEditMode.EditProgrammatically;
            _preview.Location = new Point(0, 0);
            _preview.Name = "_preview";
            _preview.ReadOnly = true;
            _preview.RowHeadersVisible = false;
            _preview.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _preview.Size = new Size(600, 556);
            _preview.TabIndex = 0;
            //
            // _previewBefore
            //
            _previewBefore.HeaderText = "Original";
            _previewBefore.Name = "_previewBefore";
            _previewBefore.ReadOnly = true;
            _previewBefore.SortMode = DataGridViewColumnSortMode.NotSortable;
            //
            // _previewAfter
            //
            _previewAfter.HeaderText = "Redacted (preview)";
            _previewAfter.Name = "_previewAfter";
            _previewAfter.ReadOnly = true;
            _previewAfter.SortMode = DataGridViewColumnSortMode.NotSortable;
            //
            // _selectTab
            //
            _selectTab.Controls.Add(_originalBox);
            _selectTab.Location = new Point(4, 24);
            _selectTab.Name = "_selectTab";
            _selectTab.Padding = new Padding(3);
            _selectTab.Size = new Size(592, 528);
            _selectTab.TabIndex = 1;
            _selectTab.Text = "Select text to redact";
            _selectTab.UseVisualStyleBackColor = true;
            //
            // _originalBox
            //
            _originalBox.BorderStyle = BorderStyle.None;
            _originalBox.Dock = DockStyle.Fill;
            _originalBox.Font = new Font("Consolas", 9.75F);
            _originalBox.HideSelection = false;
            _originalBox.Location = new Point(3, 3);
            _originalBox.Multiline = true;
            _originalBox.Name = "_originalBox";
            _originalBox.ReadOnly = true;
            _originalBox.ScrollBars = ScrollBars.Both;
            _originalBox.Size = new Size(586, 522);
            _originalBox.TabIndex = 0;
            _originalBox.WordWrap = false;
            //
            // _spanList
            //
            _spanList.Columns.AddRange(new ColumnHeader[] { _colType, _colText, _colReplacement, _colPara, _colStart, _colStop });
            _spanList.Dock = DockStyle.Fill;
            _spanList.FullRowSelect = true;
            _spanList.GridLines = true;
            _spanList.MultiSelect = false;
            _spanList.Name = "_spanList";
            _spanList.Size = new Size(394, 512);
            _spanList.TabIndex = 0;
            _spanList.UseCompatibleStateImageBehavior = false;
            _spanList.View = View.Details;
            _spanList.SelectedIndexChanged += SpanList_SelectedIndexChanged;
            //
            // _colType
            //
            _colType.Text = "Type";
            _colType.Width = 100;
            //
            // _colText
            //
            _colText.Text = "Text";
            _colText.Width = 100;
            //
            // _colReplacement
            //
            _colReplacement.Text = "Replacement";
            _colReplacement.Width = 100;
            //
            // _colPara
            //
            _colPara.Text = "Para";
            _colPara.TextAlign = HorizontalAlignment.Right;
            _colPara.Width = 44;
            //
            // _colStart
            //
            _colStart.Text = "Start";
            _colStart.TextAlign = HorizontalAlignment.Right;
            _colStart.Width = 44;
            //
            // _colStop
            //
            _colStop.Text = "Stop";
            _colStop.TextAlign = HorizontalAlignment.Right;
            _colStop.Width = 44;
            //
            // _spanButtons
            //
            _spanButtons.Controls.Add(_redactSelection);
            _spanButtons.Controls.Add(_add);
            _spanButtons.Controls.Add(_edit);
            _spanButtons.Controls.Add(_remove);
            _spanButtons.Dock = DockStyle.Bottom;
            _spanButtons.Location = new Point(0, 512);
            _spanButtons.Name = "_spanButtons";
            _spanButtons.Padding = new Padding(0, 6, 0, 0);
            _spanButtons.Size = new Size(394, 44);
            _spanButtons.TabIndex = 1;
            //
            // _redactSelection
            //
            _redactSelection.Margin = new Padding(0, 0, 8, 0);
            _redactSelection.Name = "_redactSelection";
            _redactSelection.Size = new Size(124, 30);
            _redactSelection.TabIndex = 0;
            _redactSelection.Text = "Redact selection";
            _redactSelection.UseVisualStyleBackColor = true;
            _redactSelection.Click += OnRedactSelection;
            //
            // _add
            //
            _add.Location = new Point(0, 6);
            _add.Margin = new Padding(0, 0, 8, 0);
            _add.Name = "_add";
            _add.Size = new Size(72, 30);
            _add.TabIndex = 1;
            _add.Text = "&Add…";
            _add.UseVisualStyleBackColor = true;
            _add.Click += OnAdd;
            //
            // _edit
            //
            _edit.Location = new Point(98, 6);
            _edit.Margin = new Padding(0, 0, 8, 0);
            _edit.Name = "_edit";
            _edit.Size = new Size(64, 30);
            _edit.TabIndex = 2;
            _edit.Text = "Edit…";
            _edit.UseVisualStyleBackColor = true;
            _edit.Click += OnEdit;
            //
            // _remove
            //
            _remove.Location = new Point(196, 6);
            _remove.Margin = new Padding(0, 0, 8, 0);
            _remove.Name = "_remove";
            _remove.Size = new Size(80, 30);
            _remove.TabIndex = 3;
            _remove.Text = "&Remove";
            _remove.UseVisualStyleBackColor = true;
            _remove.Click += OnRemove;
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
            _openAfter.Size = new Size(210, 19);
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
            _cancel.Text = "&Cancel";
            _cancel.UseVisualStyleBackColor = true;
            //
            // WordRedactionPreviewForm
            //
            AcceptButton = _save;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = _cancel;
            ClientSize = new Size(1000, 680);
            Controls.Add(_split);
            Controls.Add(_topPanel);
            Controls.Add(_bottomPanel);
            MinimumSize = new Size(720, 460);
            Name = "WordRedactionPreviewForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Redact (Preview)";
            Load += WordRedactionPreviewForm_Load;
            _topPanel.ResumeLayout(false);
            _topPanel.PerformLayout();
            _split.Panel1.ResumeLayout(false);
            _split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_split).EndInit();
            _split.ResumeLayout(false);
            _leftTabs.ResumeLayout(false);
            _compareTab.ResumeLayout(false);
            _selectTab.ResumeLayout(false);
            _selectTab.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_preview).EndInit();
            _spanButtons.ResumeLayout(false);
            _bottomPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel _topPanel;
        private Label _fileLabel;
        private Label _policyLabel;
        private ComboBox _policyCombo;
        private Label _contextLabel;
        private ComboBox _contextCombo;
        private CheckBox _highlight;
        private SplitContainer _split;
        private TabControl _leftTabs;
        private TabPage _compareTab;
        private TabPage _selectTab;
        private TextBox _originalBox;
        private DataGridView _preview;
        private DataGridViewTextBoxColumn _previewBefore;
        private DataGridViewTextBoxColumn _previewAfter;
        private ListView _spanList;
        private ColumnHeader _colType;
        private ColumnHeader _colText;
        private ColumnHeader _colReplacement;
        private ColumnHeader _colPara;
        private ColumnHeader _colStart;
        private ColumnHeader _colStop;
        private FlowLayoutPanel _spanButtons;
        private Button _redactSelection;
        private Button _add;
        private Button _edit;
        private Button _remove;
        private Panel _bottomPanel;
        private CheckBox _openAfter;
        private Button _save;
        private Button _cancel;
    }
}
