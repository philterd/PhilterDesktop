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
    // MainForm is built entirely in code rather than with the Windows Forms designer (see the Build*
    // methods in MainForm.cs). This file holds only the control field declarations and Dispose, so the
    // whole form is constructed in one consistent, hand-written style (the designer will not open it).
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        /// <summary>Clean up any resources being used.</summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem clearRedactionHistoryToolStripMenuItem;
        private ToolStripSeparator toolStripSeparatorFile;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem1;
        private ToolStripMenuItem checkForUpdatesToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem viewLicenseToolStripMenuItem;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private StatusStrip statusStrip1;
        private ToolStrip toolStrip1;
        private ToolStripSplitButton toolStripButtonRedact;
        private ToolStripMenuItem redactDropDownItem;
        private ToolStripMenuItem previewDropDownItem;
        private ToolStripMenuItem findRedactDropDownItem;
        private ToolStripMenuItem spreadsheetDropDownItem;
        private ToolStripMenuItem folderDropDownItem;
        private ListView listView1;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private ColumnHeader columnHeader4;
        private ColumnHeader columnHeader3;
        private ColumnHeader columnHeader5;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripButton policiesToolStripButton;
        private ToolStripButton contextsToolStripButton;
        private ToolStripButton listsToolStripButton;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripButton settingsToolStripButton;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem addFilesToRedactToolStripMenuItem;
        private ToolStripMenuItem redactPreviewToolStripMenuItem;
        private ToolStripMenuItem findAndRedactToolStripMenuItem;
        private ToolStripMenuItem redactSpreadsheetToolStripMenuItem;
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
        private ToolStripMenuItem exportExplanationToolStripMenuItem;
        private ToolStripMenuItem verifyRedactionToolStripMenuItem;
        private ToolStripMenuItem verifyWithSamePolicyToolStripMenuItem;
        private ToolStripMenuItem verifyWithBroadPolicyToolStripMenuItem;
        private ToolStripMenuItem generateReportToolStripMenuItem;
        private System.Windows.Forms.Timer redactionQueueTimer;
        private ToolStripSeparator toolStripSeparator7;
        private ToolStripMenuItem refreshToolStripMenuItem;
        private ToolStripMenuItem removeCompletedToolStripMenuItem;
        private ToolStripMenuItem retryToolStripMenuItem;
        private ToolStripMenuItem retryAllFailedToolStripMenuItem;
        private ToolStripSeparator toolStripSeparatorRetry;
        private ImageList imageList1;
        private ToolStripSeparator toolStripSeparator6;
        private ToolStripButton HelpToolStripButton;
        private ToolStripButton refreshToolStripButton;
        private ToolStripSeparator toolStripSeparator8;
        private ToolStripSeparator toolStripSeparator9;
    }
}
