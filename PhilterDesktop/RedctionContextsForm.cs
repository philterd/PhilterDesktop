using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PhilterData;

namespace PhilterDesktop
{
    public partial class RedctionContextsForm : Form
    {
        private ContextRepository _repo;

        public RedctionContextsForm(ContextRepository repo)
        {
            InitializeComponent();
            _repo = repo;
        }

        private void RedctionContextsForm_Load(object sender, EventArgs e)
        {
            LoadContexts();
        }

        private void LoadContexts()
        {
            listBoxContexts.Items.Clear();
            
            var contexts = _repo.GetAllOrderedByDate();
            foreach (var context in contexts)
            {
                listBoxContexts.Items.Add(new ContextListItem(context));
            }

            // Update button states
            UpdateButtonStates();
        }

        private void BtnCreate_Click(object? sender, EventArgs e)
        {
            using var inputDialog = new Form();
            inputDialog.Text = "Create Redaction Context";
            inputDialog.ClientSize = new Size(400, 120);
            inputDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            inputDialog.StartPosition = FormStartPosition.CenterParent;
            inputDialog.MaximizeBox = false;
            inputDialog.MinimizeBox = false;

            var label = new Label
            {
                Text = "Context Name:",
                Location = new Point(10, 15),
                AutoSize = true
            };

            var textBox = new TextBox
            {
                Location = new Point(10, 35),
                Width = 370
            };

            var btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(220, 70),
                Width = 80
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(310, 70),
                Width = 80
            };

            inputDialog.Controls.Add(label);
            inputDialog.Controls.Add(textBox);
            inputDialog.Controls.Add(btnOk);
            inputDialog.Controls.Add(btnCancel);
            inputDialog.AcceptButton = btnOk;
            inputDialog.CancelButton = btnCancel;

            if (inputDialog.ShowDialog() == DialogResult.OK)
            {
                var contextName = textBox.Text.Trim();
                
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
            if (listBoxContexts.SelectedItem is not ContextListItem selectedItem)
                return;

            var result = MessageBox.Show(
                $"Are you sure you want to delete the context '{selectedItem.Context.Name}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _repo.Delete(selectedItem.Context.Id);
                LoadContexts();

                MessageBox.Show("Redaction context deleted successfully.", "Success", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ListBoxContexts_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            btnDelete.Enabled = listBoxContexts.SelectedItem != null;
        }

        private void BtnClose_Click(object? sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Helper class to wrap ContextEntity for ListBox display.
        /// </summary>
        private class ContextListItem
        {
            public ContextEntity Context { get; }

            public ContextListItem(ContextEntity context)
            {
                Context = context;
            }

            public override string ToString()
            {
                return $"{Context.Name} (Created: {Context.CreatedAt:g})";
            }
        }
    }
}
