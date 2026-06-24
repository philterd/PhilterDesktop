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
        private readonly PolicyRepository? _policyRepository;
        private readonly ContextRepository? _contextRepository;
        private readonly WatchedFolderRepository? _watchedFolderRepository;
        private readonly WatchedFolderLogRepository? _watchedFolderLogRepository;
        private readonly StartupManager _startupManager = StartupManager.CreateDefault();
        private readonly ContextMenuManager _contextMenuManager = ContextMenuManager.CreateDefault();
        private bool _suppressStartupToggle;
        private bool _suppressContextMenuToggle;
        private SettingsEntity _settings;

        /// <summary>
        /// True if the watched-folder list was changed (added/removed) while the dialog was open,
        /// so the caller knows to restart the folder watcher. Watched folders are persisted
        /// immediately on add/remove, independent of the Save button.
        /// </summary>
        public bool WatchedFoldersChanged { get; private set; }

        public SettingsForm(SettingsRepository settingsRepository)
            : this(settingsRepository, null, null, null, null)
        {
        }

        public SettingsForm(
            SettingsRepository settingsRepository,
            PolicyRepository? policyRepository,
            ContextRepository? contextRepository,
            WatchedFolderRepository? watchedFolderRepository,
            WatchedFolderLogRepository? watchedFolderLogRepository = null)
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(btnSave);
            _settingsRepository = settingsRepository;
            _policyRepository = policyRepository;
            _contextRepository = contextRepository;
            _watchedFolderRepository = watchedFolderRepository;
            _watchedFolderLogRepository = watchedFolderLogRepository;
            _settings = new SettingsEntity();

            // The watched-folder tab needs the policy/context/watched repositories; hide it otherwise.
            if (_policyRepository is null || _contextRepository is null || _watchedFolderRepository is null)
            {
                tabControl.TabPages.Remove(tabWatched);
            }
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            LoadSettings();
            LoadWatchedFolders();
            ConfigureStartupToggle();
            ConfigureContextMenuToggle();
        }

        private void ConfigureContextMenuToggle()
        {
            _suppressContextMenuToggle = true;
            try
            {
                if (StartupManager.IsPackaged)
                {
                    // Packaged (MSIX) builds virtualize HKCU\Software\Classes, so a runtime-written
                    // context menu wouldn't surface in Explorer; it must be declared in the package.
                    chkContextMenu.Checked = false;
                    chkContextMenu.Enabled = false;
                    lblContextMenuHint.Text = "Managed by the installed package for Store/MSIX builds.";
                }
                else
                {
                    chkContextMenu.Checked = _contextMenuManager.IsEnabled();
                    lblContextMenuHint.Text = string.Empty;
                }
            }
            finally
            {
                _suppressContextMenuToggle = false;
            }
        }

        private void ChkContextMenu_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressContextMenuToggle || StartupManager.IsPackaged)
            {
                return;
            }

            try
            {
                if (chkContextMenu.Checked)
                {
                    _contextMenuManager.Enable();
                }
                else
                {
                    _contextMenuManager.Disable();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not update the Explorer context-menu setting: {ex.Message}",
                    "Settings",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void ConfigureStartupToggle()
        {
            _suppressStartupToggle = true;
            try
            {
                if (StartupManager.IsPackaged)
                {
                    // For installed (MSIX) builds, auto-start is the package's startup task and is
                    // turned on/off by Windows, not by writing the registry.
                    chkStartWithWindows.Checked = true;
                    chkStartWithWindows.Enabled = false;
                    lblStartupHint.Text = "Managed by Windows — change this in Task Manager → Startup apps.";
                }
                else
                {
                    chkStartWithWindows.Checked = _startupManager.IsEnabled();
                    lblStartupHint.Text = string.Empty;
                }
            }
            finally
            {
                _suppressStartupToggle = false;
            }
        }

        private void ChkStartWithWindows_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressStartupToggle || StartupManager.IsPackaged)
            {
                return;
            }

            try
            {
                if (chkStartWithWindows.Checked)
                {
                    _startupManager.Enable();
                }
                else
                {
                    _startupManager.Disable();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not update the start-at-sign-in setting: {ex.Message}",
                    "Settings",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void LoadWatchedFolders()
        {
            if (_watchedFolderRepository is null)
            {
                return;
            }

            listWatched.BeginUpdate();
            listWatched.Items.Clear();
            foreach (WatchedFolderEntity folder in _watchedFolderRepository.GetAll().OrderBy(f => f.FolderPath))
            {
                var item = new ListViewItem(folder.FolderPath) { Tag = folder };
                item.SubItems.Add(folder.Policy);
                item.SubItems.Add(folder.Context);
                item.SubItems.Add(folder.OutputFolder);
                item.SubItems.Add(folder.Highlight ? "Yes" : "No");
                listWatched.Items.Add(item);
            }
            listWatched.EndUpdate();
            UpdateWatchedButtons();
        }

        private void UpdateWatchedButtons()
        {
            bool hasSelection = listWatched.SelectedItems.Count > 0;
            btnEditWatched.Enabled = hasSelection;
            btnRemoveWatched.Enabled = hasSelection;
            btnViewLog.Enabled = hasSelection && _watchedFolderLogRepository is not null;
        }

        private void ListWatched_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateWatchedButtons();
        }

        private void BtnViewLog_Click(object sender, EventArgs e)
        {
            if (_watchedFolderLogRepository is null || listWatched.SelectedItems.Count == 0)
            {
                return;
            }
            if (listWatched.SelectedItems[0].Tag is not WatchedFolderEntity folder)
            {
                return;
            }
            using var dialog = new WatchedFolderLogForm(_watchedFolderLogRepository, folder);
            dialog.ShowDialog(this);
        }

        private void BtnAddWatched_Click(object sender, EventArgs e)
        {
            if (_policyRepository is null || _contextRepository is null || _watchedFolderRepository is null)
            {
                return;
            }

            using var dialog = new WatchedFolderForm(_policyRepository, _contextRepository);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _watchedFolderRepository.Insert(dialog.WatchedFolder);
                WatchedFoldersChanged = true;
                LoadWatchedFolders();
            }
        }

        private void BtnEditWatched_Click(object sender, EventArgs e)
        {
            if (_policyRepository is null || _contextRepository is null || _watchedFolderRepository is null ||
                listWatched.SelectedItems.Count == 0)
            {
                return;
            }
            if (listWatched.SelectedItems[0].Tag is not WatchedFolderEntity folder)
            {
                return;
            }

            using var dialog = new WatchedFolderForm(_policyRepository, _contextRepository, folder);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _watchedFolderRepository.Update(dialog.WatchedFolder);
                WatchedFoldersChanged = true;
                LoadWatchedFolders();
            }
        }

        private void BtnRemoveWatched_Click(object sender, EventArgs e)
        {
            if (_watchedFolderRepository is null || listWatched.SelectedItems.Count == 0)
            {
                return;
            }

            if (listWatched.SelectedItems[0].Tag is not WatchedFolderEntity folder)
            {
                return;
            }

            if (MessageBox.Show(
                    $"Stop watching '{folder.FolderPath}'?",
                    "Remove Watched Folder",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            _watchedFolderRepository.Delete(folder.Id);
            _watchedFolderLogRepository?.DeleteForFolder(folder.Id);
            WatchedFoldersChanged = true;
            LoadWatchedFolders();
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
            txtSuffix.Text = RedactionService.NormalizeSuffix(_settings.RedactedSuffix);
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
            _settings.RedactedSuffix = RedactionService.NormalizeSuffix(txtSuffix.Text);
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