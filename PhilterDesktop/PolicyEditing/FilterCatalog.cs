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

using System.Reflection;
using Phileas.Policy;
using Phileas.Policy.Filters;

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// The single source of truth for how the built-in (pattern/dictionary) filters are grouped into
    /// categories, used by both the Policy Editor tabs and the New-from-Wizard checklist. The actual
    /// filter set is discovered by reflection over the Phileas <c>Identifiers</c> type, so it never
    /// drifts from the engine: unknown identifiers fall into "Other".
    /// </summary>
    internal static class FilterCatalog
    {
        /// <summary>Category → ordered filter property names.</summary>
        public static readonly IReadOnlyList<(string Category, string[] Properties)> Categories = new[]
        {
            ("Personal", new[] { "FirstName", "Surname", "Age" }),
            ("Contact", new[] { "EmailAddress", "PhoneNumber", "PhoneNumberExtension" }),
            ("Location", new[] { "City", "County", "State", "StateAbbreviation", "ZipCode", "StreetAddress" }),
            ("Financial", new[] { "CreditCard", "BankRoutingNumber", "IbanCode", "BitcoinAddress", "Currency" }),
            ("Identifiers", new[] { "Ssn", "DriversLicense", "PassportNumber", "Vin", "TrackingNumber" }),
            ("Technical", new[] { "IpAddress", "MacAddress", "Url" }),
            ("Medical", new[] { "Hospital" }),
            ("Other", new[] { "Date" }),
        };

        public sealed record FilterInfo(string Category, string Property, string Display);

        /// <summary>All pattern/dictionary filter properties on <c>Identifiers</c>, keyed by name.</summary>
        public static Dictionary<string, PropertyInfo> Discover() =>
            typeof(Identifiers)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => typeof(AbstractPolicyFilter).IsAssignableFrom(p.PropertyType))
                .ToDictionary(p => p.Name);

        /// <summary>
        /// The discovered filters grouped by category in display order; identifiers not listed in
        /// <see cref="Categories"/> are appended under "Other" so nothing is ever hidden.
        /// </summary>
        public static IReadOnlyList<IGrouping<string, FilterInfo>> Grouped()
        {
            Dictionary<string, PropertyInfo> discovered = Discover();
            var placed = new HashSet<string>();
            var result = new List<FilterInfo>();

            foreach ((string category, string[] props) in Categories)
            {
                foreach (string prop in props.Where(discovered.ContainsKey))
                {
                    result.Add(new FilterInfo(category, prop, FilterLabel.Humanize(prop)));
                    placed.Add(prop);
                }
            }

            foreach (PropertyInfo prop in discovered.Values
                         .Where(p => !placed.Contains(p.Name))
                         .OrderBy(p => FilterLabel.Humanize(p.Name)))
            {
                result.Add(new FilterInfo("Other", prop.Name, FilterLabel.Humanize(prop.Name)));
            }

            return result.GroupBy(f => f.Category).ToList();
        }
    }
}
