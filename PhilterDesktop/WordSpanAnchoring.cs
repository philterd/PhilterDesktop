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

using Phileas.Services.Office;

namespace PhilterDesktop
{
    /// <summary>
    /// Checks saved Word spans against the source's current paragraph text before they're re-applied.
    /// Phileas applies a span purely by paragraph index and character offsets, so a span whose offsets
    /// no longer line up (the source was edited, or the span was saved by a Phileas version that read
    /// paragraph text differently) would silently redact the wrong characters and leave part of the
    /// value in the output.
    /// </summary>
    internal static class WordSpanAnchoring
    {
        // Characters newer Phileas versions add to paragraph text for elements 1.6.0 left out: line
        // breaks, tabs, non-breaking hyphens and symbol characters. A span saved by 1.6.0 indexes text
        // without them, so its value now appears with some of these characters inserted.
        private const string InsertedCharacters = "\n\t- ";

        /// <summary>The outcome of anchoring a span set.</summary>
        /// <param name="Moved">Spans whose text was found at a new position, now pointing there.</param>
        /// <param name="Unmatched">Spans that couldn't be placed: their text isn't in the paragraph, it's
        /// there more than once, or the position is outside the paragraph.</param>
        internal sealed record Result(IReadOnlyList<OfficeRedactionSpan> Moved, IReadOnlyList<OfficeRedactionSpan> Unmatched);

        /// <summary>
        /// Points each positional span at its saved text in <paramref name="paragraphs"/> (the current
        /// <c>WordDocumentRedactor.ReadParagraphs</c> text, which is what paragraph indices index),
        /// updating the spans in place. Spans with no paragraph index (drawing and hyperlink text, which
        /// re-apply by re-detection) are left alone, as are spans with no saved text, which can only be
        /// bounds-checked.
        /// </summary>
        public static Result Anchor(IReadOnlyList<string> paragraphs, IReadOnlyList<OfficeRedactionSpan> spans)
        {
            var moved = new List<OfficeRedactionSpan>();
            var unmatched = new List<OfficeRedactionSpan>();

            foreach (OfficeRedactionSpan span in spans)
            {
                if (span.ParagraphIndex < 0)
                {
                    continue;
                }
                if (span.ParagraphIndex >= paragraphs.Count)
                {
                    unmatched.Add(span);
                    continue;
                }

                string paragraph = paragraphs[span.ParagraphIndex];
                bool inBounds = span.CharacterStart >= 0 && span.CharacterEnd > span.CharacterStart && span.CharacterEnd <= paragraph.Length;

                if (string.IsNullOrEmpty(span.Text))
                {
                    if (!inBounds)
                    {
                        unmatched.Add(span);
                    }
                    continue;
                }

                if (inBounds && span.CharacterEnd - span.CharacterStart == span.Text.Length
                    && string.CompareOrdinal(paragraph, span.CharacterStart, span.Text, 0, span.Text.Length) == 0)
                {
                    continue; // still where it was saved
                }

                if (Locate(paragraph, span.Text, span.CharacterStart) is (int start, int end))
                {
                    span.CharacterStart = start;
                    span.CharacterEnd = end;
                    span.Text = paragraph[start..end];
                    moved.Add(span);
                }
                else
                {
                    unmatched.Add(span);
                }
            }

            return new Result(moved, unmatched);
        }

        /// <summary>
        /// Finds <paramref name="text"/> in <paramref name="paragraph"/>, allowing only
        /// <see cref="InsertedCharacters"/> between its characters. Prefers the one match consistent with
        /// <paramref name="savedStart"/> having moved right by inserted characters; otherwise accepts the
        /// match only if it's the only one in the paragraph. Returns null when there is none, or more
        /// than one candidate.
        /// </summary>
        internal static (int Start, int End)? Locate(string paragraph, string text, int savedStart)
        {
            var matches = new List<(int Start, int End)>();
            for (int i = 0; i < paragraph.Length; i++)
            {
                if (MatchAt(paragraph, i, text) is int end)
                {
                    matches.Add((i, end));
                }
            }

            List<(int Start, int End)> plausible = matches
                .Where(m => m.Start >= savedStart && m.Start - savedStart <= CountInserted(paragraph, m.Start))
                .ToList();

            if (plausible.Count == 1)
            {
                return plausible[0];
            }
            return plausible.Count == 0 && matches.Count == 1 ? matches[0] : null;
        }

        // The end of a match of text starting exactly at position, skipping inserted characters
        // between (not before or after) text's characters; null if it doesn't match there.
        private static int? MatchAt(string paragraph, int position, string text)
        {
            int p = position;
            for (int t = 0; t < text.Length; t++)
            {
                while (t > 0 && p < paragraph.Length && paragraph[p] != text[t] && InsertedCharacters.Contains(paragraph[p]))
                {
                    p++;
                }
                if (p >= paragraph.Length || paragraph[p] != text[t])
                {
                    return null;
                }
                p++;
            }
            return p;
        }

        private static int CountInserted(string paragraph, int before)
        {
            int count = 0;
            for (int i = 0; i < before; i++)
            {
                if (InsertedCharacters.Contains(paragraph[i]))
                {
                    count++;
                }
            }
            return count;
        }
    }

    /// <summary>
    /// Thrown before anything is written when saved redactions no longer line up with the source
    /// document, so re-applying them would redact the wrong text.
    /// </summary>
    internal sealed class StaleRedactionSpansException : Exception
    {
        public StaleRedactionSpansException(IReadOnlyList<OfficeRedactionSpan> unmatched)
            : base($"{unmatched.Count} saved redaction{(unmatched.Count == 1 ? " no longer matches" : "s no longer match")} the original document.")
        {
            Unmatched = unmatched;
        }

        public IReadOnlyList<OfficeRedactionSpan> Unmatched { get; }
    }
}
