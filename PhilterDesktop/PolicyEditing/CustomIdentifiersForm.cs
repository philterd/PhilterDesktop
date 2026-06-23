using Phileas.Policy.Filters;

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// Lists and edits custom regex identifiers (Phileas <see cref="Identifier"/>).
    /// </summary>
    internal sealed class CustomIdentifiersForm : Form
    {
        private readonly List<Identifier> _items;
        private readonly ListView _list = new()
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            HideSelection = false
        };
        private readonly Button _add = new() { Text = "Add…", AutoSize = true };
        private readonly Button _edit = new() { Text = "Edit…", AutoSize = true, Enabled = false };
        private readonly Button _remove = new() { Text = "Remove", AutoSize = true, Enabled = false };
        private readonly Button _ok = new() { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        private readonly Button _cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };

        public List<Identifier> Identifiers => _items;

        public CustomIdentifiersForm(IEnumerable<Identifier> existing)
        {
            _items = existing.Select(Clone).ToList();

            Text = "Custom Identifiers";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(540, 300);
            MinimumSize = new Size(420, 240);
            AcceptButton = _ok;
            CancelButton = _cancel;

            _list.Columns.Add("Classification", 200);
            _list.Columns.Add("Pattern", 300);

            BuildLayout();
            RefreshList();

            _list.SelectedIndexChanged += (_, _) => UpdateButtons();
            _add.Click += OnAdd;
            _edit.Click += OnEdit;
            _remove.Click += OnRemove;

            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_ok);
        }

        private void BuildLayout()
        {
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.TopDown, Width = 100, Padding = new Padding(8) };
            buttons.Controls.AddRange(new Control[] { _add, _edit, _remove });

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
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (Identifier id in _items)
            {
                var item = new ListViewItem(id.Classification ?? string.Empty);
                item.SubItems.Add(id.Pattern ?? string.Empty);
                _list.Items.Add(item);
            }
            _list.EndUpdate();
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            _edit.Enabled = _list.SelectedItems.Count > 0;
            _remove.Enabled = _list.SelectedItems.Count > 0;
        }

        private void OnAdd(object? sender, EventArgs e)
        {
            var identifier = new Identifier();
            using var dlg = new CustomIdentifierForm(identifier);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _items.Add(identifier);
                RefreshList();
            }
        }

        private void OnEdit(object? sender, EventArgs e)
        {
            if (_list.SelectedIndices.Count == 0)
            {
                return;
            }
            int i = _list.SelectedIndices[0];
            using var dlg = new CustomIdentifierForm(_items[i]);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                RefreshList();
            }
        }

        private void OnRemove(object? sender, EventArgs e)
        {
            if (_list.SelectedIndices.Count == 0)
            {
                return;
            }
            if (MessageBox.Show("Remove the selected custom identifier?", "Custom Identifiers",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _items.RemoveAt(_list.SelectedIndices[0]);
                RefreshList();
            }
        }

        private static Identifier Clone(Identifier source) => new()
        {
            Classification = source.Classification,
            Pattern = source.Pattern,
            CaseSensitive = source.CaseSensitive,
            GroupNumber = source.GroupNumber,
            Enabled = source.Enabled,
            Strategies = source.Strategies
        };
    }

    /// <summary>Edits a single custom <see cref="Identifier"/>.</summary>
    internal sealed class CustomIdentifierForm : Form
    {
        private readonly Identifier _identifier;
        private readonly TextBox _classification = new();
        private readonly TextBox _pattern = new();
        private readonly CheckBox _caseSensitive = new() { Text = "Case sensitive", AutoSize = true };
        private readonly Button _ok = new() { Text = "OK", DialogResult = DialogResult.OK };
        private readonly Button _cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel };

        public CustomIdentifierForm(Identifier identifier)
        {
            _identifier = identifier;

            Text = "Custom Identifier";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 160);
            AcceptButton = _ok;
            CancelButton = _cancel;

            var classLabel = new Label { Text = "Classification:", AutoSize = true, Location = new Point(12, 18) };
            _classification.SetBounds(120, 15, 285, 23);
            var patternLabel = new Label { Text = "Pattern (regex):", AutoSize = true, Location = new Point(12, 50) };
            _pattern.SetBounds(120, 47, 285, 23);
            _caseSensitive.Location = new Point(120, 80);
            _ok.SetBounds(244, 120, 80, 26);
            _cancel.SetBounds(328, 120, 80, 26);

            Controls.AddRange(new Control[] { classLabel, _classification, patternLabel, _pattern, _caseSensitive, _ok, _cancel });

            _classification.Text = identifier.Classification ?? string.Empty;
            _pattern.Text = identifier.Pattern ?? string.Empty;
            _caseSensitive.Checked = identifier.CaseSensitive;

            _ok.Click += (_, _) =>
            {
                _identifier.Classification = _classification.Text;
                _identifier.Pattern = _pattern.Text;
                _identifier.CaseSensitive = _caseSensitive.Checked;
            };

            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_ok);
        }
    }
}
