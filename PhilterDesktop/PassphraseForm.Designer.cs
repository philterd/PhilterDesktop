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
    partial class PassphraseForm
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
            _root = new TableLayoutPanel();
            _prompt = new Label();
            _currentLabel = new Label();
            _current = new TextBox();
            _newLabel = new Label();
            _new = new TextBox();
            _confirmLabel = new Label();
            _confirm = new TextBox();
            _buttons = new FlowLayoutPanel();
            _ok = new Button();
            _cancel = new Button();
            _root.SuspendLayout();
            _buttons.SuspendLayout();
            SuspendLayout();
            //
            // _root
            //
            _root.AutoSize = true;
            _root.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _root.ColumnCount = 2;
            _root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _root.Dock = DockStyle.Fill;
            _root.Location = new Point(0, 0);
            _root.Name = "_root";
            _root.Padding = new Padding(14);
            _root.RowCount = 5;
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.Controls.Add(_prompt, 0, 0);
            _root.SetColumnSpan(_prompt, 2);
            _root.Controls.Add(_currentLabel, 0, 1);
            _root.Controls.Add(_current, 1, 1);
            _root.Controls.Add(_newLabel, 0, 2);
            _root.Controls.Add(_new, 1, 2);
            _root.Controls.Add(_confirmLabel, 0, 3);
            _root.Controls.Add(_confirm, 1, 3);
            _root.Controls.Add(_buttons, 0, 4);
            _root.SetColumnSpan(_buttons, 2);
            _root.TabIndex = 0;
            //
            // _prompt
            //
            _prompt.AutoSize = true;
            _prompt.MaximumSize = new Size(360, 0);
            _prompt.Margin = new Padding(0, 0, 0, 12);
            _prompt.Name = "_prompt";
            _prompt.Text = "Enter your passphrase.";
            //
            // _currentLabel
            //
            _currentLabel.AutoSize = true;
            _currentLabel.Anchor = AnchorStyles.Left;
            _currentLabel.Margin = new Padding(0, 8, 12, 6);
            _currentLabel.Name = "_currentLabel";
            _currentLabel.Text = "Current passphrase:";
            //
            // _current
            //
            _current.Anchor = AnchorStyles.Left;
            _current.Margin = new Padding(0, 4, 0, 4);
            _current.Name = "_current";
            _current.Size = new Size(240, 23);
            _current.UseSystemPasswordChar = true;
            //
            // _newLabel
            //
            _newLabel.AutoSize = true;
            _newLabel.Anchor = AnchorStyles.Left;
            _newLabel.Margin = new Padding(0, 8, 12, 6);
            _newLabel.Name = "_newLabel";
            _newLabel.Text = "Passphrase:";
            //
            // _new
            //
            _new.Anchor = AnchorStyles.Left;
            _new.Margin = new Padding(0, 4, 0, 4);
            _new.Name = "_new";
            _new.Size = new Size(240, 23);
            _new.UseSystemPasswordChar = true;
            //
            // _confirmLabel
            //
            _confirmLabel.AutoSize = true;
            _confirmLabel.Anchor = AnchorStyles.Left;
            _confirmLabel.Margin = new Padding(0, 8, 12, 6);
            _confirmLabel.Name = "_confirmLabel";
            _confirmLabel.Text = "Confirm passphrase:";
            //
            // _confirm
            //
            _confirm.Anchor = AnchorStyles.Left;
            _confirm.Margin = new Padding(0, 4, 0, 4);
            _confirm.Name = "_confirm";
            _confirm.Size = new Size(240, 23);
            _confirm.UseSystemPasswordChar = true;
            //
            // _buttons
            //
            _buttons.AutoSize = true;
            _buttons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _buttons.Dock = DockStyle.Fill;
            _buttons.FlowDirection = FlowDirection.RightToLeft;
            _buttons.Margin = new Padding(0, 14, 0, 0);
            _buttons.Name = "_buttons";
            _buttons.Controls.Add(_cancel);
            _buttons.Controls.Add(_ok);
            //
            // _ok
            //
            _ok.DialogResult = DialogResult.OK;
            _ok.Name = "_ok";
            _ok.Size = new Size(110, 34);
            _ok.Text = "&OK";
            _ok.Click += OnOk;
            //
            // _cancel
            //
            _cancel.DialogResult = DialogResult.Cancel;
            _cancel.Margin = new Padding(8, 3, 3, 3);
            _cancel.Name = "_cancel";
            _cancel.Size = new Size(110, 34);
            _cancel.Text = "&Cancel";
            //
            // PassphraseForm
            //
            AcceptButton = _ok;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = _cancel;
            ClientSize = new Size(420, 240);
            Controls.Add(_root);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PassphraseForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Passphrase";
            _root.ResumeLayout(false);
            _root.PerformLayout();
            _buttons.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TableLayoutPanel _root;
        private Label _prompt;
        private Label _currentLabel;
        private TextBox _current;
        private Label _newLabel;
        private TextBox _new;
        private Label _confirmLabel;
        private TextBox _confirm;
        private FlowLayoutPanel _buttons;
        private Button _ok;
        private Button _cancel;
    }
}
