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
            exitToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem1 = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            statusStrip1 = new StatusStrip();
            toolStrip1 = new ToolStrip();
            toolStripButtonRedactDocuments = new ToolStripButton();
            toolStripSeparator3 = new ToolStripSeparator();
            policiesToolStripButton = new ToolStripButton();
            contextsToolStripButton = new ToolStripButton();
            toolStripSeparator4 = new ToolStripSeparator();
            settingsToolStripButton = new ToolStripButton();
            listView1 = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            columnHeader4 = new ColumnHeader();
            columnHeader3 = new ColumnHeader();
            contextMenuStrip1 = new ContextMenuStrip(components);
            addFilesToRedactToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            removeToolStripMenuItem = new ToolStripMenuItem();
            removeAllToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator5 = new ToolStripSeparator();
            openRedactedFileToolStripMenuItem = new ToolStripMenuItem();
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
            menuStrip1.Size = new Size(1009, 33);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(54, 29);
            fileToolStripMenuItem.Text = "File";
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(141, 34);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { helpToolStripMenuItem1, toolStripSeparator1, aboutToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(65, 29);
            helpToolStripMenuItem.Text = "Help";
            // 
            // helpToolStripMenuItem1
            // 
            helpToolStripMenuItem1.Image = (Image)resources.GetObject("helpToolStripMenuItem1.Image");
            helpToolStripMenuItem1.Name = "helpToolStripMenuItem1";
            helpToolStripMenuItem1.Size = new Size(270, 34);
            helpToolStripMenuItem1.Text = "Help";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(267, 6);
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(270, 34);
            aboutToolStripMenuItem.Text = "About...";
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(24, 24);
            statusStrip1.Location = new Point(0, 526);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 20, 0);
            statusStrip1.Size = new Size(1009, 22);
            statusStrip1.TabIndex = 3;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new Size(24, 24);
            toolStrip1.Items.AddRange(new ToolStripItem[] { toolStripButtonRedactDocuments, toolStripSeparator3, policiesToolStripButton, contextsToolStripButton, toolStripSeparator4, settingsToolStripButton });
            toolStrip1.Location = new Point(0, 33);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(1009, 34);
            toolStrip1.TabIndex = 4;
            toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButtonRedactDocuments
            // 
            toolStripButtonRedactDocuments.Image = (Image)resources.GetObject("toolStripButtonRedactDocuments.Image");
            toolStripButtonRedactDocuments.ImageTransparentColor = Color.Magenta;
            toolStripButtonRedactDocuments.Name = "toolStripButtonRedactDocuments";
            toolStripButtonRedactDocuments.Size = new Size(201, 29);
            toolStripButtonRedactDocuments.Text = "Redact Documents...";
            toolStripButtonRedactDocuments.ToolTipText = "Select files to redact";
            toolStripButtonRedactDocuments.Click += toolStripButtonRedactDocuments_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(6, 34);
            // 
            // policiesToolStripButton
            // 
            policiesToolStripButton.Image = (Image)resources.GetObject("policiesToolStripButton.Image");
            policiesToolStripButton.ImageTransparentColor = Color.Magenta;
            policiesToolStripButton.Name = "policiesToolStripButton";
            policiesToolStripButton.Size = new Size(97, 29);
            policiesToolStripButton.Text = "Policies";
            policiesToolStripButton.ToolTipText = "Create and edit redaction policies";
            policiesToolStripButton.Click += policiesToolStripButton_Click;
            // 
            // contextsToolStripButton
            // 
            contextsToolStripButton.Image = (Image)resources.GetObject("contextsToolStripButton.Image");
            contextsToolStripButton.ImageTransparentColor = Color.Magenta;
            contextsToolStripButton.Name = "contextsToolStripButton";
            contextsToolStripButton.Size = new Size(109, 29);
            contextsToolStripButton.Text = "Contexts";
            contextsToolStripButton.ToolTipText = "Manage redaction contexts";
            contextsToolStripButton.Click += contextsToolStripButton_Click;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(6, 34);
            // 
            // settingsToolStripButton
            // 
            settingsToolStripButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            settingsToolStripButton.Image = (Image)resources.GetObject("settingsToolStripButton.Image");
            settingsToolStripButton.ImageTransparentColor = Color.Magenta;
            settingsToolStripButton.Name = "settingsToolStripButton";
            settingsToolStripButton.Size = new Size(34, 29);
            settingsToolStripButton.Text = "Settings";
            settingsToolStripButton.ToolTipText = "Modify the application settings";
            settingsToolStripButton.Click += settingsToolStripButton_Click;
            // 
            // listView1
            // 
            listView1.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2, columnHeader4, columnHeader3 });
            listView1.ContextMenuStrip = contextMenuStrip1;
            listView1.Dock = DockStyle.Fill;
            listView1.FullRowSelect = true;
            listView1.Location = new Point(0, 67);
            listView1.Name = "listView1";
            listView1.Size = new Size(1009, 459);
            listView1.TabIndex = 3;
            listView1.UseCompatibleStateImageBehavior = false;
            listView1.View = View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "File Name";
            columnHeader1.Width = 450;
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
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { addFilesToRedactToolStripMenuItem, toolStripSeparator2, removeToolStripMenuItem, removeAllToolStripMenuItem, toolStripSeparator5, openRedactedFileToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(258, 144);
            // 
            // addFilesToRedactToolStripMenuItem
            // 
            addFilesToRedactToolStripMenuItem.Image = (Image)resources.GetObject("addFilesToRedactToolStripMenuItem.Image");
            addFilesToRedactToolStripMenuItem.Name = "addFilesToRedactToolStripMenuItem";
            addFilesToRedactToolStripMenuItem.Size = new Size(257, 32);
            addFilesToRedactToolStripMenuItem.Text = "Add Files to Redact...";
            addFilesToRedactToolStripMenuItem.Click += addFilesToRedactToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(254, 6);
            // 
            // removeToolStripMenuItem
            // 
            removeToolStripMenuItem.Image = (Image)resources.GetObject("removeToolStripMenuItem.Image");
            removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            removeToolStripMenuItem.Size = new Size(257, 32);
            removeToolStripMenuItem.Text = "Remove...";
            // 
            // removeAllToolStripMenuItem
            // 
            removeAllToolStripMenuItem.Name = "removeAllToolStripMenuItem";
            removeAllToolStripMenuItem.Size = new Size(257, 32);
            removeAllToolStripMenuItem.Text = "Remove All...";
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            toolStripSeparator5.Size = new Size(254, 6);
            // 
            // openRedactedFileToolStripMenuItem
            // 
            openRedactedFileToolStripMenuItem.Image = (Image)resources.GetObject("openRedactedFileToolStripMenuItem.Image");
            openRedactedFileToolStripMenuItem.Name = "openRedactedFileToolStripMenuItem";
            openRedactedFileToolStripMenuItem.Size = new Size(257, 32);
            openRedactedFileToolStripMenuItem.Text = "Open Redacted File...";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1009, 548);
            Controls.Add(listView1);
            Controls.Add(statusStrip1);
            Controls.Add(toolStrip1);
            Controls.Add(menuStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Margin = new Padding(4, 5, 4, 5);
            MinimumSize = new Size(1031, 604);
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
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem1;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private StatusStrip statusStrip1;
        private ToolStrip toolStrip1;
        private ToolStripButton toolStripButtonRedactDocuments;
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
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem removeToolStripMenuItem;
        private ToolStripMenuItem removeAllToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator5;
        private ToolStripMenuItem openRedactedFileToolStripMenuItem;
    }
}
