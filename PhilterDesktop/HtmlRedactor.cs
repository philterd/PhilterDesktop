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

using System.Net;
using System.Text;
using Phileas.Model;
using Phileas.Services;

namespace PhilterDesktop
{
    /// <summary>
    /// Redacts an HTML email body by its <b>visible text</b>, then rewriting the corresponding raw markup.
    /// Filtering raw HTML directly misses PII that is entity-encoded (<c>john&amp;#64;example.com</c>) or
    /// split by tags (<c>john&lt;span&gt;@&lt;/span&gt;example.com</c>), so it could survive in the HTML
    /// alternative most clients render while being removed from the plain-text one. This extracts the
    /// visible text (decoding entities, skipping tags) with a per-character map back to raw offsets, runs
    /// the filter over that, and replaces each detected raw range — offsets are into the raw HTML, so the
    /// Modify/ApplySpans (position-based) path is unaffected.
    /// </summary>
    internal static class HtmlRedactor
    {
        /// <summary>A detection mapped from the visible text back to a raw-HTML <c>[RawStart, RawEnd)</c> range.</summary>
        internal readonly record struct HtmlRedaction(int RawStart, int RawEnd, Span Span);

        /// <summary>
        /// Detects PII to remove from the HTML, as raw-HTML <c>[RawStart, RawEnd)</c> ranges. Combines two
        /// passes: the raw markup (which catches PII in attribute values such as a <c>mailto:</c> href, and
        /// any PII present intact) and the decoded visible text mapped back to raw offsets (which catches
        /// entity-encoded or tag-split PII). Overlaps between the two passes are merged.
        /// </summary>
        public static List<HtmlRedaction> Detect(string html, Func<string, TextFilterResult> filter)
        {
            var redactions = new List<HtmlRedaction>();

            // Pass 1 — raw markup (preserves the original behavior; reaches attribute values).
            foreach (Span s in filter(html).Spans
                         .Where(s => s.CharacterStart >= 0 && s.CharacterEnd <= html.Length && s.CharacterEnd > s.CharacterStart))
            {
                redactions.Add(new HtmlRedaction(s.CharacterStart, s.CharacterEnd, s));
            }

            // Pass 2 — visible text (entity/tag-aware), mapped back to raw offsets.
            (string visible, int[] rawStart, int[] rawEnd) = BuildVisible(html);
            if (visible.Length > 0)
            {
                foreach (Span s in filter(visible).Spans
                             .Where(s => s.CharacterStart >= 0 && s.CharacterEnd <= visible.Length && s.CharacterEnd > s.CharacterStart))
                {
                    // Raw range from the first visible char's start to the last visible char's end — covering
                    // any tags/entities interleaved between them.
                    redactions.Add(new HtmlRedaction(rawStart[s.CharacterStart], rawEnd[s.CharacterEnd - 1], s));
                }
            }

            return MergeOverlapping(redactions);
        }

        // Merges the two passes into non-overlapping raw ranges (a PII present in both the markup and the
        // visible text yields the same range twice; a split one only appears once). Sorted by start, the
        // earlier detection's replacement wins when ranges are coalesced.
        private static List<HtmlRedaction> MergeOverlapping(List<HtmlRedaction> redactions)
        {
            var sorted = redactions.OrderBy(r => r.RawStart).ThenByDescending(r => r.RawEnd).ToList();
            var merged = new List<HtmlRedaction>();
            foreach (HtmlRedaction r in sorted)
            {
                if (merged.Count > 0 && r.RawStart < merged[^1].RawEnd)
                {
                    if (r.RawEnd > merged[^1].RawEnd)
                    {
                        merged[^1] = merged[^1] with { RawEnd = r.RawEnd }; // extend, keep the first replacement
                    }
                    continue; // otherwise fully covered — drop the duplicate
                }
                merged.Add(r);
            }
            return merged;
        }

        /// <summary>Returns the HTML with detected PII removed, plus the detections (raw-offset mapped).</summary>
        public static (string Html, List<HtmlRedaction> Redactions) Redact(string html, Func<string, TextFilterResult> filter)
        {
            List<HtmlRedaction> redactions = Detect(html, filter);
            if (redactions.Count == 0)
            {
                return (html, redactions);
            }

            var sb = new StringBuilder(html);
            // Apply back-to-front so earlier raw offsets stay valid as later ranges are spliced. Detections
            // are over non-overlapping engine spans, so their raw ranges don't overlap either.
            foreach (HtmlRedaction r in redactions.OrderByDescending(r => r.RawStart))
            {
                sb.Remove(r.RawStart, r.RawEnd - r.RawStart);
                sb.Insert(r.RawStart, r.Span.Replacement ?? string.Empty);
            }
            return (sb.ToString(), redactions);
        }

        // Builds the visible text of the HTML and, for each visible character, the raw-HTML [start, end) it
        // came from. Tags (<...>) contribute no visible text; a character entity maps its decoded char(s)
        // to the whole entity's raw span. A literal '<' is treated as a tag start (as a browser would).
        private static (string Visible, int[] RawStart, int[] RawEnd) BuildVisible(string html)
        {
            var visible = new StringBuilder(html.Length);
            var rawStart = new List<int>(html.Length);
            var rawEnd = new List<int>(html.Length);

            int i = 0;
            while (i < html.Length)
            {
                char c = html[i];

                if (c == '<')
                {
                    int gt = html.IndexOf('>', i + 1);
                    i = gt < 0 ? html.Length : gt + 1; // skip the tag (or the rest, if unterminated)
                    continue;
                }

                if (c == '&')
                {
                    int semi = html.IndexOf(';', i + 1);
                    if (semi > i && semi - i <= 12) // entities relevant to PII are short
                    {
                        string entity = html.Substring(i, semi - i + 1);
                        string decoded = WebUtility.HtmlDecode(entity);
                        if (!string.Equals(decoded, entity, StringComparison.Ordinal)) // it really is an entity
                        {
                            foreach (char dc in decoded)
                            {
                                visible.Append(dc);
                                rawStart.Add(i);
                                rawEnd.Add(semi + 1);
                            }
                            i = semi + 1;
                            continue;
                        }
                    }
                    // not a recognized entity — fall through and treat '&' as a literal character
                }

                visible.Append(c);
                rawStart.Add(i);
                rawEnd.Add(i + 1);
                i++;
            }

            return (visible.ToString(), rawStart.ToArray(), rawEnd.ToArray());
        }
    }
}
