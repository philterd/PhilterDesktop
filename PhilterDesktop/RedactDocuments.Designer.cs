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
    partial class RedactDocuments
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RedactDocuments));
            groupBoxFiles = new GroupBox();
            filesPanel = new Panel();
            btnClearAll = new Button();
            btnRemoveFile = new Button();
            filesListBox = new ListBox();
            labelDropFiles = new Label();
            btnSelectFiles = new Button();
            groupBoxSettings = new GroupBox();
            comboBoxContext = new ComboBox();
            labelContext = new Label();
            comboBoxPolicy = new ComboBox();
            labelPolicy = new Label();
            btnStartRedaction = new Button();
            btnClose = new Button();
            groupBoxOptions = new GroupBox();
            chkHighlightRedactions = new CheckBox();
            groupBoxFiles.SuspendLayout();
            filesPanel.SuspendLayout();
            groupBoxSettings.SuspendLayout();
            groupBoxOptions.SuspendLayout();
            SuspendLayout();
            // 
            // groupBoxFiles
            // 
            groupBoxFiles.Controls.Add(filesPanel);
            groupBoxFiles.Location = new Point(10, 9);
            groupBoxFiles.Margin = new Padding(3, 2, 3, 2);
            groupBoxFiles.Name = "groupBoxFiles";
            groupBoxFiles.Padding = new Padding(3, 2, 3, 2);
            groupBoxFiles.Size = new Size(665, 278);
            groupBoxFiles.TabIndex = 0;
            groupBoxFiles.TabStop = false;
            groupBoxFiles.Text = "Files to Redact";
            // 
            // filesPanel
            // 
            filesPanel.AllowDrop = true;
            filesPanel.Controls.Add(btnClearAll);
            filesPanel.Controls.Add(btnRemoveFile);
            filesPanel.Controls.Add(filesListBox);
            filesPanel.Controls.Add(labelDropFiles);
            filesPanel.Controls.Add(btnSelectFiles);
            filesPanel.Location = new Point(6, 19);
            filesPanel.Margin = new Padding(3, 2, 3, 2);
            filesPanel.Name = "filesPanel";
            filesPanel.Size = new Size(654, 248);
            filesPanel.TabIndex = 5;
            filesPanel.DragDrop += FilesControl_DragDrop;
            filesPanel.DragEnter += FilesControl_DragEnter;
            // 
            // btnClearAll
            // 
            btnClearAll.Location = new Point(544, 202);
            btnClearAll.Name = "btnClearAll";
            btnClearAll.Size = new Size(110, 34);
            btnClearAll.TabIndex = 4;
            btnClearAll.Text = "Clear All";
            btnClearAll.UseVisualStyleBackColor = true;
            btnClearAll.Click += BtnClearAll_Click;
            // 
            // btnRemoveFile
            // 
            btnRemoveFile.Enabled = false;
            btnRemoveFile.Location = new Point(428, 202);
            btnRemoveFile.Name = "btnRemoveFile";
            btnRemoveFile.Size = new Size(110, 34);
            btnRemoveFile.TabIndex = 3;
            btnRemoveFile.Text = "Remove File";
            btnRemoveFile.UseVisualStyleBackColor = true;
            btnRemoveFile.Click += BtnRemoveFile_Click;
            // 
            // filesListBox
            // 
            filesListBox.FormattingEnabled = true;
            filesListBox.HorizontalScrollbar = true;
            filesListBox.Location = new Point(13, 53);
            filesListBox.Margin = new Padding(3, 2, 3, 2);
            filesListBox.Name = "filesListBox";
            filesListBox.AccessibleName = "Files to redact";
            filesListBox.Size = new Size(627, 139);
            filesListBox.TabIndex = 2;
            filesListBox.SelectedIndexChanged += FilesListBox_SelectedIndexChanged;
            // 
            // labelDropFiles
            // 
            labelDropFiles.AutoSize = true;
            labelDropFiles.ForeColor = SystemColors.GrayText;
            labelDropFiles.Location = new Point(243, 26);
            labelDropFiles.Name = "labelDropFiles";
            labelDropFiles.Size = new Size(177, 15);
            labelDropFiles.TabIndex = 1;
            labelDropFiles.Text = "or drop files here to redact them";
            labelDropFiles.Click += labelDropFiles_Click;
            // 
            // btnSelectFiles
            // 
            btnSelectFiles.Location = new Point(13, 19);
            btnSelectFiles.Name = "btnSelectFiles";
            btnSelectFiles.Size = new Size(224, 28);
            btnSelectFiles.TabIndex = 0;
            btnSelectFiles.Text = "Select Files to Redact...";
            btnSelectFiles.UseVisualStyleBackColor = true;
            btnSelectFiles.Click += BtnSelectFiles_Click;
            // 
            // groupBoxSettings
            // 
            groupBoxSettings.Controls.Add(comboBoxContext);
            groupBoxSettings.Controls.Add(labelContext);
            groupBoxSettings.Controls.Add(comboBoxPolicy);
            groupBoxSettings.Controls.Add(labelPolicy);
            groupBoxSettings.Location = new Point(8, 311);
            groupBoxSettings.Margin = new Padding(3, 2, 3, 2);
            groupBoxSettings.Name = "groupBoxSettings";
            groupBoxSettings.Padding = new Padding(3, 2, 3, 2);
            groupBoxSettings.Size = new Size(665, 75);
            groupBoxSettings.TabIndex = 1;
            groupBoxSettings.TabStop = false;
            groupBoxSettings.Text = "Redaction Settings";
            // 
            // comboBoxContext
            // 
            comboBoxContext.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxContext.FormattingEnabled = true;
            comboBoxContext.Location = new Point(438, 26);
            comboBoxContext.Margin = new Padding(3, 2, 3, 2);
            comboBoxContext.Name = "comboBoxContext";
            comboBoxContext.AccessibleName = "Context";
            comboBoxContext.Size = new Size(210, 23);
            comboBoxContext.TabIndex = 3;
            comboBoxContext.DropDown += ComboBoxContext_DropDown;
            // 
            // labelContext
            // 
            labelContext.AutoSize = true;
            labelContext.Location = new Point(368, 29);
            labelContext.Name = "labelContext";
            labelContext.Size = new Size(51, 15);
            labelContext.TabIndex = 2;
            labelContext.Text = "Context:";
            // 
            // comboBoxPolicy
            // 
            comboBoxPolicy.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxPolicy.FormattingEnabled = true;
            comboBoxPolicy.Location = new Point(78, 26);
            comboBoxPolicy.Margin = new Padding(3, 2, 3, 2);
            comboBoxPolicy.Name = "comboBoxPolicy";
            comboBoxPolicy.AccessibleName = "Policy";
            comboBoxPolicy.Size = new Size(263, 23);
            comboBoxPolicy.TabIndex = 1;
            comboBoxPolicy.DropDown += ComboBoxPolicy_DropDown;
            // 
            // labelPolicy
            // 
            labelPolicy.AutoSize = true;
            labelPolicy.Location = new Point(18, 29);
            labelPolicy.Name = "labelPolicy";
            labelPolicy.Size = new Size(42, 15);
            labelPolicy.TabIndex = 0;
            labelPolicy.Text = "Policy:";
            //
            // groupBoxOptions
            //
            groupBoxOptions.Controls.Add(chkHighlightRedactions);
            groupBoxOptions.Location = new Point(8, 392);
            groupBoxOptions.Margin = new Padding(3, 2, 3, 2);
            groupBoxOptions.Name = "groupBoxOptions";
            groupBoxOptions.Padding = new Padding(3, 2, 3, 2);
            groupBoxOptions.Size = new Size(665, 58);
            groupBoxOptions.TabIndex = 4;
            groupBoxOptions.TabStop = false;
            groupBoxOptions.Text = "Options";
            //
            // chkHighlightRedactions
            //
            chkHighlightRedactions.AutoSize = true;
            chkHighlightRedactions.Location = new Point(18, 24);
            chkHighlightRedactions.Name = "chkHighlightRedactions";
            chkHighlightRedactions.Size = new Size(296, 19);
            chkHighlightRedactions.TabIndex = 0;
            chkHighlightRedactions.Text = "Highlight redactions in Word (.docx) documents";
            chkHighlightRedactions.UseVisualStyleBackColor = true;
            //
            // btnStartRedaction
            //
            btnStartRedaction.Location = new Point(448, 462);
            btnStartRedaction.Name = "btnStartRedaction";
            btnStartRedaction.Size = new Size(110, 34);
            btnStartRedaction.TabIndex = 2;
            btnStartRedaction.Text = "Add to Queue";
            btnStartRedaction.UseVisualStyleBackColor = true;
            btnStartRedaction.Click += BtnStartRedaction_Click;
            // 
            // btnClose
            // 
            btnClose.Location = new Point(564, 462);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(110, 34);
            btnClose.TabIndex = 3;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += BtnClose_Click;
            // 
            // RedactDocuments
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(686, 505);
            Controls.Add(btnClose);
            Controls.Add(btnStartRedaction);
            Controls.Add(groupBoxOptions);
            Controls.Add(groupBoxSettings);
            Controls.Add(groupBoxFiles);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(3, 2, 3, 2);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "RedactDocuments";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Redact Documents";
            Load += RedactDocumentsForm_Load;
            groupBoxFiles.ResumeLayout(false);
            filesPanel.ResumeLayout(false);
            filesPanel.PerformLayout();
            groupBoxSettings.ResumeLayout(false);
            groupBoxSettings.PerformLayout();
            groupBoxOptions.ResumeLayout(false);
            groupBoxOptions.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBoxFiles;
        private ListBox filesListBox;
        private Button btnSelectFiles;
        private Label labelDropFiles;
        private Button btnRemoveFile;
        private Button btnClearAll;
        private GroupBox groupBoxSettings;
        private ComboBox comboBoxPolicy;
        private Label labelPolicy;
        private ComboBox comboBoxContext;
        private Label labelContext;
        private Button btnStartRedaction;
        private Button btnClose;
        private Panel filesPanel;
        private GroupBox groupBoxOptions;
        private CheckBox chkHighlightRedactions;
    }
}