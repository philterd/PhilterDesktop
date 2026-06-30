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
    partial class WatchedFolderLogForm
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
            _list = new ListView();
            timeColumn = new ColumnHeader();
            levelColumn = new ColumnHeader();
            messageColumn = new ColumnHeader();
            _refresh = new Button();
            _clear = new Button();
            _close = new Button();
            SuspendLayout();
            //
            // _list
            //
            _list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _list.Columns.AddRange(new ColumnHeader[] { timeColumn, levelColumn, messageColumn });
            _list.FullRowSelect = true;
            _list.GridLines = true;
            _list.Location = new Point(12, 12);
            _list.Name = "_list";
            _list.Size = new Size(736, 360);
            _list.TabIndex = 0;
            _list.UseCompatibleStateImageBehavior = false;
            _list.View = View.Details;
            //
            // timeColumn
            //
            timeColumn.Text = "Time";
            timeColumn.Width = 150;
            //
            // levelColumn
            //
            levelColumn.Text = "Level";
            levelColumn.Width = 70;
            //
            // messageColumn
            //
            messageColumn.Text = "Message";
            messageColumn.Width = 500;
            //
            // _refresh
            //
            _refresh.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _refresh.Location = new Point(12, 384);
            _refresh.Name = "_refresh";
            _refresh.Size = new Size(110, 34);
            _refresh.TabIndex = 1;
            _refresh.Text = "Refresh";
            _refresh.UseVisualStyleBackColor = true;
            _refresh.Click += RefreshButton_Click;
            //
            // _clear
            //
            _clear.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _clear.Location = new Point(130, 384);
            _clear.Name = "_clear";
            _clear.Size = new Size(110, 34);
            _clear.TabIndex = 2;
            _clear.Text = "Clear &Log";
            _clear.UseVisualStyleBackColor = true;
            _clear.Click += ClearButton_Click;
            //
            // _close
            //
            _close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _close.DialogResult = DialogResult.OK;
            _close.Location = new Point(638, 384);
            _close.Name = "_close";
            _close.Size = new Size(110, 34);
            _close.TabIndex = 3;
            _close.Text = "&Close";
            _close.UseVisualStyleBackColor = true;
            //
            // WatchedFolderLogForm
            //
            AcceptButton = _close;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = _close;
            ClientSize = new Size(760, 432);
            Controls.Add(_list);
            Controls.Add(_refresh);
            Controls.Add(_clear);
            Controls.Add(_close);
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(560, 360);
            Name = "WatchedFolderLogForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Activity Log";
            ResumeLayout(false);

        }

        #endregion

        private ListView _list;
        private ColumnHeader timeColumn;
        private ColumnHeader levelColumn;
        private ColumnHeader messageColumn;
        private Button _refresh;
        private Button _clear;
        private Button _close;
    }
}
