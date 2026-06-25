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
    partial class DownloadProgressForm
    {
        private System.ComponentModel.IContainer components = null;

        // Dispose(bool) is implemented in DownloadProgressForm.cs (it also disposes the
        // cancellation token source).

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            _label = new Label();
            _progress = new ProgressBar();
            _cancel = new Button();
            SuspendLayout();
            //
            // _label
            //
            _label.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _label.AutoEllipsis = true;
            _label.Location = new Point(12, 14);
            _label.Name = "_label";
            _label.Size = new Size(436, 20);
            _label.TabIndex = 0;
            _label.Text = "Preparing download…";
            //
            // _progress
            //
            _progress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _progress.Location = new Point(12, 40);
            _progress.Name = "_progress";
            _progress.Size = new Size(436, 22);
            _progress.Style = ProgressBarStyle.Marquee;
            _progress.TabIndex = 1;
            //
            // _cancel
            //
            _cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _cancel.DialogResult = DialogResult.Cancel;
            _cancel.Location = new Point(338, 76);
            _cancel.Name = "_cancel";
            _cancel.Size = new Size(110, 34);
            _cancel.TabIndex = 2;
            _cancel.Text = "Cancel";
            _cancel.UseVisualStyleBackColor = true;
            _cancel.Click += Cancel_Click;
            //
            // DownloadProgressForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = _cancel;
            ClientSize = new Size(460, 122);
            ControlBox = false;
            Controls.Add(_cancel);
            Controls.Add(_progress);
            Controls.Add(_label);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "DownloadProgressForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Downloading Update";
            ResumeLayout(false);
        }

        #endregion

        private Label _label;
        private ProgressBar _progress;
        private Button _cancel;
    }
}
