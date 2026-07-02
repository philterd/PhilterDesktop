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
    /// list like <c>1,2,5</c>, or <c>0</c> for all pages) plus the rectangle. It stays a single list row;
    /// it's expanded into one <see cref="BoundingBox"/> per page only when the policy is saved.
    /// </summary>
    internal sealed record PdfRegionEntry(string PageSpec, float X, float Y, float W, float H, string Color)
    {
        /// <summary>How the pages read in the list: "All" for every page, otherwise the spec as entered.</summary>
        public string PageDisplay =>
            AddRegionForm.ParsePages(PageSpec, out _, out bool allPages, out _) && allPages ? "All" : PageSpec;

        /// <summary>One box per resolved page (an all-pages spec yields a single page-0 box).</summary>
        public IEnumerable<BoundingBox> ToBoundingBoxes()
        {
            if (!AddRegionForm.ParsePages(PageSpec, out List<int> pages, out bool allPages, out _))
            {
                yield break;
            }
            if (allPages)
            {
                yield return MakeBox(0);
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
            new(b.Page < 1 ? "0" : b.Page.ToString(CultureInfo.InvariantCulture),
                b.X, b.Y, b.W, b.H, b.Color ?? PdfRegionPickerForm.DefaultRegionColor);

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
                string spec = pages.Any(p => p < 1) ? "0" : FormatPageSpec(pages);
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
