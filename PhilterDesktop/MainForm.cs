using LiteDB;
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using Philter;
using PhilterData;
using System.Text.Json;
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
        private readonly RedactionQueueRepository _redactionQueueRepository;
        private bool _loggingEnabled;

        public MainForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);

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
            _redactionQueueRepository = new RedactionQueueRepository(_database);

            // Load settings and check if logging is enabled
            var settings = _settingsRepository.GetSettings();
            _loggingEnabled = settings.LoggingEnabled;

            if (_loggingEnabled)
            {
                Logger.LogInfo("PhilterDesktop application started");
            }

            // Insert default policy.
            if (_policyRepository.FindByName("default") == null)
            {
                PolicyEntity policyEntity = new PolicyEntity
                {
                    Name = "default",
                    Json = "{}"

                };
                _policyRepository.Insert(policyEntity);

                if (_loggingEnabled)
                {
                    Logger.LogInfo("Created default redaction policy");
                }
            }

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
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            redactionQueueTimer.Start();
            LoadRedactionQueue();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("PhilterDesktop application closing");
            }

            Application.Exit();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening About dialog");
            }

            var aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }

        private void licenseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening License dialog");
            }

            var licenseForm = new LicenseForm();
            licenseForm.ShowDialog();
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

        private void redactionContextsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening Redaction Contexts dialog");
            }

            var redactionContextsForm = new Contexts(_contextRepository, _contextEntryRepository);
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

        private void toolStripButtonRedactDocuments_Click(object sender, EventArgs e)
        {
            var redactDocumentsForm = new RedactDocuments(_policyRepository, _contextRepository, _redactionQueueRepository, _loggingEnabled);
            redactDocumentsForm.ShowDialog();
            LoadRedactionQueue();
        }

        private void policiesToolStripButton_Click(object sender, EventArgs e)
        {
            var f = new PolicyEditorForm(_policyRepository);
            f.ShowDialog();
        }

        private void contextsToolStripButton_Click(object sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening Redaction Contexts dialog");
            }

            var redactionContextsForm = new Contexts(_contextRepository, _contextEntryRepository);
            redactionContextsForm.ShowDialog();
        }

        private void settingsToolStripButton_Click(object sender, EventArgs e)
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

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadRedactionQueue();
        }

        private void removeCompletedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _redactionQueueRepository.DeleteWhere(x => x.Status == "Completed");
            LoadRedactionQueue();
        }

        private void addFilesToRedactToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var redactDocumentsForm = new RedactDocuments(_policyRepository, _contextRepository, _redactionQueueRepository, _loggingEnabled);
            redactDocumentsForm.ShowDialog();
            LoadRedactionQueue();
        }

        private async void RedactionQueueTimer_Tick(object sender, EventArgs e)
        {
            redactionQueueTimer.Stop();

            try
            {
                var pendingEntities = _redactionQueueRepository
                    .Find(x => x.Status == "Pending")
                    .ToList();

                if (pendingEntities.Count == 0)
                {
                    return;
                }

                if (_loggingEnabled)
                {
                    Logger.LogInfo($"Redaction queue timer fired: {pendingEntities.Count} item(s) pending redaction.");
                }

                var settings = _settingsRepository.GetSettings();
                var filterService = new FilterService();

                foreach (var entity in pendingEntities)
                {
                    UpdateEntityStatus(entity, "Processing");

                    try
                    {
                        var policyEntity = _policyRepository.FindByName(entity.Policy);
                        if (policyEntity == null)
                        {
                            UpdateEntityStatus(entity, "Failed");

                            if (_loggingEnabled)
                            {
                                Logger.LogError($"Policy '{entity.Policy}' not found for file: {entity.Name}");
                            }

                            continue;
                        }

                        if (!File.Exists(entity.Name))
                        {
                            UpdateEntityStatus(entity, "Failed");

                            if (_loggingEnabled)
                            {
                                Logger.LogError($"File not found: {entity.Name}");
                            }

                            continue;
                        }

                        var policy = PolicySerializer.DeserializeFromJson(policyEntity.Json);
                        string input = await File.ReadAllTextAsync(entity.Name);
                        var result = filterService.Filter(policy, entity.Context, 0, input);

                        string outputPath = GetOutputPath(entity.Name, settings);
                        await File.WriteAllTextAsync(outputPath, result.FilteredText);

                        UpdateEntityStatus(entity, "Completed");

                        if (_loggingEnabled)
                        {
                            Logger.LogInfo($"Redaction completed: {entity.Name} -> {outputPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        UpdateEntityStatus(entity, "Failed");

                        if (_loggingEnabled)
                        {
                            Logger.LogError($"Redaction failed for {entity.Name}", ex);
                        }
                    }
                }

                LoadRedactionQueue();
            }
            finally
            {
                redactionQueueTimer.Start();
            }
        }

        private void UpdateEntityStatus(RedactionQueueEntity entity, string status)
        {
            entity.Status = status;
            _redactionQueueRepository.Update(entity);

            foreach (ListViewItem item in listView1.Items)
            {
                if (item.Tag is ObjectId id && id == entity.Id)
                {
                    item.SubItems[1].Text = status;
                    break;
                }
            }
        }

        private static string GetOutputPath(string inputPath, SettingsEntity settings)
        {
            string directory = settings.OutputToOriginalLocation
                ? Path.GetDirectoryName(inputPath) ?? string.Empty
                : settings.CustomOutputFolder;

            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);
            string extension = Path.GetExtension(inputPath);

            return Path.Combine(directory, $"{fileNameWithoutExt}_redacted{extension}");
        }

        private void LoadRedactionQueue()
        {
            listView1.BeginUpdate();
            listView1.Items.Clear();

            foreach (RedactionQueueEntity entity in _redactionQueueRepository.GetAll())
            {
                var item = new ListViewItem(entity.Name);
                item.SubItems.Add(entity.Status);
                item.SubItems.Add(entity.Policy);
                item.SubItems.Add(entity.Context);
                item.Tag = entity.Id;
                item.ImageIndex = 0;
                listView1.Items.Add(item);
            }

            listView1.EndUpdate();
        }

        private void openRedactedFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
            {
                return;
            }

            ListViewItem selected = listView1.SelectedItems[0];

            if (selected.Tag is not ObjectId id)
            {
                return;
            }

            RedactionQueueEntity? entity = _redactionQueueRepository.GetById(id);
            if (entity == null)
            {
                return;
            }

            string outputPath = GetOutputPath(entity.Name, _settingsRepository.GetSettings());

            if (!File.Exists(outputPath))
            {
                MessageBox.Show(
                    $"The redacted file could not be found:\n\n{outputPath}",
                    "File Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = outputPath,
                UseShellExecute = true
            });
        }

        private void openOriginalFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
            {
                return;
            }

            ListViewItem selected = listView1.SelectedItems[0];

            if (selected.Tag is not ObjectId id)
            {
                return;
            }

            RedactionQueueEntity? entity = _redactionQueueRepository.GetById(id);
            if (entity == null)
            {
                return;
            }

            if (!File.Exists(entity.Name))
            {
                MessageBox.Show(
                    $"The original file could not be found:\n\n{entity.Name}",
                    "File Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = entity.Name,
                UseShellExecute = true
            });
        }

        private void removeAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _redactionQueueRepository.DeleteWhere(x => x.Status != "Processing");
            LoadRedactionQueue();
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
            {
                return;
            }

            ListViewItem selected = listView1.SelectedItems[0];

            if (selected.Tag is not ObjectId id)
            {
                return;
            }

            RedactionQueueEntity? entity = _redactionQueueRepository.GetById(id);
            if (entity == null)
            {
                return;
            }

            if (entity.Status == "Processing")
            {
                MessageBox.Show(
                    $"'{Path.GetFileName(entity.Name)}' cannot be removed because it is currently being processed.",
                    "Cannot Remove",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            _redactionQueueRepository.Delete(id);
            listView1.Items.Remove(selected);
        }
    }
}
