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

using System.Drawing;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Finds layout defects in a shown form: overlapping sibling controls, children clipped by a
    /// non-scrolling parent, and text that doesn't fit its control. Walks every tab page.
    /// </summary>
    internal static class LayoutAudit
    {
        private const int Tolerance = 1;

        public static List<string> Audit(Form form)
        {
            var problems = new List<string>();
            AuditTree(form, problems);
            return problems.Distinct().ToList();
        }

        /// <summary>Fails with the full list of problems, if any.</summary>
        public static void AssertClean(List<string> problems) =>
            Xunit.Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));

        private static void AuditTree(Control parent, List<string> problems)
        {
            if (parent is TabControl tabs)
            {
                // Only the selected page is laid out and visible, so audit each in turn.
                TabPage? original = tabs.SelectedTab;
                foreach (TabPage page in tabs.TabPages)
                {
                    tabs.SelectedTab = page;
                    Application.DoEvents();
                    AuditTree(page, problems);
                }
                tabs.SelectedTab = original;
                Application.DoEvents();
                return;
            }

            List<Control> children = parent.Controls.Cast<Control>()
                .Where(c => c.Visible && c.Width > 0 && c.Height > 0)
                .ToList();

            for (int i = 0; i < children.Count; i++)
            {
                for (int j = i + 1; j < children.Count; j++)
                {
                    Rectangle a = Shrink(children[i].Bounds);
                    Rectangle b = Shrink(children[j].Bounds);
                    if (a.IntersectsWith(b))
                    {
                        problems.Add($"{Path(parent)}: '{Name(children[i])}' {children[i].Bounds} overlaps '{Name(children[j])}' {children[j].Bounds}");
                    }
                }
            }

            bool scrolls = parent is ScrollableControl { AutoScroll: true };
            if (!scrolls && parent is not ToolStrip)
            {
                Size client = parent.ClientSize;
                foreach (Control child in children)
                {
                    if (child.Right > client.Width + Tolerance || child.Bottom > client.Height + Tolerance)
                    {
                        problems.Add($"{Path(parent)}: '{Name(child)}' {child.Bounds} is clipped by its parent (client {client})");
                    }
                }
            }

            foreach (Control child in children)
            {
                CheckText(child, problems);
                if (child is not ToolStrip and not ListView and not DataGridView and not TextBoxBase
                    and not ComboBox and not NumericUpDown and not ListBox and not WebBrowser)
                {
                    AuditTree(child, problems);
                }
            }
        }

        private static void CheckText(Control c, List<string> problems)
        {
            if (string.IsNullOrEmpty(c.Text) || c is not (Label or ButtonBase))
            {
                return;
            }
            if (c is LinkLabel or Label { AutoSize: true } || c is ButtonBase { AutoSize: true })
            {
                // Auto-sized controls fit their text by construction; only their placement can be wrong.
                return;
            }

            // Fixed-size label/button: its preferred size at its current width must fit.
            Size needed = c is Label
                ? c.GetPreferredSize(new Size(c.Width, 0))
                : c.GetPreferredSize(Size.Empty);
            bool wraps = c is Label;
            if ((!wraps && needed.Width > c.Width + Tolerance) || needed.Height > c.Height + Tolerance)
            {
                problems.Add($"{Path(c.Parent!)}: '{Name(c)}' text needs {needed} but has {c.Size}");
            }
        }

        private static Rectangle Shrink(Rectangle r) => Rectangle.Inflate(r, -Tolerance, -Tolerance);

        private static string Name(Control c) =>
            !string.IsNullOrEmpty(c.Name) ? c.Name
            : !string.IsNullOrEmpty(c.Text) ? $"{c.GetType().Name}:{Trim(c.Text)}"
            : c.GetType().Name;

        private static string Trim(string s) => s.Length > 30 ? s[..30] + "…" : s;

        private static string Path(Control c) =>
            c.Parent is null ? Name(c) : Path(c.Parent) + "/" + Name(c);
    }
}
