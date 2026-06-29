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
    /// Guards against runaway (catastrophic-backtracking) regular expressions. Detection patterns can
    /// come from user-supplied or imported policies (custom identifiers), so a pathological pattern such
    /// as <c>(a+)+$</c> over a large document could otherwise hang the redaction thread forever.
    /// </summary>
    internal static class RegexSafety
    {
        /// <summary>Smallest selectable match timeout, in seconds — also the safe default floor.</summary>
        public const int MinTimeoutSeconds = 5;

        /// <summary>Largest selectable match timeout, in seconds.</summary>
        public const int MaxTimeoutSeconds = 15;

        /// <summary>
        /// Per-process cap on how long a single regex match may run. .NET applies this as the default for
        /// every <see cref="Regex"/> created without its own timeout — including those built inside the
        /// redaction engine — turning an unbounded hang into a bounded, surfaced error.
        /// </summary>
        public static readonly TimeSpan DefaultMatchTimeout = TimeSpan.FromSeconds(MinTimeoutSeconds);

        // The AppDomain data key the .NET regex engine reads its default match timeout from. The value
        // must be a TimeSpan despite the "_MS" suffix.
        private const string RegexTimeoutKey = "REGEX_DEFAULT_MATCH_TIMEOUT_MS";

        /// <summary>Clamps a configured timeout to the supported <see cref="MinTimeoutSeconds"/>–<see cref="MaxTimeoutSeconds"/> range.</summary>
        public static int ClampSeconds(int seconds) => Math.Clamp(seconds, MinTimeoutSeconds, MaxTimeoutSeconds);

        /// <summary>
        /// Installs the safe default (floor) regex match timeout. Call once at process start, before the
        /// settings database is available, so a runaway pattern is bounded even during startup.
        /// </summary>
        public static void InstallDefaultMatchTimeout() => Install(DefaultMatchTimeout);

        /// <summary>
        /// Installs the user-configured regex match timeout (clamped to the supported range). Applied once
        /// settings are loaded and again whenever they change; it governs every regex compiled afterward,
        /// including the per-redaction custom-identifier patterns.
        /// </summary>
        public static void InstallMatchTimeout(int seconds) => Install(TimeSpan.FromSeconds(ClampSeconds(seconds)));

        private static void Install(TimeSpan timeout) => AppDomain.CurrentDomain.SetData(RegexTimeoutKey, timeout);

        /// <summary>
        /// True if <paramref name="pattern"/> is a syntactically valid regular expression. This checks
        /// <b>syntax only</b>; catastrophic-backtracking patterns still compile and are contained at match
        /// time by <see cref="DefaultMatchTimeout"/>, not here.
        /// </summary>
        public static bool IsValidPattern(string? pattern, out string? error)
        {
            error = null;
            if (string.IsNullOrEmpty(pattern))
            {
                return true; // no pattern; treated as a no-op by the engine
            }
            try
            {
                _ = new Regex(pattern);
                return true;
            }
            catch (ArgumentException ex) // includes RegexParseException
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
