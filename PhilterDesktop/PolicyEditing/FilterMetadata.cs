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

using System.Collections;
using System.Reflection;
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Policy.Filters.Strategies;
using PhileasPolicy = Phileas.Policy.Policy;

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

    /// <summary>
    /// Short, plain-language examples shown under each filter's name in the Policy Editor, keyed by the
    /// Phileas <c>Identifiers</c> property name. Anything without an entry simply shows no example.
    /// </summary>
    internal static class FilterExamples
    {
        private static readonly IReadOnlyDictionary<string, string> Examples =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["FirstName"] = "e.g. Jane",
                ["Surname"] = "e.g. Smith",
                ["Age"] = "e.g. 64 years old",
                ["EmailAddress"] = "e.g. jane@example.com",
                ["PhoneNumber"] = "e.g. (555) 123-4567",
                ["PhoneNumberExtension"] = "e.g. ext. 4567",
                ["City"] = "e.g. Springfield",
                ["County"] = "e.g. Cook County",
                ["State"] = "e.g. California",
                ["StateAbbreviation"] = "e.g. CA",
                ["ZipCode"] = "e.g. 90210",
                ["StreetAddress"] = "e.g. 123 Main St",
                ["CreditCard"] = "e.g. 4111 1111 1111 1111",
                ["BankRoutingNumber"] = "e.g. 021000021",
                ["IbanCode"] = "e.g. DE89 3704 0044 0532 0130 00",
                ["BitcoinAddress"] = "e.g. 1A1zP1eP5QGefi2DMPTfTL5SLmv7Divf",
                ["Currency"] = "e.g. $1,250.00",
                ["Ssn"] = "e.g. 123-45-6789",
                ["DriversLicense"] = "e.g. D1234567",
                ["PassportNumber"] = "e.g. X12345678",
                ["Vin"] = "e.g. 1HGCM82633A004352",
                ["TrackingNumber"] = "e.g. 1Z999AA10123456784",
                ["IpAddress"] = "e.g. 192.168.0.1",
                ["MacAddress"] = "e.g. 00:1A:2B:3C:4D:5E",
                ["Url"] = "e.g. https://example.com",
                ["Hospital"] = "e.g. Mercy Hospital",
                ["Date"] = "e.g. 03/04/2020",
            };

        /// <summary>Returns a short example for the filter, or null if there isn't one.</summary>
        public static string? For(string propertyName) =>
            Examples.TryGetValue(propertyName, out string? example) ? example : null;
    }

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

    /// <summary>
    /// Makes the engine's <em>implicit</em> default explicit: phileas-dotnet redacts an enabled filter
    /// that has no strategy using <c>{{{REDACTED-%t}}}</c>. When the editor loads a policy, we add that
    /// default strategy to any enabled filter that lacks one, so the user can see (and tweak) it in the
    /// Configure dialog instead of it being hidden. A freshly constructed strategy already defaults to
    /// REDACT + <c>{{{REDACTED-%t}}}</c>, so this is behavior-preserving.
    /// </summary>
    internal static class FilterStrategyDefaults
    {
        public static void MaterializeMissing(PhileasPolicy? policy)
        {
            if (policy?.Identifiers is null)
            {
                return;
            }

            foreach (PropertyInfo prop in typeof(Identifiers).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!typeof(AbstractPolicyFilter).IsAssignableFrom(prop.PropertyType))
                {
                    continue;
                }

                object? filter = prop.GetValue(policy.Identifiers);
                if (filter is null)
                {
                    continue; // filter not enabled
                }

                PropertyInfo? strategiesProp = prop.PropertyType.GetProperty("Strategies");
                if (strategiesProp is null)
                {
                    continue;
                }

                if (strategiesProp.GetValue(filter) is IList existing && existing.Count > 0)
                {
                    continue; // a strategy is already configured — leave it alone
                }

                Type strategyType = strategiesProp.PropertyType.GetGenericArguments()[0];
                var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(strategyType))!;
                list.Add(Activator.CreateInstance(strategyType)!); // defaults to REDACT + {{{REDACTED-%t}}}
                strategiesProp.SetValue(filter, list);
            }

            // The on-device name (PhEye) filters aren't AbstractPolicyFilter properties; handle them too.
            if (policy.Identifiers.PhEyes is { } phEyes)
            {
                foreach (PhEye phEye in phEyes)
                {
                    if (phEye.Strategies is null || phEye.Strategies.Count == 0)
                    {
                        phEye.Strategies = new List<PhEyeFilterStrategy> { new() }; // REDACT + {{{REDACTED-%t}}}
                    }
                }
            }
        }
    }
}
