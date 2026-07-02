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

using Phileas.Model;
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using PhilterDesktop;
using PhilterDesktop.PolicyEditing;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The policy editor's "Always Redact" feature: terms must be stored so the policy still
    /// validates against the engine's schema (the editor blocks any save that fails it) and so the engine
    /// actually redacts them. Previously they were stored under the schema-invalid "dictionary" key,
    /// which made the policy unsaveable.
    /// </summary>
    public sealed class AlwaysRedactPolicyTests
    {
        private static PhileasPolicy NewPolicy() => new() { Name = "test", Identifiers = new Identifiers() };

        // The core regression: a policy carrying always-redact terms now passes the schema gate the
        // editor enforces before saving (PolicyValidator.Validate == EnsureValid).
        [Fact]
        public void PolicyWithAlwaysRedactTerms_IsSchemaValid_AndSaveable()
        {
            PhileasPolicy policy = NewPolicy();
            AlwaysRedactPolicy.SetTerms(policy, new[] { "Voldemort", "Acme Corp" });

            string json = PolicySerializer.SerializeToJson(policy);
            PolicyValidationResult result = PolicyValidator.Validate(json);

            Assert.True(result.IsValid, "policy with always-redact terms failed schema validation: " + string.Join(" | ", result.Errors));
        }

        // Documents the bug: the old storage (Identifiers.Dictionaries -> "dictionary") is schema-invalid,
        // which is exactly why saving was blocked. Guards against regressing to it.
        [Fact]
        public void OldDictionaryStorage_IsSchemaInvalid()
        {
            PhileasPolicy policy = NewPolicy();
            policy.Identifiers.Dictionaries = new List<Dictionary>
            {
                new() { Name = "always-redact", Terms = new List<string> { "Voldemort" }, Enabled = true }
            };

            PolicyValidationResult result = PolicyValidator.Validate(PolicySerializer.SerializeToJson(policy));

            Assert.False(result.IsValid); // the schema has no "dictionary" property (additionalProperties:false)
        }

        [Fact]
        public void SetTerms_StoresUnderCustomDictionaries_NotDictionaries()
        {
            PhileasPolicy policy = NewPolicy();
            AlwaysRedactPolicy.SetTerms(policy, new[] { "Voldemort" });

            Assert.Null(policy.Identifiers.Dictionaries);              // never the schema-invalid list
            Assert.NotNull(policy.Identifiers.CustomDictionaries);
            CustomDictionary dict = Assert.Single(policy.Identifiers.CustomDictionaries!);
            Assert.Equal(AlwaysRedactPolicy.Classification, dict.Classification);
            Assert.True(dict.Enabled);

            string json = PolicySerializer.SerializeToJson(policy);
            Assert.Contains("\"dictionaries\"", json);
            Assert.DoesNotContain("\"dictionary\"", json);
        }

        [Fact]
        public void GetTerms_RoundTripsThroughSerializer()
        {
            PhileasPolicy policy = NewPolicy();
            AlwaysRedactPolicy.SetTerms(policy, new[] { "Voldemort", "Acme" });

            PhileasPolicy reloaded = PolicySerializer.DeserializeFromJson(PolicySerializer.SerializeToJson(policy));

            Assert.Equal(new[] { "Voldemort", "Acme" }, AlwaysRedactPolicy.GetTerms(reloaded));
        }

        [Fact]
        public void GetTerms_IsEmpty_WhenNoneSet()
        {
            Assert.Empty(AlwaysRedactPolicy.GetTerms(NewPolicy()));
        }

        [Fact]
        public void SetTerms_Empty_RemovesTheDictionary()
        {
            PhileasPolicy policy = NewPolicy();
            AlwaysRedactPolicy.SetTerms(policy, new[] { "Voldemort" });

            AlwaysRedactPolicy.SetTerms(policy, Array.Empty<string>());

            Assert.Empty(AlwaysRedactPolicy.GetTerms(policy));
            Assert.True(policy.Identifiers.CustomDictionaries is null || policy.Identifiers.CustomDictionaries.Count == 0);
            Assert.True(PolicyValidator.Validate(PolicySerializer.SerializeToJson(policy)).IsValid);
        }

        [Fact]
        public void SetTerms_Twice_ReplacesNotDuplicates()
        {
            PhileasPolicy policy = NewPolicy();
            AlwaysRedactPolicy.SetTerms(policy, new[] { "first" });
            AlwaysRedactPolicy.SetTerms(policy, new[] { "second", "third" });

            Assert.Equal(new[] { "second", "third" }, AlwaysRedactPolicy.GetTerms(policy));
            Assert.Single(policy.Identifiers.CustomDictionaries!.Where(d => d.Classification == AlwaysRedactPolicy.Classification));
        }

        [Fact]
        public void SetTerms_LeavesOtherCustomDictionariesIntact()
        {
            PhileasPolicy policy = NewPolicy();
            policy.Identifiers.CustomDictionaries = new List<CustomDictionary>
            {
                new() { Classification = "diagnoses", Terms = new List<string> { "fever" }, Enabled = true }
            };

            AlwaysRedactPolicy.SetTerms(policy, new[] { "Voldemort" });

            Assert.Equal(2, policy.Identifiers.CustomDictionaries!.Count);
            Assert.Contains(policy.Identifiers.CustomDictionaries!, d => d.Classification == "diagnoses");
            Assert.Contains(policy.Identifiers.CustomDictionaries!, d => d.Classification == AlwaysRedactPolicy.Classification);
        }

        // End-to-end: the stored terms are actually redacted by the engine.
        [Fact]
        public void AlwaysRedactTerms_AreRedactedByTheEngine()
        {
            PhileasPolicy policy = NewPolicy();
            AlwaysRedactPolicy.SetTerms(policy, new[] { "Voldemort" });

            var fs = new FilterService();
            TextFilterResult result = fs.Filter(policy, "ctx", 0, "Owl mail for Voldemort today.");

            Assert.DoesNotContain("Voldemort", result.FilteredText);
        }
    }
}
