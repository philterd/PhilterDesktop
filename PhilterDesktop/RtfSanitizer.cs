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
    ///
    /// <para>It also strips comment/annotation destinations (<c>{\annotation …}</c> and the
    /// <c>{\*\atnauthor …}</c>/<c>{\*\atnid …}</c> companions). RichEdit renders the spec-conformant
    /// <c>\annotation</c> comment body as visible text glued directly onto the surrounding prose with no
    /// boundary, so a reviewer comment would be silently merged into the document. Removing the comment
    /// groups keeps the redacted body clean — consistent with dropping reviewer comments elsewhere.</para>
    /// </summary>
    internal static class RtfSanitizer
    {
        private static readonly string[] ObjectDestinations = { "object" };

        // Comment/annotation destinations. RichEdit flattens the \annotation comment body into visible
        // prose with no separator, so the whole group set is removed before loading.
        private static readonly string[] CommentDestinations =
            { "annotation", "atnid", "atnauthor", "atndate", "atnparent", "atnicn", "atnref" };

        /// <summary>
        /// Removes every <c>{\object …}</c> group — including the ignorable-destination form
        /// <c>{\*\object …}</c>, and its nested <c>\objdata</c>, <c>\objclass</c>, and <c>\result</c>
        /// destinations — from <paramref name="rtf"/>, returning valid RTF. Input without embedded objects
        /// is returned effectively unchanged.
        /// </summary>
        public static string RemoveEmbeddedObjects(string rtf) => RemoveDestinationGroups(rtf, ObjectDestinations);

        /// <summary>
        /// Removes comment/annotation destination groups (<c>{\annotation …}</c>, <c>{\*\annotation …}</c>,
        /// and the <c>{\*\atnauthor …}</c>/<c>{\*\atnid …}</c>/… companions) so reviewer comments aren't
        /// flattened into the redacted body. Input without comments is returned effectively unchanged.
        /// </summary>
        public static string RemoveComments(string rtf) => RemoveDestinationGroups(rtf, CommentDestinations);

        // Removes every "{\word …}" (or "{\*\word …}") destination group whose control word is in
        // <paramref name="words"/>, honoring nesting and escaped braces. Returns valid RTF.
        private static string RemoveDestinationGroups(string rtf, string[] words)
        {
            if (string.IsNullOrEmpty(rtf) || !ContainsAnyDestination(rtf, words))
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

                if (c == '{' && IsDestinationGroupAt(rtf, i, words))
                {
                    i = SkipGroup(rtf, i); // drop the whole "{\word …}" (or "{\*\word …}") group
                    continue;
                }

                sb.Append(c);
                i++;
            }
            return sb.ToString();
        }

        // Cheap pre-check: is any target control word even present? Avoids the full walk otherwise.
        private static bool ContainsAnyDestination(string rtf, string[] words)
        {
            foreach (string word in words)
            {
                if (rtf.IndexOf("\\" + word, StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        // True if the '{' at <paramref name="openBrace"/> begins a destination group for one of
        // <paramref name="words"/>: an optional ignorable-destination marker (\*) followed by the control
        // word — e.g. "{\object", "{\*\object", "{\annotation", or "{\*\atnauthor".
        private static bool IsDestinationGroupAt(string rtf, int openBrace, string[] words)
        {
            int pos = openBrace + 1; // just past '{'
            if (pos + 1 < rtf.Length && rtf[pos] == '\\' && rtf[pos + 1] == '*')
            {
                pos += 2; // skip the "\*" ignorable-destination marker
            }
            foreach (string word in words)
            {
                if (IsControlWordAt(rtf, pos, word))
                {
                    return true;
                }
            }
            return false;
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
