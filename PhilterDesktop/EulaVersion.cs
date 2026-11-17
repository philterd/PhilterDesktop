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

using System.Text.RegularExpressions;

namespace PhilterDesktop
{
    /// <summary>
    /// Reads the EULA's version, declared as a "Version 2.0" line in the agreement's header. Recorded when the
    /// user agrees; whether to ask again is decided by the app's version (see LicenseForm.ShouldShow).
    /// </summary>
    internal static class EulaVersion
    {
        // Only the header is searched, so a "Version ..." line in the body can't be mistaken for it.
        private const int HeaderLines = 20;

        private static readonly Regex VersionLine = new(
            @"^\s*Version\s+(\d+(?:\.\d+){1,3})\s*$",
            RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
            TimeSpan.FromSeconds(1));

        /// <summary>The version declared in the EULA text's header, or null if there is none.</summary>
        public static Version? Parse(string? eulaText)
        {
            if (string.IsNullOrWhiteSpace(eulaText))
            {
                return null;
            }
            string header = string.Join("\n", eulaText.ReplaceLineEndings("\n").Split('\n').Take(HeaderLines));
            Match match = VersionLine.Match(header);
            return match.Success ? TryParse(match.Groups[1].Value) : null;
        }

        /// <summary>Parses a bare version string such as "2.0"; null if it isn't one.</summary>
        public static Version? TryParse(string? value) =>
            Version.TryParse(value?.Trim(), out Version? v) ? Normalize(v) : null;


        // "2.0" and "2.0.0" are the same version; System.Version treats missing parts as -1.
        private static Version Normalize(Version v) =>
            new(Math.Max(v.Major, 0), Math.Max(v.Minor, 0), Math.Max(v.Build, 0), Math.Max(v.Revision, 0));
    }

    /// <summary>The EULA file shipped next to the executable, and the version it declares.</summary>
    internal static class BundledEula
    {
        /// <summary>
        /// The EULA text file shipped next to the executable. The installer build refreshes it from
        /// https://philterd.ai/philterd-eula.txt at packaging time; a checked-in snapshot is copied to the
        /// build output so it is present in development too.
        /// </summary>
        internal const string FileName = "philterd-eula.txt";

        /// <summary>The bundled EULA text, or null if the file is missing, empty, or unreadable.</summary>
        public static string? ReadText()
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, FileName);
                string text = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
                return string.IsNullOrWhiteSpace(text) ? null : text;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>The version the bundled EULA declares, or null if it can't be read.</summary>
        public static Version? Version() => EulaVersion.Parse(ReadText());
    }
}
