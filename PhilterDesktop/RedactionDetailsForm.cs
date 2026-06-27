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

namespace PhilterDesktop
{
    /// <summary>
    /// Shows details about a redacted document (source/redacted file, policy, context, redaction
    /// count, timestamp, …) in a simple two-column property/value list.
    /// </summary>
    public partial class RedactionDetailsForm : Form
    {
        private readonly ToolStripMenuItem _copyItem;
        private Action<IWin32Window>? _exportExplanation;

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public RedactionDetailsForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            _list.Resize += (_, _) => SizeValueColumn();
            _export.Click += (_, _) => _exportExplanation?.Invoke(this);

            // Right-click a row to copy its value (e.g. the redacted file path) to the clipboard.
            var menu = new ContextMenuStrip();
            _copyItem = new ToolStripMenuItem("Copy", null, (_, _) => CopySelectedValue())
            {
                ShortcutKeyDisplayString = "Ctrl+C"
            };
            menu.Items.Add(_copyItem);
            menu.Opening += (_, e) =>
            {
                if (_list.SelectedItems.Count == 0)
                {
                    e.Cancel = true;
                }
            };
            _list.ContextMenuStrip = menu;
            _list.KeyDown += (_, e) =>
            {
                if (e.Control && e.KeyCode == Keys.C)
                {
                    CopySelectedValue();
                    e.Handled = true;
                }
            };
        }

        // Copy the Value column of the selected row to the clipboard.
        private void CopySelectedValue()
        {
            if (_list.SelectedItems.Count == 0)
            {
                return;
            }

            ListViewItem item = _list.SelectedItems[0];
            string value = item.SubItems.Count > 1 ? item.SubItems[1].Text : string.Empty;
            try
            {
                if (string.IsNullOrEmpty(value))
                {
                    Clipboard.Clear();
                }
                else
                {
                    Clipboard.SetText(value);
                }
            }
            catch
            {
                // The clipboard can be transiently locked by another process; ignore.
            }
        }

        /// <param name="exportExplanation">
        /// When supplied, an "Export Explanation (JSON)…" button is shown; the action is invoked with
        /// this dialog as the owner so its save/confirm dialogs are modal over it.
        /// </param>
        public RedactionDetailsForm(string title, IReadOnlyList<(string Label, string Value)> rows,
            Action<IWin32Window>? exportExplanation = null)
            : this()
        {
            Text = title;
            _exportExplanation = exportExplanation;
            _export.Visible = exportExplanation is not null;
            _list.BeginUpdate();
            foreach ((string label, string value) in rows)
            {
                var item = new ListViewItem(label);
                item.SubItems.Add(value);
                _list.Items.Add(item);
            }
            _list.EndUpdate();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SizeValueColumn();
        }

        // Let the value column fill the remaining width.
        private void SizeValueColumn()
        {
            if (_list.Columns.Count < 2)
            {
                return;
            }
            int width = _list.ClientSize.Width - _list.Columns[0].Width - 4;
            if (width > 80)
            {
                _list.Columns[1].Width = width;
            }
        }
    }
}
