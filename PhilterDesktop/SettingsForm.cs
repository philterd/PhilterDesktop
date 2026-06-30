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
        private readonly DatabaseKeyStore? _keyStore;
        private readonly StartupManager _startupManager = StartupManager.CreateDefault();
        private readonly ContextMenuManager _contextMenuManager = ContextMenuManager.CreateDefault();
        private bool _suppressStartupToggle;
        private bool _suppressContextMenuToggle;
        private bool _suppressPassphraseToggle;
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
            WatchedFolderLogRepository? watchedFolderLogRepository = null,
            DatabaseKeyStore? keyStore = null)
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(btnSave);
            _settingsRepository = settingsRepository;
            _policyRepository = policyRepository;
            _contextRepository = contextRepository;
            _watchedFolderRepository = watchedFolderRepository;
            _watchedFolderLogRepository = watchedFolderLogRepository;
            _keyStore = keyStore;
            _settings = new SettingsEntity();

            // The watched-folder tab needs the policy/context/watched repositories; hide it otherwise.
            if (_policyRepository is null || _contextRepository is null || _watchedFolderRepository is null)
            {
                tabControl.TabPages.Remove(tabWatched);
            }
            else
            {
                // Watching folders is hand-rolled automation — the moment to mention the real pipeline tool.
                LinkLabel philterLink = Links.CreateLink(
                    "Automating redaction across systems or at scale? Philter runs it in your data pipeline →",
                    Links.PhilterUrl("watched-folders"));
                philterLink.Location = new Point(6, 292);
                tabWatched.Controls.Add(philterLink);
                philterLink.BringToFront();
            }

            // The security tab manages the database passphrase; hide it without a key store.
            if (_keyStore is null)
            {
                tabControl.TabPages.Remove(tabSecurity);
            }
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            LoadSettings();
            LoadWatchedFolders();
            ConfigureStartupToggle();
            ConfigureContextMenuToggle();
            ConfigureSecurityTab();
        }

        private void ConfigureSecurityTab()
        {
            if (_keyStore is null)
            {
                return;
            }
            _suppressPassphraseToggle = true;
            try
            {
                bool isProtected = _keyStore.IsPassphraseProtected;
                chkPassphrase.Checked = isProtected;
                btnChangePassphrase.Enabled = isProtected;
                lblSecurityStatus.Text = isProtected
                    ? "Protected with a passphrase — you'll be asked for it when Philter Desktop starts."
                    : "Protected by your Windows account (no passphrase required to open).";
            }
            finally
            {
                _suppressPassphraseToggle = false;
            }
        }

        private void SetPassphraseCheck(bool value)
        {
            _suppressPassphraseToggle = true;
            try { chkPassphrase.Checked = value; }
            finally { _suppressPassphraseToggle = false; }
        }

        // Advanced OCR tuning is only meaningful when OCR is on.
        private void ChkOcrScannedPdfs_CheckedChanged(object? sender, EventArgs e) =>
            btnOcrAdvanced.Enabled = chkOcrScannedPdfs.Checked;

        // Opens the advanced OCR threshold dialog, seeded from (and writing back to) the in-memory
        // settings; the values are persisted with everything else when the user clicks Save.
        private void BtnOcrAdvanced_Click(object? sender, EventArgs e)
        {
            using var dialog = new OcrAdvancedSettingsForm(
                _settings.OcrTextCoverageThreshold, _settings.OcrImageCoverageThreshold, _settings.OcrMaxPages);
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _settings.OcrTextCoverageThreshold = dialog.TextCoverageThreshold;
                _settings.OcrImageCoverageThreshold = dialog.ImageCoverageThreshold;
                _settings.OcrMaxPages = dialog.MaxPages;
            }
        }

        // The "same vs broad policy" choice only applies when automatic verification is on.
        private void ChkVerifyAfterRedaction_CheckedChanged(object? sender, EventArgs e) => UpdateVerifyPolicyEnabled();

        private void UpdateVerifyPolicyEnabled()
        {
            rdoVerifySamePolicy.Enabled = chkVerifyAfterRedaction.Checked;
            rdoVerifyBroadPolicy.Enabled = chkVerifyAfterRedaction.Checked;
        }

        private void ChkPassphrase_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressPassphraseToggle || _keyStore is null)
            {
                return;
            }

            try
            {
                if (chkPassphrase.Checked)
                {
                    using var dialog = new PassphraseForm(PassphraseFormMode.Set);
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                    {
                        _keyStore.EnablePassphrase(dialog.NewPassphrase);
                    }
                    else
                    {
                        SetPassphraseCheck(false); // user cancelled
                    }
                }
                else
                {
                    DialogResult confirm = MessageBox.Show(
                        this,
                        "Remove passphrase protection? The database will then be protected only by your Windows account.",
                        "Passphrase",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (confirm == DialogResult.Yes)
                    {
                        _keyStore.DisablePassphrase();
                    }
                    else
                    {
                        SetPassphraseCheck(true); // keep protection
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not update passphrase protection: {ex.Message}",
                    "Passphrase", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            ConfigureSecurityTab();
        }

        private void BtnChangePassphrase_Click(object sender, EventArgs e)
        {
            if (_keyStore is null || !_keyStore.IsPassphraseProtected)
            {
                return;
            }

            using var dialog = new PassphraseForm(PassphraseFormMode.Change);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (!_keyStore.VerifyPassphrase(dialog.CurrentPassphrase))
            {
                MessageBox.Show(this, "The current passphrase is incorrect.",
                    "Passphrase", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _keyStore.ChangePassphrase(dialog.NewPassphrase);
                MessageBox.Show(this, "Passphrase changed.",
                    "Passphrase", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not change the passphrase: {ex.Message}",
                    "Passphrase", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ConfigureContextMenuToggle()
        {
            _suppressContextMenuToggle = true;
            try
            {
                chkContextMenu.Checked = _contextMenuManager.IsEnabled();
                lblContextMenuHint.Text = string.Empty;
            }
            finally
            {
                _suppressContextMenuToggle = false;
            }
        }

        private void ChkContextMenu_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressContextMenuToggle)
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
                chkStartWithWindows.Checked = _startupManager.IsEnabled();
                lblStartupHint.Text = string.Empty;
            }
            finally
            {
                _suppressStartupToggle = false;
            }
        }

        private void ChkStartWithWindows_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressStartupToggle)
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
            chkShowNotifications.Checked = _settings.NotificationsEnabled;
            chkScrubMetadata.Checked = _settings.ScrubDocumentMetadata;
            chkScrubComments.Checked = _settings.ScrubWordComments;
            chkScrubTrackedChanges.Checked = _settings.ScrubWordTrackedChanges;
            chkScrubHiddenText.Checked = _settings.ScrubWordHiddenText;
            chkOcrScannedPdfs.Checked = _settings.OcrScannedPdfs;
            btnOcrAdvanced.Enabled = _settings.OcrScannedPdfs;
            chkScrubEmailHeaders.Checked = _settings.ScrubEmailHeaders;
            chkRemoveCommonHeaders.Checked = _settings.RemoveCommonEmailHeaders;
            chkVerifyAfterRedaction.Checked = _settings.VerifyAfterRedaction;
            rdoVerifyBroadPolicy.Checked = _settings.VerificationUseBroadPolicy;
            rdoVerifySamePolicy.Checked = !_settings.VerificationUseBroadPolicy;
            UpdateVerifyPolicyEnabled();
            cmbConcurrency.SelectedItem = Math.Clamp(_settings.WatchedFolderMaxConcurrency, 1, 4).ToString();
            numMaxFileSize.Value = Math.Clamp(_settings.MaxInputFileSizeMb, (int)numMaxFileSize.Minimum, (int)numMaxFileSize.Maximum);
            numRegexTimeout.Value = RegexSafety.ClampSeconds(_settings.RegexMatchTimeoutSeconds);
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

        private void BtnClearLog_Click(object sender, EventArgs e)
        {
            string logFilePath = GetLogFilePath();

            if (!File.Exists(logFilePath))
            {
                MessageBox.Show(
                    "There is no log file to clear.",
                    "Clear Log File",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            DialogResult result = MessageBox.Show(
                "Permanently delete the application log file?\n\n" +
                "The log records application activity for troubleshooting (such as errors and the " +
                "names of files that were processed). It does not contain the detected/redacted text " +
                "from your documents. Clearing it cannot be undone, and a new log will start the next " +
                "time something is logged.",
                "Clear Log File",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                return;
            }

            try
            {
                Logger.ClearLog();
                MessageBox.Show(
                    "The application log file has been cleared.",
                    "Clear Log File",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to clear the log file: {ex.Message}",
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

            // Validate the redacted-file-name suffix: an empty/whitespace suffix (output would collide
            // with the original) or one with characters that aren't legal in file names is normalized;
            // surface the adjustment so the user can review it before it's saved.
            string normalizedSuffix = RedactionService.NormalizeSuffix(txtSuffix.Text);
            if (!string.Equals(normalizedSuffix, txtSuffix.Text.Trim(), StringComparison.Ordinal))
            {
                txtSuffix.Text = normalizedSuffix;
                MessageBox.Show(this,
                    $"The redacted file-name suffix was adjusted to \"{normalizedSuffix}\".\n\n" +
                    "It can't be empty or contain characters that aren't allowed in file names.",
                    "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return; // let the user see the corrected value, then save again
            }

            // Save settings
            _settings.OutputToOriginalLocation = radioOriginalLocation.Checked;
            _settings.CustomOutputFolder = txtCustomFolder.Text.Trim();
            _settings.RedactedSuffix = RedactionService.NormalizeSuffix(txtSuffix.Text);
            _settings.LoggingEnabled = chkEnableLogging.Checked;
            _settings.NotificationsEnabled = chkShowNotifications.Checked;
            _settings.ScrubDocumentMetadata = chkScrubMetadata.Checked;
            _settings.ScrubWordComments = chkScrubComments.Checked;
            _settings.ScrubWordTrackedChanges = chkScrubTrackedChanges.Checked;
            _settings.ScrubWordHiddenText = chkScrubHiddenText.Checked;
            _settings.OcrScannedPdfs = chkOcrScannedPdfs.Checked;
            _settings.ScrubEmailHeaders = chkScrubEmailHeaders.Checked;
            _settings.RemoveCommonEmailHeaders = chkRemoveCommonHeaders.Checked;
            _settings.VerifyAfterRedaction = chkVerifyAfterRedaction.Checked;
            _settings.VerificationUseBroadPolicy = rdoVerifyBroadPolicy.Checked;
            _settings.WatchedFolderMaxConcurrency = int.TryParse(cmbConcurrency.Text, out int n) ? Math.Clamp(n, 1, 4) : 1;
            _settings.MaxInputFileSizeMb = (int)numMaxFileSize.Value;
            _settings.RegexMatchTimeoutSeconds = (int)numRegexTimeout.Value;

            _settingsRepository.SaveSettings(_settings);

            // Apply the new regex timeout immediately so it governs subsequent redactions without a restart.
            RegexSafety.InstallMatchTimeout(_settings.RegexMatchTimeoutSeconds);

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