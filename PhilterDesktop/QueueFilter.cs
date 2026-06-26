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
}
