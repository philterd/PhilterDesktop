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

using Phileas.Policy;
using PhilterData;
using PhileasPolicy = Phileas.Policy.Policy;
using PhileasDictionary = Phileas.Policy.Filters.Dictionary;

namespace PhilterDesktop
{
    /// <summary>
    /// The user's global "always redact" and "always ignore" term lists, which apply on top of
    /// <em>every</em> policy. They're merged into a policy at redaction time by adding a dedicated
    /// dictionary (always redact) and ignored set (always ignore), using distinct names so they never
    /// clash with a policy's own editor-managed lists.
    /// </summary>
    internal static class GlobalLists
    {
        public const string AlwaysRedactName = "global-always-redact";
        public const string AlwaysIgnoreName = "global-always-ignore";

        /// <summary>Splits newline-separated text into trimmed, non-empty, de-duplicated terms.</summary>
        public static List<string> ParseTerms(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<string>();
            }
            return text.Replace("\r\n", "\n").Split('\n')
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>Merges the global lists from settings into the policy (no-op if both are empty).</summary>
        public static void Apply(PhileasPolicy? policy, SettingsEntity? settings)
        {
            if (policy is null || settings is null)
            {
                return;
            }
            ApplyTerms(policy, ParseTerms(settings.GlobalAlwaysRedact), ParseTerms(settings.GlobalAlwaysIgnore));
        }

        /// <summary>Merges the given term lists into the policy. Replaces only our own managed entries.</summary>
        public static void ApplyTerms(PhileasPolicy policy, IReadOnlyList<string> alwaysRedact, IReadOnlyList<string> alwaysIgnore)
        {
            policy.Identifiers ??= new Identifiers();

            policy.Identifiers.Dictionaries?.RemoveAll(d => d.Name == AlwaysRedactName);
            if (alwaysRedact.Count > 0)
            {
                policy.Identifiers.Dictionaries ??= new List<PhileasDictionary>();
                policy.Identifiers.Dictionaries.Add(new PhileasDictionary
                {
                    Name = AlwaysRedactName,
                    Terms = alwaysRedact.ToList(),
                    Enabled = true
                });
            }

            policy.Ignored?.RemoveAll(i => i.Name == AlwaysIgnoreName);
            if (alwaysIgnore.Count > 0)
            {
                policy.Ignored ??= new List<Ignored>();
                policy.Ignored.Add(new Ignored
                {
                    Name = AlwaysIgnoreName,
                    Terms = alwaysIgnore.ToList(),
                    Files = new List<string>(),
                    CaseSensitive = false
                });
            }
        }
    }
}
