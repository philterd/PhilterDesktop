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
