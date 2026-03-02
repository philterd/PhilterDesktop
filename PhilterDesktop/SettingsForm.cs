using PhilterData;

namespace PhilterDesktop
{
    /// <summary>
    /// Form for managing application settings.
    /// </summary>
    public partial class SettingsForm : Form
    {
        private readonly SettingsRepository _settingsRepository;
        private SettingsEntity _settings;

        public SettingsForm(SettingsRepository settingsRepository)
        {
            InitializeComponent();
            _settingsRepository = settingsRepository;
            _settings = new SettingsEntity();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            _settings = _settingsRepository.GetSettings();

            // Update UI controls
            radioOriginalLocation.Checked = _settings.OutputToOriginalLocation;
            radioCustomFolder.Checked = !_settings.OutputToOriginalLocation;
            txtCustomFolder.Text = _settings.CustomOutputFolder;
            txtCustomFolder.Enabled = !_settings.OutputToOriginalLocation;
            btnBrowse.Enabled = !_settings.OutputToOriginalLocation;
        }

        private void RadioOriginalLocation_CheckedChanged(object sender, EventArgs e)
        {
            txtCustomFolder.Enabled = !radioOriginalLocation.Checked;
            btnBrowse.Enabled = !radioOriginalLocation.Checked;
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using var folderDialog = new FolderBrowserDialog
            {
                Description = "Select output folder for redacted files",
                ShowNewFolderButton = true,
                SelectedPath = txtCustomFolder.Text
            };

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                txtCustomFolder.Text = folderDialog.SelectedPath;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Validate custom folder if that option is selected
            if (radioCustomFolder.Checked)
            {
                if (string.IsNullOrWhiteSpace(txtCustomFolder.Text))
                {
                    MessageBox.Show(
                        "Please specify a custom output folder or select 'Output to original location'.",
                        "Validation Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (!Directory.Exists(txtCustomFolder.Text))
                {
                    var result = MessageBox.Show(
                        $"The folder '{txtCustomFolder.Text}' does not exist.\n\nDo you want to create it?",
                        "Folder Not Found",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        try
                        {
                            Directory.CreateDirectory(txtCustomFolder.Text);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                $"Failed to create folder: {ex.Message}",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }

            // Save settings
            _settings.OutputToOriginalLocation = radioOriginalLocation.Checked;
            _settings.CustomOutputFolder = txtCustomFolder.Text.Trim();
            
            _settingsRepository.SaveSettings(_settings);

            MessageBox.Show(
                "Settings saved successfully.",
                "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}