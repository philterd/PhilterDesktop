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

using System.Globalization;
using Phileas.Policy;

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// One region as the user entered it: a page <b>spec</b> (a single page, a range like <c>2-5</c>, a
    /// list like <c>1,2,5</c>, <c>0</c> for all pages, or an open-ended range like <c>2-</c> for "all but
    /// the first page") plus the rectangle. It stays a single list row; it's expanded into one
    /// <see cref="BoundingBox"/> per page only when the policy is saved (open-ended specs stay a single
    /// deferred box, expanded at redaction time when the page count is known).
    /// </summary>
    internal sealed record PdfRegionEntry(string PageSpec, float X, float Y, float W, float H, string Color)
    {
        /// <summary>How the pages read in the list: a friendly label for open-ended specs, else the spec as entered.</summary>
        public string PageDisplay
        {
            get
            {
                if (!AddRegionForm.ParsePages(PageSpec, out _, out int openFrom, out _))
                {
                    return PageSpec;
                }
                return openFrom switch
                {
                    0 => PageSpec,          // an explicit page/range/list — show as entered
                    1 => "All",             // 0 spec
                    2 => "All but first",   // 2- spec
                    _ => $"From {openFrom}" // N- spec
                };
            }
        }

        /// <summary>One box per resolved page; an open-ended spec yields a single deferred sentinel box.</summary>
        public IEnumerable<BoundingBox> ToBoundingBoxes()
        {
            if (!AddRegionForm.ParsePages(PageSpec, out List<int> pages, out int openFrom, out _))
            {
                yield break;
            }
            if (openFrom > 0)
            {
                // Deferred: page 0 = all pages; a negative page -N = "from page N to the last page".
                yield return MakeBox(openFrom == 1 ? 0 : -openFrom);
            }
            else
            {
                foreach (int page in pages)
                {
                    yield return MakeBox(page);
                }
            }
        }

        private BoundingBox MakeBox(int page) =>
            new() { Page = page, X = X, Y = Y, W = W, H = H, Color = Color, Enabled = true };

        /// <summary>Builds an entry for a single existing box (e.g. a drawn region).</summary>
        public static PdfRegionEntry FromBox(BoundingBox b) =>
            new(PageSpecFor(b.Page), b.X, b.Y, b.W, b.H, b.Color ?? PdfRegionPickerForm.DefaultRegionColor);

        // Turns a stored box page back into a spec: 0 = all, -N = open-ended "N-", otherwise the number.
        private static string PageSpecFor(int page) => page switch
        {
            0 => "0",
            < 0 => $"{-page}-",
            _ => page.ToString(CultureInfo.InvariantCulture)
        };

        /// <summary>
        /// Rebuilds entries from a saved policy's boxes: boxes with an identical rectangle and color are
        /// regrouped into one entry, and their pages compacted back into a spec (e.g. pages 1,2 → "1,2";
        /// 3,4,5 → "3-5"), so a region entered as a range/list shows as a single row again.
        /// </summary>
        public static List<PdfRegionEntry> FromBoxes(IEnumerable<BoundingBox> boxes)
        {
            var entries = new List<PdfRegionEntry>();
            foreach (var group in boxes.GroupBy(b => (b.X, b.Y, b.W, b.H, Color: b.Color ?? PdfRegionPickerForm.DefaultRegionColor)))
            {
                List<int> pages = group.Select(b => b.Page).Distinct().OrderBy(p => p).ToList();
                int negative = pages.FirstOrDefault(p => p < 0); // pages are sorted, so negatives come first
                string spec = pages.Contains(0) ? "0"
                    : negative < 0 ? $"{-negative}-"
                    : FormatPageSpec(pages);
                BoundingBox first = group.First();
                entries.Add(new PdfRegionEntry(spec, first.X, first.Y, first.W, first.H,
                    first.Color ?? PdfRegionPickerForm.DefaultRegionColor));
            }
            return entries;
        }

        // Compacts a sorted, distinct page list into a spec, collapsing runs of 3+ into ranges
        // (so "1,2" stays "1,2" but "1,2,3" becomes "1-3").
        internal static string FormatPageSpec(List<int> pages)
        {
            var parts = new List<string>();
            int i = 0;
            while (i < pages.Count)
            {
                int start = pages[i];
                int end = start;
                while (i + 1 < pages.Count && pages[i + 1] == end + 1)
                {
                    end = pages[++i];
                }
                if (end - start >= 2)
                {
                    parts.Add($"{start}-{end}");
                }
                else
                {
                    for (int p = start; p <= end; p++)
                    {
                        parts.Add(p.ToString(CultureInfo.InvariantCulture));
                    }
                }
                i++;
            }
            return string.Join(",", parts);
        }
    }
}
