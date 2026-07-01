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
using System.Security.Cryptography;

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

    /// <summary>
    /// Computes file content hashes for the redaction report, so a report can record exactly which
    /// source and output bytes it describes (tamper-evidence for a case file / compliance record).
    /// </summary>
    internal static class FileHash
    {
        /// <summary>Lower-case hex SHA-256 of the file's contents.</summary>
        public static string Sha256(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
        }

        /// <summary>
        /// SHA-256 of the file, or a short placeholder when the file can't be read (e.g. the original
        /// was moved after redaction). A report should still generate even if one input is gone.
        /// </summary>
        public static string Sha256OrUnavailable(string path)
        {
            try
            {
                return string.IsNullOrEmpty(path) || !File.Exists(path) ? "(file not available)" : Sha256(path);
            }
            catch
            {
                return "(file not available)";
            }
        }
    }

    /// <summary>Formats a redaction duration for display.</summary>
    internal static class DurationFormat
    {
        /// <summary>
        /// "—" when unmeasured (0 or less), milliseconds under one second, otherwise seconds with two
        /// decimals (e.g. <c>"850 ms"</c>, <c>"1.50 s"</c>).
        /// </summary>
        public static string Humanize(long milliseconds)
        {
            if (milliseconds <= 0)
            {
                return "—";
            }
            return milliseconds < 1000
                ? $"{milliseconds} ms"
                : string.Create(CultureInfo.InvariantCulture, $"{milliseconds / 1000.0:0.00} s");
        }
    }
}
