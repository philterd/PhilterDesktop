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
    /// Shared rule for choosing which policy/context entry a combo box should pre-select:
    /// the last-used value if it's present, otherwise "default", otherwise the first item.
    /// </summary>
    internal static class ComboSelection
    {
        /// <summary>
        /// Returns the index to select in <paramref name="items"/>: <paramref name="preferred"/> if
        /// present, then "default", then 0. Returns -1 when there are no items.
        /// </summary>
        public static int ResolveIndex(IReadOnlyList<string> items, string? preferred)
        {
            if (items.Count == 0)
            {
                return -1;
            }
            if (!string.IsNullOrEmpty(preferred))
            {
                int index = IndexOf(items, preferred);
                if (index >= 0)
                {
                    return index;
                }
            }
            int defaultIndex = IndexOf(items, "default");
            return defaultIndex >= 0 ? defaultIndex : 0;
        }

        /// <summary>Selects the preferred entry (per <see cref="ResolveIndex"/>) in the combo box.</summary>
        public static void Select(ComboBox combo, string? preferred)
        {
            var items = new List<string>(combo.Items.Count);
            foreach (object? item in combo.Items)
            {
                items.Add(item?.ToString() ?? string.Empty);
            }
            int index = ResolveIndex(items, preferred);
            if (index >= 0)
            {
                combo.SelectedIndex = index;
            }
        }

        private static int IndexOf(IReadOnlyList<string> items, string value)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (string.Equals(items[i], value, StringComparison.Ordinal))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
