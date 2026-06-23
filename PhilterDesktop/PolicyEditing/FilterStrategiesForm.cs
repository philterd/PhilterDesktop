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

using System.Collections;
using Phileas.Policy.Filters.Strategies;

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// Lists and edits the strategies for one filter type. Driven by a runtime strategy
    /// <see cref="Type"/> so the reflection-based editor can use it for any filter.
    /// </summary>
    internal sealed class FilterStrategiesForm : Form
    {
        private readonly string _display;
        private readonly Type _strategyType;
        private readonly List<AbstractFilterStrategy> _items;
        private static readonly Size ButtonSize = new(90, 34);

        private readonly ListBox _list = new() { Dock = DockStyle.Fill, IntegralHeight = false };
        private readonly Button _new = new() { Text = "New…", Size = ButtonSize };
        private readonly Button _edit = new() { Text = "Edit…", Size = ButtonSize, Enabled = false };
        private readonly Button _remove = new() { Text = "Remove", Size = ButtonSize, Enabled = false };
        private readonly Button _ok = new() { Text = "OK", DialogResult = DialogResult.OK, Size = ButtonSize };
        private readonly Button _cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = ButtonSize };

        public FilterStrategiesForm(string filterTypeDisplay, IEnumerable existing, Type strategyType)
        {
            _display = filterTypeDisplay;
            _strategyType = strategyType;
            _items = existing.Cast<AbstractFilterStrategy>().ToList();

            Text = $"{filterTypeDisplay} Filter Strategies";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 340);
            MinimumSize = new Size(380, 300);
            AcceptButton = _ok;
            CancelButton = _cancel;

            BuildLayout();
            RefreshList();

            _list.SelectedIndexChanged += (_, _) => UpdateButtons();
            _new.Click += OnNew;
            _edit.Click += OnEdit;
            _remove.Click += OnRemove;

            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_ok);
        }

        /// <summary>Builds a strongly-typed <c>List&lt;TStrategy&gt;</c> of the edited items.</summary>
        public IList BuildResultList()
        {
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(_strategyType))!;
            foreach (AbstractFilterStrategy item in _items)
            {
                list.Add(item);
            }
            return list;
        }

        private void BuildLayout()
        {
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.TopDown, Width = 112, Padding = new Padding(8) };
            buttons.Controls.AddRange(new Control[] { _new, _edit, _remove });

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 58, Padding = new Padding(8) };
            bottom.Controls.AddRange(new Control[] { _cancel, _ok });

            var listPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            listPanel.Controls.Add(_list);

            Controls.Add(listPanel);
            Controls.Add(buttons);
            Controls.Add(bottom);
        }

        private void RefreshList()
        {
            int selected = _list.SelectedIndex;
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (AbstractFilterStrategy s in _items)
            {
                _list.Items.Add(Describe(s));
            }
            _list.EndUpdate();
            if (selected >= 0 && selected < _list.Items.Count)
            {
                _list.SelectedIndex = selected;
            }
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            _edit.Enabled = _list.SelectedIndex >= 0;
            _remove.Enabled = _list.SelectedIndex >= 0;
        }

        private void OnNew(object? sender, EventArgs e)
        {
            var strategy = (AbstractFilterStrategy)Activator.CreateInstance(_strategyType)!;
            using var dlg = new AddFilterStrategyForm(strategy, _display);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _items.Add(strategy);
                RefreshList();
            }
        }

        private void OnEdit(object? sender, EventArgs e)
        {
            int i = _list.SelectedIndex;
            if (i < 0)
            {
                return;
            }
            using var dlg = new AddFilterStrategyForm(_items[i], _display);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                RefreshList();
            }
        }

        private void OnRemove(object? sender, EventArgs e)
        {
            int i = _list.SelectedIndex;
            if (i >= 0)
            {
                _items.RemoveAt(i);
                RefreshList();
            }
        }

        private static string Describe(AbstractFilterStrategy s)
        {
            string text = s.Strategy switch
            {
                AbstractFilterStrategy.StaticReplace => $"Replace with \"{s.StaticReplacement}\"",
                AbstractFilterStrategy.RandomReplace => $"Random replacement (scope: {s.ReplacementScope})",
                _ => $"Redact with \"{s.RedactionFormat}\""
            };
            if (!string.IsNullOrWhiteSpace(s.Condition))
            {
                text += $"  [when {s.Condition}]";
            }
            return text;
        }
    }
}
