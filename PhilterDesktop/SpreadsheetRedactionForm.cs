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
    /// Configures redaction of a single spreadsheet (<c>.xlsx</c>) or CSV file and adds it to the
    /// redaction queue (the actual redaction runs in the background like any other queued document).
    /// Detected sensitive information is removed from every cell; the user may also tick whole columns
    /// to remove their data cells entirely — useful for name/ID columns the detector can miss when a
    /// value sits alone in a cell (the column's header label is kept).
    /// </summary>
    internal sealed partial class SpreadsheetRedactionForm : Form
    {
        private readonly PolicyRepository _policies = null!;
        private readonly ContextRepository _contexts = null!;
        private readonly RedactionQueueRepository _queue = null!;
        private readonly SettingsEntity _settings = new();

        private List<SpreadsheetColumn> _columnList = new();

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public SpreadsheetRedactionForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_redact);

            _selectAll.LinkClicked += (_, _) => SetAllColumns(true);
            _clearAll.LinkClicked += (_, _) => SetAllColumns(false);
            // ItemCheck fires before the state flips, so refresh the count after it settles.
            _columns.ItemCheck += (_, _) => BeginInvoke(UpdateSelectedCount);
        }

        private void SetAllColumns(bool selected)
        {
            for (int i = 0; i < _columns.Items.Count; i++)
            {
                _columns.SetItemChecked(i, selected);
            }
            UpdateSelectedCount();
        }

        private void UpdateSelectedCount()
        {
            int n = _columns.CheckedIndices.Count;
            _selectedCount.Text = $"{n} selected";
            bool hasColumns = _columns.Items.Count > 0;
            _selectAll.Enabled = hasColumns;
            _clearAll.Enabled = hasColumns;
        }

        public SpreadsheetRedactionForm(
            PolicyRepository policies,
            ContextRepository contexts,
            RedactionQueueRepository queue,
            SettingsEntity settings,
            string? initialSource = null)
            : this()
        {
            _policies = policies;
            _contexts = contexts;
            _queue = queue;
            _settings = settings;
            if (!string.IsNullOrEmpty(initialSource))
            {
                _source.Text = initialSource;
            }
        }

        private void SpreadsheetRedactionForm_Load(object? sender, EventArgs e)
        {
            if (_policies is null || _contexts is null)
            {
                return; // designer / smoke-test construction
            }

            LoadNames(_policy, _policies.GetAll().Select(p => p.Name), _settings.LastPolicy);
            LoadNames(_context, _contexts.GetAll().Select(c => c.Name), _settings.LastContext);

            if (!string.IsNullOrEmpty(_source.Text))
            {
                LoadFile(_source.Text);
            }
            UpdateCanRedact();
        }

        // The Redact button is only enabled once a real file is chosen.
        private void UpdateCanRedact() => _redact.Enabled = File.Exists(_source.Text.Trim());

        private static void LoadNames(ComboBox combo, IEnumerable<string> names, string? preferred)
        {
            combo.Items.Clear();
            foreach (string name in names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            {
                combo.Items.Add(name);
            }
            ComboSelection.Select(combo, preferred);
        }

        private void OnBrowse(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Choose a spreadsheet",
                Filter = "Spreadsheets (*.xlsx;*.csv)|*.xlsx;*.csv|Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv",
                CheckFileExists = true
            };
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _source.Text = dlg.FileName;
                LoadFile(dlg.FileName);
                UpdateCanRedact();
            }
        }

        // Loads the worksheet list (Excel only) and the column checklist for the chosen file. For .xlsx,
        // redaction targets a single worksheet, so the columns shown are that sheet's.
        private void LoadFile(string path)
        {
            bool isXlsx = !path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
            _worksheetLabel.Visible = isXlsx;
            _worksheet.Visible = isXlsx;

            if (isXlsx)
            {
                // Suppress the change handler while repopulating so it doesn't reload columns mid-setup.
                _worksheet.SelectedIndexChanged -= OnWorksheetChanged;
                _worksheet.Items.Clear();
                try
                {
                    foreach (string name in XlsxRedactor.ReadSheetNames(path))
                    {
                        _worksheet.Items.Add(name);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, UserError.Describe(ex, path, writing: false), "Redact Spreadsheet",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                if (_worksheet.Items.Count > 0)
                {
                    _worksheet.SelectedIndex = 0;
                }
                _worksheet.SelectedIndexChanged += OnWorksheetChanged;
            }

            LoadColumns(path);
        }

        private void OnWorksheetChanged(object? sender, EventArgs e)
        {
            string path = _source.Text.Trim();
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                LoadColumns(path);
            }
        }

        // Populates the column checklist from the chosen file (the selected worksheet, for Excel).
        private void LoadColumns(string path)
        {
            _columns.Items.Clear();
            _columnList = new List<SpreadsheetColumn>();
            try
            {
                _columnList = path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
                    ? CsvRedactor.ReadColumns(path)
                    : XlsxRedactor.ReadColumns(path, _worksheet.SelectedItem as string);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, UserError.Describe(ex, path, writing: false), "Redact Spreadsheet",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            foreach (SpreadsheetColumn column in _columnList)
            {
                _columns.Items.Add(column.Label);
            }
            UpdateSelectedCount();
        }

        // Adds the configured spreadsheet (with any whole-column selections) to the redaction queue;
        // the background queue processor does the actual redaction. The form just closes.
        private void OnRedact(object? sender, EventArgs e)
        {
            string source = _source.Text.Trim();
            if (string.IsNullOrEmpty(source) || !File.Exists(source))
            {
                MessageBox.Show(this, "Choose a spreadsheet to redact.", "Redact Spreadsheet", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!RedactionService.IsSupported(source))
            {
                MessageBox.Show(this, "Unsupported file type. Choose a .xlsx or .csv file.", "Redact Spreadsheet", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_policy.SelectedItem is null || _context.SelectedItem is null)
            {
                MessageBox.Show(this, "Choose a policy and a context.", "Redact Spreadsheet", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Map the checked rows back to 1-based column indices.
            var fullColumns = new List<int>();
            foreach (int index in _columns.CheckedIndices)
            {
                if (index >= 0 && index < _columnList.Count)
                {
                    fullColumns.Add(_columnList[index].Index);
                }
            }

            if (!LargeFileWarning.ConfirmIfLarge(this, new[] { source }))
            {
                return;
            }

            // .xlsx redaction targets a single worksheet; .csv has none.
            string worksheet = source.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                ? _worksheet.SelectedItem as string ?? string.Empty
                : string.Empty;

            bool queued = QueueBulkActions.TryEnqueue(_queue, new RedactionQueueEntity
            {
                Name = source,
                Policy = _policy.Text,
                Context = _context.Text,
                FullyRedactedColumns = fullColumns,
                Worksheet = worksheet
            });
            if (!queued)
            {
                MessageBox.Show(this, "This spreadsheet is already queued with the same policy and context.",
                    "Redact Spreadsheet", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return; // leave the dialog open
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
