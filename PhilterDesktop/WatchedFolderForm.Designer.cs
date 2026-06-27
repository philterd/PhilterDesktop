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
    partial class WatchedFolderForm
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
            folderLabel = new Label();
            _folder = new TextBox();
            _browseFolder = new Button();
            policyLabel = new Label();
            contextLabel = new Label();
            _policy = new ComboBox();
            _context = new ComboBox();
            fileTypesLabel = new Label();
            _typePdf = new CheckBox();
            _typeDocx = new CheckBox();
            _typeTxt = new CheckBox();
            _typeRtf = new CheckBox();
            _typeSpreadsheet = new CheckBox();
            _typeEmail = new CheckBox();
            _includeSubfolders = new CheckBox();
            _highlight = new CheckBox();
            _notify = new CheckBox();
            outputLabel = new Label();
            _output = new TextBox();
            _browseOutput = new Button();
            _ok = new Button();
            _cancel = new Button();
            SuspendLayout();
            //
            // folderLabel
            //
            folderLabel.AutoSize = true;
            folderLabel.Location = new Point(12, 20);
            folderLabel.Name = "folderLabel";
            folderLabel.Text = "Folder to watch:";
            //
            // _folder
            //
            _folder.Location = new Point(12, 44);
            _folder.Name = "_folder";
            _folder.Size = new Size(416, 23);
            _folder.TabIndex = 0;
            //
            // _browseFolder
            //
            _browseFolder.Location = new Point(436, 42);
            _browseFolder.Name = "_browseFolder";
            _browseFolder.Size = new Size(110, 34);
            _browseFolder.TabIndex = 1;
            _browseFolder.Text = "Browse…";
            _browseFolder.UseVisualStyleBackColor = true;
            _browseFolder.Click += BrowseFolder_Click;
            //
            // policyLabel
            //
            policyLabel.AutoSize = true;
            policyLabel.Location = new Point(12, 84);
            policyLabel.Name = "policyLabel";
            policyLabel.Text = "Policy:";
            //
            // contextLabel
            //
            contextLabel.AutoSize = true;
            contextLabel.Location = new Point(290, 84);
            contextLabel.Name = "contextLabel";
            contextLabel.Text = "Context:";
            //
            // _policy
            //
            _policy.DropDownStyle = ComboBoxStyle.DropDownList;
            _policy.Location = new Point(12, 104);
            _policy.Name = "_policy";
            _policy.Size = new Size(255, 23);
            _policy.TabIndex = 2;
            //
            // _context
            //
            _context.DropDownStyle = ComboBoxStyle.DropDownList;
            _context.Location = new Point(290, 104);
            _context.Name = "_context";
            _context.Size = new Size(256, 23);
            _context.TabIndex = 3;
            //
            // fileTypesLabel
            //
            fileTypesLabel.AutoSize = true;
            fileTypesLabel.Location = new Point(12, 148);
            fileTypesLabel.Name = "fileTypesLabel";
            fileTypesLabel.Text = "File types:";
            //
            // _typePdf
            //
            _typePdf.AutoSize = true;
            _typePdf.Location = new Point(95, 146);
            _typePdf.Name = "_typePdf";
            _typePdf.TabIndex = 4;
            _typePdf.Text = "PDF (.pdf)";
            _typePdf.UseVisualStyleBackColor = true;
            //
            // _typeDocx
            //
            _typeDocx.AutoSize = true;
            _typeDocx.Location = new Point(190, 146);
            _typeDocx.Name = "_typeDocx";
            _typeDocx.TabIndex = 5;
            _typeDocx.Text = "Word (.docx)";
            _typeDocx.UseVisualStyleBackColor = true;
            //
            // _typeTxt
            //
            _typeTxt.AutoSize = true;
            _typeTxt.Location = new Point(310, 146);
            _typeTxt.Name = "_typeTxt";
            _typeTxt.TabIndex = 6;
            _typeTxt.Text = "Text (.txt)";
            _typeTxt.UseVisualStyleBackColor = true;
            //
            // _typeRtf
            //
            _typeRtf.AutoSize = true;
            _typeRtf.Location = new Point(95, 172);
            _typeRtf.Name = "_typeRtf";
            _typeRtf.TabIndex = 7;
            _typeRtf.Text = "Rich Text (.rtf)";
            _typeRtf.UseVisualStyleBackColor = true;
            //
            // _typeSpreadsheet
            //
            _typeSpreadsheet.AutoSize = true;
            _typeSpreadsheet.Location = new Point(200, 172);
            _typeSpreadsheet.Name = "_typeSpreadsheet";
            _typeSpreadsheet.TabIndex = 8;
            _typeSpreadsheet.Text = "Spreadsheet (.xlsx, .csv)";
            _typeSpreadsheet.UseVisualStyleBackColor = true;
            //
            // _typeEmail
            //
            _typeEmail.AutoSize = true;
            _typeEmail.Location = new Point(375, 172);
            _typeEmail.Name = "_typeEmail";
            _typeEmail.TabIndex = 9;
            _typeEmail.Text = "Email (.eml, .msg)";
            _typeEmail.UseVisualStyleBackColor = true;
            //
            // _includeSubfolders
            //
            _includeSubfolders.AutoSize = true;
            _includeSubfolders.Location = new Point(12, 208);
            _includeSubfolders.Name = "_includeSubfolders";
            _includeSubfolders.TabIndex = 9;
            _includeSubfolders.Text = "Include subfolders";
            _includeSubfolders.UseVisualStyleBackColor = true;
            //
            // _highlight
            //
            _highlight.AutoSize = true;
            _highlight.Location = new Point(12, 238);
            _highlight.Name = "_highlight";
            _highlight.TabIndex = 10;
            _highlight.Text = "Highlight redactions in Word (.docx) documents";
            _highlight.UseVisualStyleBackColor = true;
            //
            // _notify
            //
            _notify.AutoSize = true;
            _notify.Location = new Point(12, 268);
            _notify.Name = "_notify";
            _notify.TabIndex = 11;
            _notify.Text = "Show a notification when a file is redacted";
            _notify.UseVisualStyleBackColor = true;
            //
            // outputLabel
            //
            outputLabel.AutoSize = true;
            outputLabel.Location = new Point(12, 308);
            outputLabel.Name = "outputLabel";
            outputLabel.Text = "Output folder:";
            //
            // _output
            //
            _output.Location = new Point(12, 330);
            _output.Name = "_output";
            _output.Size = new Size(416, 23);
            _output.TabIndex = 12;
            //
            // _browseOutput
            //
            _browseOutput.Location = new Point(436, 328);
            _browseOutput.Name = "_browseOutput";
            _browseOutput.Size = new Size(110, 34);
            _browseOutput.TabIndex = 13;
            _browseOutput.Text = "Browse…";
            _browseOutput.UseVisualStyleBackColor = true;
            _browseOutput.Click += BrowseOutput_Click;
            //
            // _ok
            //
            _ok.Location = new Point(316, 384);
            _ok.Name = "_ok";
            _ok.Size = new Size(110, 34);
            _ok.TabIndex = 14;
            _ok.Text = "OK";
            _ok.UseVisualStyleBackColor = true;
            _ok.Click += OkButton_Click;
            //
            // _cancel
            //
            _cancel.DialogResult = DialogResult.Cancel;
            _cancel.Location = new Point(436, 384);
            _cancel.Name = "_cancel";
            _cancel.Size = new Size(110, 34);
            _cancel.TabIndex = 15;
            _cancel.Text = "Cancel";
            _cancel.UseVisualStyleBackColor = true;
            //
            // WatchedFolderForm
            //
            AcceptButton = _ok;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = _cancel;
            ClientSize = new Size(560, 442);
            Controls.Add(folderLabel);
            Controls.Add(_folder);
            Controls.Add(_browseFolder);
            Controls.Add(policyLabel);
            Controls.Add(contextLabel);
            Controls.Add(_policy);
            Controls.Add(_context);
            Controls.Add(fileTypesLabel);
            Controls.Add(_typePdf);
            Controls.Add(_typeDocx);
            Controls.Add(_typeTxt);
            Controls.Add(_typeRtf);
            Controls.Add(_typeSpreadsheet);
            Controls.Add(_typeEmail);
            Controls.Add(_includeSubfolders);
            Controls.Add(_highlight);
            Controls.Add(_notify);
            Controls.Add(outputLabel);
            Controls.Add(_output);
            Controls.Add(_browseOutput);
            Controls.Add(_ok);
            Controls.Add(_cancel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "WatchedFolderForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Add Watched Folder";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label folderLabel;
        private TextBox _folder;
        private Button _browseFolder;
        private Label policyLabel;
        private Label contextLabel;
        private ComboBox _policy;
        private ComboBox _context;
        private Label fileTypesLabel;
        private CheckBox _typePdf;
        private CheckBox _typeDocx;
        private CheckBox _typeTxt;
        private CheckBox _typeRtf;
        private CheckBox _typeSpreadsheet;
        private CheckBox _typeEmail;
        private CheckBox _includeSubfolders;
        private CheckBox _highlight;
        private CheckBox _notify;
        private Label outputLabel;
        private TextBox _output;
        private Button _browseOutput;
        private Button _ok;
        private Button _cancel;
    }
}
