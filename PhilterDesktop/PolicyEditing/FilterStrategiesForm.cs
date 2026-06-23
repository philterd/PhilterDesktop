using Phileas.Policy.Filters.Strategies;

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// Lists and edits the strategies for one filter type. Generic over the concrete
    /// strategy type, replacing the 19 thin VB "XFilterStrategiesForm" subclasses.
    /// </summary>
    internal sealed class FilterStrategiesForm<TStrategy> : Form
        where TStrategy : AbstractFilterStrategy, new()
    {
        private readonly string _display;
        private readonly List<TStrategy> _items;
        private readonly ListBox _list = new() { Dock = DockStyle.Fill, IntegralHeight = false };
        private readonly Button _new = new() { Text = "New…", AutoSize = true };
        private readonly Button _edit = new() { Text = "Edit…", AutoSize = true, Enabled = false };
        private readonly Button _remove = new() { Text = "Remove", AutoSize = true, Enabled = false };
        private readonly Button _ok = new() { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        private readonly Button _cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };

        public FilterStrategiesForm(string filterTypeDisplay, IEnumerable<TStrategy> existing)
        {
            _display = filterTypeDisplay;
            _items = existing.ToList();

            Text = $"{filterTypeDisplay} Filter Strategies";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 280);
            MinimumSize = new Size(380, 240);
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

        public List<TStrategy> GetStrategies() => _items;

        private void BuildLayout()
        {
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.TopDown, Width = 100, Padding = new Padding(8) };
            buttons.Controls.AddRange(new Control[] { _new, _edit, _remove });

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 44, Padding = new Padding(8) };
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
            foreach (TStrategy s in _items)
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
            var strategy = new TStrategy();
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
