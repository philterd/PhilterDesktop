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
    /// One-shot "Redact Folder" command: pick a folder (optionally recursing into subfolders), a
    /// policy, and a context, then add every supported file to the redaction queue in one action. The
    /// queue redacts them with the app's normal pipeline, so per-file success/failure shows up in the
    /// queue exactly like any other redaction. No persistent watcher is created.
    /// </summary>
    public partial class FolderRedactForm : Form
    {
        private readonly PolicyRepository _policies = null!;
        private readonly ContextRepository _contexts = null!;
        private readonly RedactionQueueRepository _queue = null!;
        private readonly SettingsRepository? _settings;

        private SettingsEntity _settingsSnapshot = new();
        private List<string> _selectedFiles = new();
        private bool _loading;

        /// <summary>Number of files added to the queue when the user confirmed (0 if cancelled).</summary>
        public int EnqueuedCount { get; private set; }

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public FolderRedactForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_redact);
        }

        public FolderRedactForm(
            PolicyRepository policies,
            ContextRepository contexts,
            RedactionQueueRepository queue,
            SettingsRepository? settings = null,
            string? initialFolder = null)
            : this()
        {
            _policies = policies;
            _contexts = contexts;
            _queue = queue;
            _settings = settings;
            if (!string.IsNullOrEmpty(initialFolder))
            {
                _folderBox.Text = initialFolder;
            }
        }

        private void FolderRedactForm_Load(object? sender, EventArgs e)
        {
            _loading = true;
            try
            {
                _settingsSnapshot = _settings?.GetSettings() ?? new SettingsEntity();
                LoadNames(_policyCombo, _policies?.GetAll().Select(p => p.Name), _settingsSnapshot.LastPolicy);
                LoadNames(_contextCombo, _contexts?.GetAll().Select(c => c.Name), _settingsSnapshot.LastContext);
            }
            finally
            {
                _loading = false;
            }

            UpdateSummary();
        }

        private static void LoadNames(ComboBox combo, IEnumerable<string>? names, string? preferred)
        {
            combo.Items.Clear();
            foreach (string name in (names ?? Enumerable.Empty<string>()).OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            {
                combo.Items.Add(name);
            }
            ComboSelection.Select(combo, preferred);
        }

        private void OnBrowse(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Choose a folder to redact",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };
            if (Directory.Exists(_folderBox.Text))
            {
                dialog.SelectedPath = _folderBox.Text;
            }
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _folderBox.Text = dialog.SelectedPath;
            }
        }

        private void OnFolderChanged(object? sender, EventArgs e)
        {
            if (!_loading)
            {
                UpdateSummary();
            }
        }

        private void OnRecurseChanged(object? sender, EventArgs e)
        {
            if (!_loading)
            {
                UpdateSummary();
            }
        }

        // Re-scans the chosen folder and refreshes the count/summary and the enabled state of "Add".
        private void UpdateSummary()
        {
            string folder = _folderBox.Text.Trim();

            if (string.IsNullOrEmpty(folder))
            {
                _selectedFiles = new List<string>();
                _summaryLabel.Text = "Choose a folder to see how many files will be redacted.";
                _redact.Enabled = false;
                return;
            }

            if (!Directory.Exists(folder))
            {
                _selectedFiles = new List<string>();
                _summaryLabel.Text = "That folder doesn't exist.";
                _redact.Enabled = false;
                return;
            }

            Cursor? previous = Cursor.Current;
            UseWaitCursor = true;
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                _selectedFiles = FolderEnumerator.EnumerateRedactable(
                    folder, _recurse.Checked, _settingsSnapshot.RedactedSuffix);
            }
            finally
            {
                Cursor.Current = previous;
                UseWaitCursor = false;
            }

            _summaryLabel.Text = DescribeSelection(_selectedFiles);
            _redact.Enabled = _selectedFiles.Count > 0;
        }

        // "12 files will be redacted (5 .pdf, 4 .docx, 3 .txt)." Lists per-type counts so the user can
        // sanity-check the scan before committing a batch.
        private static string DescribeSelection(IReadOnlyList<string> files)
        {
            if (files.Count == 0)
            {
                return "No supported files were found in that folder.";
            }

            IReadOnlyList<(string Extension, int Count)> byType = FolderEnumerator.SummarizeByType(files);
            string breakdown = string.Join(", ", byType.Select(t => $"{t.Count} {t.Extension}"));
            string noun = files.Count == 1 ? "file" : "files";
            return $"{files.Count} {noun} will be added to the redaction queue ({breakdown}).";
        }

        private void OnRedact(object? sender, EventArgs e)
        {
            if (_policyCombo.SelectedItem is null || _contextCombo.SelectedItem is null)
            {
                MessageBox.Show(this, "Select a policy and a context.", "Redact Folder",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            if (_selectedFiles.Count == 0)
            {
                MessageBox.Show(this, "No supported files were found in that folder.", "Redact Folder",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            string policy = _policyCombo.Text;
            string context = _contextCombo.Text;

            RememberLastUsed(policy, context);

            int queued = 0;
            foreach (string path in _selectedFiles)
            {
                // Skip files already queued for this policy/context so re-running the folder doesn't re-queue them.
                if (QueueBulkActions.TryEnqueue(_queue, new RedactionQueueEntity
                {
                    Name = path,
                    Policy = policy,
                    Context = context,
                    Highlight = _highlight.Checked
                }))
                {
                    queued++;
                }
            }

            EnqueuedCount = queued;
            DialogResult = DialogResult.OK;
            Close();
        }

        // Persist the chosen policy/context so they're pre-selected next time. Best effort.
        private void RememberLastUsed(string policy, string context)
        {
            if (_settings is null)
            {
                return;
            }
            try
            {
                SettingsEntity s = _settings.GetSettings();
                s.LastPolicy = policy;
                s.LastContext = context;
                _settings.SaveSettings(s);
            }
            catch
            {
                // best effort
            }
        }
    }
}
