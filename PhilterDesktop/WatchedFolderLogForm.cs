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
    /// Read-only view of a watched folder's activity log (files found, redacted, skipped, and
    /// errors), with refresh and clear actions.
    /// </summary>
    public partial class WatchedFolderLogForm : Form
    {
        private readonly WatchedFolderLogRepository? _repository;
        private readonly WatchedFolderEntity? _folder;

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public WatchedFolderLogForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_close);
        }

        public WatchedFolderLogForm(WatchedFolderLogRepository repository, WatchedFolderEntity folder)
            : this()
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _folder = folder ?? throw new ArgumentNullException(nameof(folder));

            Text = $"Activity Log — {folder.FolderPath}";
            LoadEntries();
        }

        private void LoadEntries()
        {
            if (_repository is null || _folder is null)
            {
                return;
            }

            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (WatchedFolderLogEntity entry in _repository.GetForFolder(_folder.Id))
            {
                var item = new ListViewItem(entry.Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
                item.SubItems.Add(entry.Level);
                item.SubItems.Add(entry.Message);
                if (string.Equals(entry.Level, "Error", StringComparison.OrdinalIgnoreCase))
                {
                    item.ForeColor = Color.Firebrick;
                }
                _list.Items.Add(item);
            }
            _list.EndUpdate();

            if (_list.Items.Count == 0)
            {
                _list.Items.Add(new ListViewItem(string.Empty) { SubItems = { string.Empty, "No activity recorded yet." } });
            }
        }

        private void RefreshButton_Click(object? sender, EventArgs e) => LoadEntries();

        private void ClearButton_Click(object? sender, EventArgs e)
        {
            if (_repository is null || _folder is null)
            {
                return;
            }
            if (MessageBox.Show(
                    "Clear all log entries for this watched folder?",
                    "Clear Log",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }
            _repository.DeleteForFolder(_folder.Id);
            LoadEntries();
        }
    }
}
