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
    /// <summary>Small filesystem-path helpers.</summary>
    internal static class PathUtils
    {
        /// <summary>
        /// Returns true if <paramref name="path"/> is the same folder as, or nested inside,
        /// <paramref name="ancestor"/> (case-insensitive, after normalization). Used to keep a
        /// watched folder and its output folder from overlapping when subfolders are watched.
        /// </summary>
        public static bool IsSameOrInside(string path, string ancestor)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(ancestor))
            {
                return false;
            }

            string p = Normalize(path);
            string a = Normalize(ancestor);

            return string.Equals(p, a, StringComparison.OrdinalIgnoreCase)
                || p.StartsWith(a + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string p) =>
            Path.TrimEndingDirectorySeparator(Path.GetFullPath(p));
    }
}
