using LiteDB;
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using Philter;
using PhilterData;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop
{
    public partial class MainForm : Form
    {
        private readonly LiteDatabase _database;
        private readonly PolicyRepository _policyRepository;
        private readonly ContextRepository _contextRepository;
        private readonly ContextEntryRepository _contextEntryRepository;
        private readonly SettingsRepository _settingsRepository;
        private bool _loggingEnabled;
       
        public MainForm()
        {
            InitializeComponent();

            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // 2. Combine with your specific App Folder
            string folder = Path.Combine(root, "PhilterDesktop");
            string dbPath = Path.Combine(folder, "data.db");

            // 3. The Magic Step: Ensure the directory exists
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // Create a single shared database instance
            _database = new LiteDatabase(dbPath);

            // Pass the shared database to all repositories
            _policyRepository = new PolicyRepository(_database);
            _contextRepository = new ContextRepository(_database);
            _contextEntryRepository = new ContextEntryRepository(_database);
            _settingsRepository = new SettingsRepository(_database);

            // Load settings and check if logging is enabled
            var settings = _settingsRepository.GetSettings();
            _loggingEnabled = settings.LoggingEnabled;

            if (_loggingEnabled)
            {
                Logger.LogInfo("PhilterDesktop application started");
            }

            // Insert default policy.
            // TODO

            // Insert default context.
            if (_contextRepository.FindByName("default") == null)
            {
                ContextEntity contextEntity = new ContextEntity
                {
                    Name = "default"
                };
                _contextRepository.Insert(contextEntity);

                if (_loggingEnabled)
                {
                    Logger.LogInfo("Created default redaction context");
                }
            }

            // Enable drag and drop for filesListBox
            filesListBox.AllowDrop = true;
            filesListBox.DragEnter += FilesControl_DragEnter;
            filesListBox.DragDrop += FilesControl_DragDrop;

            // Enable drag and drop for filesPanel
            filesPanel.AllowDrop = true;
            filesPanel.DragEnter += FilesControl_DragEnter;
            filesPanel.DragDrop += FilesControl_DragDrop;
        }

        private void FilesControl_DragEnter(object? sender, DragEventArgs e)
        {
            // Check if the data being dragged contains files
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
            // Get the dropped files
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
                {
                    foreach (string file in files)
                    {
                        // Only add if the file doesn't already exist in the list and is an acceptable file type.
                        if (!filesListBox.Items.Contains(file))
                        {
                            if (file.EndsWith(".txt") || file.EndsWith(".docx") || file.EndsWith(".pdf"))
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
                                // TODO: Show a message to the user that the file type is not supported.
                            }
                        }
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {


        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("PhilterDesktop application closing");
            }

            Application.Exit();
        }

        private void policiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening Policy Editor");
            }

            var f = new PolicyEditorForm(_policyRepository);
            f.ShowDialog();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;
            openFileDialog1.Multiselect = true;
            openFileDialog1.Filter = "Text Files (*.txt)|*.txt|PDF Documents (*.pdf)|*.pdf|Microsoft Word Documents (*.docx)|*.docx";
            openFileDialog1.FilterIndex = 0;
            openFileDialog1.FileName = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (string s in openFileDialog1.FileNames)
                {
                    filesListBox.Items.Add(s);

                    if (_loggingEnabled)
                    {
                        Logger.LogInfo($"File added via file dialog: {s}");
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var policy = new PhileasPolicy
            {
                Name = "my-policy",
                Identifiers = new Identifiers
                {
                    EmailAddress = new EmailAddress()
                }
            };

            var result = new FilterService().Filter(
                policy: policy,
                context: "default",
                piece: 0,
                input: "Contact john.doe@example.com for help."
            );

            System.Diagnostics.Debug.WriteLine(result.FilteredText);

            if (_loggingEnabled)
            {
                Logger.LogInfo($"Test filter executed - Result: {result.FilteredText}");
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox1_DropDown(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            foreach (PolicyEntity p in _policyRepository.GetAll())
            {
                comboBox1.Items.Add(p.Name);
            }
        }

        private void contextsComboBox_DropDown(object sender, EventArgs e)
        {
            contextsComboBox.Items.Clear();
            foreach (ContextEntity p in _contextRepository.GetAll())
            {
                contextsComboBox.Items.Add(p.Name);
            }
        }

        private void redactionContextsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening Redaction Contexts dialog");
            }

            var redactionContextsForm = new RedctionContextsForm(_contextRepository, _contextEntryRepository);
            redactionContextsForm.ShowDialog();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening Settings dialog");
            }

            var settingsForm = new SettingsForm(_settingsRepository);
            var result = settingsForm.ShowDialog();

            // Reload logging setting in case it changed
            if (result == DialogResult.OK)
            {
                var settings = _settingsRepository.GetSettings();
                bool previousLoggingState = _loggingEnabled;
                _loggingEnabled = settings.LoggingEnabled;

                if (_loggingEnabled && !previousLoggingState)
                {
                    Logger.LogInfo("Logging enabled via settings");
                }
                else if (!_loggingEnabled && previousLoggingState)
                {
                    Logger.LogInfo("Logging disabled via settings");
                }
            }
        }
    }

}
