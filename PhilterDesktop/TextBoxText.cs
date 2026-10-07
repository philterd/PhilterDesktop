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

using System.Text;

namespace PhilterDesktop
{
    /// <summary>
    /// Text prepared for a Windows TextBox, which only breaks lines on CRLF (a lone LF or CR shows as
    /// nothing, running lines together). <see cref="Display"/> has every line break as CRLF, and
    /// <see cref="ToOriginal"/> maps a position in it back to the original text, so selections still
    /// index the original exactly.
    /// </summary>
    internal sealed class TextBoxText
    {
        // Display positions of the lone line breaks that were widened to CRLF, ascending.
        private readonly List<int> _inserted = new();

        public TextBoxText(string original)
        {
            ArgumentNullException.ThrowIfNull(original);
            Original = original;

            var display = new StringBuilder(original.Length);
            for (int i = 0; i < original.Length; i++)
            {
                char c = original[i];
                if (c == '\r' && i + 1 < original.Length && original[i + 1] == '\n')
                {
                    display.Append("\r\n");
                    i++;
                }
                else if (c == '\n' || c == '\r')
                {
                    // A lone break becomes CRLF; counting its first half as the added character makes a
                    // position inside the break map to the break's start.
                    _inserted.Add(display.Length);
                    display.Append("\r\n");
                }
                else
                {
                    display.Append(c);
                }
            }
            Display = display.ToString();
        }

        public string Original { get; }

        public string Display { get; }

        /// <summary>
        /// The original-text position for a display position (clamped to the text). A position inside a
        /// completed line break maps to the start of that break, so a selection covers it only when it
        /// covers the whole break.
        /// </summary>
        public int ToOriginal(int displayIndex)
        {
            int index = Math.Clamp(displayIndex, 0, Display.Length);
            int found = _inserted.BinarySearch(index);
            int insertedBefore = found >= 0 ? found : ~found; // inserted characters strictly before index
            return index - insertedBefore;
        }
    }
}
