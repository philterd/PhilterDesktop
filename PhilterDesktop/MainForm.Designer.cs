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
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            clearRedactionHistoryToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparatorFile = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem1 = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            checkForUpdatesToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            statusStrip1 = new StatusStrip();
            toolStrip1 = new ToolStrip();
            toolStripButtonRedactDocuments = new ToolStripButton();
            toolStripButtonRedactPreview = new ToolStripButton();
            toolStripSeparator3 = new ToolStripSeparator();
            refreshToolStripButton = new ToolStripButton();
            toolStripSeparator4 = new ToolStripSeparator();
            policiesToolStripButton = new ToolStripButton();
            contextsToolStripButton = new ToolStripButton();
            toolStripSeparator9 = new ToolStripSeparator();
            settingsToolStripButton = new ToolStripButton();
            toolStripSeparator6 = new ToolStripSeparator();
            HelpToolStripButton = new ToolStripButton();
            toolStripSeparator8 = new ToolStripSeparator();
            listView1 = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            columnHeader4 = new ColumnHeader();
            columnHeader3 = new ColumnHeader();
            contextMenuStrip1 = new ContextMenuStrip(components);
            addFilesToRedactToolStripMenuItem = new ToolStripMenuItem();
            redactPreviewToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            removeToolStripMenuItem = new ToolStripMenuItem();
            removeAllToolStripMenuItem = new ToolStripMenuItem();
            removeCompletedToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator5 = new ToolStripSeparator();
            openRedactedFileToolStripMenuItem = new ToolStripMenuItem();
            openOriginalFileToolStripMenuItem = new ToolStripMenuItem();
            openContainingFolderToolStripMenuItem = new ToolStripMenuItem();
            modifyRedactionToolStripMenuItem = new ToolStripMenuItem();
            viewDiffToolStripMenuItem = new ToolStripMenuItem();
            viewDetailsToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator7 = new ToolStripSeparator();
            refreshToolStripMenuItem = new ToolStripMenuItem();
            imageList1 = new ImageList(components);
            redactionQueueTimer = new System.Windows.Forms.Timer(components);
            menuStrip1.SuspendLayout();
            toolStrip1.SuspendLayout();
            contextMenuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(24, 24);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new Padding(4, 1, 0, 1);
            menuStrip1.Size = new Size(866, 24);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { clearRedactionHistoryToolStripMenuItem, toolStripSeparatorFile, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 22);
            fileToolStripMenuItem.Text = "File";
            // 
            // clearRedactionHistoryToolStripMenuItem
            // 
            clearRedactionHistoryToolStripMenuItem.Name = "clearRedactionHistoryToolStripMenuItem";
            clearRedactionHistoryToolStripMenuItem.Size = new Size(207, 22);
            clearRedactionHistoryToolStripMenuItem.Text = "Clear Redaction History...";
            clearRedactionHistoryToolStripMenuItem.Click += clearRedactionHistoryToolStripMenuItem_Click;
            // 
            // toolStripSeparatorFile
            // 
            toolStripSeparatorFile.Name = "toolStripSeparatorFile";
            toolStripSeparatorFile.Size = new Size(204, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(207, 22);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { helpToolStripMenuItem1, toolStripSeparator1, checkForUpdatesToolStripMenuItem, aboutToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(44, 22);
            helpToolStripMenuItem.Text = "Help";
            // 
            // helpToolStripMenuItem1
            // 
            helpToolStripMenuItem1.Name = "helpToolStripMenuItem1";
            helpToolStripMenuItem1.Size = new Size(180, 22);
            helpToolStripMenuItem1.Text = "Help";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(177, 6);
            // 
            // checkForUpdatesToolStripMenuItem
            // 
            checkForUpdatesToolStripMenuItem.Name = "checkForUpdatesToolStripMenuItem";
            checkForUpdatesToolStripMenuItem.Size = new Size(180, 22);
            checkForUpdatesToolStripMenuItem.Text = "Check for Updates...";
            checkForUpdatesToolStripMenuItem.Click += checkForUpdatesToolStripMenuItem_Click;
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(180, 22);
            aboutToolStripMenuItem.Text = "About...";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(24, 24);
            statusStrip1.Location = new Point(0, 317);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(866, 22);
            statusStrip1.TabIndex = 3;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStrip1
            // 
            toolStrip1.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip1.ImageScalingSize = new Size(24, 24);
            toolStrip1.Items.AddRange(new ToolStripItem[] { toolStripButtonRedactDocuments, toolStripButtonRedactPreview, toolStripSeparator3, policiesToolStripButton, contextsToolStripButton, toolStripSeparator9, settingsToolStripButton, toolStripSeparator6, refreshToolStripButton, toolStripSeparator4, HelpToolStripButton });
            toolStrip1.Location = new Point(0, 24);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Padding = new Padding(0, 0, 2, 0);
            toolStrip1.Size = new Size(866, 46);
            toolStrip1.TabIndex = 4;
            toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButtonRedactDocuments
            // 
            toolStripButtonRedactDocuments.AutoToolTip = false;
            toolStripButtonRedactDocuments.Image = (Image)resources.GetObject("toolStripButtonRedactDocuments.Image");
            toolStripButtonRedactDocuments.ImageTransparentColor = Color.Magenta;
            toolStripButtonRedactDocuments.Name = "toolStripButtonRedactDocuments";
            toolStripButtonRedactDocuments.Size = new Size(47, 43);
            toolStripButtonRedactDocuments.Text = "Redact";
            toolStripButtonRedactDocuments.TextImageRelation = TextImageRelation.ImageAboveText;
            toolStripButtonRedactDocuments.ToolTipText = "Add documents to the redaction queue";
            toolStripButtonRedactDocuments.Click += toolStripButtonRedactDocuments_Click;
            // 
            // toolStripButtonRedactPreview
            // 
            toolStripButtonRedactPreview.AutoToolTip = false;
            toolStripButtonRedactPreview.ImageTransparentColor = Color.Magenta;
            toolStripButtonRedactPreview.Name = "toolStripButtonRedactPreview";
            toolStripButtonRedactPreview.Size = new Size(52, 43);
            toolStripButtonRedactPreview.Text = "Preview";
            toolStripButtonRedactPreview.TextImageRelation = TextImageRelation.ImageAboveText;
            toolStripButtonRedactPreview.ToolTipText = "Preview a redaction before saving (.txt, .docx, .pdf)";
            toolStripButtonRedactPreview.Click += redactPreviewToolStripMenuItem_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(6, 46);
            // 
            // refreshToolStripButton
            // 
            refreshToolStripButton.AutoToolTip = false;
            refreshToolStripButton.Image = (Image)resources.GetObject("refreshToolStripButton.Image");
            refreshToolStripButton.ImageTransparentColor = Color.Magenta;
            refreshToolStripButton.Name = "refreshToolStripButton";
            refreshToolStripButton.Size = new Size(50, 43);
            refreshToolStripButton.Text = "Refresh";
            refreshToolStripButton.TextImageRelation = TextImageRelation.ImageAboveText;
            refreshToolStripButton.ToolTipText = "Refresh the queue (F5)";
            refreshToolStripButton.Click += refreshToolStripMenuItem_Click;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(6, 46);
            // 
            // policiesToolStripButton
            // 
            policiesToolStripButton.AutoToolTip = false;
            policiesToolStripButton.Image = (Image)resources.GetObject("policiesToolStripButton.Image");
            policiesToolStripButton.ImageTransparentColor = Color.Magenta;
            policiesToolStripButton.Name = "policiesToolStripButton";
            policiesToolStripButton.Size = new Size(51, 43);
            policiesToolStripButton.Text = "Policies";
            policiesToolStripButton.TextImageRelation = TextImageRelation.ImageAboveText;
            policiesToolStripButton.ToolTipText = "Create and edit redaction policies";
            policiesToolStripButton.Click += policiesToolStripButton_Click;
            // 
            // contextsToolStripButton
            // 
            contextsToolStripButton.AutoToolTip = false;
            contextsToolStripButton.Image = (Image)resources.GetObject("contextsToolStripButton.Image");
            contextsToolStripButton.ImageTransparentColor = Color.Magenta;
            contextsToolStripButton.Name = "contextsToolStripButton";
            contextsToolStripButton.Size = new Size(57, 43);
            contextsToolStripButton.Text = "Contexts";
            contextsToolStripButton.TextImageRelation = TextImageRelation.ImageAboveText;
            contextsToolStripButton.ToolTipText = "Manage contexts for consistent replacements";
            contextsToolStripButton.Click += contextsToolStripButton_Click;
            // 
            // toolStripSeparator9
            // 
            toolStripSeparator9.Name = "toolStripSeparator9";
            toolStripSeparator9.Size = new Size(6, 46);
            // 
            // settingsToolStripButton
            // 
            settingsToolStripButton.AutoToolTip = false;
            settingsToolStripButton.Image = (Image)resources.GetObject("settingsToolStripButton.Image");
            settingsToolStripButton.ImageTransparentColor = Color.Magenta;
            settingsToolStripButton.Name = "settingsToolStripButton";
            settingsToolStripButton.Size = new Size(53, 43);
            settingsToolStripButton.Text = "Settings";
            settingsToolStripButton.TextImageRelation = TextImageRelation.ImageAboveText;
            settingsToolStripButton.ToolTipText = "Open settings (output location, logging, watched folders, security)";
            settingsToolStripButton.Click += settingsToolStripButton_Click;
            // 
            // toolStripSeparator6
            // 
            toolStripSeparator6.Name = "toolStripSeparator6";
            toolStripSeparator6.Size = new Size(6, 46);
            // 
            // HelpToolStripButton
            // 
            HelpToolStripButton.AutoToolTip = false;
            HelpToolStripButton.Image = (Image)resources.GetObject("HelpToolStripButton.Image");
            HelpToolStripButton.ImageTransparentColor = Color.Magenta;
            HelpToolStripButton.Name = "HelpToolStripButton";
            HelpToolStripButton.Size = new Size(36, 43);
            HelpToolStripButton.Text = "Help";
            HelpToolStripButton.TextImageRelation = TextImageRelation.ImageAboveText;
            HelpToolStripButton.ToolTipText = "Open the help documentation";
            // 
            // toolStripSeparator8
            // 
            toolStripSeparator8.Name = "toolStripSeparator8";
            toolStripSeparator8.Size = new Size(282, 6);
            // 
            // listView1
            // 
            listView1.AccessibleDescription = "Documents to redact, with their status, policy, and context";
            listView1.AccessibleName = "Redaction queue";
            listView1.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2, columnHeader4, columnHeader3 });
            listView1.ContextMenuStrip = contextMenuStrip1;
            listView1.Dock = DockStyle.Fill;
            listView1.FullRowSelect = true;
            listView1.GridLines = true;
            listView1.LargeImageList = imageList1;
            listView1.Location = new Point(0, 70);
            listView1.Margin = new Padding(2);
            listView1.Name = "listView1";
            listView1.Size = new Size(866, 247);
            listView1.SmallImageList = imageList1;
            listView1.TabIndex = 3;
            listView1.UseCompatibleStateImageBehavior = false;
            listView1.View = View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "File Name";
            columnHeader1.Width = 350;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "Status";
            columnHeader2.Width = 180;
            // 
            // columnHeader4
            // 
            columnHeader4.Text = "Policy";
            columnHeader4.Width = 120;
            // 
            // columnHeader3
            // 
            columnHeader3.Text = "Context";
            columnHeader3.Width = 120;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.ImageScalingSize = new Size(24, 24);
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { addFilesToRedactToolStripMenuItem, redactPreviewToolStripMenuItem, toolStripSeparator2, removeToolStripMenuItem, removeAllToolStripMenuItem, removeCompletedToolStripMenuItem, toolStripSeparator5, openRedactedFileToolStripMenuItem, openOriginalFileToolStripMenuItem, openContainingFolderToolStripMenuItem, toolStripSeparator8, modifyRedactionToolStripMenuItem, viewDiffToolStripMenuItem, viewDetailsToolStripMenuItem, toolStripSeparator7, refreshToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(286, 388);
            // 
            // addFilesToRedactToolStripMenuItem
            // 
            addFilesToRedactToolStripMenuItem.Image = (Image)resources.GetObject("addFilesToRedactToolStripMenuItem.Image");
            addFilesToRedactToolStripMenuItem.Name = "addFilesToRedactToolStripMenuItem";
            addFilesToRedactToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+O";
            addFilesToRedactToolStripMenuItem.Size = new Size(285, 30);
            addFilesToRedactToolStripMenuItem.Text = "Add Files to Redact...";
            addFilesToRedactToolStripMenuItem.Click += addFilesToRedactToolStripMenuItem_Click;
            // 
            // redactPreviewToolStripMenuItem
            // 
            redactPreviewToolStripMenuItem.Name = "redactPreviewToolStripMenuItem";
            redactPreviewToolStripMenuItem.Size = new Size(285, 30);
            redactPreviewToolStripMenuItem.Text = "Redact with Preview... (.txt, .docx, .pdf)";
            redactPreviewToolStripMenuItem.Click += redactPreviewToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(282, 6);
            // 
            // removeToolStripMenuItem
            // 
            removeToolStripMenuItem.Image = (Image)resources.GetObject("removeToolStripMenuItem.Image");
            removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            removeToolStripMenuItem.ShortcutKeyDisplayString = "Del";
            removeToolStripMenuItem.Size = new Size(285, 30);
            removeToolStripMenuItem.Text = "Remove...";
            removeToolStripMenuItem.Click += removeToolStripMenuItem_Click;
            // 
            // removeAllToolStripMenuItem
            // 
            removeAllToolStripMenuItem.Name = "removeAllToolStripMenuItem";
            removeAllToolStripMenuItem.Size = new Size(285, 30);
            removeAllToolStripMenuItem.Text = "Remove All...";
            removeAllToolStripMenuItem.Click += removeAllToolStripMenuItem_Click;
            // 
            // removeCompletedToolStripMenuItem
            // 
            removeCompletedToolStripMenuItem.Name = "removeCompletedToolStripMenuItem";
            removeCompletedToolStripMenuItem.Size = new Size(285, 30);
            removeCompletedToolStripMenuItem.Text = "Remove Completed...";
            removeCompletedToolStripMenuItem.Click += removeCompletedToolStripMenuItem_Click;
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            toolStripSeparator5.Size = new Size(282, 6);
            // 
            // openRedactedFileToolStripMenuItem
            // 
            openRedactedFileToolStripMenuItem.Image = (Image)resources.GetObject("openRedactedFileToolStripMenuItem.Image");
            openRedactedFileToolStripMenuItem.Name = "openRedactedFileToolStripMenuItem";
            openRedactedFileToolStripMenuItem.ShortcutKeyDisplayString = "Enter";
            openRedactedFileToolStripMenuItem.Size = new Size(285, 30);
            openRedactedFileToolStripMenuItem.Text = "Open Redacted File...";
            openRedactedFileToolStripMenuItem.Click += openRedactedFileToolStripMenuItem_Click;
            // 
            // openOriginalFileToolStripMenuItem
            // 
            openOriginalFileToolStripMenuItem.Image = (Image)resources.GetObject("openOriginalFileToolStripMenuItem.Image");
            openOriginalFileToolStripMenuItem.Name = "openOriginalFileToolStripMenuItem";
            openOriginalFileToolStripMenuItem.Size = new Size(285, 30);
            openOriginalFileToolStripMenuItem.Text = "Open Original File...";
            openOriginalFileToolStripMenuItem.Click += openOriginalFileToolStripMenuItem_Click;
            // 
            // openContainingFolderToolStripMenuItem
            // 
            openContainingFolderToolStripMenuItem.Name = "openContainingFolderToolStripMenuItem";
            openContainingFolderToolStripMenuItem.Size = new Size(285, 30);
            openContainingFolderToolStripMenuItem.Text = "Open Containing Folder";
            openContainingFolderToolStripMenuItem.Click += openContainingFolderToolStripMenuItem_Click;
            // 
            // modifyRedactionToolStripMenuItem
            // 
            modifyRedactionToolStripMenuItem.Name = "modifyRedactionToolStripMenuItem";
            modifyRedactionToolStripMenuItem.Size = new Size(285, 30);
            modifyRedactionToolStripMenuItem.Text = "Modify Redaction...";
            modifyRedactionToolStripMenuItem.Click += modifyRedactionToolStripMenuItem_Click;
            // 
            // viewDiffToolStripMenuItem
            // 
            viewDiffToolStripMenuItem.Name = "viewDiffToolStripMenuItem";
            viewDiffToolStripMenuItem.Size = new Size(285, 30);
            viewDiffToolStripMenuItem.Text = "View Diff...";
            viewDiffToolStripMenuItem.Click += viewDiffToolStripMenuItem_Click;
            // 
            // viewDetailsToolStripMenuItem
            // 
            viewDetailsToolStripMenuItem.Name = "viewDetailsToolStripMenuItem";
            viewDetailsToolStripMenuItem.Size = new Size(285, 30);
            viewDetailsToolStripMenuItem.Text = "View Details...";
            viewDetailsToolStripMenuItem.Click += viewDetailsToolStripMenuItem_Click;
            // 
            // toolStripSeparator7
            // 
            toolStripSeparator7.Name = "toolStripSeparator7";
            toolStripSeparator7.Size = new Size(282, 6);
            // 
            // refreshToolStripMenuItem
            // 
            refreshToolStripMenuItem.Image = (Image)resources.GetObject("refreshToolStripMenuItem.Image");
            refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            refreshToolStripMenuItem.ShortcutKeyDisplayString = "F5";
            refreshToolStripMenuItem.Size = new Size(285, 30);
            refreshToolStripMenuItem.Text = "Refresh";
            refreshToolStripMenuItem.Click += refreshToolStripMenuItem_Click;
            // 
            // imageList1
            // 
            imageList1.ColorDepth = ColorDepth.Depth32Bit;
            imageList1.ImageStream = (ImageListStreamer)resources.GetObject("imageList1.ImageStream");
            imageList1.TransparentColor = Color.Transparent;
            imageList1.Images.SetKeyName(0, "document.ico");
            // 
            // redactionQueueTimer
            // 
            redactionQueueTimer.Interval = 15000;
            redactionQueueTimer.Tick += RedactionQueueTimer_Tick;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(866, 339);
            Controls.Add(listView1);
            Controls.Add(statusStrip1);
            Controls.Add(toolStrip1);
            Controls.Add(menuStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            MinimumSize = new Size(722, 364);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Philter Desktop";
            Load += Form1_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            contextMenuStrip1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem clearRedactionHistoryToolStripMenuItem;
        private ToolStripSeparator toolStripSeparatorFile;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem1;
        private ToolStripMenuItem checkForUpdatesToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private StatusStrip statusStrip1;
        private ToolStrip toolStrip1;
        private ToolStripButton toolStripButtonRedactDocuments;
        private ToolStripButton toolStripButtonRedactPreview;
        private ListView listView1;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private ColumnHeader columnHeader4;
        private ColumnHeader columnHeader3;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripButton policiesToolStripButton;
        private ToolStripButton contextsToolStripButton;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripButton settingsToolStripButton;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem addFilesToRedactToolStripMenuItem;
        private ToolStripMenuItem redactPreviewToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem removeToolStripMenuItem;
        private ToolStripMenuItem removeAllToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator5;
        private ToolStripMenuItem openRedactedFileToolStripMenuItem;
        private ToolStripMenuItem openOriginalFileToolStripMenuItem;
        private ToolStripMenuItem openContainingFolderToolStripMenuItem;
        private ToolStripMenuItem modifyRedactionToolStripMenuItem;
        private ToolStripMenuItem viewDiffToolStripMenuItem;
        private ToolStripMenuItem viewDetailsToolStripMenuItem;
        private System.Windows.Forms.Timer redactionQueueTimer;
        private ToolStripSeparator toolStripSeparator7;
        private ToolStripMenuItem refreshToolStripMenuItem;
        private ToolStripMenuItem removeCompletedToolStripMenuItem;
        private ImageList imageList1;
        private ToolStripSeparator toolStripSeparator6;
        private ToolStripButton HelpToolStripButton;
        private ToolStripButton refreshToolStripButton;
        private ToolStripSeparator toolStripSeparator8;
        private ToolStripSeparator toolStripSeparator9;
    }
}
