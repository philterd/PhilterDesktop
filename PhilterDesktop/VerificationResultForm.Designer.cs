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
    partial class VerificationResultForm
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
            _summary = new Label();
            _list = new ListView();
            _colType = new ColumnHeader();
            _colText = new ColumnHeader();
            _colLocation = new ColumnHeader();
            _disclaimer = new Label();
            _close = new Button();
            SuspendLayout();
            //
            // _summary
            //
            _summary.Dock = DockStyle.Top;
            _summary.Padding = new Padding(14, 14, 14, 8);
            _summary.AutoSize = false;
            _summary.Height = 56;
            _summary.Name = "_summary";
            _summary.TabIndex = 0;
            _summary.Text = "Verification result";
            //
            // _list
            //
            _list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _list.Columns.AddRange(new ColumnHeader[] { _colType, _colText, _colLocation });
            _list.FullRowSelect = true;
            _list.Location = new Point(14, 60);
            _list.Name = "_list";
            _list.Size = new Size(612, 326);
            _list.TabIndex = 1;
            _list.UseCompatibleStateImageBehavior = false;
            _list.View = View.Details;
            //
            // _colType
            //
            _colType.Text = "Type";
            _colType.Width = 150;
            //
            // _colText
            //
            _colText.Text = "Text still present";
            _colText.Width = 300;
            //
            // _colLocation
            //
            _colLocation.Text = "Location";
            _colLocation.Width = 140;
            //
            // _disclaimer
            //
            _disclaimer.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _disclaimer.ForeColor = SystemColors.GrayText;
            _disclaimer.Location = new Point(14, 394);
            _disclaimer.Name = "_disclaimer";
            _disclaimer.Size = new Size(612, 50);
            _disclaimer.TabIndex = 2;
            _disclaimer.Text = "This check is not a guarantee that all sensitive information was correctly identified and removed. The redacted file should still be carefully reviewed by a person before it is shared.";
            //
            // _close
            //
            _close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _close.DialogResult = DialogResult.OK;
            _close.Location = new Point(516, 450);
            _close.Name = "_close";
            _close.Size = new Size(110, 34);
            _close.TabIndex = 3;
            _close.Text = "&Close";
            _close.UseVisualStyleBackColor = true;
            //
            // VerificationResultForm
            //
            AcceptButton = _close;
            CancelButton = _close;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(640, 498);
            Controls.Add(_list);
            Controls.Add(_summary);
            Controls.Add(_disclaimer);
            Controls.Add(_close);
            MinimizeBox = false;
            MinimumSize = new Size(520, 380);
            Name = "VerificationResultForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Verification";
            ResumeLayout(false);
        }

        #endregion

        private Label _summary;
        private ListView _list;
        private ColumnHeader _colType;
        private ColumnHeader _colText;
        private ColumnHeader _colLocation;
        private Label _disclaimer;
        private Button _close;
    }
}
