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
            groupBoxOutput.Location = new Point(12, 12);
            groupBoxOutput.Name = "groupBoxOutput";
            groupBoxOutput.Size = new Size(560, 120);
            groupBoxOutput.TabIndex = 0;
            groupBoxOutput.TabStop = false;
            groupBoxOutput.Text = "Output Location";
            // 
            // btnBrowse
            // 
            btnBrowse.Location = new Point(460, 76);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(85, 28);
            btnBrowse.TabIndex = 3;
            btnBrowse.Text = "Browse...";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += BtnBrowse_Click;
            // 
            // txtCustomFolder
            // 
            txtCustomFolder.Location = new Point(40, 78);
            txtCustomFolder.Name = "txtCustomFolder";
            txtCustomFolder.Size = new Size(410, 23);
            txtCustomFolder.TabIndex = 2;
            // 
            // radioCustomFolder
            // 
            radioCustomFolder.AutoSize = true;
            radioCustomFolder.Location = new Point(20, 55);
            radioCustomFolder.Name = "radioCustomFolder";
            radioCustomFolder.Size = new Size(235, 19);
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
            radioOriginalLocation.Size = new Size(173, 19);
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
            groupBoxLogging.Location = new Point(12, 138);
            groupBoxLogging.Name = "groupBoxLogging";
            groupBoxLogging.Size = new Size(560, 65);
            groupBoxLogging.TabIndex = 1;
            groupBoxLogging.TabStop = false;
            groupBoxLogging.Text = "Logging";
            // 
            // btnOpenLog
            // 
            btnOpenLog.Location = new Point(165, 27);
            btnOpenLog.Name = "btnOpenLog";
            btnOpenLog.Size = new Size(100, 25);
            btnOpenLog.TabIndex = 1;
            btnOpenLog.Text = "Open Log File";
            btnOpenLog.UseVisualStyleBackColor = true;
            btnOpenLog.Enabled = true;
            btnOpenLog.Click += BtnOpenLog_Click;
            // 
            // chkEnableLogging
            // 
            chkEnableLogging.AutoSize = true;
            chkEnableLogging.Location = new Point(20, 30);
            chkEnableLogging.Name = "chkEnableLogging";
            chkEnableLogging.Size = new Size(107, 19);
            chkEnableLogging.TabIndex = 0;
            chkEnableLogging.Text = "Enable logging";
            chkEnableLogging.UseVisualStyleBackColor = true;
            chkEnableLogging.CheckedChanged += ChkEnableLogging_CheckedChanged;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(390, 216);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(90, 30);
            btnSave.TabIndex = 2;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += BtnSave_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(486, 216);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 30);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += BtnCancel_Click;
            // 
            // SettingsForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(584, 258);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
            Controls.Add(groupBoxLogging);
            Controls.Add(groupBoxOutput);
            FormBorderStyle = FormBorderStyle.FixedDialog;
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