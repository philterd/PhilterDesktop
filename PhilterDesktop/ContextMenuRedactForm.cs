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
    /// Shown when files are redacted via the Windows Explorer right-click menu. Lists the selected
    /// files and lets the user pick the policy and context; clicking <b>Add to Queue</b> adds each file
    /// to the Philter Desktop redaction queue (processed by the main app).
    /// </summary>
    public partial class ContextMenuRedactForm : Form
    {
        private readonly PolicyRepository _policies = null!;
        private readonly ContextRepository _contexts = null!;
        private readonly RedactionQueueRepository _queue = null!;
        private readonly SettingsRepository? _settings;
        private readonly IReadOnlyList<string> _files = Array.Empty<string>();

        /// <summary>Number of files added to the queue when the user clicked Add to Queue (0 if cancelled).</summary>
        public int EnqueuedCount { get; private set; }

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public ContextMenuRedactForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_redact);
        }

        public ContextMenuRedactForm(
            IReadOnlyList<string> files,
            PolicyRepository policies,
            ContextRepository contexts,
            RedactionQueueRepository queue,
            SettingsRepository? settings = null)
            : this()
        {
            _files = files;
            _policies = policies;
            _contexts = contexts;
            _queue = queue;
            _settings = settings;
        }

        private void ContextMenuRedactForm_Load(object? sender, EventArgs e)
        {
            _fileList.BeginUpdate();
            _fileList.Items.Clear();
            foreach (string path in _files)
            {
                _fileList.Items.Add(new ListViewItem(path));
            }
            _fileList.EndUpdate();

            SettingsEntity? settings = _settings?.GetSettings();
            LoadNames(_policyCombo, _policies?.GetAll().Select(p => p.Name), settings?.LastPolicy);
            LoadNames(_contextCombo, _contexts?.GetAll().Select(c => c.Name), settings?.LastContext);

            _redact.Enabled = _files.Count > 0 && _policyCombo.Items.Count > 0 && _contextCombo.Items.Count > 0;
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

        private void OnRedact(object? sender, EventArgs e)
        {
            if (_policyCombo.SelectedItem is null || _contextCombo.SelectedItem is null)
            {
                MessageBox.Show("Select a policy and a context.", "Redact", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            string policy = _policyCombo.Text;
            string context = _contextCombo.Text;

            // Remember the choice so it's pre-selected next time (shared with the main app's settings).
            if (_settings is not null)
            {
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

            int queued = 0;
            foreach (string path in _files)
            {
                // Skip files already queued for this policy/context (avoids duplicate redactions).
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
    }
}
