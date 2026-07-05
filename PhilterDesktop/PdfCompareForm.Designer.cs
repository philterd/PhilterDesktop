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
    partial class PdfCompareForm
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
            _view = new PdfSideBySideView();
            _bottom = new Panel();
            _close = new Button();
            _bottom.SuspendLayout();
            SuspendLayout();
            //
            // _view
            //
            _view.Dock = DockStyle.Fill;
            _view.Location = new Point(0, 0);
            _view.Name = "_view";
            _view.Size = new Size(1000, 600);
            _view.TabIndex = 0;
            //
            // _bottom
            //
            _bottom.Controls.Add(_close);
            _bottom.Dock = DockStyle.Bottom;
            _bottom.Location = new Point(0, 600);
            _bottom.Name = "_bottom";
            _bottom.Size = new Size(1000, 52);
            _bottom.TabIndex = 1;
            //
            // _close
            //
            _close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _close.DialogResult = DialogResult.OK;
            _close.Location = new Point(880, 9);
            _close.Name = "_close";
            _close.Size = new Size(110, 34);
            _close.TabIndex = 0;
            _close.Text = "&Close";
            _close.UseVisualStyleBackColor = true;
            //
            // PdfCompareForm
            //
            AcceptButton = _close;
            CancelButton = _close;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1000, 652);
            Controls.Add(_view);
            Controls.Add(_bottom);
            MinimizeBox = false;
            MinimumSize = new Size(820, 460);
            Name = "PdfCompareForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Compare PDF";
            _bottom.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private PdfSideBySideView _view;
        private Panel _bottom;
        private Button _close;
    }
}
