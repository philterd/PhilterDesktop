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

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>Turns a PascalCase filter/property name into a spaced, acronym-aware label.</summary>
    internal static class FilterLabel
    {
        private static readonly Dictionary<string, string> Acronyms = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Ssn"] = "SSN", ["Vin"] = "VIN", ["Url"] = "URL",
            ["Ip"] = "IP", ["Iban"] = "IBAN", ["Mac"] = "MAC"
        };

        public static string Humanize(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }

            var words = new List<string>();
            int start = 0;
            for (int i = 1; i <= name.Length; i++)
            {
                if (i == name.Length || (char.IsUpper(name[i]) && char.IsLower(name[i - 1])))
                {
                    words.Add(name.Substring(start, i - start));
                    start = i;
                }
            }

            for (int i = 0; i < words.Count; i++)
            {
                if (Acronyms.TryGetValue(words[i], out string? upper))
                {
                    words[i] = upper;
                }
            }
            return string.Join(" ", words);
        }
    }
}
