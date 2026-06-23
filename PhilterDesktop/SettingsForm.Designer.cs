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
    partial class SettingsForm
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
            groupBoxOutput = new GroupBox();
            btnBrowse = new Button();
            txtCustomFolder = new TextBox();
            radioCustomFolder = new RadioButton();
            radioOriginalLocation = new RadioButton();
            groupBoxLogging = new GroupBox();
            btnOpenLog = new Button();
            chkEnableLogging = new CheckBox();
            btnSave = new Button();
            btnCancel = new Button();
            groupBoxOutput.SuspendLayout();
            groupBoxLogging.SuspendLayout();
            SuspendLayout();
            // 
            // groupBoxOutput
            // 
            groupBoxOutput.Controls.Add(btnBrowse);
            groupBoxOutput.Controls.Add(txtCustomFolder);
            groupBoxOutput.Controls.Add(radioCustomFolder);
            groupBoxOutput.Controls.Add(radioOriginalLocation);
            groupBoxOutput.Location = new Point(17, 20);
            groupBoxOutput.Margin = new Padding(4, 5, 4, 5);
            groupBoxOutput.Name = "groupBoxOutput";
            groupBoxOutput.Padding = new Padding(4, 5, 4, 5);
            groupBoxOutput.Size = new Size(800, 200);
            groupBoxOutput.TabIndex = 0;
            groupBoxOutput.TabStop = false;
            groupBoxOutput.Text = "Output Location";
            // 
            // btnBrowse
            // 
            btnBrowse.Location = new Point(657, 127);
            btnBrowse.Margin = new Padding(4, 5, 4, 5);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(129, 47);
            btnBrowse.TabIndex = 3;
            btnBrowse.Text = "Browse...";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += BtnBrowse_Click;
            // 
            // txtCustomFolder
            // 
            txtCustomFolder.Location = new Point(57, 130);
            txtCustomFolder.Margin = new Padding(4, 5, 4, 5);
            txtCustomFolder.Name = "txtCustomFolder";
            txtCustomFolder.Size = new Size(584, 31);
            txtCustomFolder.TabIndex = 2;
            // 
            // radioCustomFolder
            // 
            radioCustomFolder.AutoSize = true;
            radioCustomFolder.Location = new Point(29, 92);
            radioCustomFolder.Margin = new Padding(4, 5, 4, 5);
            radioCustomFolder.Name = "radioCustomFolder";
            radioCustomFolder.Size = new Size(345, 29);
            radioCustomFolder.TabIndex = 1;
            radioCustomFolder.Text = "Output to the following custom folder:";
            radioCustomFolder.UseVisualStyleBackColor = true;
            // 
            // radioOriginalLocation
            // 
            radioOriginalLocation.AutoSize = true;
            radioOriginalLocation.Checked = true;
            radioOriginalLocation.Location = new Point(29, 50);
            radioOriginalLocation.Margin = new Padding(4, 5, 4, 5);
            radioOriginalLocation.Name = "radioOriginalLocation";
            radioOriginalLocation.Size = new Size(248, 29);
            radioOriginalLocation.TabIndex = 0;
            radioOriginalLocation.TabStop = true;
            radioOriginalLocation.Text = "Output to original location";
            radioOriginalLocation.UseVisualStyleBackColor = true;
            radioOriginalLocation.CheckedChanged += RadioOriginalLocation_CheckedChanged;
            // 
            // groupBoxLogging
            // 
            groupBoxLogging.Controls.Add(btnOpenLog);
            groupBoxLogging.Controls.Add(chkEnableLogging);
            groupBoxLogging.Location = new Point(17, 230);
            groupBoxLogging.Margin = new Padding(4, 5, 4, 5);
            groupBoxLogging.Name = "groupBoxLogging";
            groupBoxLogging.Padding = new Padding(4, 5, 4, 5);
            groupBoxLogging.Size = new Size(800, 108);
            groupBoxLogging.TabIndex = 1;
            groupBoxLogging.TabStop = false;
            groupBoxLogging.Text = "Logging";
            // 
            // btnOpenLog
            // 
            btnOpenLog.AutoSize = true;
            btnOpenLog.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnOpenLog.Location = new Point(194, 40);
            btnOpenLog.Margin = new Padding(4, 5, 4, 5);
            btnOpenLog.Name = "btnOpenLog";
            btnOpenLog.Padding = new Padding(12, 6, 12, 6);
            btnOpenLog.Size = new Size(230, 47);
            btnOpenLog.TabIndex = 1;
            btnOpenLog.Text = "Open Log File";
            btnOpenLog.UseVisualStyleBackColor = true;
            btnOpenLog.Click += BtnOpenLog_Click;
            // 
            // chkEnableLogging
            // 
            chkEnableLogging.AutoSize = true;
            chkEnableLogging.Location = new Point(29, 50);
            chkEnableLogging.Margin = new Padding(4, 5, 4, 5);
            chkEnableLogging.Name = "chkEnableLogging";
            chkEnableLogging.Size = new Size(157, 29);
            chkEnableLogging.TabIndex = 0;
            chkEnableLogging.Text = "Enable logging";
            chkEnableLogging.UseVisualStyleBackColor = true;
            chkEnableLogging.CheckedChanged += ChkEnableLogging_CheckedChanged;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(557, 360);
            btnSave.Margin = new Padding(4, 5, 4, 5);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(129, 47);
            btnSave.TabIndex = 2;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += BtnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(694, 360);
            btnCancel.Margin = new Padding(4, 5, 4, 5);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(129, 47);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += BtnCancel_Click;
            // 
            // SettingsForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(834, 430);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(groupBoxLogging);
            Controls.Add(groupBoxOutput);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4, 5, 4, 5);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Settings";
            Load += SettingsForm_Load;
            groupBoxOutput.ResumeLayout(false);
            groupBoxOutput.PerformLayout();
            groupBoxLogging.ResumeLayout(false);
            groupBoxLogging.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBoxOutput;
        private RadioButton radioOriginalLocation;
        private RadioButton radioCustomFolder;
        private TextBox txtCustomFolder;
        private Button btnBrowse;
        private GroupBox groupBoxLogging;
        private CheckBox chkEnableLogging;
        private Button btnOpenLog;
        private Button btnSave;
        private Button btnCancel;
    }
}