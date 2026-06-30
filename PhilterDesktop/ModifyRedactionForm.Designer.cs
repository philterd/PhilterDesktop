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
    partial class ModifyRedactionForm
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
            _split = new Panel();
            _rightPanel = new Panel();
            _spanList = new ListView();
            _columnType = new ColumnHeader();
            _columnText = new ColumnHeader();
            _columnReplacement = new ColumnHeader();
            _columnStart = new ColumnHeader();
            _columnStop = new ColumnHeader();
            _columnLocation = new ColumnHeader();
            _spanButtons = new FlowLayoutPanel();
            _add = new Button();
            _edit = new Button();
            _remove = new Button();
            _treePanel = new Panel();
            _versionTree = new TreeView();
            _treeButtons = new FlowLayoutPanel();
            _newVersion = new Button();
            _deleteVersion = new Button();
            _versionsLabel = new Label();
            _bottom = new FlowLayoutPanel();
            _close = new Button();
            _redact = new Button();
            _split.SuspendLayout();
            _rightPanel.SuspendLayout();
            _spanButtons.SuspendLayout();
            _treePanel.SuspendLayout();
            _treeButtons.SuspendLayout();
            _bottom.SuspendLayout();
            SuspendLayout();
            // 
            // _split
            // 
            _split.Controls.Add(_rightPanel);
            _split.Controls.Add(_treePanel);
            _split.Dock = DockStyle.Fill;
            _split.Location = new Point(0, 0);
            _split.Name = "_split";
            _split.Size = new Size(940, 364);
            _split.TabIndex = 0;
            // 
            // _rightPanel
            // 
            _rightPanel.Controls.Add(_spanList);
            _rightPanel.Controls.Add(_spanButtons);
            _rightPanel.Dock = DockStyle.Fill;
            _rightPanel.Location = new Point(276, 0);
            _rightPanel.Name = "_rightPanel";
            _rightPanel.Padding = new Padding(8);
            _rightPanel.Size = new Size(664, 364);
            _rightPanel.TabIndex = 1;
            // 
            // _spanList
            // 
            _spanList.Columns.AddRange(new ColumnHeader[] { _columnType, _columnText, _columnReplacement, _columnStart, _columnStop, _columnLocation });
            _spanList.Dock = DockStyle.Fill;
            _spanList.FullRowSelect = true;
            _spanList.GridLines = true;
            _spanList.Location = new Point(8, 8);
            _spanList.MultiSelect = false;
            _spanList.Name = "_spanList";
            _spanList.Size = new Size(648, 304);
            _spanList.TabIndex = 0;
            _spanList.UseCompatibleStateImageBehavior = false;
            _spanList.View = View.Details;
            _spanList.SelectedIndexChanged += SpanList_SelectedIndexChanged;
            _spanList.DoubleClick += OnEdit;
            // 
            // _columnType
            // 
            _columnType.Text = "Type";
            _columnType.Width = 150;
            // 
            // _columnText
            // 
            _columnText.Text = "Text";
            _columnText.Width = 220;
            // 
            // _columnReplacement
            // 
            _columnReplacement.Text = "Replacement";
            _columnReplacement.Width = 180;
            // 
            // _columnStart
            // 
            _columnStart.Text = "Start";
            _columnStart.TextAlign = HorizontalAlignment.Right;
            _columnStart.Width = 70;
            // 
            // _columnStop
            // 
            _columnStop.Text = "Stop";
            _columnStop.TextAlign = HorizontalAlignment.Right;
            _columnStop.Width = 70;
            // 
            // _columnLocation
            // 
            _columnLocation.Text = "Location";
            _columnLocation.Width = 90;
            // 
            // _spanButtons
            // 
            _spanButtons.Controls.Add(_add);
            _spanButtons.Controls.Add(_edit);
            _spanButtons.Controls.Add(_remove);
            _spanButtons.Dock = DockStyle.Bottom;
            _spanButtons.Location = new Point(8, 312);
            _spanButtons.Name = "_spanButtons";
            _spanButtons.Padding = new Padding(0, 6, 0, 0);
            _spanButtons.Size = new Size(648, 44);
            _spanButtons.TabIndex = 1;
            // 
            // _add
            // 
            _add.Location = new Point(0, 6);
            _add.Margin = new Padding(0, 0, 8, 0);
            _add.Name = "_add";
            _add.Size = new Size(110, 34);
            _add.TabIndex = 0;
            _add.Text = "&Add…";
            _add.UseVisualStyleBackColor = true;
            _add.Click += OnAdd;
            // 
            // _edit
            // 
            _edit.Location = new Point(118, 6);
            _edit.Margin = new Padding(0, 0, 8, 0);
            _edit.Name = "_edit";
            _edit.Size = new Size(110, 34);
            _edit.TabIndex = 1;
            _edit.Text = "Edit…";
            _edit.UseVisualStyleBackColor = true;
            _edit.Click += OnEdit;
            // 
            // _remove
            // 
            _remove.Location = new Point(236, 6);
            _remove.Margin = new Padding(0, 0, 8, 0);
            _remove.Name = "_remove";
            _remove.Size = new Size(110, 34);
            _remove.TabIndex = 2;
            _remove.Text = "&Remove";
            _remove.UseVisualStyleBackColor = true;
            _remove.Click += OnRemove;
            // 
            // _treePanel
            // 
            _treePanel.Controls.Add(_versionTree);
            _treePanel.Controls.Add(_treeButtons);
            _treePanel.Controls.Add(_versionsLabel);
            _treePanel.Dock = DockStyle.Left;
            _treePanel.Location = new Point(0, 0);
            _treePanel.Name = "_treePanel";
            _treePanel.Padding = new Padding(8);
            _treePanel.Size = new Size(276, 364);
            _treePanel.TabIndex = 0;
            // 
            // _versionTree
            // 
            _versionTree.Dock = DockStyle.Fill;
            _versionTree.FullRowSelect = true;
            _versionTree.HideSelection = false;
            _versionTree.Location = new Point(8, 30);
            _versionTree.Name = "_versionTree";
            _versionTree.ShowLines = false;
            _versionTree.ShowPlusMinus = false;
            _versionTree.ShowRootLines = false;
            _versionTree.Size = new Size(260, 286);
            _versionTree.TabIndex = 1;
            _versionTree.AfterSelect += VersionTree_AfterSelect;
            // 
            // _treeButtons
            // 
            _treeButtons.Controls.Add(_newVersion);
            _treeButtons.Controls.Add(_deleteVersion);
            _treeButtons.Dock = DockStyle.Bottom;
            _treeButtons.Location = new Point(8, 316);
            _treeButtons.Name = "_treeButtons";
            _treeButtons.Padding = new Padding(0, 6, 0, 0);
            _treeButtons.Size = new Size(260, 40);
            _treeButtons.TabIndex = 2;
            // 
            // _newVersion
            // 
            _newVersion.Location = new Point(0, 6);
            _newVersion.Margin = new Padding(0, 0, 8, 0);
            _newVersion.Name = "_newVersion";
            _newVersion.Size = new Size(110, 34);
            _newVersion.TabIndex = 0;
            _newVersion.Text = "New Version";
            _newVersion.UseVisualStyleBackColor = true;
            _newVersion.Click += OnNewVersion;
            // 
            // _deleteVersion
            // 
            _deleteVersion.Location = new Point(118, 6);
            _deleteVersion.Margin = new Padding(0, 0, 8, 0);
            _deleteVersion.Name = "_deleteVersion";
            _deleteVersion.Size = new Size(110, 34);
            _deleteVersion.TabIndex = 1;
            _deleteVersion.Text = "&Delete";
            _deleteVersion.UseVisualStyleBackColor = true;
            _deleteVersion.Click += OnDeleteVersion;
            // 
            // _versionsLabel
            // 
            _versionsLabel.Dock = DockStyle.Top;
            _versionsLabel.Location = new Point(8, 8);
            _versionsLabel.Name = "_versionsLabel";
            _versionsLabel.Size = new Size(260, 22);
            _versionsLabel.TabIndex = 0;
            _versionsLabel.Text = "Versions";
            _versionsLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // _bottom
            // 
            _bottom.Controls.Add(_close);
            _bottom.Controls.Add(_redact);
            _bottom.Dock = DockStyle.Bottom;
            _bottom.FlowDirection = FlowDirection.RightToLeft;
            _bottom.Location = new Point(0, 364);
            _bottom.Name = "_bottom";
            _bottom.Padding = new Padding(10);
            _bottom.Size = new Size(940, 56);
            _bottom.TabIndex = 1;
            // 
            // _close
            // 
            _close.DialogResult = DialogResult.Cancel;
            _close.Location = new Point(810, 10);
            _close.Margin = new Padding(8, 0, 0, 0);
            _close.Name = "_close";
            _close.Size = new Size(110, 34);
            _close.TabIndex = 0;
            _close.Text = "&Close";
            _close.UseVisualStyleBackColor = true;
            // 
            // _redact
            // 
            _redact.Location = new Point(692, 10);
            _redact.Margin = new Padding(8, 0, 0, 0);
            _redact.Name = "_redact";
            _redact.Size = new Size(110, 34);
            _redact.TabIndex = 1;
            _redact.Text = "Reda&ct";
            _redact.UseVisualStyleBackColor = true;
            _redact.Click += OnRedact;
            //
            // ModifyRedactionForm
            //
            // No AcceptButton: "Redact" re-writes the output (destructive), so it must be an explicit
            // click — Enter must not trigger it.
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = _close;
            ClientSize = new Size(940, 420);
            Controls.Add(_split);
            Controls.Add(_bottom);
            MinimumSize = new Size(716, 459);
            Name = "ModifyRedactionForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Modify Redaction";
            _split.ResumeLayout(false);
            _rightPanel.ResumeLayout(false);
            _spanButtons.ResumeLayout(false);
            _treePanel.ResumeLayout(false);
            _treeButtons.ResumeLayout(false);
            _bottom.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel _split;
        private Panel _rightPanel;
        private ListView _spanList;
        private ColumnHeader _columnType;
        private ColumnHeader _columnText;
        private ColumnHeader _columnReplacement;
        private ColumnHeader _columnStart;
        private ColumnHeader _columnStop;
        private ColumnHeader _columnLocation;
        private FlowLayoutPanel _spanButtons;
        private Button _add;
        private Button _edit;
        private Button _remove;
        private Panel _treePanel;
        private TreeView _versionTree;
        private FlowLayoutPanel _treeButtons;
        private Button _newVersion;
        private Button _deleteVersion;
        private Label _versionsLabel;
        private FlowLayoutPanel _bottom;
        private Button _close;
        private Button _redact;
    }
}
