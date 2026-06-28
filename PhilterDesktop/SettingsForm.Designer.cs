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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            groupBoxOutput = new GroupBox();
            btnBrowse = new Button();
            txtCustomFolder = new TextBox();
            radioCustomFolder = new RadioButton();
            radioOriginalLocation = new RadioButton();
            lblSuffix = new Label();
            txtSuffix = new TextBox();
            chkScrubMetadata = new CheckBox();
            chkScrubComments = new CheckBox();
            chkScrubTrackedChanges = new CheckBox();
            chkScrubHiddenText = new CheckBox();
            lblWordInfo = new Label();
            tabWord = new TabPage();
            groupBoxLogging = new GroupBox();
            btnOpenLog = new Button();
            btnClearLog = new Button();
            chkEnableLogging = new CheckBox();
            btnSave = new Button();
            btnCancel = new Button();
            tabControl = new TabControl();
            tabGeneral = new TabPage();
            chkContextMenu = new CheckBox();
            lblContextMenuHint = new Label();
            tabNotifications = new TabPage();
            chkShowNotifications = new CheckBox();
            lblNotificationsHint = new Label();
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
            lblConcurrency = new Label();
            cmbConcurrency = new ComboBox();
            tabSecurity = new TabPage();
            lblSecurityInfo = new Label();
            chkPassphrase = new CheckBox();
            btnChangePassphrase = new Button();
            lblSecurityStatus = new Label();
            chkVerifyAfterRedaction = new CheckBox();
            rdoVerifySamePolicy = new RadioButton();
            rdoVerifyBroadPolicy = new RadioButton();
            lblVerifyHint = new Label();
            tabPdf = new TabPage();
            chkOcrScannedPdfs = new CheckBox();
            lblOcrInfo = new Label();
            btnOcrAdvanced = new Button();
            groupBoxOutput.SuspendLayout();
            tabWord.SuspendLayout();
            groupBoxLogging.SuspendLayout();
            tabControl.SuspendLayout();
            tabGeneral.SuspendLayout();
            tabNotifications.SuspendLayout();
            tabWatched.SuspendLayout();
            tabSecurity.SuspendLayout();
            tabPdf.SuspendLayout();
            SuspendLayout();
            // 
            // groupBoxOutput
            // 
            groupBoxOutput.Controls.Add(btnBrowse);
            groupBoxOutput.Controls.Add(txtCustomFolder);
            groupBoxOutput.Controls.Add(radioCustomFolder);
            groupBoxOutput.Controls.Add(radioOriginalLocation);
            groupBoxOutput.Controls.Add(lblSuffix);
            groupBoxOutput.Controls.Add(txtSuffix);
            groupBoxOutput.Location = new Point(3, 5);
            groupBoxOutput.Name = "groupBoxOutput";
            groupBoxOutput.Size = new Size(560, 163);
            groupBoxOutput.TabIndex = 0;
            groupBoxOutput.TabStop = false;
            groupBoxOutput.Text = "Output Location";
            // 
            // btnBrowse
            // 
            btnBrowse.Location = new Point(440, 73);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(110, 34);
            btnBrowse.TabIndex = 3;
            btnBrowse.Text = "Browse...";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += BtnBrowse_Click;
            // 
            // txtCustomFolder
            // 
            txtCustomFolder.Location = new Point(40, 78);
            txtCustomFolder.Name = "txtCustomFolder";
            txtCustomFolder.Size = new Size(394, 23);
            txtCustomFolder.TabIndex = 2;
            // 
            // radioCustomFolder
            // 
            radioCustomFolder.AutoSize = true;
            radioCustomFolder.Location = new Point(20, 55);
            radioCustomFolder.Name = "radioCustomFolder";
            radioCustomFolder.Size = new Size(230, 19);
            radioCustomFolder.TabIndex = 1;
            radioCustomFolder.Text = "Output to the following custom folder:";
            radioCustomFolder.UseVisualStyleBackColor = true;
            // 
            // radioOriginalLocation
            // 
            radioOriginalLocation.AutoSize = true;
            radioOriginalLocation.Checked = true;
            radioOriginalLocation.Location = new Point(20, 30);
            radioOriginalLocation.Name = "radioOriginalLocation";
            radioOriginalLocation.Size = new Size(166, 19);
            radioOriginalLocation.TabIndex = 0;
            radioOriginalLocation.TabStop = true;
            radioOriginalLocation.Text = "Output to original location";
            radioOriginalLocation.UseVisualStyleBackColor = true;
            radioOriginalLocation.CheckedChanged += RadioOriginalLocation_CheckedChanged;
            // 
            // lblSuffix
            // 
            lblSuffix.AutoSize = true;
            lblSuffix.Location = new Point(20, 122);
            lblSuffix.Name = "lblSuffix";
            lblSuffix.Size = new Size(142, 15);
            lblSuffix.TabIndex = 4;
            lblSuffix.Text = "Redacted file name suffix:";
            // 
            // txtSuffix
            // 
            txtSuffix.Location = new Point(200, 119);
            txtSuffix.Name = "txtSuffix";
            txtSuffix.Size = new Size(200, 23);
            txtSuffix.TabIndex = 5;
            // 
            // chkScrubMetadata
            // 
            chkScrubMetadata.AutoSize = true;
            chkScrubMetadata.Location = new Point(13, 65);
            chkScrubMetadata.Name = "chkScrubMetadata";
            chkScrubMetadata.Size = new Size(441, 19);
            chkScrubMetadata.TabIndex = 1;
            chkScrubMetadata.Text = "Remove document metadata (author, company, title, keywords, custom fields)";
            chkScrubMetadata.UseVisualStyleBackColor = true;
            // 
            // chkScrubComments
            // 
            chkScrubComments.AutoSize = true;
            chkScrubComments.Location = new Point(13, 93);
            chkScrubComments.Name = "chkScrubComments";
            chkScrubComments.Size = new Size(176, 19);
            chkScrubComments.TabIndex = 2;
            chkScrubComments.Text = "Remove reviewer comments";
            chkScrubComments.UseVisualStyleBackColor = true;
            // 
            // chkScrubTrackedChanges
            // 
            chkScrubTrackedChanges.AutoSize = true;
            chkScrubTrackedChanges.Location = new Point(13, 121);
            chkScrubTrackedChanges.Name = "chkScrubTrackedChanges";
            chkScrubTrackedChanges.Size = new Size(275, 19);
            chkScrubTrackedChanges.TabIndex = 3;
            chkScrubTrackedChanges.Text = "Accept and remove tracked changes (revisions)";
            chkScrubTrackedChanges.UseVisualStyleBackColor = true;
            // 
            // chkScrubHiddenText
            // 
            chkScrubHiddenText.AutoSize = true;
            chkScrubHiddenText.Location = new Point(13, 149);
            chkScrubHiddenText.Name = "chkScrubHiddenText";
            chkScrubHiddenText.Size = new Size(131, 19);
            chkScrubHiddenText.TabIndex = 4;
            chkScrubHiddenText.Text = "Remove hidden text";
            chkScrubHiddenText.UseVisualStyleBackColor = true;
            // 
            // lblWordInfo
            // 
            lblWordInfo.AutoSize = true;
            lblWordInfo.ForeColor = SystemColors.GrayText;
            lblWordInfo.Location = new Point(13, 18);
            lblWordInfo.MaximumSize = new Size(530, 0);
            lblWordInfo.Name = "lblWordInfo";
            lblWordInfo.Size = new Size(528, 30);
            lblWordInfo.TabIndex = 0;
            lblWordInfo.Text = "When redacting Word (.docx) files, also strip these hidden information channels so a redacted copy doesn't leak through them. Applied to every redacted Word document.";
            // 
            // tabWord
            // 
            tabWord.Controls.Add(lblWordInfo);
            tabWord.Controls.Add(chkScrubMetadata);
            tabWord.Controls.Add(chkScrubComments);
            tabWord.Controls.Add(chkScrubTrackedChanges);
            tabWord.Controls.Add(chkScrubHiddenText);
            tabWord.Location = new Point(4, 24);
            tabWord.Name = "tabWord";
            tabWord.Padding = new Padding(3);
            tabWord.Size = new Size(565, 318);
            tabWord.TabIndex = 4;
            tabWord.Text = "Microsoft Word";
            tabWord.UseVisualStyleBackColor = true;
            // 
            // groupBoxLogging
            // 
            groupBoxLogging.Controls.Add(btnOpenLog);
            groupBoxLogging.Controls.Add(btnClearLog);
            groupBoxLogging.Controls.Add(chkEnableLogging);
            groupBoxLogging.Location = new Point(5, 174);
            groupBoxLogging.Name = "groupBoxLogging";
            groupBoxLogging.Size = new Size(560, 65);
            groupBoxLogging.TabIndex = 1;
            groupBoxLogging.TabStop = false;
            groupBoxLogging.Text = "Logging";
            // 
            // btnOpenLog
            // 
            btnOpenLog.AutoSize = true;
            btnOpenLog.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnOpenLog.Location = new Point(136, 24);
            btnOpenLog.Name = "btnOpenLog";
            btnOpenLog.Padding = new Padding(8, 4, 8, 4);
            btnOpenLog.Size = new Size(106, 33);
            btnOpenLog.TabIndex = 1;
            btnOpenLog.Text = "Open Log File";
            btnOpenLog.UseVisualStyleBackColor = true;
            btnOpenLog.Click += BtnOpenLog_Click;
            // 
            // btnClearLog
            // 
            btnClearLog.AutoSize = true;
            btnClearLog.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnClearLog.Location = new Point(257, 24);
            btnClearLog.Name = "btnClearLog";
            btnClearLog.Padding = new Padding(8, 4, 8, 4);
            btnClearLog.Size = new Size(104, 33);
            btnClearLog.TabIndex = 2;
            btnClearLog.Text = "Clear Log File";
            btnClearLog.UseVisualStyleBackColor = true;
            btnClearLog.Click += BtnClearLog_Click;
            // 
            // chkEnableLogging
            // 
            chkEnableLogging.AutoSize = true;
            chkEnableLogging.Location = new Point(20, 30);
            chkEnableLogging.Name = "chkEnableLogging";
            chkEnableLogging.Size = new Size(105, 19);
            chkEnableLogging.TabIndex = 0;
            chkEnableLogging.Text = "Enable logging";
            chkEnableLogging.UseVisualStyleBackColor = true;
            chkEnableLogging.CheckedChanged += ChkEnableLogging_CheckedChanged;
            // 
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSave.Location = new Point(346, 356);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(110, 34);
            btnSave.TabIndex = 1;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += BtnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.Location = new Point(462, 356);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(110, 34);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += BtnCancel_Click;
            // 
            // tabControl
            // 
            tabControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl.Controls.Add(tabGeneral);
            tabControl.Controls.Add(tabWord);
            tabControl.Controls.Add(tabPdf);
            tabControl.Controls.Add(tabNotifications);
            tabControl.Controls.Add(tabWatched);
            tabControl.Controls.Add(tabSecurity);
            tabControl.Location = new Point(6, 7);
            tabControl.Margin = new Padding(2);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(573, 346);
            tabControl.TabIndex = 0;
            // 
            // tabGeneral
            // 
            tabGeneral.Controls.Add(groupBoxOutput);
            tabGeneral.Controls.Add(groupBoxLogging);
            tabGeneral.Controls.Add(chkContextMenu);
            tabGeneral.Controls.Add(lblContextMenuHint);
            tabGeneral.Location = new Point(4, 24);
            tabGeneral.Margin = new Padding(2);
            tabGeneral.Name = "tabGeneral";
            tabGeneral.Padding = new Padding(2);
            tabGeneral.Size = new Size(565, 318);
            tabGeneral.TabIndex = 0;
            tabGeneral.Text = "General";
            tabGeneral.UseVisualStyleBackColor = true;
            // 
            // chkContextMenu
            // 
            chkContextMenu.AutoSize = true;
            chkContextMenu.Location = new Point(12, 256);
            chkContextMenu.Name = "chkContextMenu";
            chkContextMenu.Size = new Size(506, 19);
            chkContextMenu.TabIndex = 2;
            chkContextMenu.Text = "Add \"Redact with Philter Desktop\" to the Explorer right-click menu (all supported file types)";
            chkContextMenu.UseVisualStyleBackColor = true;
            chkContextMenu.CheckedChanged += ChkContextMenu_CheckedChanged;
            // 
            // lblContextMenuHint
            // 
            lblContextMenuHint.AutoSize = true;
            lblContextMenuHint.ForeColor = SystemColors.GrayText;
            lblContextMenuHint.Location = new Point(30, 288);
            lblContextMenuHint.Name = "lblContextMenuHint";
            lblContextMenuHint.Size = new Size(0, 15);
            lblContextMenuHint.TabIndex = 3;
            // 
            // tabNotifications
            // 
            tabNotifications.Controls.Add(chkShowNotifications);
            tabNotifications.Controls.Add(lblNotificationsHint);
            tabNotifications.Location = new Point(4, 24);
            tabNotifications.Margin = new Padding(2);
            tabNotifications.Name = "tabNotifications";
            tabNotifications.Padding = new Padding(2);
            tabNotifications.Size = new Size(565, 318);
            tabNotifications.TabIndex = 3;
            tabNotifications.Text = "Notifications";
            tabNotifications.UseVisualStyleBackColor = true;
            // 
            // chkShowNotifications
            // 
            chkShowNotifications.AutoSize = true;
            chkShowNotifications.Location = new Point(12, 18);
            chkShowNotifications.Name = "chkShowNotifications";
            chkShowNotifications.Size = new Size(346, 19);
            chkShowNotifications.TabIndex = 0;
            chkShowNotifications.Text = "Show a tray notification when a document finishes redacting";
            chkShowNotifications.UseVisualStyleBackColor = true;
            // 
            // lblNotificationsHint
            // 
            lblNotificationsHint.AutoSize = true;
            lblNotificationsHint.ForeColor = SystemColors.GrayText;
            lblNotificationsHint.Location = new Point(30, 41);
            lblNotificationsHint.MaximumSize = new Size(520, 0);
            lblNotificationsHint.Name = "lblNotificationsHint";
            lblNotificationsHint.Size = new Size(506, 30);
            lblNotificationsHint.TabIndex = 1;
            lblNotificationsHint.Text = "Notifications appear only when Philter Desktop's window is hidden in the tray or minimized — never while you're looking at it. Click a notification to open the folder with the finished files.";
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
            tabWatched.Controls.Add(lblConcurrency);
            tabWatched.Controls.Add(cmbConcurrency);
            tabWatched.Location = new Point(4, 24);
            tabWatched.Margin = new Padding(2);
            tabWatched.Name = "tabWatched";
            tabWatched.Padding = new Padding(2);
            tabWatched.Size = new Size(565, 318);
            tabWatched.TabIndex = 1;
            tabWatched.Text = "Watched Folders";
            tabWatched.UseVisualStyleBackColor = true;
            // 
            // listWatched
            // 
            listWatched.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            listWatched.Columns.AddRange(new ColumnHeader[] { colFolder, colPolicy, colContext, colOutput, colHighlight });
            listWatched.FullRowSelect = true;
            listWatched.GridLines = true;
            listWatched.Location = new Point(6, 7);
            listWatched.Margin = new Padding(2);
            listWatched.MultiSelect = false;
            listWatched.Name = "listWatched";
            listWatched.Size = new Size(557, 182);
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
            btnAddWatched.Location = new Point(6, 193);
            btnAddWatched.Margin = new Padding(2);
            btnAddWatched.Name = "btnAddWatched";
            btnAddWatched.Size = new Size(110, 34);
            btnAddWatched.TabIndex = 1;
            btnAddWatched.Text = "Add...";
            btnAddWatched.UseVisualStyleBackColor = true;
            btnAddWatched.Click += BtnAddWatched_Click;
            // 
            // btnEditWatched
            // 
            btnEditWatched.Enabled = false;
            btnEditWatched.Location = new Point(122, 193);
            btnEditWatched.Margin = new Padding(2);
            btnEditWatched.Name = "btnEditWatched";
            btnEditWatched.Size = new Size(110, 34);
            btnEditWatched.TabIndex = 2;
            btnEditWatched.Text = "Edit...";
            btnEditWatched.UseVisualStyleBackColor = true;
            btnEditWatched.Click += BtnEditWatched_Click;
            // 
            // btnRemoveWatched
            // 
            btnRemoveWatched.Enabled = false;
            btnRemoveWatched.Location = new Point(238, 193);
            btnRemoveWatched.Margin = new Padding(2);
            btnRemoveWatched.Name = "btnRemoveWatched";
            btnRemoveWatched.Size = new Size(110, 34);
            btnRemoveWatched.TabIndex = 3;
            btnRemoveWatched.Text = "Remove";
            btnRemoveWatched.UseVisualStyleBackColor = true;
            btnRemoveWatched.Click += BtnRemoveWatched_Click;
            // 
            // btnViewLog
            // 
            btnViewLog.Enabled = false;
            btnViewLog.Location = new Point(354, 193);
            btnViewLog.Margin = new Padding(2);
            btnViewLog.Name = "btnViewLog";
            btnViewLog.Size = new Size(110, 34);
            btnViewLog.TabIndex = 4;
            btnViewLog.Text = "View Log...";
            btnViewLog.UseVisualStyleBackColor = true;
            btnViewLog.Click += BtnViewLog_Click;
            // 
            // chkStartWithWindows
            // 
            chkStartWithWindows.AutoSize = true;
            chkStartWithWindows.Location = new Point(6, 230);
            chkStartWithWindows.Margin = new Padding(2);
            chkStartWithWindows.Name = "chkStartWithWindows";
            chkStartWithWindows.Size = new Size(444, 19);
            chkStartWithWindows.TabIndex = 5;
            chkStartWithWindows.Text = "Start Philter Desktop at sign-in (runs minimized to the tray and keeps watching)";
            chkStartWithWindows.UseVisualStyleBackColor = true;
            chkStartWithWindows.CheckedChanged += ChkStartWithWindows_CheckedChanged;
            // 
            // lblStartupHint
            // 
            lblStartupHint.AutoSize = true;
            lblStartupHint.ForeColor = SystemColors.GrayText;
            lblStartupHint.Location = new Point(20, 247);
            lblStartupHint.Margin = new Padding(2, 0, 2, 0);
            lblStartupHint.Name = "lblStartupHint";
            lblStartupHint.Size = new Size(0, 15);
            lblStartupHint.TabIndex = 6;
            // 
            // lblConcurrency
            // 
            lblConcurrency.AutoSize = true;
            lblConcurrency.Location = new Point(6, 266);
            lblConcurrency.Margin = new Padding(2, 0, 2, 0);
            lblConcurrency.Name = "lblConcurrency";
            lblConcurrency.Size = new Size(309, 15);
            lblConcurrency.TabIndex = 7;
            lblConcurrency.Text = "Watched-folder files to redact at once (usually leave at 1):";
            // 
            // cmbConcurrency
            // 
            cmbConcurrency.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbConcurrency.Items.AddRange(new object[] { "1", "2", "3", "4" });
            cmbConcurrency.Location = new Point(372, 262);
            cmbConcurrency.Name = "cmbConcurrency";
            cmbConcurrency.Size = new Size(55, 23);
            cmbConcurrency.TabIndex = 8;
            // 
            // tabSecurity
            // 
            tabSecurity.Controls.Add(lblSecurityInfo);
            tabSecurity.Controls.Add(chkPassphrase);
            tabSecurity.Controls.Add(btnChangePassphrase);
            tabSecurity.Controls.Add(lblSecurityStatus);
            tabSecurity.Controls.Add(chkVerifyAfterRedaction);
            tabSecurity.Controls.Add(rdoVerifySamePolicy);
            tabSecurity.Controls.Add(rdoVerifyBroadPolicy);
            tabSecurity.Controls.Add(lblVerifyHint);
            tabSecurity.Location = new Point(4, 24);
            tabSecurity.Name = "tabSecurity";
            tabSecurity.Padding = new Padding(3);
            tabSecurity.Size = new Size(565, 318);
            tabSecurity.TabIndex = 2;
            tabSecurity.Text = "Security";
            tabSecurity.UseVisualStyleBackColor = true;
            // 
            // lblSecurityInfo
            // 
            lblSecurityInfo.Location = new Point(12, 15);
            lblSecurityInfo.Name = "lblSecurityInfo";
            lblSecurityInfo.Size = new Size(540, 70);
            lblSecurityInfo.TabIndex = 0;
            lblSecurityInfo.Text = resources.GetString("lblSecurityInfo.Text");
            // 
            // chkPassphrase
            // 
            chkPassphrase.AutoSize = true;
            chkPassphrase.Location = new Point(12, 95);
            chkPassphrase.Name = "chkPassphrase";
            chkPassphrase.Size = new Size(250, 19);
            chkPassphrase.TabIndex = 1;
            chkPassphrase.Text = "Require a passphrase to open the database";
            chkPassphrase.UseVisualStyleBackColor = true;
            chkPassphrase.CheckedChanged += ChkPassphrase_CheckedChanged;
            // 
            // btnChangePassphrase
            // 
            btnChangePassphrase.Location = new Point(32, 124);
            btnChangePassphrase.Name = "btnChangePassphrase";
            btnChangePassphrase.Size = new Size(170, 34);
            btnChangePassphrase.TabIndex = 2;
            btnChangePassphrase.Text = "Change Passphrase…";
            btnChangePassphrase.UseVisualStyleBackColor = true;
            btnChangePassphrase.Click += BtnChangePassphrase_Click;
            // 
            // lblSecurityStatus
            // 
            lblSecurityStatus.AutoSize = true;
            lblSecurityStatus.ForeColor = SystemColors.GrayText;
            lblSecurityStatus.Location = new Point(12, 175);
            lblSecurityStatus.Name = "lblSecurityStatus";
            lblSecurityStatus.Size = new Size(0, 15);
            lblSecurityStatus.TabIndex = 3;
            // 
            // chkVerifyAfterRedaction
            // 
            chkVerifyAfterRedaction.AutoSize = true;
            chkVerifyAfterRedaction.Location = new Point(12, 200);
            chkVerifyAfterRedaction.Name = "chkVerifyAfterRedaction";
            chkVerifyAfterRedaction.Size = new Size(401, 19);
            chkVerifyAfterRedaction.TabIndex = 4;
            chkVerifyAfterRedaction.Text = "Verify each redaction by re-scanning the output for PII that may remain";
            chkVerifyAfterRedaction.UseVisualStyleBackColor = true;
            chkVerifyAfterRedaction.CheckedChanged += ChkVerifyAfterRedaction_CheckedChanged;
            // 
            // rdoVerifySamePolicy
            // 
            rdoVerifySamePolicy.AutoSize = true;
            rdoVerifySamePolicy.Location = new Point(32, 225);
            rdoVerifySamePolicy.Name = "rdoVerifySamePolicy";
            rdoVerifySamePolicy.Size = new Size(240, 19);
            rdoVerifySamePolicy.TabIndex = 5;
            rdoVerifySamePolicy.TabStop = true;
            rdoVerifySamePolicy.Text = "Scan with the same policy used to redact";
            rdoVerifySamePolicy.UseVisualStyleBackColor = true;
            // 
            // rdoVerifyBroadPolicy
            // 
            rdoVerifyBroadPolicy.AutoSize = true;
            rdoVerifyBroadPolicy.Location = new Point(32, 248);
            rdoVerifyBroadPolicy.Name = "rdoVerifyBroadPolicy";
            rdoVerifyBroadPolicy.Size = new Size(257, 19);
            rdoVerifyBroadPolicy.TabIndex = 6;
            rdoVerifyBroadPolicy.Text = "Scan with a broad policy (every detector on)";
            rdoVerifyBroadPolicy.UseVisualStyleBackColor = true;
            // 
            // lblVerifyHint
            // 
            lblVerifyHint.AutoSize = true;
            lblVerifyHint.ForeColor = SystemColors.GrayText;
            lblVerifyHint.Location = new Point(30, 273);
            lblVerifyHint.MaximumSize = new Size(520, 0);
            lblVerifyHint.Name = "lblVerifyHint";
            lblVerifyHint.Size = new Size(511, 45);
            lblVerifyHint.TabIndex = 7;
            lblVerifyHint.Text = resources.GetString("lblVerifyHint.Text");
            //
            // tabPdf
            //
            tabPdf.Controls.Add(lblOcrInfo);
            tabPdf.Controls.Add(chkOcrScannedPdfs);
            tabPdf.Controls.Add(btnOcrAdvanced);
            tabPdf.Location = new Point(4, 24);
            tabPdf.Name = "tabPdf";
            tabPdf.Padding = new Padding(3);
            tabPdf.Size = new Size(565, 318);
            tabPdf.TabIndex = 5;
            tabPdf.Text = "PDF";
            tabPdf.UseVisualStyleBackColor = true;
            //
            // chkOcrScannedPdfs
            //
            chkOcrScannedPdfs.AutoSize = true;
            chkOcrScannedPdfs.Location = new Point(13, 18);
            chkOcrScannedPdfs.Name = "chkOcrScannedPdfs";
            chkOcrScannedPdfs.Size = new Size(372, 19);
            chkOcrScannedPdfs.TabIndex = 0;
            chkOcrScannedPdfs.Text = "Read scanned (image-only) PDF pages with on-device OCR";
            chkOcrScannedPdfs.UseVisualStyleBackColor = true;
            chkOcrScannedPdfs.CheckedChanged += ChkOcrScannedPdfs_CheckedChanged;
            //
            // lblOcrInfo
            //
            lblOcrInfo.AutoSize = true;
            lblOcrInfo.ForeColor = SystemColors.GrayText;
            lblOcrInfo.Location = new Point(30, 41);
            lblOcrInfo.MaximumSize = new Size(520, 0);
            lblOcrInfo.Name = "lblOcrInfo";
            lblOcrInfo.Size = new Size(515, 60);
            lblOcrInfo.TabIndex = 1;
            lblOcrInfo.Text = "Scanned PDFs are images with no text to detect. When enabled, Philter Desktop recognizes the text on scanned pages on your own computer (nothing is uploaded) so PII can be found and redacted. OCR is slower and best-effort: it can miss low-quality scans and does not read handwriting, so always review the redacted file before sharing it.";
            //
            // btnOcrAdvanced
            //
            btnOcrAdvanced.Location = new Point(13, 165);
            btnOcrAdvanced.Name = "btnOcrAdvanced";
            btnOcrAdvanced.Size = new Size(160, 34);
            btnOcrAdvanced.TabIndex = 2;
            btnOcrAdvanced.Text = "Advanced…";
            btnOcrAdvanced.UseVisualStyleBackColor = true;
            btnOcrAdvanced.Click += BtnOcrAdvanced_Click;
            //
            // SettingsForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(584, 396);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(tabControl);
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(600, 405);
            Name = "SettingsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Settings";
            Load += SettingsForm_Load;
            groupBoxOutput.ResumeLayout(false);
            groupBoxOutput.PerformLayout();
            tabWord.ResumeLayout(false);
            tabWord.PerformLayout();
            groupBoxLogging.ResumeLayout(false);
            groupBoxLogging.PerformLayout();
            tabControl.ResumeLayout(false);
            tabGeneral.ResumeLayout(false);
            tabGeneral.PerformLayout();
            tabNotifications.ResumeLayout(false);
            tabNotifications.PerformLayout();
            tabWatched.ResumeLayout(false);
            tabWatched.PerformLayout();
            tabSecurity.ResumeLayout(false);
            tabSecurity.PerformLayout();
            tabPdf.ResumeLayout(false);
            tabPdf.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBoxOutput;
        private RadioButton radioOriginalLocation;
        private RadioButton radioCustomFolder;
        private TextBox txtCustomFolder;
        private Button btnBrowse;
        private Label lblSuffix;
        private TextBox txtSuffix;
        private GroupBox groupBoxLogging;
        private CheckBox chkEnableLogging;
        private Button btnOpenLog;
        private Button btnClearLog;
        private Button btnSave;
        private Button btnCancel;
        private TabControl tabControl;
        private TabPage tabGeneral;
        private TabPage tabNotifications;
        private CheckBox chkShowNotifications;
        private TabPage tabWord;
        private Label lblWordInfo;
        private CheckBox chkScrubMetadata;
        private CheckBox chkScrubComments;
        private CheckBox chkScrubTrackedChanges;
        private CheckBox chkScrubHiddenText;
        private CheckBox chkVerifyAfterRedaction;
        private RadioButton rdoVerifySamePolicy;
        private RadioButton rdoVerifyBroadPolicy;
        private Label lblVerifyHint;
        private Label lblNotificationsHint;
        private TabPage tabWatched;
        private Label lblConcurrency;
        private ComboBox cmbConcurrency;
        private TabPage tabSecurity;
        private Label lblSecurityInfo;
        private CheckBox chkPassphrase;
        private Button btnChangePassphrase;
        private Label lblSecurityStatus;
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
        private CheckBox chkContextMenu;
        private Label lblContextMenuHint;
        private TabPage tabPdf;
        private CheckBox chkOcrScannedPdfs;
        private Label lblOcrInfo;
        private Button btnOcrAdvanced;
    }
}