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

using System.Text.Json;
using System.Text.Json.Nodes;
using Phileas.Filters.Conditions;

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// Removes strategy conditions the engine can't parse. Older engines treated them as always true;
    /// the current engine refuses to load a policy that has one, so removing them keeps the old behavior
    /// and lets the policy open again.
    /// </summary>
    internal static class LegacyConditions
    {
        private const string StrategiesSuffix = "FilterStrategies";

        /// <summary>
        /// Returns <paramref name="json"/> without unparseable conditions (unchanged when there are none,
        /// or when it isn't JSON). <paramref name="removed"/> describes each one as "Filter: condition".
        /// </summary>
        internal static string RemoveUnparseable(string json, out IReadOnlyList<string> removed)
        {
            var found = new List<string>();
            removed = found;

            JsonNode? root;
            try
            {
                root = JsonNode.Parse(json);
            }
            catch (JsonException)
            {
                return json;
            }

            Visit(root, found);
            return found.Count == 0 ? json : root!.ToJsonString();
        }

        private static void Visit(JsonNode? node, List<string> removed)
        {
            if (node is JsonArray array)
            {
                foreach (JsonNode? item in array)
                {
                    Visit(item, removed);
                }
                return;
            }
            if (node is not JsonObject obj)
            {
                return;
            }

            foreach ((string name, JsonNode? value) in obj.ToList())
            {
                if (value is JsonArray strategies && name.EndsWith(StrategiesSuffix, StringComparison.Ordinal))
                {
                    string key = name[..^StrategiesSuffix.Length];
                    string filter = key.Length == 0 ? name : FilterLabel.Humanize(char.ToUpperInvariant(key[0]) + key[1..]);
                    foreach (JsonObject strategy in strategies.OfType<JsonObject>())
                    {
                        RemoveIfUnparseable(strategy, filter, removed);
                    }
                }
                else
                {
                    Visit(value, removed);
                }
            }
        }

        private static void RemoveIfUnparseable(JsonObject strategy, string filter, List<string> removed)
        {
            if (strategy["condition"] is not JsonValue value || !value.TryGetValue(out string? condition))
            {
                return;
            }
            // A blank condition means "always", which the engine accepts.
            if (string.IsNullOrWhiteSpace(condition) || ConditionParser.GetError(condition) is null)
            {
                return;
            }
            strategy.Remove("condition");
            removed.Add(filter + ": " + condition);
        }
    }
}
