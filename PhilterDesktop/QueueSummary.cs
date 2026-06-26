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

namespace PhilterDesktop
{
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
}
