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
    public partial class Contexts : Form
    {
        private ContextRepository _repo;
        private ContextEntryRepository _contextEntryRepository;

        public Contexts(ContextRepository contextRepository, ContextEntryRepository contextEntryRepository)
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(btnCreate);
            _repo = contextRepository;
            _contextEntryRepository = contextEntryRepository;
        }

        private void RedctionContextsForm_Load(object sender, EventArgs e)
        {
            LoadContexts();
        }

        private void LoadContexts()
        {
            listViewContexts.Items.Clear();
            
            var contexts = _repo.GetAllOrderedByDate();
            foreach (var context in contexts)
            {
                var entryCount = _contextEntryRepository.CountByContext(context.Name);
                var item = new ListViewItem(context.Name);
                item.SubItems.Add(entryCount.ToString());
                item.Tag = context;
                listViewContexts.Items.Add(item);
            }

            // Update button states
            UpdateButtonStates();
        }

        private void BtnCreate_Click(object? sender, EventArgs e)
        {
            using var inputDialog = new CreateContextDialog();

            if (inputDialog.ShowDialog() == DialogResult.OK)
            {
                var contextName = inputDialog.ContextName;
                
                if (string.IsNullOrEmpty(contextName))
                {
                    MessageBox.Show("Context name cannot be empty.", "Validation Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Check if context with same name already exists
                var existing = _repo.FindByName(contextName);
                if (existing != null)
                {
                    MessageBox.Show("A context with this name already exists.", "Duplicate Name", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Create new context
                var newContext = new ContextEntity
                {
                    Name = contextName,
                    CreatedAt = DateTime.UtcNow
                };

                _repo.Insert(newContext);
                LoadContexts();

                MessageBox.Show("Redaction context created successfully.", "Success", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (listViewContexts.SelectedItems.Count == 0)
                return;

            var selectedContext = (ContextEntity)listViewContexts.SelectedItems[0].Tag;

            if (selectedContext.Name.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("The 'default' context cannot be deleted.", "Cannot Delete",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete the context '{selectedContext.Name}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _repo.Delete(selectedContext.Id);
                LoadContexts();

                MessageBox.Show("Redaction context deleted successfully.", "Success", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnEmpty_Click(object? sender, EventArgs e)
        {
            if (listViewContexts.SelectedItems.Count == 0)
                return;

            var selectedContext = (ContextEntity)listViewContexts.SelectedItems[0].Tag;
            var contextName = selectedContext.Name;
            var entryCount = _contextEntryRepository.CountByContext(contextName);

            if (entryCount == 0)
            {
                MessageBox.Show(
                    $"The context '{contextName}' is already empty.",
                    "No Entries",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to empty the context '{contextName}'?\n\n" +
                $"This will delete {entryCount} {(entryCount == 1 ? "entry" : "entries")}.",
                "Confirm Empty Context",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                var deletedCount = _contextEntryRepository.DeleteAllByContext(contextName);
                
                MessageBox.Show(
                    $"Successfully emptied context '{contextName}'.\n{deletedCount} {(deletedCount == 1 ? "entry was" : "entries were")} deleted.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Refresh the list to update the entry count
                LoadContexts();
            }
        }

        private void ListViewContexts_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            var hasSelection = listViewContexts.SelectedItems.Count > 0;
            btnDelete.Enabled = hasSelection;
            btnEmpty.Enabled = hasSelection;
        }

        private void BtnClose_Click(object? sender, EventArgs e)
        {
            Close();
        }
    }
}
