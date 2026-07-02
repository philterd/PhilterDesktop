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

using System.Globalization;
using Phileas.Policy;

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// Edits a policy's fixed PDF redaction regions (bounding boxes). Each box always paints over a
    /// rectangle on a page, regardless of content — for signatures, photos, logos, or fixed form fields
    /// that text detection can't catch. Regions are entered via <see cref="AddRegionForm"/> or drawn on a
    /// sample PDF via <see cref="PdfRegionPickerForm"/>.
    /// </summary>
    internal sealed class PdfRegionsForm : Form
    {
        private readonly ListView _list = new()
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            HideSelection = false
        };

        /// <summary>The edited boxes (valid on <see cref="DialogResult.OK"/>).</summary>
        public List<BoundingBox> Boxes { get; private set; } = new();

        public PdfRegionsForm(IEnumerable<BoundingBox> existing)
        {
            Text = "PDF Redaction Regions";
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            Size = new System.Drawing.Size(600, 380);
            MinimumSize = Size; // don't let it shrink below its opening size

            var info = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = false,
                Height = 40,
                Padding = new Padding(6),
                Text = "Rectangles always painted over when redacting a PDF (signatures, photos, logos, form " +
                       "fields). Add a region by entering its coordinates, or draw it on a sample PDF."
            };

            foreach ((string header, int width) in new[] { ("Page", 60), ("X", 70), ("Y", 70), ("Width", 70), ("Height", 70), ("Color", 100) })
            {
                _list.Columns.Add(header, width);
            }
            foreach (PdfRegionEntry entry in PdfRegionEntry.FromBoxes(existing)) // regroup same-rect boxes into one row
            {
                AddItem(entry);
            }
            _list.DoubleClick += (_, _) => EditSelected();

            var add = new Button { Text = "&Add Region…", AutoSize = true };
            add.Click += OnAddRegion;
            var draw = new Button { Text = "Dra&w on PDF…", AutoSize = true };
            draw.Click += OnDrawOnPdf;
            var remove = new Button { Text = "&Remove", AutoSize = true, Enabled = false };
            remove.Click += (_, _) => RemoveSelected();

            // Right-click menu: Add is always available; Modify/Duplicate/Remove need a selected region.
            var menu = new ContextMenuStrip();
            var miAdd = new ToolStripMenuItem("&Add Region…", null, (_, _) => OnAddRegion(this, EventArgs.Empty));
            var miModify = new ToolStripMenuItem("&Modify Region…", null, (_, _) => EditSelected());
            var miDuplicate = new ToolStripMenuItem("D&uplicate", null, (_, _) => DuplicateSelected());
            var miRemove = new ToolStripMenuItem("&Remove", null, (_, _) => RemoveSelected());
            menu.Items.AddRange(new ToolStripItem[] { miAdd, miModify, miDuplicate, miRemove });
            _list.ContextMenuStrip = menu;

            void UpdateSelectionActions()
            {
                bool hasSelection = _list.SelectedItems.Count > 0;
                remove.Enabled = hasSelection;
                miModify.Enabled = hasSelection;
                miDuplicate.Enabled = hasSelection;
                miRemove.Enabled = hasSelection;
            }
            _list.SelectedIndexChanged += (_, _) => UpdateSelectionActions();
            menu.Opening += (_, _) => UpdateSelectionActions();
            UpdateSelectionActions(); // start with nothing selected

            var ok = new Button { Text = "&OK", DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = "&Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            // Each row is one entry (page spec + rectangle); expand to one box per page when saving.
            ok.Click += (_, _) => Boxes = _list.Items.Cast<ListViewItem>()
                .SelectMany(i => ((PdfRegionEntry)i.Tag!).ToBoundingBoxes()).ToList();

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 40, Padding = new Padding(6) };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);
            buttons.Controls.Add(remove);
            buttons.Controls.Add(draw);
            buttons.Controls.Add(add);

            Controls.Add(_list);
            Controls.Add(buttons);
            Controls.Add(info);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        private void RemoveSelected()
        {
            foreach (ListViewItem item in _list.SelectedItems)
            {
                _list.Items.Remove(item);
            }
        }

        private void DuplicateSelected()
        {
            // Copy each selected region to a new row (entries are immutable, so reusing them is safe).
            List<PdfRegionEntry> copies = _list.SelectedItems.Cast<ListViewItem>().Select(i => (PdfRegionEntry)i.Tag!).ToList();
            foreach (PdfRegionEntry entry in copies)
            {
                AddItem(entry);
            }
        }

        private void OnAddRegion(object? sender, EventArgs e)
        {
            using var dlg = new AddRegionForm();
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Entry is not null)
            {
                AddItem(dlg.Entry); // one row, keeping the pages as entered
            }
        }

        private void OnDrawOnPdf(object? sender, EventArgs e)
        {
            using var open = new OpenFileDialog { Filter = "PDF files (*.pdf)|*.pdf", Title = "Choose a sample PDF to draw on" };
            if (open.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(open.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not read the PDF: {ex.Message}", "Draw PDF Regions", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var picker = new PdfRegionPickerForm(bytes);
            if (picker.ShowDialog(this) == DialogResult.OK)
            {
                foreach (BoundingBox b in picker.Boxes) // drawn regions are single-page
                {
                    AddItem(PdfRegionEntry.FromBox(b));
                }
            }
        }

        private void EditSelected()
        {
            if (_list.SelectedItems.Count == 0)
            {
                return;
            }
            ListViewItem item = _list.SelectedItems[0];
            using var dlg = new AddRegionForm((PdfRegionEntry)item.Tag!);
            if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Entry is not null)
            {
                int index = item.Index;
                _list.Items.RemoveAt(index);
                AddItem(dlg.Entry, index);
            }
        }

        private void AddItem(PdfRegionEntry entry, int index = -1)
        {
            string S(float v) => v.ToString(CultureInfo.InvariantCulture);
            var item = new ListViewItem(entry.PageDisplay) { Tag = entry };
            item.SubItems.Add(S(entry.X));
            item.SubItems.Add(S(entry.Y));
            item.SubItems.Add(S(entry.W));
            item.SubItems.Add(S(entry.H));
            item.SubItems.Add(entry.Color);
            if (index >= 0)
            {
                _list.Items.Insert(index, item);
            }
            else
            {
                _list.Items.Add(item);
            }
        }
    }
}
