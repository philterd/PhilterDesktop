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
        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public RedactionDetailsForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            _list.Resize += (_, _) => SizeValueColumn();
        }

        public RedactionDetailsForm(string title, IReadOnlyList<(string Label, string Value)> rows)
            : this()
        {
            Text = title;
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
