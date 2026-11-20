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
    partial class PreviewSourceForm
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
            _dropZone = new FileDropZone();
            _continue = new Button();
            _cancel = new Button();
            SuspendLayout();
            //
            // _sourceLabel
            //
            _sourceLabel.AutoSize = true;
            _sourceLabel.Location = new Point(12, 12);
            _sourceLabel.Name = "_sourceLabel";
            _sourceLabel.Size = new Size(122, 15);
            _sourceLabel.TabIndex = 0;
            _sourceLabel.Text = "Document to preview:";
            //
            // _source
            //
            _source.AccessibleName = "Document to preview";
            _source.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _source.Location = new Point(12, 33);
            _source.Name = "_source";
            _source.ReadOnly = true;
            _source.Size = new Size(440, 23);
            _source.TabIndex = 1;
            _source.TabStop = false;
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
            // _dropZone
            //
            _dropZone.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _dropZone.Location = new Point(12, 64);
            _dropZone.Name = "_dropZone";
            _dropZone.Size = new Size(536, 60);
            _dropZone.TabIndex = 5;
            //
            // _continue
            //
            _continue.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _continue.Enabled = false;
            _continue.Location = new Point(372, 140);
            _continue.Name = "_continue";
            _continue.Size = new Size(90, 26);
            _continue.TabIndex = 3;
            _continue.Text = "C&ontinue";
            _continue.UseVisualStyleBackColor = true;
            _continue.Click += OnContinue;
            //
            // _cancel
            //
            _cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _cancel.DialogResult = DialogResult.Cancel;
            _cancel.Location = new Point(468, 140);
            _cancel.Name = "_cancel";
            _cancel.Size = new Size(80, 26);
            _cancel.TabIndex = 4;
            _cancel.Text = "&Cancel";
            _cancel.UseVisualStyleBackColor = true;
            //
            // PreviewSourceForm
            //
            AcceptButton = _continue;
            CancelButton = _cancel;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(560, 178);
            Controls.Add(_sourceLabel);
            Controls.Add(_source);
            Controls.Add(_browse);
            Controls.Add(_dropZone);
            Controls.Add(_continue);
            Controls.Add(_cancel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PreviewSourceForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Redact with Preview";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label _sourceLabel;
        private TextBox _source;
        private Button _browse;
        private FileDropZone _dropZone;
        private Button _continue;
        private Button _cancel;
    }
}
