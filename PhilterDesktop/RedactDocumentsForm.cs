using PhilterData;

namespace PhilterDesktop
{
    /// <summary>
    /// Form for managing document redaction queue.
    /// </summary>
    public partial class RedactDocumentsForm : Form
    {
        private readonly PolicyRepository _policyRepository;
        private readonly ContextRepository _contextRepository;
        private readonly bool _loggingEnabled;

        public RedactDocumentsForm(PolicyRepository policyRepository, ContextRepository contextRepository, bool loggingEnabled)
        {
            InitializeComponent();
            _policyRepository = policyRepository;
            _contextRepository = contextRepository;
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
            comboBoxPolicy.Items.Clear();
            foreach (PolicyEntity p in _policyRepository.GetAll())
            {
                comboBoxPolicy.Items.Add(p.Name);
            }
        }

        private void LoadContexts()
        {
            comboBoxContext.Items.Clear();
            foreach (ContextEntity c in _contextRepository.GetAll())
            {
                comboBoxContext.Items.Add(c.Name);
            }
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
                            if (file.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) || 
                                file.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) || 
                                file.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                            {
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
                                    $"File '{Path.GetFileName(file)}' is not a supported file type.\n\nSupported types: .txt, .docx, .pdf",
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
                Filter = "Text Files (*.txt)|*.txt|PDF Documents (*.pdf)|*.pdf|Microsoft Word Documents (*.docx)|*.docx|All Supported Files|*.txt;*.pdf;*.docx",
                FilterIndex = 4,
                Title = "Select Files to Redact"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                foreach (string file in openFileDialog.FileNames)
                {
                    if (!filesListBox.Items.Contains(file))
                    {
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

            if (_loggingEnabled)
            {
                Logger.LogInfo($"Starting redaction - Policy: {policy}, Context: {context}, Files: {filesListBox.Items.Count}");
            }

            // TODO: Implement actual redaction logic
            // For now, just show a message
            MessageBox.Show(
                $"Redaction queued for {filesListBox.Items.Count} file(s)\n\nPolicy: {policy}\nContext: {context}",
                "Redaction Queued",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

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
    }
}