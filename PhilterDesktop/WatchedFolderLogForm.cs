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
    public sealed class WatchedFolderLogForm : Form
    {
        private readonly WatchedFolderLogRepository _repository;
        private readonly WatchedFolderEntity _folder;

        private readonly ListView _list = new()
        {
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Location = new Point(12, 12),
            Size = new Size(736, 360),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        private readonly Button _refresh = new() { Text = "Refresh", Size = new Size(110, 34) };
        private readonly Button _clear = new() { Text = "Clear Log", Size = new Size(110, 34) };
        private readonly Button _close = new() { Text = "Close", DialogResult = DialogResult.OK, Size = new Size(110, 34) };

        public WatchedFolderLogForm(WatchedFolderLogRepository repository, WatchedFolderEntity folder)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _folder = folder ?? throw new ArgumentNullException(nameof(folder));

            Text = $"Activity Log — {folder.FolderPath}";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(760, 432);
            MinimumSize = new Size(560, 360);

            _list.Columns.Add("Time", 150);
            _list.Columns.Add("Level", 70);
            _list.Columns.Add("Message", 500);

            int buttonsY = 384;
            _refresh.Location = new Point(12, buttonsY);
            _clear.Location = new Point(130, buttonsY);
            _close.Location = new Point(638, buttonsY);
            _refresh.Anchor = _clear.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _close.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            Controls.AddRange(new Control[] { _list, _refresh, _clear, _close });
            AcceptButton = _close;
            CancelButton = _close;

            _refresh.Click += (_, _) => LoadEntries();
            _clear.Click += OnClear;

            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_close);

            LoadEntries();
        }

        private void LoadEntries()
        {
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

        private void OnClear(object? sender, EventArgs e)
        {
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
