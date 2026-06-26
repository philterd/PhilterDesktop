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
using PhilterData;

namespace PhilterDesktop
{
    /// <summary>A text replacement range used when re-applying redaction spans.</summary>
    internal readonly record struct ReplacementRange(int Start, int End, string Replacement);

    /// <summary>Resolves and applies redaction spans/ranges to text (the heart of producing redacted output).</summary>
    internal static class RedactionSpanMath
    {
        /// <summary>
        /// Sorts ranges by start and drops any that overlap one already kept (highest priority is
        /// earliest start). Returns them ordered by start ascending.
        /// </summary>
        public static List<ReplacementRange> ResolveNonOverlapping(IEnumerable<ReplacementRange> ranges)
        {
            var result = new List<ReplacementRange>();
            int lastEnd = -1;
            foreach (ReplacementRange r in ranges.Where(r => r.End > r.Start).OrderBy(r => r.Start).ThenByDescending(r => r.End))
            {
                if (r.Start >= lastEnd)
                {
                    result.Add(r);
                    lastEnd = r.End;
                }
            }
            return result;
        }

        /// <summary>
        /// Builds replacement ranges from spans that are valid for text of <paramref name="textLength"/>
        /// (in-bounds and non-empty), substituting <paramref name="defaultReplacement"/> when a span
        /// carries no replacement.
        /// </summary>
        public static List<ReplacementRange> BuildRanges(IEnumerable<RedactionSpanEntity> spans, int textLength, string defaultReplacement)
        {
            var ranges = new List<ReplacementRange>();
            foreach (RedactionSpanEntity s in spans)
            {
                if (s.CharacterStart >= 0 && s.CharacterEnd <= textLength && s.CharacterEnd > s.CharacterStart)
                {
                    string replacement = string.IsNullOrEmpty(s.Replacement) ? defaultReplacement : s.Replacement;
                    ranges.Add(new ReplacementRange(s.CharacterStart, s.CharacterEnd, replacement));
                }
            }
            return ranges;
        }

        /// <summary>
        /// Applies the ranges to <paramref name="text"/>, resolving overlaps first and applying
        /// right-to-left so earlier offsets stay valid as the string changes length.
        /// </summary>
        public static string Apply(string text, IEnumerable<ReplacementRange> ranges)
        {
            var sb = new StringBuilder(text);
            foreach (ReplacementRange r in ResolveNonOverlapping(ranges).OrderByDescending(r => r.Start))
            {
                sb.Remove(r.Start, r.End - r.Start);
                sb.Insert(r.Start, r.Replacement ?? string.Empty);
            }
            return sb.ToString();
        }

        /// <summary>Builds ranges from <paramref name="spans"/> and applies them to <paramref name="text"/>.</summary>
        public static string ApplySpans(string text, IEnumerable<RedactionSpanEntity> spans, string defaultReplacement)
            => Apply(text, BuildRanges(spans, text.Length, defaultReplacement));
    }
}
