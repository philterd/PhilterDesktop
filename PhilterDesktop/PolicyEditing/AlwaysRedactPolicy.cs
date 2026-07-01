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

using Phileas.Policy.Filters;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// The policy editor's per-policy "Always Redact" term list, stored as a reserved custom dictionary
    /// (classification <c>always-redact</c>). It uses <c>CustomDictionaries</c> (serializes to the
    /// schema's <c>"dictionaries"</c>), not <c>Dictionaries</c> (<c>"dictionary"</c>, which is not in the
    /// schema, so a policy with it could never be saved).
    /// </summary>
    internal static class AlwaysRedactPolicy
    {
        public const string Classification = "always-redact";

        /// <summary>The editor-managed always-redact terms in the policy (empty if none).</summary>
        public static List<string> GetTerms(PhileasPolicy policy) =>
            policy.Identifiers.CustomDictionaries?
                .FirstOrDefault(d => d.Classification == Classification)?
                .Terms?.ToList() ?? new List<string>();

        /// <summary>
        /// Replaces the editor-managed always-redact dictionary with <paramref name="terms"/>, leaving any
        /// other custom dictionaries untouched. Removes it entirely when the list is empty.
        /// </summary>
        public static void SetTerms(PhileasPolicy policy, IReadOnlyCollection<string> terms)
        {
            policy.Identifiers.CustomDictionaries?.RemoveAll(d => d.Classification == Classification);
            if (terms.Count == 0)
            {
                return;
            }
            policy.Identifiers.CustomDictionaries ??= new List<CustomDictionary>();
            policy.Identifiers.CustomDictionaries.Add(new CustomDictionary
            {
                Classification = Classification,
                Terms = terms.ToList(),
                Enabled = true
            });
        }
    }
}
