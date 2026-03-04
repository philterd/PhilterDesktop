namespace PhilterDesktop
{
    /// <summary>
    /// License dialog for entering and activating product license keys.
    /// </summary>
    public partial class LicenseForm : Form
    {
        public LicenseForm()
        {
            InitializeComponent();
            LoadLicenseStatus();
        }

        private void LoadLicenseStatus()
        {
            // TODO: Load existing license status from settings/database
            // For now, just show placeholder text
            txtActivationKey.Text = string.Empty;
            lblStatus.Text = "No license activated";
            lblStatus.ForeColor = Color.Gray;
        }

        private void btnActivate_Click(object sender, EventArgs e)
        {
            string activationKey = txtActivationKey.Text.Trim();

            if (string.IsNullOrWhiteSpace(activationKey))
            {
                MessageBox.Show("Please enter an activation key.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // TODO: Implement actual license validation logic
            // For now, just show a placeholder implementation
            if (ValidateLicenseKey(activationKey))
            {
                lblStatus.Text = "License activated successfully!";
                lblStatus.ForeColor = Color.Green;
                
                // TODO: Save license to settings/database
                
                MessageBox.Show("Your license has been activated successfully.", "License Activated", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                lblStatus.Text = "Invalid activation key";
                lblStatus.ForeColor = Color.Red;
                
                MessageBox.Show("The activation key you entered is invalid. Please check and try again.", "Invalid License", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ValidateLicenseKey(string key)
        {
            // TODO: Implement actual license validation
            // This is a placeholder implementation
            // You would typically validate against:
            // - Key format
            // - Checksum/signature
            // - Server-side validation
            // - Expiration date
            
            return key.Length >= 20; // Placeholder validation
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnDeactivate_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Are you sure you want to deactivate this license?",
                "Confirm Deactivation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // TODO: Implement license deactivation logic
                txtActivationKey.Text = string.Empty;
                lblStatus.Text = "License deactivated";
                lblStatus.ForeColor = Color.Gray;
                
                MessageBox.Show("Your license has been deactivated.", "License Deactivated",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
