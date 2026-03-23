namespace PhilterDesktop
{
    partial class LicenseForm
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
            lblTitle = new Label();
            lblInstructions = new Label();
            lblActivationKey = new Label();
            txtActivationKey = new TextBox();
            btnActivate = new Button();
            btnClose = new Button();
            lblStatus = new Label();
            btnDeactivate = new Button();
            groupBoxLicense = new GroupBox();
            groupBoxLicense.SuspendLayout();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTitle.Location = new Point(17, 20);
            lblTitle.Margin = new Padding(4, 0, 4, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(279, 38);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Software Activation";
            // 
            // lblInstructions
            // 
            lblInstructions.Location = new Point(17, 75);
            lblInstructions.Margin = new Padding(4, 0, 4, 0);
            lblInstructions.Name = "lblInstructions";
            lblInstructions.Size = new Size(657, 67);
            lblInstructions.TabIndex = 1;
            lblInstructions.Text = "Enter your activation key below to activate Philter Desktop. You can obtain an activation key from https://www.philterd.ai";
            // 
            // lblActivationKey
            // 
            lblActivationKey.AutoSize = true;
            lblActivationKey.Location = new Point(9, 47);
            lblActivationKey.Margin = new Padding(4, 0, 4, 0);
            lblActivationKey.Name = "lblActivationKey";
            lblActivationKey.Size = new Size(128, 25);
            lblActivationKey.TabIndex = 2;
            lblActivationKey.Text = "Activation Key:";
            // 
            // txtActivationKey
            // 
            txtActivationKey.Font = new Font("Consolas", 9F);
            txtActivationKey.Location = new Point(9, 77);
            txtActivationKey.Margin = new Padding(4, 5, 4, 5);
            txtActivationKey.MaxLength = 100;
            txtActivationKey.Multiline = true;
            txtActivationKey.Name = "txtActivationKey";
            txtActivationKey.Size = new Size(621, 97);
            txtActivationKey.TabIndex = 3;
            // 
            // btnActivate
            // 
            btnActivate.Location = new Point(9, 187);
            btnActivate.Margin = new Padding(4, 5, 4, 5);
            btnActivate.Name = "btnActivate";
            btnActivate.Size = new Size(143, 47);
            btnActivate.TabIndex = 4;
            btnActivate.Text = "Activate";
            btnActivate.UseVisualStyleBackColor = true;
            btnActivate.Click += btnActivate_Click;
            // 
            // btnClose
            // 
            btnClose.Location = new Point(546, 457);
            btnClose.Margin = new Padding(4, 5, 4, 5);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(129, 47);
            btnClose.TabIndex = 5;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += btnClose_Click;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblStatus.ForeColor = Color.Gray;
            lblStatus.Location = new Point(9, 255);
            lblStatus.Margin = new Padding(4, 0, 4, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(184, 25);
            lblStatus.TabIndex = 6;
            lblStatus.Text = "No license activated";
            // 
            // btnDeactivate
            // 
            btnDeactivate.Location = new Point(160, 187);
            btnDeactivate.Margin = new Padding(4, 5, 4, 5);
            btnDeactivate.Name = "btnDeactivate";
            btnDeactivate.Size = new Size(143, 47);
            btnDeactivate.TabIndex = 7;
            btnDeactivate.Text = "Deactivate";
            btnDeactivate.UseVisualStyleBackColor = true;
            btnDeactivate.Click += btnDeactivate_Click;
            // 
            // groupBoxLicense
            // 
            groupBoxLicense.Controls.Add(lblActivationKey);
            groupBoxLicense.Controls.Add(btnDeactivate);
            groupBoxLicense.Controls.Add(txtActivationKey);
            groupBoxLicense.Controls.Add(lblStatus);
            groupBoxLicense.Controls.Add(btnActivate);
            groupBoxLicense.Location = new Point(17, 147);
            groupBoxLicense.Margin = new Padding(4, 5, 4, 5);
            groupBoxLicense.Name = "groupBoxLicense";
            groupBoxLicense.Padding = new Padding(4, 5, 4, 5);
            groupBoxLicense.Size = new Size(657, 300);
            groupBoxLicense.TabIndex = 8;
            groupBoxLicense.TabStop = false;
            groupBoxLicense.Text = "License Information";
            // 
            // LicenseForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(691, 522);
            Controls.Add(groupBoxLicense);
            Controls.Add(btnClose);
            Controls.Add(lblInstructions);
            Controls.Add(lblTitle);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4, 5, 4, 5);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "LicenseForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "License Activation";
            groupBoxLicense.ResumeLayout(false);
            groupBoxLicense.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblTitle;
        private Label lblInstructions;
        private Label lblActivationKey;
        private TextBox txtActivationKey;
        private Button btnActivate;
        private Button btnClose;
        private Label lblStatus;
        private Button btnDeactivate;
        private GroupBox groupBoxLicense;
    }
}
