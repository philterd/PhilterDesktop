namespace PhilterDesktop
{
    partial class RedactDocumentsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RedactDocumentsForm));
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
            groupBoxFiles.SuspendLayout();
            filesPanel.SuspendLayout();
            groupBoxSettings.SuspendLayout();
            SuspendLayout();
            // 
            // groupBoxFiles
            // 
            groupBoxFiles.Controls.Add(filesPanel);
            groupBoxFiles.Location = new Point(15, 15);
            groupBoxFiles.Margin = new Padding(4);
            groupBoxFiles.Name = "groupBoxFiles";
            groupBoxFiles.Padding = new Padding(4);
            groupBoxFiles.Size = new Size(950, 463);
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
            filesPanel.Location = new Point(8, 32);
            filesPanel.Margin = new Padding(4);
            filesPanel.Name = "filesPanel";
            filesPanel.Size = new Size(935, 413);
            filesPanel.TabIndex = 5;
            filesPanel.DragDrop += FilesControl_DragDrop;
            filesPanel.DragEnter += FilesControl_DragEnter;
            // 
            // btnClearAll
            // 
            btnClearAll.Location = new Point(784, 337);
            btnClearAll.Margin = new Padding(4, 5, 4, 5);
            btnClearAll.Name = "btnClearAll";
            btnClearAll.Size = new Size(129, 47);
            btnClearAll.TabIndex = 4;
            btnClearAll.Text = "Clear All";
            btnClearAll.UseVisualStyleBackColor = true;
            btnClearAll.Click += BtnClearAll_Click;
            // 
            // btnRemoveFile
            // 
            btnRemoveFile.Enabled = false;
            btnRemoveFile.Location = new Point(647, 337);
            btnRemoveFile.Margin = new Padding(4, 5, 4, 5);
            btnRemoveFile.Name = "btnRemoveFile";
            btnRemoveFile.Size = new Size(129, 47);
            btnRemoveFile.TabIndex = 3;
            btnRemoveFile.Text = "Remove File";
            btnRemoveFile.UseVisualStyleBackColor = true;
            btnRemoveFile.Click += BtnRemoveFile_Click;
            // 
            // filesListBox
            // 
            filesListBox.FormattingEnabled = true;
            filesListBox.HorizontalScrollbar = true;
            filesListBox.Location = new Point(19, 88);
            filesListBox.Margin = new Padding(4);
            filesListBox.Name = "filesListBox";
            filesListBox.Size = new Size(894, 229);
            filesListBox.TabIndex = 2;
            filesListBox.SelectedIndexChanged += FilesListBox_SelectedIndexChanged;
            // 
            // labelDropFiles
            // 
            labelDropFiles.AutoSize = true;
            labelDropFiles.ForeColor = SystemColors.GrayText;
            labelDropFiles.Location = new Point(264, 42);
            labelDropFiles.Margin = new Padding(4, 0, 4, 0);
            labelDropFiles.Name = "labelDropFiles";
            labelDropFiles.Size = new Size(270, 25);
            labelDropFiles.TabIndex = 1;
            labelDropFiles.Text = "or drop files here to redact them";
            // 
            // btnSelectFiles
            // 
            btnSelectFiles.Location = new Point(19, 31);
            btnSelectFiles.Margin = new Padding(4, 5, 4, 5);
            btnSelectFiles.Name = "btnSelectFiles";
            btnSelectFiles.Size = new Size(237, 47);
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
            groupBoxSettings.Location = new Point(12, 518);
            groupBoxSettings.Margin = new Padding(4);
            groupBoxSettings.Name = "groupBoxSettings";
            groupBoxSettings.Padding = new Padding(4);
            groupBoxSettings.Size = new Size(950, 125);
            groupBoxSettings.TabIndex = 1;
            groupBoxSettings.TabStop = false;
            groupBoxSettings.Text = "Redaction Settings";
            // 
            // comboBoxContext
            // 
            comboBoxContext.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxContext.FormattingEnabled = true;
            comboBoxContext.Location = new Point(625, 44);
            comboBoxContext.Margin = new Padding(4);
            comboBoxContext.Name = "comboBoxContext";
            comboBoxContext.Size = new Size(299, 33);
            comboBoxContext.TabIndex = 3;
            comboBoxContext.DropDown += ComboBoxContext_DropDown;
            // 
            // labelContext
            // 
            labelContext.AutoSize = true;
            labelContext.Location = new Point(525, 48);
            labelContext.Margin = new Padding(4, 0, 4, 0);
            labelContext.Name = "labelContext";
            labelContext.Size = new Size(77, 25);
            labelContext.TabIndex = 2;
            labelContext.Text = "Context:";
            // 
            // comboBoxPolicy
            // 
            comboBoxPolicy.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxPolicy.FormattingEnabled = true;
            comboBoxPolicy.Location = new Point(112, 44);
            comboBoxPolicy.Margin = new Padding(4);
            comboBoxPolicy.Name = "comboBoxPolicy";
            comboBoxPolicy.Size = new Size(374, 33);
            comboBoxPolicy.TabIndex = 1;
            comboBoxPolicy.DropDown += ComboBoxPolicy_DropDown;
            // 
            // labelPolicy
            // 
            labelPolicy.AutoSize = true;
            labelPolicy.Location = new Point(25, 48);
            labelPolicy.Margin = new Padding(4, 0, 4, 0);
            labelPolicy.Name = "labelPolicy";
            labelPolicy.Size = new Size(61, 25);
            labelPolicy.TabIndex = 0;
            labelPolicy.Text = "Policy:";
            // 
            // btnStartRedaction
            // 
            btnStartRedaction.Location = new Point(656, 661);
            btnStartRedaction.Margin = new Padding(4, 5, 4, 5);
            btnStartRedaction.Name = "btnStartRedaction";
            btnStartRedaction.Size = new Size(172, 47);
            btnStartRedaction.TabIndex = 2;
            btnStartRedaction.Text = "Start Redaction";
            btnStartRedaction.UseVisualStyleBackColor = true;
            btnStartRedaction.Click += BtnStartRedaction_Click;
            // 
            // btnClose
            // 
            btnClose.Location = new Point(838, 661);
            btnClose.Margin = new Padding(4, 5, 4, 5);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(129, 47);
            btnClose.TabIndex = 3;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += BtnClose_Click;
            // 
            // RedactDocumentsForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(980, 733);
            Controls.Add(btnClose);
            Controls.Add(btnStartRedaction);
            Controls.Add(groupBoxSettings);
            Controls.Add(groupBoxFiles);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "RedactDocumentsForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Redact Documents";
            Load += RedactDocumentsForm_Load;
            groupBoxFiles.ResumeLayout(false);
            filesPanel.ResumeLayout(false);
            filesPanel.PerformLayout();
            groupBoxSettings.ResumeLayout(false);
            groupBoxSettings.PerformLayout();
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
    }
}