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

using PhilterData;
using System.Diagnostics;

namespace PhilterDesktop
{
    /// <summary>
    /// Form for managing application settings.
    /// /// </summary>
    public partial class SettingsForm : Form
    {
        private readonly SettingsRepository _settingsRepository;
        private SettingsEntity _settings;

        public SettingsForm(SettingsRepository settingsRepository)
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(btnSave);
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
            chkEnableLogging.Checked = _settings.LoggingEnabled;
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

        private void ChkEnableLogging_CheckedChanged(object sender, EventArgs e)
        {
  
        }

        private string GetLogFilePath()
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string folder = Path.Combine(root, "PhilterDesktop");
            return Path.Combine(folder, "application.log");
        }

        private void BtnOpenLog_Click(object sender, EventArgs e)
        {
            string logFilePath = GetLogFilePath();

            if (!File.Exists(logFilePath))
            {
                MessageBox.Show(
                    "Log file does not exist yet. The log file will be created when logging is enabled and events are logged.",
                    "Log File Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = logFilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to open log file: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
            _settings.LoggingEnabled = chkEnableLogging.Checked;
            
            _settingsRepository.SaveSettings(_settings);

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