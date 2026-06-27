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
using PhileasPolicy = Phileas.Policy.Policy;
using PhileasDictionary = Phileas.Policy.Filters.Dictionary;

namespace PhilterDesktop
{
    /// <summary>
    /// Builds a throwaway, terms-only policy for the ad-hoc "Find &amp; Redact" action — redact exactly
    /// the given terms in one document, without creating or using a saved policy. Terms are matched via
    /// a dictionary filter (the same mechanism behind the Always Redact lists).
    /// </summary>
    internal static class FindAndRedact
    {
        public const string DictionaryName = "find-and-redact";

        public static PhileasPolicy BuildPolicy(IReadOnlyList<string> terms)
        {
            var policy = new PhileasPolicy { Name = "Find and Redact", Identifiers = new Identifiers() };
            if (terms.Count > 0)
            {
                policy.Identifiers.Dictionaries = new List<PhileasDictionary>
                {
                    new() { Name = DictionaryName, Terms = terms.ToList(), Enabled = true }
                };
            }
            return policy;
        }
    }
}
