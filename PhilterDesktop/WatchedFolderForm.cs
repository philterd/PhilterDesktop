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
    /// Dialog to add or edit a watched folder: the folder to monitor, the policy and context to
    /// redact with, the file types, subfolder/highlight/notify options, and the output folder.
    /// </summary>
    public partial class WatchedFolderForm : Form
    {
        private WatchedFolderEntity _entity = new();

        /// <summary>The configured watched folder (valid after the dialog returns <see cref="DialogResult.OK"/>).</summary>
        public WatchedFolderEntity WatchedFolder => _entity;

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public WatchedFolderForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_ok);
        }

        public WatchedFolderForm(PolicyRepository policyRepository, ContextRepository contextRepository, WatchedFolderEntity? existing = null)
            : this()
        {
            ArgumentNullException.ThrowIfNull(policyRepository);
            ArgumentNullException.ThrowIfNull(contextRepository);

            _entity = existing ?? new WatchedFolderEntity();
            if (existing is not null)
            {
                Text = "Edit Watched Folder";
            }

            LoadNames(policyRepository, contextRepository);
            PopulateFromEntity();
        }

        private void LoadNames(PolicyRepository policyRepository, ContextRepository contextRepository)
        {
            foreach (PolicyEntity p in policyRepository.GetAll().OrderBy(p => p.Name))
            {
                _policy.Items.Add(p.Name);
            }
            foreach (ContextEntity c in contextRepository.GetAll().OrderBy(c => c.Name))
            {
                _context.Items.Add(c.Name);
            }
        }

        private void PopulateFromEntity()
        {
            _folder.Text = _entity.FolderPath;
            _output.Text = _entity.OutputFolder;
            _highlight.Checked = _entity.Highlight;
            _includeSubfolders.Checked = _entity.IncludeSubfolders;
            _notify.Checked = _entity.Notify;

            // No selection (e.g., a folder created before this option existed) means all types.
            bool all = _entity.FileTypes is not { Count: > 0 };
            _typePdf.Checked = all || _entity.FileTypes.Contains(".pdf", StringComparer.OrdinalIgnoreCase);
            _typeDocx.Checked = all || _entity.FileTypes.Contains(".docx", StringComparer.OrdinalIgnoreCase);
            _typeTxt.Checked = all || _entity.FileTypes.Contains(".txt", StringComparer.OrdinalIgnoreCase);

            SelectOrFirst(_policy, _entity.Policy);
            SelectOrFirst(_context, _entity.Context);
        }

        private static void SelectOrFirst(ComboBox combo, string value)
        {
            int index = combo.Items.IndexOf(value);
            if (index < 0 && combo.Items.Count > 0)
            {
                index = 0;
            }
            if (index >= 0)
            {
                combo.SelectedIndex = index;
            }
        }

        private void BrowseFolder_Click(object? sender, EventArgs e) =>
            BrowseInto(_folder, "Select the folder to watch for new files");

        private void BrowseOutput_Click(object? sender, EventArgs e) =>
            BrowseInto(_output, "Select the folder to write redacted files to");

        private static void BrowseInto(TextBox target, string description)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = description,
                ShowNewFolderButton = true,
                SelectedPath = target.Text
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                target.Text = dialog.SelectedPath;
            }
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            string folder = _folder.Text.Trim();
            string output = _output.Text.Trim();

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                Warn("Please choose an existing folder to watch.");
                return;
            }
            if (_policy.SelectedItem is null)
            {
                Warn("Please select a policy.");
                return;
            }
            if (_context.SelectedItem is null)
            {
                Warn("Please select a context.");
                return;
            }
            if (string.IsNullOrEmpty(output))
            {
                Warn("Please choose an output folder.");
                return;
            }
            if (!_typePdf.Checked && !_typeDocx.Checked && !_typeTxt.Checked)
            {
                Warn("Please select at least one file type to redact.");
                return;
            }
            if (string.Equals(Path.GetFullPath(folder), Path.GetFullPath(output), StringComparison.OrdinalIgnoreCase))
            {
                Warn("The output folder must be different from the watched folder.");
                return;
            }
            if (_includeSubfolders.Checked &&
                (PathUtils.IsSameOrInside(output, folder) || PathUtils.IsSameOrInside(folder, output)))
            {
                Warn("When watching subfolders, the output folder must be outside the watched folder (and vice versa).");
                return;
            }

            _entity.FolderPath = folder;
            _entity.OutputFolder = output;
            _entity.Policy = (string)_policy.SelectedItem;
            _entity.Context = (string)_context.SelectedItem;
            _entity.Highlight = _highlight.Checked;
            _entity.IncludeSubfolders = _includeSubfolders.Checked;
            _entity.Notify = _notify.Checked;

            var types = new List<string>();
            if (_typePdf.Checked) { types.Add(".pdf"); }
            if (_typeDocx.Checked) { types.Add(".docx"); }
            if (_typeTxt.Checked) { types.Add(".txt"); }
            _entity.FileTypes = types;

            DialogResult = DialogResult.OK;
            Close();
        }

        private static void Warn(string message) =>
            MessageBox.Show(message, "Watched Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
