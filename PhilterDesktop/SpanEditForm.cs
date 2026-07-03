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
    /// <summary>The way a span's location is expressed, which depends on the document type.</summary>
    public enum SpanPositionKind
    {
        /// <summary>Plain text: character start/stop offsets into the whole document.</summary>
        TextOffset,
        /// <summary>Word: paragraph index plus character start/stop offsets within that paragraph.</summary>
        Paragraph,
        /// <summary>PDF: page number plus a bounding box (no character offsets).</summary>
        Pdf
    }

    /// <summary>
    /// Adds or edits a single redaction span <b>by position</b> (not by text). The location fields
    /// shown depend on the document type: character offsets for text, paragraph + offsets for Word,
    /// and page + bounding box for PDF. When editing a detected span the position is fixed (anchored
    /// to where it was found); only the replacement is editable.
    /// </summary>
    public partial class SpanEditForm : Form
    {
        private SpanPositionKind _kind;
        private int _maxOffset; // document/paragraph length for bounds checking text offsets (0 = unknown)
        // For Word (Paragraph kind): the length of each paragraph's text, indexed by paragraph. Lets the
        // offset bounds track the selected paragraph so a manual span can't run past that paragraph's end.
        private IReadOnlyList<int>? _paragraphLengths;

        // The paragraph field is shown 1-based (to match the list's "¶ N"); ParagraphIndex is 0-based.
        public int Paragraph => (int)_paragraph.Value - 1;
        public int Start => (int)_start.Value;
        public int Stop => (int)_stop.Value;
        public int Page => (int)_page.Value;
        public double LowerLeftX => (double)_llx.Value;
        public double LowerLeftY => (double)_lly.Value;
        public double UpperRightX => (double)_urx.Value;
        public double UpperRightY => (double)_ury.Value;
        public string Replacement => _replacement.Text;

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public SpanEditForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
        }

        /// <param name="positionEditable">
        /// When false the location fields are shown read-only (used when editing a detected span,
        /// whose position is anchored); only the replacement can change.
        /// </param>
        /// <param name="maxOffset">
        /// The length of the text the offsets index into (the whole document, for plain text). When
        /// &gt; 0, the start/stop fields are capped at it and validated so a manual span can't run past
        /// the end. For Word, pass <paramref name="paragraphLengths"/> instead.
        /// </param>
        /// <param name="paragraphLengths">
        /// For Word (<see cref="SpanPositionKind.Paragraph"/>): the length of each paragraph's text, by
        /// paragraph index. When supplied, the paragraph field is capped at the paragraph count and the
        /// start/stop bounds follow the selected paragraph, so a manual span can't run past that
        /// paragraph's end (which would otherwise be silently dropped when the redaction is applied).
        /// </param>
        public SpanEditForm(string title, SpanPositionKind kind, RedactionSpanEntity span, bool positionEditable,
            int maxOffset = 0, IReadOnlyList<int>? paragraphLengths = null)
            : this()
        {
            _kind = kind;
            _maxOffset = maxOffset;
            _paragraphLengths = paragraphLengths;
            Text = title;

            bool perParagraph = kind == SpanPositionKind.Paragraph && paragraphLengths is { Count: > 0 };
            if (perParagraph)
            {
                _paragraph.Maximum = paragraphLengths!.Count; // 1-based field; can't exceed the paragraph count
            }
            else if (maxOffset > 0)
            {
                _start.Maximum = maxOffset;
                _stop.Maximum = maxOffset;
            }

            // Seed the fields from the span being added/edited (clamped to each control's range).
            Set(_paragraph, span.ParagraphIndex < 0 ? 1 : span.ParagraphIndex + 1);
            Set(_start, span.CharacterStart);
            Set(_stop, span.CharacterEnd);
            Set(_page, span.PageNumber < 1 ? 1 : span.PageNumber);
            Set(_llx, (decimal)span.LowerLeftX);
            Set(_lly, (decimal)span.LowerLeftY);
            Set(_urx, (decimal)span.UpperRightX);
            Set(_ury, (decimal)span.UpperRightY);
            _replacement.Text = span.Replacement;

            // Track the selected paragraph's length so the offset bounds/validation follow it.
            if (perParagraph)
            {
                UpdateParagraphOffsetBounds();
                _paragraph.ValueChanged += (_, _) => UpdateParagraphOffsetBounds();
            }

            foreach (NumericUpDown n in new[] { _paragraph, _start, _stop, _page, _llx, _lly, _urx, _ury })
            {
                n.Enabled = positionEditable;
            }

            ShowFieldsForKind();
        }

        // Caps the start/stop offset fields at the currently-selected paragraph's length (and updates the
        // value used for validation), so an offset can't point past that paragraph's text.
        private void UpdateParagraphOffsetBounds()
        {
            if (_paragraphLengths is null)
            {
                return;
            }
            int index = (int)_paragraph.Value - 1;
            int length = index >= 0 && index < _paragraphLengths.Count ? _paragraphLengths[index] : 0;
            _maxOffset = length;
            _start.Maximum = length; // NumericUpDown clamps its value down if it now exceeds the maximum
            _stop.Maximum = length;
        }

        // Show only the location fields relevant to this document type; the rest collapse (their
        // table rows are AutoSize, so hidden controls take no space). Replacement is always shown.
        private void ShowFieldsForKind()
        {
            bool paragraph = _kind == SpanPositionKind.Paragraph;
            bool text = _kind == SpanPositionKind.TextOffset || paragraph;
            bool pdf = _kind == SpanPositionKind.Pdf;

            ShowRow(_paragraphLabel, _paragraph, paragraph);
            ShowRow(_startLabel, _start, text);
            ShowRow(_stopLabel, _stop, text);
            ShowRow(_pageLabel, _page, pdf);
            ShowRow(_llxLabel, _llx, pdf);
            ShowRow(_llyLabel, _lly, pdf);
            ShowRow(_urxLabel, _urx, pdf);
            ShowRow(_uryLabel, _ury, pdf);
        }

        private static void ShowRow(Control label, Control field, bool visible)
        {
            label.Visible = visible;
            field.Visible = visible;
        }

        // Size the dialog to its content once layout/scaling has settled, so the buttons fit at any DPI.
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ClientSize = _root.PreferredSize;
        }

        private void OnOk(object? sender, EventArgs e)
        {
            if (!_start.Enabled && _kind != SpanPositionKind.Pdf)
            {
                return; // position read-only (editing a detected span) — nothing to validate
            }

            if (_kind == SpanPositionKind.Pdf)
            {
                if (_urx.Enabled && (UpperRightX <= LowerLeftX || UpperRightY <= LowerLeftY))
                {
                    Warn("The upper-right corner must be above and to the right of the lower-left corner.");
                }
            }
            else if (_start.Enabled)
            {
                string? error = ValidateTextOffsets(Start, Stop, _maxOffset);
                if (error is not null)
                {
                    Warn(error);
                }
            }
        }

        /// <summary>
        /// Validates manual text-offset spans: non-negative, start before stop, and (when
        /// <paramref name="maxOffset"/> &gt; 0) within the document length. Returns an error message or null.
        /// </summary>
        internal static string? ValidateTextOffsets(int start, int stop, int maxOffset)
        {
            if (start < 0 || stop < 0)
            {
                return "Character offsets can't be negative.";
            }
            if (stop <= start)
            {
                return "The stop character must be greater than the start character.";
            }
            if (maxOffset > 0 && stop > maxOffset)
            {
                return $"The stop character can't be past the end of the text (length {maxOffset}).";
            }
            return null;
        }

        private void Warn(string message)
        {
            MessageBox.Show(message, "Redaction", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
        }

        private static void Set(NumericUpDown control, decimal value) =>
            control.Value = value < control.Minimum ? control.Minimum : value > control.Maximum ? control.Maximum : value;
    }
}
