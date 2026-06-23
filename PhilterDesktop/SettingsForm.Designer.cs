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
            tabControl = new TabControl();
            tabGeneral = new TabPage();
            tabWatched = new TabPage();
            listWatched = new ListView();
            colFolder = new ColumnHeader();
            colPolicy = new ColumnHeader();
            colContext = new ColumnHeader();
            colOutput = new ColumnHeader();
            colHighlight = new ColumnHeader();
            btnAddWatched = new Button();
            btnEditWatched = new Button();
            btnRemoveWatched = new Button();
            btnViewLog = new Button();
            chkStartWithWindows = new CheckBox();
            lblStartupHint = new Label();
            tabControl.SuspendLayout();
            tabGeneral.SuspendLayout();
            tabWatched.SuspendLayout();
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
            groupBoxOutput.Location = new Point(4, 8);
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
            groupBoxLogging.Location = new Point(4, 216);
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
            // tabGeneral
            //
            tabGeneral.Controls.Add(groupBoxOutput);
            tabGeneral.Controls.Add(groupBoxLogging);
            tabGeneral.Location = new Point(4, 34);
            tabGeneral.Name = "tabGeneral";
            tabGeneral.Padding = new Padding(3);
            tabGeneral.Size = new Size(810, 432);
            tabGeneral.TabIndex = 0;
            tabGeneral.Text = "General";
            tabGeneral.UseVisualStyleBackColor = true;
            //
            // tabWatched
            //
            tabWatched.Controls.Add(listWatched);
            tabWatched.Controls.Add(btnAddWatched);
            tabWatched.Controls.Add(btnEditWatched);
            tabWatched.Controls.Add(btnRemoveWatched);
            tabWatched.Controls.Add(btnViewLog);
            tabWatched.Controls.Add(chkStartWithWindows);
            tabWatched.Controls.Add(lblStartupHint);
            tabWatched.Location = new Point(4, 34);
            tabWatched.Name = "tabWatched";
            tabWatched.Padding = new Padding(3);
            tabWatched.Size = new Size(810, 432);
            tabWatched.TabIndex = 1;
            tabWatched.Text = "Watched Folder";
            tabWatched.UseVisualStyleBackColor = true;
            //
            // listWatched
            //
            listWatched.Columns.AddRange(new ColumnHeader[] { colFolder, colPolicy, colContext, colOutput, colHighlight });
            listWatched.FullRowSelect = true;
            listWatched.GridLines = true;
            listWatched.Location = new Point(8, 12);
            listWatched.MultiSelect = false;
            listWatched.Name = "listWatched";
            listWatched.Size = new Size(794, 300);
            listWatched.TabIndex = 0;
            listWatched.UseCompatibleStateImageBehavior = false;
            listWatched.View = View.Details;
            listWatched.SelectedIndexChanged += ListWatched_SelectedIndexChanged;
            //
            // colFolder
            //
            colFolder.Text = "Watched Folder";
            colFolder.Width = 260;
            //
            // colPolicy
            //
            colPolicy.Text = "Policy";
            colPolicy.Width = 120;
            //
            // colContext
            //
            colContext.Text = "Context";
            colContext.Width = 120;
            //
            // colOutput
            //
            colOutput.Text = "Output Folder";
            colOutput.Width = 220;
            //
            // colHighlight
            //
            colHighlight.Text = "Highlight";
            colHighlight.Width = 70;
            //
            // btnAddWatched
            //
            btnAddWatched.Location = new Point(8, 322);
            btnAddWatched.Name = "btnAddWatched";
            btnAddWatched.Size = new Size(129, 47);
            btnAddWatched.TabIndex = 1;
            btnAddWatched.Text = "Add...";
            btnAddWatched.UseVisualStyleBackColor = true;
            btnAddWatched.Click += BtnAddWatched_Click;
            //
            // btnEditWatched
            //
            btnEditWatched.Enabled = false;
            btnEditWatched.Location = new Point(145, 322);
            btnEditWatched.Name = "btnEditWatched";
            btnEditWatched.Size = new Size(129, 47);
            btnEditWatched.TabIndex = 2;
            btnEditWatched.Text = "Edit...";
            btnEditWatched.UseVisualStyleBackColor = true;
            btnEditWatched.Click += BtnEditWatched_Click;
            //
            // btnRemoveWatched
            //
            btnRemoveWatched.Enabled = false;
            btnRemoveWatched.Location = new Point(282, 322);
            btnRemoveWatched.Name = "btnRemoveWatched";
            btnRemoveWatched.Size = new Size(129, 47);
            btnRemoveWatched.TabIndex = 3;
            btnRemoveWatched.Text = "Remove";
            btnRemoveWatched.UseVisualStyleBackColor = true;
            btnRemoveWatched.Click += BtnRemoveWatched_Click;
            //
            // btnViewLog
            //
            btnViewLog.Enabled = false;
            btnViewLog.Location = new Point(419, 322);
            btnViewLog.Name = "btnViewLog";
            btnViewLog.Size = new Size(129, 47);
            btnViewLog.TabIndex = 4;
            btnViewLog.Text = "View Log...";
            btnViewLog.UseVisualStyleBackColor = true;
            btnViewLog.Click += BtnViewLog_Click;
            //
            // chkStartWithWindows
            //
            chkStartWithWindows.AutoSize = true;
            chkStartWithWindows.Location = new Point(8, 384);
            chkStartWithWindows.Name = "chkStartWithWindows";
            chkStartWithWindows.TabIndex = 5;
            chkStartWithWindows.Text = "Start Philter Desktop at sign-in (runs minimized to the tray and keeps watching)";
            chkStartWithWindows.UseVisualStyleBackColor = true;
            chkStartWithWindows.CheckedChanged += ChkStartWithWindows_CheckedChanged;
            //
            // lblStartupHint
            //
            lblStartupHint.AutoSize = true;
            lblStartupHint.ForeColor = SystemColors.GrayText;
            lblStartupHint.Location = new Point(28, 412);
            lblStartupHint.Name = "lblStartupHint";
            lblStartupHint.Text = "";
            //
            // tabControl
            //
            tabControl.Controls.Add(tabGeneral);
            tabControl.Controls.Add(tabWatched);
            tabControl.Location = new Point(8, 12);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(818, 470);
            tabControl.TabIndex = 0;
            //
            // btnSave
            //
            btnSave.Location = new Point(557, 494);
            btnSave.Margin = new Padding(4, 5, 4, 5);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(129, 47);
            btnSave.TabIndex = 1;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += BtnSave_Click;
            //
            // btnCancel
            //
            btnCancel.Location = new Point(694, 494);
            btnCancel.Margin = new Padding(4, 5, 4, 5);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(129, 47);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += BtnCancel_Click;
            //
            // SettingsForm
            //
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(834, 553);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(tabControl);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4, 5, 4, 5);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SettingsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Settings";
            Load += SettingsForm_Load;
            tabControl.ResumeLayout(false);
            tabGeneral.ResumeLayout(false);
            tabWatched.ResumeLayout(false);
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
        private TabControl tabControl;
        private TabPage tabGeneral;
        private TabPage tabWatched;
        private ListView listWatched;
        private ColumnHeader colFolder;
        private ColumnHeader colPolicy;
        private ColumnHeader colContext;
        private ColumnHeader colOutput;
        private ColumnHeader colHighlight;
        private Button btnAddWatched;
        private Button btnEditWatched;
        private Button btnRemoveWatched;
        private Button btnViewLog;
        private CheckBox chkStartWithWindows;
        private Label lblStartupHint;
    }
}