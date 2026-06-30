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

namespace PhilterDesktop.PolicyEditing
{
    partial class TemplatePickerForm
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
            _list = new ListBox();
            _description = new Label();
            _disclaimer = new Label();
            _ok = new Button();
            _cancel = new Button();
            SuspendLayout();
            // 
            // _list
            // 
            _list.IntegralHeight = false;
            _list.Location = new Point(12, 12);
            _list.Name = "_list";
            _list.Size = new Size(220, 260);
            _list.TabIndex = 0;
            // 
            // _description
            // 
            _description.Location = new Point(244, 12);
            _description.Name = "_description";
            _description.Size = new Size(304, 131);
            _description.TabIndex = 1;
            // 
            // _disclaimer
            // 
            _disclaimer.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _disclaimer.ForeColor = Color.FromArgb(176, 92, 0);
            _disclaimer.Location = new Point(244, 155);
            _disclaimer.Name = "_disclaimer";
            _disclaimer.Size = new Size(304, 117);
            _disclaimer.TabIndex = 2;
            // 
            // _ok
            // 
            _ok.DialogResult = DialogResult.OK;
            _ok.Location = new Point(356, 282);
            _ok.Name = "_ok";
            _ok.Size = new Size(88, 30);
            _ok.TabIndex = 3;
            _ok.Text = "Cr&eate";
            // 
            // _cancel
            // 
            _cancel.DialogResult = DialogResult.Cancel;
            _cancel.Location = new Point(450, 282);
            _cancel.Name = "_cancel";
            _cancel.Size = new Size(88, 30);
            _cancel.TabIndex = 4;
            _cancel.Text = "&Cancel";
            // 
            // TemplatePickerForm
            // 
            AcceptButton = _ok;
            CancelButton = _cancel;
            ClientSize = new Size(560, 320);
            Controls.Add(_list);
            Controls.Add(_description);
            Controls.Add(_disclaimer);
            Controls.Add(_ok);
            Controls.Add(_cancel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "TemplatePickerForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "New Policy from Template";
            ResumeLayout(false);
        }

        #endregion

        private ListBox _list;
        private Label _description;
        private Label _disclaimer;
        private Button _ok;
        private Button _cancel;
    }
}
