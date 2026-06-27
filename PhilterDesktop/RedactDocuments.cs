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

namespace PhilterDesktop
{
    /// <summary>
    /// Form for managing document redaction queue.
    /// </summary>
    public partial class RedactDocuments : Form
    {
        private const int MaxFiles = 25;

        private readonly PolicyRepository _policyRepository;
        private readonly ContextRepository _contextRepository;
        private readonly RedactionQueueRepository _redactionQueueRepository;
        private readonly SettingsRepository? _settingsRepository;
        private readonly bool _loggingEnabled;

        public RedactDocuments(PolicyRepository policyRepository, ContextRepository contextRepository, RedactionQueueRepository redactionQueueRepository, bool loggingEnabled, SettingsRepository? settingsRepository = null)
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(btnStartRedaction);
            _policyRepository = policyRepository;
            _contextRepository = contextRepository;
            _redactionQueueRepository = redactionQueueRepository;
            _settingsRepository = settingsRepository;
            _loggingEnabled = loggingEnabled;
        }

        private void RedactDocumentsForm_Load(object sender, EventArgs e)
        {
            // Load policies and contexts
            LoadPolicies();
            LoadContexts();
        }

        private void LoadPolicies()
        {
            string current = comboBoxPolicy.Text; // preserve the selection across dropdown reloads
            comboBoxPolicy.Items.Clear();
            foreach (PolicyEntity p in _policyRepository.GetAll())
            {
                comboBoxPolicy.Items.Add(p.Name);
            }
            ComboSelection.Select(comboBoxPolicy, !string.IsNullOrEmpty(current) ? current : _settingsRepository?.GetSettings().LastPolicy);
        }

        private void LoadContexts()
        {
            string current = comboBoxContext.Text;
            comboBoxContext.Items.Clear();
            foreach (ContextEntity c in _contextRepository.GetAll())
            {
                comboBoxContext.Items.Add(c.Name);
            }
            ComboSelection.Select(comboBoxContext, !string.IsNullOrEmpty(current) ? current : _settingsRepository?.GetSettings().LastContext);
        }

        private void ComboBoxPolicy_DropDown(object sender, EventArgs e)
        {
            LoadPolicies();
        }

        private void ComboBoxContext_DropDown(object sender, EventArgs e)
        {
            LoadContexts();
        }

        private void FilesControl_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void FilesControl_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
                {
                    foreach (string file in files)
                    {
                        if (!filesListBox.Items.Contains(file))
                        {
                            if (RedactionService.IsSupported(file))
                            {
                                if (filesListBox.Items.Count >= MaxFiles)
                                {
                                    MessageBox.Show(
                                        $"The queue is limited to {MaxFiles} documents at a time.",
                                        "Queue Limit Reached",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Warning);
                                    break;
                                }

                                filesListBox.Items.Add(file);

                                if (_loggingEnabled)
                                {
                                    Logger.LogInfo($"File added via drag-drop: {file}");
                                }
                            }
                            else
                            {
                                if (_loggingEnabled)
                                {
                                    Logger.LogWarning($"Unsupported file type rejected: {file}");
                                }

                                MessageBox.Show(
                                    $"File '{Path.GetFileName(file)}' is not a supported file type.\n\nSupported types: {string.Join(", ", RedactionService.SupportedExtensions)}",
                                    "Unsupported File Type",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                            }
                        }
                    }
                }
            }
        }

        private void BtnSelectFiles_Click(object sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = true,
                Filter = "Text Files (*.txt)|*.txt|Microsoft Word Documents (*.docx)|*.docx|PDF Documents (*.pdf)|*.pdf|Rich Text (*.rtf)|*.rtf|Email (*.eml;*.msg)|*.eml;*.msg|All Supported Files|*.txt;*.docx;*.pdf;*.rtf;*.eml;*.msg",
                FilterIndex = 6,
                Title = "Select Files to Redact"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                foreach (string file in openFileDialog.FileNames)
                {
                    if (!filesListBox.Items.Contains(file))
                    {
                        if (filesListBox.Items.Count >= MaxFiles)
                        {
                            MessageBox.Show(
                                $"The queue is limited to {MaxFiles} documents at a time.",
                                "Queue Limit Reached",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            break;
                        }

                        filesListBox.Items.Add(file);

                        if (_loggingEnabled)
                        {
                            Logger.LogInfo($"File added via file dialog: {file}");
                        }
                    }
                }
            }
        }

        private void BtnRemoveFile_Click(object sender, EventArgs e)
        {
            if (filesListBox.SelectedIndex >= 0)
            {
                string removedFile = filesListBox.SelectedItem?.ToString() ?? string.Empty;
                filesListBox.Items.RemoveAt(filesListBox.SelectedIndex);

                if (_loggingEnabled && !string.IsNullOrEmpty(removedFile))
                {
                    Logger.LogInfo($"File removed from queue: {removedFile}");
                }
            }
        }

        private void BtnClearAll_Click(object sender, EventArgs e)
        {
            if (filesListBox.Items.Count > 0)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to clear all files from the queue?",
                    "Clear All Files",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    int count = filesListBox.Items.Count;
                    filesListBox.Items.Clear();

                    if (_loggingEnabled)
                    {
                        Logger.LogInfo($"Cleared {count} file(s) from queue");
                    }
                }
            }
        }

        private void BtnStartRedaction_Click(object sender, EventArgs e)
        {
            if (comboBoxPolicy.Text == string.Empty)
            {
                MessageBox.Show("A policy must be selected.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (comboBoxContext.Text == string.Empty)
            {
                MessageBox.Show("A context must be selected.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (filesListBox.Items.Count == 0)
            {
                MessageBox.Show("No files selected for redaction.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string policy = comboBoxPolicy.Text;
            string context = comboBoxContext.Text;

            // Remember the choice so it's pre-selected next time.
            if (_settingsRepository is not null)
            {
                try
                {
                    SettingsEntity settings = _settingsRepository.GetSettings();
                    settings.LastPolicy = policy;
                    settings.LastContext = context;
                    _settingsRepository.SaveSettings(settings);
                }
                catch
                {
                    // best effort — don't block redaction on a settings write
                }
            }

            if (_loggingEnabled)
            {
                Logger.LogInfo($"Starting redaction - Policy: {policy}, Context: {context}, Files: {filesListBox.Items.Count}");
            }

            foreach (string file in filesListBox.Items.Cast<string>())
            {
                var entity = new RedactionQueueEntity
                {
                    Name = file,
                    Policy = policy,
                    Context = context,
                    Highlight = chkHighlightRedactions.Checked
                };
                _redactionQueueRepository.Insert(entity);

                if (_loggingEnabled)
                {
                    Logger.LogInfo($"Queued for redaction: {file}");
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void FilesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnRemoveFile.Enabled = filesListBox.SelectedIndex >= 0;
        }

        private void labelDropFiles_Click(object sender, EventArgs e)
        {

        }
    }
}