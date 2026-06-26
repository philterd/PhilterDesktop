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
    /// Assembles a policy from the wizard's answers. It reuses the same building blocks as the rest of
    /// the editor (the Phileas object model, on-device PhEye for names) and always produces schema-valid
    /// JSON. A wizard policy is a starting point, not a compliance guarantee.
    /// </summary>
    internal static class PolicyWizard
    {
        /// <summary>How the wizard sets each enabled filter to replace what it finds.</summary>
        public enum ReplacementStyle
        {
            LabeledMarker,      // REDACT with {{{REDACTED-%t}}}
            FixedWord,          // STATIC_REPLACE with a fixed word
            ConsistentStandIn,  // RANDOM_REPLACE
        }

        public const string FixedWordValue = "REDACTED";

        /// <summary>
        /// Builds policy JSON enabling the named filters (Phileas <c>Identifiers</c> property names),
        /// optionally on-device name detection, each using the chosen replacement style.
        /// </summary>
        public static string BuildJson(
            string name,
            IEnumerable<string> filterProperties,
            bool includeNames,
            ReplacementStyle style)
        {
            var policy = new PhileasPolicy { Name = name, Identifiers = new Identifiers() };

            foreach (string propName in filterProperties.Distinct())
            {
                PropertyInfo? prop = typeof(Identifiers).GetProperty(propName);
                if (prop is null || !typeof(AbstractPolicyFilter).IsAssignableFrom(prop.PropertyType))
                {
                    continue;
                }
                object filter = Activator.CreateInstance(prop.PropertyType)!;
                ApplyStrategy(filter, style);
                prop.SetValue(policy.Identifiers, filter);
            }

            if (includeNames)
            {
                PhEye phEye = PhEyeModel.CreateDefaultFilter();
                ApplyStrategy(phEye, style);
                policy.Identifiers.PhEyes = new List<PhEye> { phEye };
            }

            return PolicySerializer.SerializeToJson(policy);
        }

        /// <summary>
        /// The filters a starting-point template enables, used to pre-check the wizard's checklist.
        /// Reads them back from the template's own JSON so baselines never drift from the templates.
        /// </summary>
        public static (HashSet<string> Properties, bool IncludeNames) BaselineFrom(string templateId)
        {
            var props = new HashSet<string>(StringComparer.Ordinal);
            bool names = false;

            PolicyTemplate? template = PolicyTemplates.All.FirstOrDefault(t => t.Id == templateId);
            if (template is null)
            {
                return (props, names);
            }

            PhileasPolicy policy = PolicySerializer.DeserializeFromJson(template.BuildJson());
            if (policy.Identifiers is null)
            {
                return (props, names);
            }

            foreach (PropertyInfo prop in FilterCatalog.Discover().Values)
            {
                if (prop.GetValue(policy.Identifiers) is not null)
                {
                    props.Add(prop.Name);
                }
            }
            names = policy.Identifiers.PhEyes is { Count: > 0 };

            return (props, names);
        }

        private static void ApplyStrategy(object filter, ReplacementStyle style)
        {
            PropertyInfo? strategiesProp = filter.GetType().GetProperty("Strategies");
            if (strategiesProp is null)
            {
                return;
            }

            Type strategyType = strategiesProp.PropertyType.GetGenericArguments()[0];
            var strategy = (AbstractFilterStrategy)Activator.CreateInstance(strategyType)!;

            switch (style)
            {
                case ReplacementStyle.FixedWord:
                    strategy.Strategy = AbstractFilterStrategy.StaticReplace;
                    strategy.StaticReplacement = FixedWordValue;
                    break;
                case ReplacementStyle.ConsistentStandIn:
                    strategy.Strategy = AbstractFilterStrategy.RandomReplace;
                    break;
                default:
                    strategy.Strategy = AbstractFilterStrategy.Redact;
                    strategy.RedactionFormat = "{{{REDACTED-%t}}}";
                    break;
            }

            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(strategyType))!;
            list.Add(strategy);
            strategiesProp.SetValue(filter, list);
        }
    }
}
