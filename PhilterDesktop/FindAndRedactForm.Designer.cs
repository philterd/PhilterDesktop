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
    partial class FindAndRedactForm
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
            _termsLabel = new Label();
            _terms = new TextBox();
            _import = new Button();
            _redact = new Button();
            _close = new Button();
            SuspendLayout();
            //
            // _sourceLabel
            //
            _sourceLabel.AutoSize = true;
            _sourceLabel.Location = new Point(12, 12);
            _sourceLabel.Name = "_sourceLabel";
            _sourceLabel.Size = new Size(118, 15);
            _sourceLabel.TabIndex = 0;
            _sourceLabel.Text = "Document to redact:";
            //
            // _source
            //
            _source.AccessibleName = "Document to redact";
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
            _browse.Text = "Browse…";
            _browse.UseVisualStyleBackColor = true;
            _browse.Click += OnBrowse;
            //
            // _termsLabel
            //
            _termsLabel.AutoSize = true;
            _termsLabel.Location = new Point(12, 70);
            _termsLabel.Name = "_termsLabel";
            _termsLabel.Size = new Size(310, 15);
            _termsLabel.TabIndex = 3;
            _termsLabel.Text = "Redact every occurrence of these terms (one per line):";
            //
            // _terms
            //
            _terms.AcceptsReturn = true;
            _terms.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _terms.Location = new Point(12, 92);
            _terms.Multiline = true;
            _terms.Name = "_terms";
            _terms.ScrollBars = ScrollBars.Both;
            _terms.Size = new Size(536, 282);
            _terms.TabIndex = 4;
            _terms.WordWrap = false;
            //
            // _import
            //
            _import.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _import.AutoSize = true;
            _import.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _import.Location = new Point(12, 382);
            _import.MinimumSize = new Size(0, 26);
            _import.Name = "_import";
            _import.Padding = new Padding(10, 0, 10, 0);
            _import.TabIndex = 5;
            _import.Text = "Import from file…";
            _import.UseVisualStyleBackColor = true;
            _import.Click += OnImport;
            //
            // _redact
            //
            _redact.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _redact.Location = new Point(382, 382);
            _redact.Name = "_redact";
            _redact.Size = new Size(80, 26);
            _redact.TabIndex = 6;
            _redact.Text = "Redact";
            _redact.UseVisualStyleBackColor = true;
            _redact.Click += OnRedact;
            //
            // _close
            //
            _close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _close.DialogResult = DialogResult.Cancel;
            _close.Location = new Point(468, 382);
            _close.Name = "_close";
            _close.Size = new Size(80, 26);
            _close.TabIndex = 7;
            _close.Text = "Close";
            _close.UseVisualStyleBackColor = true;
            //
            // FindAndRedactForm
            //
            AcceptButton = _redact;
            CancelButton = _close;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(560, 420);
            Controls.Add(_source);
            Controls.Add(_browse);
            Controls.Add(_sourceLabel);
            Controls.Add(_terms);
            Controls.Add(_termsLabel);
            Controls.Add(_import);
            Controls.Add(_redact);
            Controls.Add(_close);
            MinimizeBox = false;
            MinimumSize = new Size(456, 360);
            Name = "FindAndRedactForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Find & Redact";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label _sourceLabel;
        private TextBox _source;
        private Button _browse;
        private Label _termsLabel;
        private TextBox _terms;
        private Button _import;
        private Button _redact;
        private Button _close;
    }
}
