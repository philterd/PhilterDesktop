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
    /// Hardens RTF before it is loaded into the Windows <see cref="RichTextBox"/> for redaction.
    ///
    /// <para>RTF can embed OLE objects (<c>{\object …}</c> groups), which carry a self-contained copy
    /// of another document (e.g. an embedded spreadsheet or Word file). Those copies are <b>not</b>
    /// visible text, so the position-based redactor never touches them — an embedded object could ferry
    /// an unredacted copy of sensitive content straight into the output. Loading such a document into
    /// <c>RichTextBox</c> can also instantiate the embedded OLE server, a well-known malicious-RTF code
    /// execution vector.</para>
    ///
    /// <para>So we strip embedded object groups from the raw RTF up front. Embedded images
    /// (<c>{\pict …}</c>) are deliberately left intact: removing them would damage legitimate documents
    /// (logos, charts), and text inside images is a separate OCR concern handled elsewhere.</para>
    /// </summary>
    internal static class RtfSanitizer
    {
        /// <summary>
        /// Removes every top-level <c>{\object …}</c> group (including its nested <c>\objdata</c>,
        /// <c>\objclass</c>, and <c>\result</c> destinations) from <paramref name="rtf"/>, returning
        /// valid RTF. Input without embedded objects is returned effectively unchanged.
        /// </summary>
        public static string RemoveEmbeddedObjects(string rtf)
        {
            if (string.IsNullOrEmpty(rtf) || rtf.IndexOf("\\object", StringComparison.Ordinal) < 0)
            {
                return rtf;
            }

            var sb = new StringBuilder(rtf.Length);
            int i = 0;
            while (i < rtf.Length)
            {
                char c = rtf[i];

                // Preserve escaped characters verbatim (\{  \}  \\), so escaped braces don't throw off
                // group matching.
                if (c == '\\' && i + 1 < rtf.Length)
                {
                    sb.Append(c);
                    sb.Append(rtf[i + 1]);
                    i += 2;
                    continue;
                }

                if (c == '{' && IsControlWordAt(rtf, i + 1, "object"))
                {
                    i = SkipGroup(rtf, i); // drop the whole {\object …} group
                    continue;
                }

                sb.Append(c);
                i++;
            }
            return sb.ToString();
        }

        // True if, starting at index pos, the text is a backslash control word exactly equal to
        // <paramref name="word"/> (delimited by a non-letter), e.g. "\object" but not "\objemb".
        private static bool IsControlWordAt(string rtf, int pos, string word)
        {
            if (pos >= rtf.Length || rtf[pos] != '\\')
            {
                return false;
            }
            int start = pos + 1;
            if (start + word.Length > rtf.Length)
            {
                return false;
            }
            if (string.CompareOrdinal(rtf, start, word, 0, word.Length) != 0)
            {
                return false;
            }
            // The control word must end here (RTF control words are letters only).
            int after = start + word.Length;
            return after >= rtf.Length || !char.IsAsciiLetter(rtf[after]);
        }

        // Given the index of an opening '{', returns the index just past its matching '}', honoring
        // nesting and escaped braces. If unbalanced, returns the end of the string.
        private static int SkipGroup(string rtf, int openBrace)
        {
            int depth = 0;
            int i = openBrace;
            while (i < rtf.Length)
            {
                char c = rtf[i];
                if (c == '\\' && i + 1 < rtf.Length)
                {
                    i += 2; // skip escaped char (incl. \{ and \})
                    continue;
                }
                if (c == '{')
                {
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i + 1;
                    }
                }
                i++;
            }
            return rtf.Length;
        }
    }
}
