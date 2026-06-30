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
    partial class SpanEditForm
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
            _root = new TableLayoutPanel();
            _paragraphLabel = new Label();
            _paragraph = new NumericUpDown();
            _startLabel = new Label();
            _start = new NumericUpDown();
            _stopLabel = new Label();
            _stop = new NumericUpDown();
            _pageLabel = new Label();
            _page = new NumericUpDown();
            _llxLabel = new Label();
            _llx = new NumericUpDown();
            _llyLabel = new Label();
            _lly = new NumericUpDown();
            _urxLabel = new Label();
            _urx = new NumericUpDown();
            _uryLabel = new Label();
            _ury = new NumericUpDown();
            _replacementLabel = new Label();
            _replacement = new TextBox();
            _buttons = new FlowLayoutPanel();
            _ok = new Button();
            _cancel = new Button();
            _root.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_paragraph).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_start).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_stop).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_page).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_llx).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_lly).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_urx).BeginInit();
            ((System.ComponentModel.ISupportInitialize)_ury).BeginInit();
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
            _root.RowCount = 10;
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _root.Controls.Add(_paragraphLabel, 0, 0);
            _root.Controls.Add(_paragraph, 1, 0);
            _root.Controls.Add(_startLabel, 0, 1);
            _root.Controls.Add(_start, 1, 1);
            _root.Controls.Add(_stopLabel, 0, 2);
            _root.Controls.Add(_stop, 1, 2);
            _root.Controls.Add(_pageLabel, 0, 3);
            _root.Controls.Add(_page, 1, 3);
            _root.Controls.Add(_llxLabel, 0, 4);
            _root.Controls.Add(_llx, 1, 4);
            _root.Controls.Add(_llyLabel, 0, 5);
            _root.Controls.Add(_lly, 1, 5);
            _root.Controls.Add(_urxLabel, 0, 6);
            _root.Controls.Add(_urx, 1, 6);
            _root.Controls.Add(_uryLabel, 0, 7);
            _root.Controls.Add(_ury, 1, 7);
            _root.Controls.Add(_replacementLabel, 0, 8);
            _root.Controls.Add(_replacement, 1, 8);
            _root.Controls.Add(_buttons, 0, 9);
            _root.SetColumnSpan(_buttons, 2);
            _root.TabIndex = 0;
            //
            // _paragraphLabel
            //
            _paragraphLabel.AutoSize = true;
            _paragraphLabel.Anchor = AnchorStyles.Left;
            _paragraphLabel.Margin = new Padding(0, 8, 12, 6);
            _paragraphLabel.Name = "_paragraphLabel";
            _paragraphLabel.Text = "Paragraph:";
            //
            // _paragraph
            //
            _paragraph.Anchor = AnchorStyles.Left;
            _paragraph.Margin = new Padding(0, 4, 0, 4);
            _paragraph.Maximum = 2147483647;
            _paragraph.Minimum = 1;
            _paragraph.Name = "_paragraph";
            _paragraph.Size = new Size(120, 23);
            _paragraph.TextAlign = HorizontalAlignment.Right;
            _paragraph.Value = 1;
            //
            // _startLabel
            //
            _startLabel.AutoSize = true;
            _startLabel.Anchor = AnchorStyles.Left;
            _startLabel.Margin = new Padding(0, 8, 12, 6);
            _startLabel.Name = "_startLabel";
            _startLabel.Text = "Start character:";
            //
            // _start
            //
            _start.Anchor = AnchorStyles.Left;
            _start.Margin = new Padding(0, 4, 0, 4);
            _start.Maximum = 2147483647;
            _start.Name = "_start";
            _start.Size = new Size(120, 23);
            _start.TextAlign = HorizontalAlignment.Right;
            //
            // _stopLabel
            //
            _stopLabel.AutoSize = true;
            _stopLabel.Anchor = AnchorStyles.Left;
            _stopLabel.Margin = new Padding(0, 8, 12, 6);
            _stopLabel.Name = "_stopLabel";
            _stopLabel.Text = "Stop character:";
            //
            // _stop
            //
            _stop.Anchor = AnchorStyles.Left;
            _stop.Margin = new Padding(0, 4, 0, 4);
            _stop.Maximum = 2147483647;
            _stop.Name = "_stop";
            _stop.Size = new Size(120, 23);
            _stop.TextAlign = HorizontalAlignment.Right;
            //
            // _pageLabel
            //
            _pageLabel.AutoSize = true;
            _pageLabel.Anchor = AnchorStyles.Left;
            _pageLabel.Margin = new Padding(0, 8, 12, 6);
            _pageLabel.Name = "_pageLabel";
            _pageLabel.Text = "Page:";
            //
            // _page
            //
            _page.Anchor = AnchorStyles.Left;
            _page.Margin = new Padding(0, 4, 0, 4);
            _page.Maximum = 2147483647;
            _page.Minimum = 1;
            _page.Name = "_page";
            _page.Size = new Size(120, 23);
            _page.TextAlign = HorizontalAlignment.Right;
            _page.Value = 1;
            //
            // _llxLabel
            //
            _llxLabel.AutoSize = true;
            _llxLabel.Anchor = AnchorStyles.Left;
            _llxLabel.Margin = new Padding(0, 8, 12, 6);
            _llxLabel.Name = "_llxLabel";
            _llxLabel.Text = "Lower-left X:";
            //
            // _llx
            //
            _llx.Anchor = AnchorStyles.Left;
            _llx.DecimalPlaces = 2;
            _llx.Margin = new Padding(0, 4, 0, 4);
            _llx.Maximum = 1000000;
            _llx.Name = "_llx";
            _llx.Size = new Size(120, 23);
            _llx.TextAlign = HorizontalAlignment.Right;
            //
            // _llyLabel
            //
            _llyLabel.AutoSize = true;
            _llyLabel.Anchor = AnchorStyles.Left;
            _llyLabel.Margin = new Padding(0, 8, 12, 6);
            _llyLabel.Name = "_llyLabel";
            _llyLabel.Text = "Lower-left Y:";
            //
            // _lly
            //
            _lly.Anchor = AnchorStyles.Left;
            _lly.DecimalPlaces = 2;
            _lly.Margin = new Padding(0, 4, 0, 4);
            _lly.Maximum = 1000000;
            _lly.Name = "_lly";
            _lly.Size = new Size(120, 23);
            _lly.TextAlign = HorizontalAlignment.Right;
            //
            // _urxLabel
            //
            _urxLabel.AutoSize = true;
            _urxLabel.Anchor = AnchorStyles.Left;
            _urxLabel.Margin = new Padding(0, 8, 12, 6);
            _urxLabel.Name = "_urxLabel";
            _urxLabel.Text = "Upper-right X:";
            //
            // _urx
            //
            _urx.Anchor = AnchorStyles.Left;
            _urx.DecimalPlaces = 2;
            _urx.Margin = new Padding(0, 4, 0, 4);
            _urx.Maximum = 1000000;
            _urx.Name = "_urx";
            _urx.Size = new Size(120, 23);
            _urx.TextAlign = HorizontalAlignment.Right;
            //
            // _uryLabel
            //
            _uryLabel.AutoSize = true;
            _uryLabel.Anchor = AnchorStyles.Left;
            _uryLabel.Margin = new Padding(0, 8, 12, 6);
            _uryLabel.Name = "_uryLabel";
            _uryLabel.Text = "Upper-right Y:";
            //
            // _ury
            //
            _ury.Anchor = AnchorStyles.Left;
            _ury.DecimalPlaces = 2;
            _ury.Margin = new Padding(0, 4, 0, 4);
            _ury.Maximum = 1000000;
            _ury.Name = "_ury";
            _ury.Size = new Size(120, 23);
            _ury.TextAlign = HorizontalAlignment.Right;
            //
            // _replacementLabel
            //
            _replacementLabel.AutoSize = true;
            _replacementLabel.Anchor = AnchorStyles.Left;
            _replacementLabel.Margin = new Padding(0, 8, 12, 6);
            _replacementLabel.Name = "_replacementLabel";
            _replacementLabel.Text = "Replacement:";
            //
            // _replacement
            //
            _replacement.Anchor = AnchorStyles.Left;
            _replacement.Margin = new Padding(0, 4, 0, 4);
            _replacement.Name = "_replacement";
            _replacement.Size = new Size(360, 23);
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
            // SpanEditForm
            //
            AcceptButton = _ok;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = _cancel;
            ClientSize = new Size(420, 200);
            Controls.Add(_root);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SpanEditForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Edit Redaction";
            _root.ResumeLayout(false);
            _root.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)_paragraph).EndInit();
            ((System.ComponentModel.ISupportInitialize)_start).EndInit();
            ((System.ComponentModel.ISupportInitialize)_stop).EndInit();
            ((System.ComponentModel.ISupportInitialize)_page).EndInit();
            ((System.ComponentModel.ISupportInitialize)_llx).EndInit();
            ((System.ComponentModel.ISupportInitialize)_lly).EndInit();
            ((System.ComponentModel.ISupportInitialize)_urx).EndInit();
            ((System.ComponentModel.ISupportInitialize)_ury).EndInit();
            _buttons.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TableLayoutPanel _root;
        private Label _paragraphLabel;
        private NumericUpDown _paragraph;
        private Label _startLabel;
        private NumericUpDown _start;
        private Label _stopLabel;
        private NumericUpDown _stop;
        private Label _pageLabel;
        private NumericUpDown _page;
        private Label _llxLabel;
        private NumericUpDown _llx;
        private Label _llyLabel;
        private NumericUpDown _lly;
        private Label _urxLabel;
        private NumericUpDown _urx;
        private Label _uryLabel;
        private NumericUpDown _ury;
        private Label _replacementLabel;
        private TextBox _replacement;
        private FlowLayoutPanel _buttons;
        private Button _ok;
        private Button _cancel;
    }
}
