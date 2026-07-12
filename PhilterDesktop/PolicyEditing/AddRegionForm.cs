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
    /// Enters or edits a fixed PDF redaction region (a bounding box). Coordinates are PDF points (1/72"),
    /// origin bottom-left. The page field accepts 0 (all pages), 2- (all but the first — an open-ended
    /// range to the last page), a single page, a range (2-5), or a list (1,2,5). Color is optional.
    /// </summary>
    internal sealed class AddRegionForm : Form
    {
        private readonly TextBox _page = new() { Width = 120 };
        private readonly TextBox _x = new() { Width = 90 };
        private readonly TextBox _y = new() { Width = 90 };
        private readonly TextBox _w = new() { Width = 90 };
        private readonly TextBox _h = new() { Width = 90 };
        private readonly ComboBox _color = new() { Width = 90, DropDownStyle = ComboBoxStyle.DropDownList };

        private static readonly string[] Colors = { "Black", "White", "Red", "Yellow", "Blue", "Green", "Gray" };

        /// <summary>The parsed region entry (valid on <see cref="DialogResult.OK"/>).</summary>
        public PdfRegionEntry? Entry { get; private set; }

        public AddRegionForm(PdfRegionEntry? existing = null)
        {
            Text = existing is null ? "Add PDF Region" : "Edit PDF Region";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new System.Drawing.Size(480, 290);

            var table = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Padding = new Padding(10), Dock = DockStyle.Top };
            AddRow(table, "Pages (0 = all; 2- = all but first; e.g. 2-5, 1,2,5)", _page);
            AddRow(table, "X", _x);
            AddRow(table, "Y", _y);
            AddRow(table, "Width", _w);
            AddRow(table, "Height", _h);
            AddRow(table, "Color", _color);

            _color.Items.AddRange(Colors);
            string initialColor = existing?.Color ?? PdfRegionPickerForm.DefaultRegionColor;
            int colorIndex = _color.FindStringExact(initialColor);
            if (colorIndex < 0)
            {
                _color.Items.Insert(0, initialColor); // preserve an unlisted/custom color
                colorIndex = 0;
            }
            _color.SelectedIndex = colorIndex;

            if (existing is not null)
            {
                _page.Text = existing.PageSpec;
                _x.Text = existing.X.ToString(CultureInfo.InvariantCulture);
                _y.Text = existing.Y.ToString(CultureInfo.InvariantCulture);
                _w.Text = existing.W.ToString(CultureInfo.InvariantCulture);
                _h.Text = existing.H.ToString(CultureInfo.InvariantCulture);
            }

            var ok = new Button { Text = "&OK", DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = "&Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            ok.Click += OnOk;

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Padding = new Padding(10) };
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);

            Controls.Add(table);
            Controls.Add(buttons);
            AcceptButton = ok;
            CancelButton = cancel;
        }

        private void OnOk(object? sender, EventArgs e)
        {
            if (TryParse(_page.Text, _x.Text, _y.Text, _w.Text, _h.Text, _color.Text, out PdfRegionEntry? entry, out string? error))
            {
                Entry = entry;
            }
            else
            {
                MessageBox.Show(error, "PDF Region", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None; // keep open to fix the value
            }
        }

        private static void AddRow(TableLayoutPanel table, string caption, Control field)
        {
            table.Controls.Add(new Label { Text = caption, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 8, 6) });
            table.Controls.Add(field);
        }

        /// <summary>
        /// Parses region fields into a single <see cref="PdfRegionEntry"/> (kept as entered — one list
        /// row). The page spec is 0 (all pages), 2- (all but the first page), a single page, a range (2-5),
        /// or a comma list (1,2,5, and mixes like 2-5,8); X/Y/W/H must be numbers with W and H positive;
        /// Color is optional. Returns false with a message on bad input.
        /// </summary>
        internal static bool TryParse(string page, string x, string y, string w, string h, string color,
            out PdfRegionEntry? entry, out string? error)
        {
            entry = null;
            if (!ParsePages(page, out _, out _, out error))
            {
                return false;
            }

            const NumberStyles num = NumberStyles.Float | NumberStyles.AllowLeadingSign;
            if (!float.TryParse(x, num, CultureInfo.InvariantCulture, out float fx) ||
                !float.TryParse(y, num, CultureInfo.InvariantCulture, out float fy) ||
                !float.TryParse(w, num, CultureInfo.InvariantCulture, out float fw) ||
                !float.TryParse(h, num, CultureInfo.InvariantCulture, out float fh))
            {
                error = "X, Y, Width, and Height must all be numbers.";
                return false;
            }
            if (fw <= 0 || fh <= 0)
            {
                error = "Width and Height must be greater than zero.";
                return false;
            }

            string fill = string.IsNullOrWhiteSpace(color) ? PdfRegionPickerForm.DefaultRegionColor : color;
            entry = new PdfRegionEntry(page.Trim(), fx, fy, fw, fh, fill);
            return true;
        }

        /// <summary>
        /// Parses a page spec into a distinct, sorted page list. Accepts 0 (all pages), an open-ended range
        /// like 2- (from page 2 to the last page — so 2- is "all but the first page"), a single page (3), a
        /// closed range (2-5), a comma list (1,2,5), or a mix (2-5,8). Pages are 1-based.
        ///
        /// <paramref name="openFrom"/> is 0 for an explicit page list (use <paramref name="pages"/>), or the
        /// first page of an open-ended "to the end" spec (1 = all pages, 2 = all but the first, and so on),
        /// which stays a single deferred box until the page count is known at redaction time.
        /// </summary>
        internal static bool ParsePages(string spec, out List<int> pages, out int openFrom, out string? error)
        {
            pages = new List<int>();
            openFrom = 0;
            error = null;

            string[] parts = (spec ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 0)
            {
                error = "Enter a page: 0 (all pages), 2- (all but the first), a page like 3, a range like 2-5, or a list like 1,2,5.";
                return false;
            }

            var set = new SortedSet<int>();
            foreach (string part in parts)
            {
                if (part == "0")
                {
                    if (parts.Length > 1)
                    {
                        error = "Page 0 means all pages and can't be combined with other pages.";
                        return false;
                    }
                    openFrom = 1; // all pages
                    return true;
                }

                // An open-ended range "N-" means page N through the last page (N- with N=2 is "all but first").
                if (part.EndsWith('-'))
                {
                    if (parts.Length > 1)
                    {
                        error = "An open-ended range like \"2-\" runs to the last page and can't be combined with other pages.";
                        return false;
                    }
                    if (int.TryParse(part[..^1].Trim(), out int from) && from >= 1)
                    {
                        openFrom = from;
                        return true;
                    }
                    error = $"'{part}' is not a valid open-ended range (use e.g. 2- for page 2 to the last page).";
                    return false;
                }

                int dash = part.IndexOf('-');
                if (dash > 0)
                {
                    if (int.TryParse(part[..dash].Trim(), out int start) && int.TryParse(part[(dash + 1)..].Trim(), out int end)
                        && start >= 1 && end >= start)
                    {
                        for (int p = start; p <= end; p++)
                        {
                            set.Add(p);
                        }
                        continue;
                    }
                    error = $"'{part}' is not a valid page range (use e.g. 2-5).";
                    return false;
                }

                if (int.TryParse(part, out int single) && single >= 1)
                {
                    set.Add(single);
                    continue;
                }
                error = $"'{part}' is not a valid page (use a whole number of 1 or more, 0 for all pages, or a range like 2-5).";
                return false;
            }

            pages = set.ToList();
            return true;
        }
    }
}
