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

using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace PhilterDesktop
{
    /// <summary>
    /// Side-by-side text diff of a source file (left) and its redacted output (right). Lines are
    /// aligned and color-coded; both panes scroll together (one grid, two columns).
    /// </summary>
    public partial class DiffViewerForm : Form
    {
        private static readonly Color DeletedColor = Color.FromArgb(255, 224, 224); // removed (left)
        private static readonly Color InsertedColor = Color.FromArgb(224, 255, 224); // added (right)
        private static readonly Color ModifiedColor = Color.FromArgb(255, 246, 213); // changed
        private static readonly Color ImaginaryColor = Color.FromArgb(245, 245, 245); // blank filler

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public DiffViewerForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
        }

        public DiffViewerForm(string beforeText, string afterText, string beforeTitle, string afterTitle)
            : this()
        {
            _beforeColumn.HeaderText = $"Before — {beforeTitle}";
            _afterColumn.HeaderText = $"After — {afterTitle}";
            BuildDiff(beforeText, afterText);
        }

        private void BuildDiff(string beforeText, string afterText)
        {
            SideBySideDiffModel model = SideBySideDiffBuilder.Diff(beforeText, afterText);

            _grid.SuspendLayout();
            _grid.Rows.Clear();
            int lineCount = Math.Max(model.OldText.Lines.Count, model.NewText.Lines.Count);
            for (int i = 0; i < lineCount; i++)
            {
                DiffPiece? left = i < model.OldText.Lines.Count ? model.OldText.Lines[i] : null;
                DiffPiece? right = i < model.NewText.Lines.Count ? model.NewText.Lines[i] : null;

                int row = _grid.Rows.Add(LineText(left), LineText(right));
                _grid.Rows[row].Cells[0].Style.BackColor = ColorFor(left);
                _grid.Rows[row].Cells[1].Style.BackColor = ColorFor(right);
            }
            _grid.ResumeLayout();
            _grid.ClearSelection();
        }

        private static string LineText(DiffPiece? piece) =>
            piece is null || piece.Type == ChangeType.Imaginary ? string.Empty : piece.Text ?? string.Empty;

        private static Color ColorFor(DiffPiece? piece) => piece?.Type switch
        {
            ChangeType.Deleted => DeletedColor,
            ChangeType.Inserted => InsertedColor,
            ChangeType.Modified => ModifiedColor,
            ChangeType.Imaginary => ImaginaryColor,
            _ => Color.White
        };
    }
}
