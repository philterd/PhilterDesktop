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
    partial class DiffViewerForm
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
            _grid = new DataGridView();
            _beforeColumn = new DataGridViewTextBoxColumn();
            _afterColumn = new DataGridViewTextBoxColumn();
            _close = new Button();
            ((System.ComponentModel.ISupportInitialize)_grid).BeginInit();
            SuspendLayout();
            //
            // _grid
            //
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AllowUserToResizeRows = false;
            _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells; // grow rows to fit wrapped text
            _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _grid.Columns.AddRange(new DataGridViewColumn[] { _beforeColumn, _afterColumn });
            _grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True; // word-wrap long lines
            _grid.EditMode = DataGridViewEditMode.EditProgrammatically;
            _grid.Location = new Point(12, 12);
            _grid.Name = "_grid";
            _grid.ReadOnly = true;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.Size = new Size(936, 520);
            _grid.TabIndex = 0;
            //
            // _beforeColumn
            //
            _beforeColumn.HeaderText = "Before";
            _beforeColumn.Name = "_beforeColumn";
            _beforeColumn.ReadOnly = true;
            _beforeColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            //
            // _afterColumn
            //
            _afterColumn.HeaderText = "After";
            _afterColumn.Name = "_afterColumn";
            _afterColumn.ReadOnly = true;
            _afterColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            //
            // _close
            //
            _close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _close.DialogResult = DialogResult.OK;
            _close.Location = new Point(838, 540);
            _close.Name = "_close";
            _close.Size = new Size(110, 34);
            _close.TabIndex = 1;
            _close.Text = "&Close";
            _close.UseVisualStyleBackColor = true;
            //
            // DiffViewerForm
            //
            AcceptButton = _close;
            CancelButton = _close;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(960, 586);
            Controls.Add(_grid);
            Controls.Add(_close);
            MinimizeBox = false;
            MinimumSize = new Size(560, 360);
            Name = "DiffViewerForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Redaction Diff";
            ((System.ComponentModel.ISupportInitialize)_grid).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private DataGridView _grid;
        private DataGridViewTextBoxColumn _beforeColumn;
        private DataGridViewTextBoxColumn _afterColumn;
        private Button _close;
    }
}
