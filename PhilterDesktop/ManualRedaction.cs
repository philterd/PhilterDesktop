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
    /// <summary>
    /// Turns a reviewer's text selection in the preview into a user-added redaction span. UI-independent
    /// (takes the text and the selection offsets) so the offset mapping and bounds handling are testable.
    /// </summary>
    internal static class ManualRedaction
    {
        /// <summary>
        /// Builds a user-added span for the selection <c>[start, start+length)</c> in <paramref name="text"/>,
        /// clamping to the text bounds. Returns null when the selection is empty or out of range.
        /// </summary>
        public static RedactionSpanEntity? FromSelection(string text, int start, int length)
        {
            if (start < 0 || length <= 0 || start >= text.Length)
            {
                return null;
            }

            int stop = Math.Min(start + length, text.Length);
            if (stop <= start)
            {
                return null;
            }

            return new RedactionSpanEntity
            {
                UserAdded = true,
                ParagraphIndex = -1,
                CharacterStart = start,
                CharacterEnd = stop,
                Text = text.Substring(start, stop - start),
                Replacement = RedactionService.DefaultReplacement
            };
        }

        /// <summary>
        /// Builds user-added spans for a selection over text formed by joining <paramref name="paragraphs"/>
        /// with a separator of <paramref name="separatorLength"/> characters (e.g. 2 for "\r\n"). Word
        /// redactions are per-paragraph, so a selection that spans paragraphs yields one span per
        /// paragraph (the portion within it); characters that fall on the separators are ignored. Returns
        /// an empty list for an empty/invalid selection.
        /// </summary>
        public static List<RedactionSpanEntity> FromParagraphSelection(
            IReadOnlyList<string> paragraphs, int start, int length, int separatorLength)
        {
            var result = new List<RedactionSpanEntity>();
            if (start < 0 || length <= 0)
            {
                return result;
            }

            int selectionEnd = start + length;
            int cursor = 0; // global offset of the current paragraph's first character
            for (int p = 0; p < paragraphs.Count; p++)
            {
                int paragraphStart = cursor;
                int paragraphEnd = cursor + paragraphs[p].Length; // exclusive, before the separator

                int overlapStart = Math.Max(start, paragraphStart);
                int overlapEnd = Math.Min(selectionEnd, paragraphEnd);
                if (overlapEnd > overlapStart)
                {
                    int localStart = overlapStart - paragraphStart;
                    int localEnd = overlapEnd - paragraphStart;
                    result.Add(new RedactionSpanEntity
                    {
                        UserAdded = true,
                        ParagraphIndex = p,
                        CharacterStart = localStart,
                        CharacterEnd = localEnd,
                        Text = paragraphs[p].Substring(localStart, localEnd - localStart),
                        Replacement = RedactionService.DefaultReplacement
                    });
                }

                cursor = paragraphEnd + separatorLength;
                if (cursor >= selectionEnd)
                {
                    break;
                }
            }
            return result;
        }
    }
}
