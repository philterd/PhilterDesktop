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

using System.Drawing;

namespace PhilterDesktop
{
    /// <summary>
    /// Pure helpers for persisting and restoring the main-window layout (size/position, sort, column
    /// widths). Kept free of WinForms control state so the tricky bits — the off-screen guard and the
    /// column-width round-trip — are unit-testable.
    /// </summary>
    internal static class UiState
    {
        // A restored window must show at least this much of itself on some screen to be considered
        // usable (so the title bar can be grabbed); otherwise we fall back to the default position.
        private const int MinVisibleWidth = 120;
        private const int MinVisibleHeight = 40;

        /// <summary>
        /// True if <paramref name="bounds"/> overlaps any screen by a grabbable amount. Guards against
        /// restoring a window onto a monitor that was unplugged or whose resolution changed.
        /// </summary>
        public static bool IsBoundsVisible(Rectangle bounds, IEnumerable<Rectangle> screenWorkingAreas)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return false;
            }

            foreach (Rectangle screen in screenWorkingAreas)
            {
                Rectangle overlap = Rectangle.Intersect(screen, bounds);
                if (overlap.Width >= MinVisibleWidth && overlap.Height >= MinVisibleHeight)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Formats column widths as a comma-separated string, e.g. "350,180,120,120".</summary>
        public static string FormatWidths(IEnumerable<int> widths) => string.Join(",", widths);

        /// <summary>
        /// Parses a comma-separated width string back into an array, but only if it has exactly
        /// <paramref name="expectedCount"/> positive integers; otherwise returns null (use defaults).
        /// </summary>
        public static int[]? ParseWidths(string? text, int expectedCount)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            string[] parts = text.Split(',');
            if (parts.Length != expectedCount)
            {
                return null;
            }

            var widths = new int[expectedCount];
            for (int i = 0; i < expectedCount; i++)
            {
                if (!int.TryParse(parts[i].Trim(), out int w) || w <= 0)
                {
                    return null;
                }
                widths[i] = w;
            }
            return widths;
        }
    }
}
