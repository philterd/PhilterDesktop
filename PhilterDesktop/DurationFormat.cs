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

namespace PhilterDesktop
{
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
