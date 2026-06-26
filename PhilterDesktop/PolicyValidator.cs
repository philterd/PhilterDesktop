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
using Json.Schema;
using Philterd.PhiSql;

namespace PhilterDesktop
{
    /// <summary>The outcome of validating a policy's JSON against the PhiSQL policy schema.</summary>
    internal sealed class PolicyValidationResult
    {
        public bool IsValid { get; init; }

        /// <summary>Human-readable validation messages (empty when valid).</summary>
        public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

        public static readonly PolicyValidationResult Success = new() { IsValid = true };

        public static PolicyValidationResult Invalid(params string[] errors) =>
            new() { IsValid = false, Errors = errors };
    }

    /// <summary>
    /// Validates Philter Desktop policy JSON against the policy JSON Schema published by PhiSQL — the
    /// <c>Philterd.PhiSql</c> build that phileas-dotnet itself depends on. Using PhiSQL's schema (rather
    /// than a hand-maintained copy) guarantees a policy we accept is one the engine will accept, and it
    /// tracks the engine's schema version automatically since PhiSQL comes in transitively with Phileas.
    /// </summary>
    internal static class PolicyValidator
    {
        /// <summary>The policy schema version supported by the bundled PhiSQL (matches phileas-dotnet).</summary>
        public static string SchemaVersion => PolicySchema.GetSupportedSchemaVersion();

        // Parse the schema once. PolicySchema.GetSchema() returns the canonical schema text from PhiSQL.
        private static readonly JsonSchema Schema = JsonSchema.FromText(PolicySchema.GetSchema());

        /// <summary>Validates the given policy JSON against the PhiSQL policy schema.</summary>
        public static PolicyValidationResult Validate(string? policyJson)
        {
            JsonNode? node;
            try
            {
                node = JsonNode.Parse(string.IsNullOrWhiteSpace(policyJson) ? "{}" : policyJson);
            }
            catch (JsonException ex)
            {
                return PolicyValidationResult.Invalid("The policy is not valid JSON: " + ex.Message);
            }

            EvaluationResults results = Schema.Evaluate(
                node, new EvaluationOptions { OutputFormat = OutputFormat.List });

            if (results.IsValid)
            {
                return PolicyValidationResult.Success;
            }

            var errors = new List<string>();
            CollectErrors(results, errors);
            if (errors.Count == 0)
            {
                errors.Add("The policy does not match the PhiSQL policy schema.");
            }
            return new PolicyValidationResult { IsValid = false, Errors = errors };
        }

        private static void CollectErrors(EvaluationResults results, List<string> errors)
        {
            // OutputFormat.List flattens all nodes; collect messages from the failing ones.
            foreach (EvaluationResults detail in results.Details)
            {
                if (detail.IsValid || detail.Errors is null)
                {
                    continue;
                }
                string location = detail.InstanceLocation.ToString();
                foreach (KeyValuePair<string, string> error in detail.Errors)
                {
                    errors.Add(string.IsNullOrEmpty(location) ? error.Value : $"{location}: {error.Value}");
                }
            }
        }
    }
}
