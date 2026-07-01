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

using System.Collections;

namespace PhilterDesktop
{
    /// <summary>
    /// Sorts the main queue list by a clicked column. The Status column (index 1) sorts in workflow
    /// order (Pending → Processing → Completed → Failed) rather than alphabetically; other columns
    /// sort case-insensitively. The pure <see cref="CompareValues"/> is unit-testable on its own.
    /// </summary>
    internal sealed class QueueColumnSorter : IComparer
    {
        /// <summary>Index of the column to sort by.</summary>
        public int Column { get; set; }

        /// <summary>Ascending when true; descending when false.</summary>
        public bool Ascending { get; set; } = true;

        public int Compare(object? x, object? y)
        {
            var a = x as ListViewItem;
            var b = y as ListViewItem;

            int result = CompareValues(CellText(a, Column), CellText(b, Column), Column);

            // Keep equal cells in a stable, deterministic order by falling back to the file name.
            if (result == 0 && Column != 0)
            {
                result = CompareValues(CellText(a, 0), CellText(b, 0), 0);
            }

            return Ascending ? result : -result;
        }

        private static string CellText(ListViewItem? item, int column)
        {
            if (item == null || column < 0 || column >= item.SubItems.Count)
            {
                return string.Empty;
            }
            return item.SubItems[column].Text;
        }

        /// <summary>
        /// Compares two cell values for the given column. Column 1 (Status) ranks by workflow order;
        /// every other column is a case-insensitive string comparison.
        /// </summary>
        internal static int CompareValues(string left, string right, int column)
        {
            if (column == 1)
            {
                int rankLeft = StatusRank(left);
                int rankRight = StatusRank(right);
                if (rankLeft != rankRight)
                {
                    return rankLeft.CompareTo(rankRight);
                }
            }
            return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static int StatusRank(string status)
        {
            for (int i = 0; i < QueueSummary.StatusOrder.Length; i++)
            {
                if (string.Equals(status, QueueSummary.StatusOrder[i], StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return QueueSummary.StatusOrder.Length; // unknown statuses sort last
        }
    }

    /// <summary>
    /// Decides whether a queue row matches the filter box text. A blank query matches everything;
    /// otherwise the (trimmed) query must appear, case-insensitively, in any of the supplied fields
    /// (file name, status, policy, context).
    /// </summary>
    internal static class QueueFilter
    {
        public static bool Matches(string? query, params string?[] fields)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            string trimmed = query.Trim();
            foreach (string? field in fields)
            {
                if (!string.IsNullOrEmpty(field) &&
                    field.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>Builds the status-bar summary text for the redaction queue.</summary>
    internal static class QueueSummary
    {
        /// <summary>Queue statuses in workflow order; shared with column sorting.</summary>
        internal static readonly string[] StatusOrder = { "Pending", "Processing", "Completed", "Failed" };

        /// <summary>
        /// e.g. "3 files  ·  2 pending  ·  1 completed", or "No documents in queue" when empty.
        /// Statuses are listed in workflow order and zero counts are omitted.
        /// </summary>
        public static string Describe(int total, IReadOnlyDictionary<string, int> counts)
        {
            if (total == 0)
            {
                return "No documents in queue";
            }

            var parts = new List<string> { $"{total} file{(total == 1 ? "" : "s")}" };
            foreach (string status in StatusOrder)
            {
                if (counts.TryGetValue(status, out int n) && n > 0)
                {
                    parts.Add($"{n} {status.ToLowerInvariant()}");
                }
            }
            return string.Join("  ·  ", parts);
        }

        /// <summary>
        /// Summary shown while the filter box narrows the list, e.g. "Showing 2 of 5 documents".
        /// </summary>
        public static string DescribeFilter(int shown, int total)
        {
            return $"Showing {shown} of {total} document{(total == 1 ? "" : "s")}";
        }
    }

    public class QueuedRedaction
    {
        public string filename { get; set; } = string.Empty;
        public string context { get; set; } = string.Empty;
        public string policy { get; set; } = string.Empty;
    }
}
