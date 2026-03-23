namespace PhilterDesktop
{
    partial class CreateContextDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            lblPrompt = new Label();
            txtInput = new TextBox();
            btnOk = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // lblPrompt
            // 
            lblPrompt.AutoSize = true;
            lblPrompt.Location = new Point(12, 15);
            lblPrompt.Name = "lblPrompt";
            lblPrompt.Size = new Size(86, 15);
            lblPrompt.TabIndex = 0;
            lblPrompt.Text = "Context Name:";
            // 
            // txtInput
            // 
            txtInput.Location = new Point(12, 43);
            txtInput.Name = "txtInput";
            txtInput.Size = new Size(406, 23);
            txtInput.TabIndex = 1;
            // 
            // btnOk
            // 
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Location = new Point(247, 90);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(78, 28);
            btnOk.TabIndex = 2;
            btnOk.Text = "OK";
            btnOk.Click += BtnOk_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(331, 90);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(78, 28);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "Cancel";
            btnCancel.Click += BtnCancel_Click;
            // 
            // CreateContextDialog
            // 
            AcceptButton = btnOk;
            CancelButton = btnCancel;
            ClientSize = new Size(433, 135);
            Controls.Add(lblPrompt);
            Controls.Add(txtInput);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CreateContextDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Create Redaction Context";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblPrompt;
        private TextBox txtInput;
        private Button btnOk;
        private Button btnCancel;
    }
}
