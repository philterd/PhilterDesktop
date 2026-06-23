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
    /// redact with, whether to highlight .docx replacements, and the output folder.
    /// </summary>
    public sealed class WatchedFolderForm : Form
    {
        private readonly TextBox _folder = new() { Location = new Point(12, 44), Width = 416 };
        private readonly Button _browseFolder = new() { Text = "Browse…", Location = new Point(436, 42), Size = new Size(110, 34) };
        private readonly ComboBox _policy = new() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(12, 104), Width = 255 };
        private readonly ComboBox _context = new() { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(290, 104), Width = 256 };
        private readonly CheckBox _typePdf = new() { Text = "PDF (.pdf)", AutoSize = true, Location = new Point(95, 146) };
        private readonly CheckBox _typeDocx = new() { Text = "Word (.docx)", AutoSize = true, Location = new Point(190, 146) };
        private readonly CheckBox _typeTxt = new() { Text = "Text (.txt)", AutoSize = true, Location = new Point(310, 146) };
        private readonly CheckBox _includeSubfolders = new() { Text = "Include subfolders", AutoSize = true, Location = new Point(12, 182) };
        private readonly CheckBox _highlight = new() { Text = "Highlight redactions in Word (.docx) documents", AutoSize = true, Location = new Point(12, 212) };
        private readonly CheckBox _notify = new() { Text = "Show a notification when a file is redacted", AutoSize = true, Location = new Point(12, 242) };
        private readonly TextBox _output = new() { Location = new Point(12, 304), Width = 416 };
        private readonly Button _browseOutput = new() { Text = "Browse…", Location = new Point(436, 302), Size = new Size(110, 34) };
        private readonly Button _ok = new() { Text = "OK", Location = new Point(316, 358), Size = new Size(110, 40) };
        private readonly Button _cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(436, 358), Size = new Size(110, 40) };

        private readonly WatchedFolderEntity _entity;

        /// <summary>The configured watched folder (valid after the dialog returns <see cref="DialogResult.OK"/>).</summary>
        public WatchedFolderEntity WatchedFolder => _entity;

        public WatchedFolderForm(PolicyRepository policyRepository, ContextRepository contextRepository, WatchedFolderEntity? existing = null)
        {
            ArgumentNullException.ThrowIfNull(policyRepository);
            ArgumentNullException.ThrowIfNull(contextRepository);

            _entity = existing ?? new WatchedFolderEntity();

            Text = existing is null ? "Add Watched Folder" : "Edit Watched Folder";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(560, 416);

            Controls.AddRange(new Control[]
            {
                new Label { Text = "Folder to watch:", AutoSize = true, Location = new Point(12, 20) },
                _folder, _browseFolder,
                new Label { Text = "Policy:", AutoSize = true, Location = new Point(12, 84) },
                new Label { Text = "Context:", AutoSize = true, Location = new Point(290, 84) },
                _policy, _context,
                new Label { Text = "File types:", AutoSize = true, Location = new Point(12, 148) },
                _typePdf, _typeDocx, _typeTxt,
                _includeSubfolders,
                _highlight,
                _notify,
                new Label { Text = "Output folder:", AutoSize = true, Location = new Point(12, 282) },
                _output, _browseOutput,
                _ok, _cancel
            });

            AcceptButton = _ok;
            CancelButton = _cancel;

            LoadNames(policyRepository, contextRepository);
            PopulateFromEntity();

            _browseFolder.Click += (_, _) => BrowseInto(_folder, "Select the folder to watch for new files");
            _browseOutput.Click += (_, _) => BrowseInto(_output, "Select the folder to write redacted files to");
            _ok.Click += OnOk;

            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_ok);
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

        private void OnOk(object? sender, EventArgs e)
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
