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
            chkRedactHeadersFooters = new CheckBox();
            chkRedactCharts = new CheckBox();
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
            chkStartWithWindows = new CheckBox();
            chkContextMenu = new CheckBox();
            lblContextMenuHint = new Label();
            tabPdf = new TabPage();
            lblOcrInfo = new Label();
            chkOcrScannedPdfs = new CheckBox();
            btnOcrAdvanced = new Button();
            tabEmail = new TabPage();
            lblEmailInfo = new Label();
            chkScrubEmailHeaders = new CheckBox();
            chkRemoveCommonHeaders = new CheckBox();
            lblCommonHeadersInfo = new Label();
            chkRemoveDateHeader = new CheckBox();
            lblDateHeaderInfo = new Label();
            chkRemoveAttachments = new CheckBox();
            lblAttachmentsInfo = new Label();
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
            colIssues = new ColumnHeader();
            btnAddWatched = new Button();
            btnEditWatched = new Button();
            btnRemoveWatched = new Button();
            btnViewLog = new Button();
            lblStartupHint = new Label();
            lblConcurrency = new Label();
            cmbConcurrency = new ComboBox();
            tabLimits = new TabPage();
            lblLimitsIntro = new Label();
            lblMaxFileSize = new Label();
            numMaxFileSize = new NumericUpDown();
            lblMaxFileSizeHint = new Label();
            lblRegexTimeout = new Label();
            numRegexTimeout = new NumericUpDown();
            lblRegexTimeoutHint = new Label();
            tabSecurity = new TabPage();
            lblSecurityInfo = new Label();
            chkPassphrase = new CheckBox();
            btnChangePassphrase = new Button();
            lblSecurityStatus = new Label();
            chkVerifyAfterRedaction = new CheckBox();
            rdoVerifySamePolicy = new RadioButton();
            rdoVerifyBroadPolicy = new RadioButton();
            lblVerifyHint = new Label();
            groupBoxOutput.SuspendLayout();
            tabWord.SuspendLayout();
            groupBoxLogging.SuspendLayout();
            tabControl.SuspendLayout();
            tabGeneral.SuspendLayout();
            tabPdf.SuspendLayout();
            tabEmail.SuspendLayout();
            tabNotifications.SuspendLayout();
            tabWatched.SuspendLayout();
            tabLimits.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numMaxFileSize).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numRegexTimeout).BeginInit();
            tabSecurity.SuspendLayout();
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
            btnBrowse.Text = "&Browse...";
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
            chkScrubMetadata.Location = new Point(13, 88);
            chkScrubMetadata.Name = "chkScrubMetadata";
            chkScrubMetadata.Size = new Size(441, 19);
            chkScrubMetadata.TabIndex = 1;
            chkScrubMetadata.Text = "Remove document metadata (author, company, title, keywords, custom fields)";
            chkScrubMetadata.UseVisualStyleBackColor = true;
            // 
            // chkScrubComments
            // 
            chkScrubComments.AutoSize = true;
            chkScrubComments.Location = new Point(13, 116);
            chkScrubComments.Name = "chkScrubComments";
            chkScrubComments.Size = new Size(176, 19);
            chkScrubComments.TabIndex = 2;
            chkScrubComments.Text = "Remove reviewer comments";
            chkScrubComments.UseVisualStyleBackColor = true;
            // 
            // chkScrubTrackedChanges
            // 
            chkScrubTrackedChanges.AutoSize = true;
            chkScrubTrackedChanges.Location = new Point(13, 144);
            chkScrubTrackedChanges.Name = "chkScrubTrackedChanges";
            chkScrubTrackedChanges.Size = new Size(275, 19);
            chkScrubTrackedChanges.TabIndex = 3;
            chkScrubTrackedChanges.Text = "Accept and remove tracked changes (revisions)";
            chkScrubTrackedChanges.UseVisualStyleBackColor = true;
            // 
            // chkScrubHiddenText
            // 
            chkScrubHiddenText.AutoSize = true;
            chkScrubHiddenText.Location = new Point(13, 172);
            chkScrubHiddenText.Name = "chkScrubHiddenText";
            chkScrubHiddenText.Size = new Size(131, 19);
            chkScrubHiddenText.TabIndex = 4;
            chkScrubHiddenText.Text = "Remove hidden text";
            chkScrubHiddenText.UseVisualStyleBackColor = true;
            //
            // chkRedactHeadersFooters
            //
            chkRedactHeadersFooters.AutoSize = true;
            chkRedactHeadersFooters.Location = new Point(13, 200);
            chkRedactHeadersFooters.Name = "chkRedactHeadersFooters";
            chkRedactHeadersFooters.Size = new Size(360, 19);
            chkRedactHeadersFooters.TabIndex = 5;
            chkRedactHeadersFooters.Text = "Redact text in page headers and footers (Word and Excel)";
            chkRedactHeadersFooters.UseVisualStyleBackColor = true;
            //
            // chkRedactCharts
            //
            chkRedactCharts.AutoSize = true;
            chkRedactCharts.Location = new Point(13, 228);
            chkRedactCharts.Name = "chkRedactCharts";
            chkRedactCharts.Size = new Size(360, 19);
            chkRedactCharts.TabIndex = 6;
            chkRedactCharts.Text = "Redact charts — titles, labels, and cached data values (Word and Excel)";
            chkRedactCharts.UseVisualStyleBackColor = true;
            // 
            // lblWordInfo
            // 
            lblWordInfo.AutoSize = true;
            lblWordInfo.ForeColor = SystemColors.GrayText;
            lblWordInfo.Location = new Point(13, 18);
            lblWordInfo.MaximumSize = new Size(530, 0);
            lblWordInfo.Name = "lblWordInfo";
            lblWordInfo.Size = new Size(529, 45);
            lblWordInfo.TabIndex = 0;
            lblWordInfo.Text = resources.GetString("lblWordInfo.Text");
            // 
            // tabWord
            // 
            tabWord.Controls.Add(lblWordInfo);
            tabWord.Controls.Add(chkScrubMetadata);
            tabWord.Controls.Add(chkScrubComments);
            tabWord.Controls.Add(chkScrubTrackedChanges);
            tabWord.Controls.Add(chkScrubHiddenText);
            tabWord.Controls.Add(chkRedactHeadersFooters);
            tabWord.Controls.Add(chkRedactCharts);
            tabWord.Location = new Point(4, 24);
            tabWord.Name = "tabWord";
            tabWord.Padding = new Padding(3);
            tabWord.Size = new Size(693, 318);
            tabWord.TabIndex = 4;
            tabWord.Text = "Microsoft Office";
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
            btnOpenLog.Text = "&Open Log File";
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
            btnClearLog.Text = "Clear &Log File";
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
            //
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSave.Location = new Point(474, 356);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(110, 34);
            btnSave.TabIndex = 1;
            btnSave.Text = "&Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += BtnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.Location = new Point(590, 356);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(110, 34);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "&Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += BtnCancel_Click;
            // 
            // tabControl
            // 
            tabControl.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl.Controls.Add(tabGeneral);
            tabControl.Controls.Add(tabWord);
            tabControl.Controls.Add(tabPdf);
            tabControl.Controls.Add(tabEmail);
            tabControl.Controls.Add(tabNotifications);
            tabControl.Controls.Add(tabWatched);
            tabControl.Controls.Add(tabLimits);
            tabControl.Controls.Add(tabSecurity);
            tabControl.Location = new Point(6, 7);
            tabControl.Margin = new Padding(2);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(701, 346);
            tabControl.TabIndex = 0;
            // 
            // tabGeneral
            // 
            tabGeneral.Controls.Add(chkStartWithWindows);
            tabGeneral.Controls.Add(groupBoxOutput);
            tabGeneral.Controls.Add(groupBoxLogging);
            tabGeneral.Controls.Add(chkContextMenu);
            tabGeneral.Controls.Add(lblContextMenuHint);
            tabGeneral.Location = new Point(4, 24);
            tabGeneral.Margin = new Padding(2);
            tabGeneral.Name = "tabGeneral";
            tabGeneral.Padding = new Padding(2);
            tabGeneral.Size = new Size(693, 318);
            tabGeneral.TabIndex = 0;
            tabGeneral.Text = "General";
            tabGeneral.UseVisualStyleBackColor = true;
            // 
            // chkStartWithWindows
            // 
            chkStartWithWindows.AutoSize = true;
            chkStartWithWindows.Location = new Point(12, 280);
            chkStartWithWindows.Margin = new Padding(2);
            chkStartWithWindows.Name = "chkStartWithWindows";
            chkStartWithWindows.Size = new Size(444, 19);
            chkStartWithWindows.TabIndex = 6;
            chkStartWithWindows.Text = "Start Philter Desktop at sign-in (runs minimized to the tray and keeps watching)";
            chkStartWithWindows.UseVisualStyleBackColor = true;
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
            // tabPdf
            // 
            tabPdf.Controls.Add(lblOcrInfo);
            tabPdf.Controls.Add(chkOcrScannedPdfs);
            tabPdf.Controls.Add(btnOcrAdvanced);
            tabPdf.Location = new Point(4, 24);
            tabPdf.Name = "tabPdf";
            tabPdf.Padding = new Padding(3);
            tabPdf.Size = new Size(693, 318);
            tabPdf.TabIndex = 5;
            tabPdf.Text = "PDF";
            tabPdf.UseVisualStyleBackColor = true;
            // 
            // lblOcrInfo
            // 
            lblOcrInfo.AutoSize = true;
            lblOcrInfo.ForeColor = SystemColors.GrayText;
            lblOcrInfo.Location = new Point(30, 41);
            lblOcrInfo.MaximumSize = new Size(520, 0);
            lblOcrInfo.Name = "lblOcrInfo";
            lblOcrInfo.Size = new Size(506, 60);
            lblOcrInfo.TabIndex = 1;
            lblOcrInfo.Text = resources.GetString("lblOcrInfo.Text");
            // 
            // chkOcrScannedPdfs
            // 
            chkOcrScannedPdfs.AutoSize = true;
            chkOcrScannedPdfs.Location = new Point(13, 18);
            chkOcrScannedPdfs.Name = "chkOcrScannedPdfs";
            chkOcrScannedPdfs.Size = new Size(338, 19);
            chkOcrScannedPdfs.TabIndex = 0;
            chkOcrScannedPdfs.Text = "Read scanned (image-only) PDF pages with on-device OCR";
            chkOcrScannedPdfs.UseVisualStyleBackColor = true;
            chkOcrScannedPdfs.CheckedChanged += ChkOcrScannedPdfs_CheckedChanged;
            // 
            // btnOcrAdvanced
            // 
            btnOcrAdvanced.Location = new Point(13, 165);
            btnOcrAdvanced.Name = "btnOcrAdvanced";
            btnOcrAdvanced.Size = new Size(160, 34);
            btnOcrAdvanced.TabIndex = 2;
            btnOcrAdvanced.Text = "Ad&vanced…";
            btnOcrAdvanced.UseVisualStyleBackColor = true;
            btnOcrAdvanced.Click += BtnOcrAdvanced_Click;
            // 
            // tabEmail
            // 
            tabEmail.Controls.Add(lblEmailInfo);
            tabEmail.Controls.Add(chkScrubEmailHeaders);
            tabEmail.Controls.Add(chkRemoveCommonHeaders);
            tabEmail.Controls.Add(lblCommonHeadersInfo);
            tabEmail.Controls.Add(chkRemoveDateHeader);
            tabEmail.Controls.Add(lblDateHeaderInfo);
            tabEmail.Controls.Add(chkRemoveAttachments);
            tabEmail.Controls.Add(lblAttachmentsInfo);
            tabEmail.Location = new Point(4, 24);
            tabEmail.Name = "tabEmail";
            tabEmail.Padding = new Padding(3);
            tabEmail.Size = new Size(693, 318);
            tabEmail.TabIndex = 6;
            tabEmail.Text = "Email";
            tabEmail.UseVisualStyleBackColor = true;
            // 
            // lblEmailInfo
            // 
            lblEmailInfo.AutoSize = true;
            lblEmailInfo.ForeColor = SystemColors.GrayText;
            lblEmailInfo.Location = new Point(30, 41);
            lblEmailInfo.MaximumSize = new Size(520, 0);
            lblEmailInfo.Name = "lblEmailInfo";
            lblEmailInfo.Size = new Size(520, 60);
            lblEmailInfo.TabIndex = 1;
            lblEmailInfo.Text = resources.GetString("lblEmailInfo.Text");
            // 
            // chkScrubEmailHeaders
            // 
            chkScrubEmailHeaders.AutoSize = true;
            chkScrubEmailHeaders.Location = new Point(13, 18);
            chkScrubEmailHeaders.Name = "chkScrubEmailHeaders";
            chkScrubEmailHeaders.Size = new Size(306, 19);
            chkScrubEmailHeaders.TabIndex = 0;
            chkScrubEmailHeaders.Text = "Remove technical email headers from redacted email";
            chkScrubEmailHeaders.UseVisualStyleBackColor = true;
            // 
            // chkRemoveCommonHeaders
            // 
            chkRemoveCommonHeaders.AutoSize = true;
            chkRemoveCommonHeaders.Location = new Point(13, 130);
            chkRemoveCommonHeaders.Name = "chkRemoveCommonHeaders";
            chkRemoveCommonHeaders.Size = new Size(373, 19);
            chkRemoveCommonHeaders.TabIndex = 2;
            chkRemoveCommonHeaders.Text = "Remove Bcc and other identity headers (Reply-To, Sender, Resent)";
            chkRemoveCommonHeaders.UseVisualStyleBackColor = true;
            // 
            // lblCommonHeadersInfo
            // 
            lblCommonHeadersInfo.AutoSize = true;
            lblCommonHeadersInfo.ForeColor = SystemColors.GrayText;
            lblCommonHeadersInfo.Location = new Point(30, 153);
            lblCommonHeadersInfo.MaximumSize = new Size(520, 0);
            lblCommonHeadersInfo.Name = "lblCommonHeadersInfo";
            lblCommonHeadersInfo.Size = new Size(519, 45);
            lblCommonHeadersInfo.TabIndex = 3;
            lblCommonHeadersInfo.Text = resources.GetString("lblCommonHeadersInfo.Text");
            //
            // chkRemoveDateHeader
            //
            chkRemoveDateHeader.AutoSize = true;
            chkRemoveDateHeader.Location = new Point(13, 210);
            chkRemoveDateHeader.Name = "chkRemoveDateHeader";
            chkRemoveDateHeader.Size = new Size(306, 19);
            chkRemoveDateHeader.TabIndex = 4;
            chkRemoveDateHeader.Text = "Remove the Date header from redacted email";
            chkRemoveDateHeader.UseVisualStyleBackColor = true;
            //
            // lblDateHeaderInfo
            //
            lblDateHeaderInfo.AutoSize = true;
            lblDateHeaderInfo.ForeColor = SystemColors.GrayText;
            lblDateHeaderInfo.Location = new Point(30, 233);
            lblDateHeaderInfo.MaximumSize = new Size(520, 0);
            lblDateHeaderInfo.Name = "lblDateHeaderInfo";
            lblDateHeaderInfo.Size = new Size(519, 30);
            lblDateHeaderInfo.TabIndex = 5;
            lblDateHeaderInfo.Text = "Drops the send date outright, so the time is removed no matter how it is formatted. Off by default.";
            //
            // chkRemoveAttachments
            //
            chkRemoveAttachments.AutoSize = true;
            chkRemoveAttachments.Location = new Point(13, 267);
            chkRemoveAttachments.Name = "chkRemoveAttachments";
            chkRemoveAttachments.Size = new Size(306, 19);
            chkRemoveAttachments.TabIndex = 6;
            chkRemoveAttachments.Text = "Remove attachments from redacted email";
            chkRemoveAttachments.UseVisualStyleBackColor = true;
            //
            // lblAttachmentsInfo
            //
            lblAttachmentsInfo.AutoSize = true;
            lblAttachmentsInfo.ForeColor = SystemColors.GrayText;
            lblAttachmentsInfo.Location = new Point(30, 290);
            lblAttachmentsInfo.MaximumSize = new Size(520, 0);
            lblAttachmentsInfo.Name = "lblAttachmentsInfo";
            lblAttachmentsInfo.Size = new Size(519, 15);
            lblAttachmentsInfo.TabIndex = 7;
            lblAttachmentsInfo.Text = "Deletes attachments entirely without redacting them (their content is never inspected). Off by default.";
            //
            // tabNotifications
            // 
            tabNotifications.Controls.Add(chkShowNotifications);
            tabNotifications.Controls.Add(lblNotificationsHint);
            tabNotifications.Location = new Point(4, 24);
            tabNotifications.Margin = new Padding(2);
            tabNotifications.Name = "tabNotifications";
            tabNotifications.Padding = new Padding(2);
            tabNotifications.Size = new Size(693, 318);
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
            tabWatched.Controls.Add(lblStartupHint);
            tabWatched.Controls.Add(lblConcurrency);
            tabWatched.Controls.Add(cmbConcurrency);
            tabWatched.Location = new Point(4, 24);
            tabWatched.Margin = new Padding(2);
            tabWatched.Name = "tabWatched";
            tabWatched.Padding = new Padding(2);
            tabWatched.Size = new Size(693, 318);
            tabWatched.TabIndex = 1;
            tabWatched.Text = "Watched Folders";
            tabWatched.UseVisualStyleBackColor = true;
            // 
            // listWatched
            // 
            listWatched.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            listWatched.Columns.AddRange(new ColumnHeader[] { colFolder, colPolicy, colContext, colOutput, colHighlight, colIssues });
            listWatched.FullRowSelect = true;
            listWatched.GridLines = true;
            listWatched.Location = new Point(6, 7);
            listWatched.Margin = new Padding(2);
            listWatched.MultiSelect = false;
            listWatched.Name = "listWatched";
            listWatched.Size = new Size(683, 182);
            listWatched.TabIndex = 0;
            listWatched.UseCompatibleStateImageBehavior = false;
            listWatched.View = View.Details;
            listWatched.ShowItemToolTips = true; // rows with failures carry a hover tooltip pointing at View Log
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
            // colIssues
            //
            colIssues.Text = "Issues";
            colIssues.Width = 120;
            //
            // btnAddWatched
            // 
            btnAddWatched.Location = new Point(6, 193);
            btnAddWatched.Margin = new Padding(2);
            btnAddWatched.Name = "btnAddWatched";
            btnAddWatched.Size = new Size(110, 34);
            btnAddWatched.TabIndex = 1;
            btnAddWatched.Text = "&Add...";
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
            btnEditWatched.Text = "&Edit...";
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
            btnRemoveWatched.Text = "&Remove";
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
            btnViewLog.Text = "&View Log...";
            btnViewLog.UseVisualStyleBackColor = true;
            btnViewLog.Click += BtnViewLog_Click;
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
            lblConcurrency.Location = new Point(6, 280);
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
            cmbConcurrency.Location = new Point(320, 277);
            cmbConcurrency.Name = "cmbConcurrency";
            cmbConcurrency.Size = new Size(77, 23);
            cmbConcurrency.TabIndex = 8;
            // 
            // tabLimits
            // 
            tabLimits.Controls.Add(lblLimitsIntro);
            tabLimits.Controls.Add(lblMaxFileSize);
            tabLimits.Controls.Add(numMaxFileSize);
            tabLimits.Controls.Add(lblMaxFileSizeHint);
            tabLimits.Controls.Add(lblRegexTimeout);
            tabLimits.Controls.Add(numRegexTimeout);
            tabLimits.Controls.Add(lblRegexTimeoutHint);
            tabLimits.Location = new Point(4, 24);
            tabLimits.Margin = new Padding(2);
            tabLimits.Name = "tabLimits";
            tabLimits.Padding = new Padding(2);
            tabLimits.Size = new Size(693, 318);
            tabLimits.TabIndex = 7;
            tabLimits.Text = "Limits";
            tabLimits.UseVisualStyleBackColor = true;
            // 
            // lblLimitsIntro
            // 
            lblLimitsIntro.AutoSize = true;
            lblLimitsIntro.Location = new Point(12, 16);
            lblLimitsIntro.Name = "lblLimitsIntro";
            lblLimitsIntro.Size = new Size(406, 15);
            lblLimitsIntro.TabIndex = 0;
            lblLimitsIntro.Text = "Safeguards that keep very large or unusual inputs from exhausting memory.";
            // 
            // lblMaxFileSize
            // 
            lblMaxFileSize.AutoSize = true;
            lblMaxFileSize.Location = new Point(12, 56);
            lblMaxFileSize.Name = "lblMaxFileSize";
            lblMaxFileSize.Size = new Size(481, 15);
            lblMaxFileSize.TabIndex = 1;
            lblMaxFileSize.Text = "Skip input files larger than this many megabytes (watched folders and the command line):";
            // 
            // numMaxFileSize
            // 
            numMaxFileSize.Increment = new decimal(new int[] { 50, 0, 0, 0 });
            numMaxFileSize.Location = new Point(12, 78);
            numMaxFileSize.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            numMaxFileSize.Name = "numMaxFileSize";
            numMaxFileSize.Size = new Size(90, 23);
            numMaxFileSize.TabIndex = 2;
            // 
            // lblMaxFileSizeHint
            // 
            lblMaxFileSizeHint.ForeColor = SystemColors.GrayText;
            lblMaxFileSizeHint.Location = new Point(12, 108);
            lblMaxFileSizeHint.Name = "lblMaxFileSizeHint";
            lblMaxFileSizeHint.Size = new Size(540, 50);
            lblMaxFileSizeHint.TabIndex = 3;
            lblMaxFileSizeHint.Text = resources.GetString("lblMaxFileSizeHint.Text");
            // 
            // lblRegexTimeout
            // 
            lblRegexTimeout.AutoSize = true;
            lblRegexTimeout.Location = new Point(12, 170);
            lblRegexTimeout.Name = "lblRegexTimeout";
            lblRegexTimeout.Size = new Size(280, 15);
            lblRegexTimeout.TabIndex = 4;
            lblRegexTimeout.Text = "Maximum time for one detection pattern (seconds):";
            // 
            // numRegexTimeout
            // 
            numRegexTimeout.Location = new Point(12, 192);
            numRegexTimeout.Maximum = new decimal(new int[] { 15, 0, 0, 0 });
            numRegexTimeout.Minimum = new decimal(new int[] { 5, 0, 0, 0 });
            numRegexTimeout.Name = "numRegexTimeout";
            numRegexTimeout.Size = new Size(90, 23);
            numRegexTimeout.TabIndex = 5;
            numRegexTimeout.Value = new decimal(new int[] { 5, 0, 0, 0 });
            // 
            // lblRegexTimeoutHint
            // 
            lblRegexTimeoutHint.AutoSize = true;
            lblRegexTimeoutHint.ForeColor = SystemColors.GrayText;
            lblRegexTimeoutHint.Location = new Point(12, 222);
            lblRegexTimeoutHint.MaximumSize = new Size(540, 0);
            lblRegexTimeoutHint.Name = "lblRegexTimeoutHint";
            lblRegexTimeoutHint.Size = new Size(538, 30);
            lblRegexTimeoutHint.TabIndex = 6;
            lblRegexTimeoutHint.Text = "Aborts a custom-identifier pattern that runs too long (for example a malformed or inefficient regular expression) so it can't hang redaction. Raise it only if a very large document needs more time.";
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
            tabSecurity.Size = new Size(693, 318);
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
            btnChangePassphrase.Text = "Change &Passphrase…";
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
            rdoVerifySamePolicy.Text = "Scan with the same policy used to redact (limited — can't catch missed PII types)";
            rdoVerifySamePolicy.UseVisualStyleBackColor = true;
            // 
            // rdoVerifyBroadPolicy
            // 
            rdoVerifyBroadPolicy.AutoSize = true;
            rdoVerifyBroadPolicy.Location = new Point(32, 248);
            rdoVerifyBroadPolicy.Name = "rdoVerifyBroadPolicy";
            rdoVerifyBroadPolicy.Size = new Size(257, 19);
            rdoVerifyBroadPolicy.TabIndex = 6;
            rdoVerifyBroadPolicy.Text = "Scan with a broad policy — every detector on (recommended)";
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
            // SettingsForm
            // 
            AcceptButton = btnSave;   // Enter saves
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel; // Esc cancels
            ClientSize = new Size(712, 396);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(tabControl);
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(728, 405);
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
            tabPdf.ResumeLayout(false);
            tabPdf.PerformLayout();
            tabEmail.ResumeLayout(false);
            tabEmail.PerformLayout();
            tabNotifications.ResumeLayout(false);
            tabNotifications.PerformLayout();
            tabWatched.ResumeLayout(false);
            tabWatched.PerformLayout();
            tabLimits.ResumeLayout(false);
            tabLimits.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numMaxFileSize).EndInit();
            ((System.ComponentModel.ISupportInitialize)numRegexTimeout).EndInit();
            tabSecurity.ResumeLayout(false);
            tabSecurity.PerformLayout();
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
        private CheckBox chkRedactHeadersFooters;
        private CheckBox chkRedactCharts;
        private CheckBox chkVerifyAfterRedaction;
        private RadioButton rdoVerifySamePolicy;
        private RadioButton rdoVerifyBroadPolicy;
        private Label lblVerifyHint;
        private Label lblNotificationsHint;
        private TabPage tabWatched;
        private Label lblConcurrency;
        private ComboBox cmbConcurrency;
        private TabPage tabLimits;
        private Label lblLimitsIntro;
        private Label lblMaxFileSize;
        private NumericUpDown numMaxFileSize;
        private Label lblMaxFileSizeHint;
        private Label lblRegexTimeout;
        private NumericUpDown numRegexTimeout;
        private Label lblRegexTimeoutHint;
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
        private ColumnHeader colIssues;
        private Button btnAddWatched;
        private Button btnEditWatched;
        private Button btnRemoveWatched;
        private Button btnViewLog;
        private Label lblStartupHint;
        private CheckBox chkContextMenu;
        private Label lblContextMenuHint;
        private TabPage tabPdf;
        private CheckBox chkOcrScannedPdfs;
        private Label lblOcrInfo;
        private Button btnOcrAdvanced;
        private TabPage tabEmail;
        private CheckBox chkScrubEmailHeaders;
        private Label lblEmailInfo;
        private CheckBox chkRemoveCommonHeaders;
        private Label lblCommonHeadersInfo;
        private CheckBox chkRemoveDateHeader;
        private Label lblDateHeaderInfo;
        private CheckBox chkRemoveAttachments;
        private Label lblAttachmentsInfo;
        private CheckBox chkStartWithWindows;
    }
}