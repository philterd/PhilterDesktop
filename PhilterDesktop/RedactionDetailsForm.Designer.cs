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
    partial class RedactionDetailsForm
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
            _list = new ListView();
            _propColumn = new ColumnHeader();
            _valueColumn = new ColumnHeader();
            _close = new Button();
            SuspendLayout();
            //
            // _list
            //
            _list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _list.Columns.AddRange(new ColumnHeader[] { _propColumn, _valueColumn });
            _list.FullRowSelect = true;
            _list.GridLines = true;
            _list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _list.Location = new Point(12, 12);
            _list.MultiSelect = false;
            _list.Name = "_list";
            _list.Size = new Size(576, 296);
            _list.TabIndex = 0;
            _list.UseCompatibleStateImageBehavior = false;
            _list.View = View.Details;
            //
            // _propColumn
            //
            _propColumn.Text = "Property";
            _propColumn.Width = 150;
            //
            // _valueColumn
            //
            _valueColumn.Text = "Value";
            _valueColumn.Width = 420;
            //
            // _close
            //
            _close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _close.DialogResult = DialogResult.OK;
            _close.Location = new Point(478, 318);
            _close.Name = "_close";
            _close.Size = new Size(110, 34);
            _close.TabIndex = 1;
            _close.Text = "Close";
            _close.UseVisualStyleBackColor = true;
            //
            // RedactionDetailsForm
            //
            AcceptButton = _close;
            CancelButton = _close;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(600, 364);
            Controls.Add(_list);
            Controls.Add(_close);
            MinimizeBox = false;
            MinimumSize = new Size(420, 280);
            Name = "RedactionDetailsForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Details";
            ResumeLayout(false);
        }

        #endregion

        private ListView _list;
        private ColumnHeader _propColumn;
        private ColumnHeader _valueColumn;
        private Button _close;
    }
}
