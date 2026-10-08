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
using System.Text.Json.Nodes;
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Policy.Filters.Strategies;
using Phileas.Services;
using PhilterDesktop.PolicyEditing;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Policies saved with conditions the engine can't parse (once treated as always true) are cleaned up
    /// so they load again, and nothing else in them changes.
    /// </summary>
    public sealed class LegacyConditionsTests
    {
        private const string TypeStartsWith = "type startswith \"SS\"";
        private const string BadNumber = "confidence == 1E3";

        private static string SsnPolicy(params (string Strategy, string? Condition)[] strategies)
        {
            var array = new JsonArray();
            foreach ((string strategy, string? condition) in strategies)
            {
                var item = new JsonObject { ["strategy"] = strategy };
                if (condition is not null)
                {
                    item["condition"] = condition;
                }
                array.Add(item);
            }
            return new JsonObject
            {
                ["identifiers"] = new JsonObject { ["ssn"] = new JsonObject { ["ssnFilterStrategies"] = array } }
            }.ToJsonString();
        }

        private static JsonArray SsnStrategies(string json) =>
            JsonNode.Parse(json)!["identifiers"]!["ssn"]!["ssnFilterStrategies"]!.AsArray();

        private static string Redact(string json, string text) =>
            new FilterService().Filter(PolicySerializer.DeserializeFromJson(json), "ctx", 0, text).FilteredText;

        [Fact]
        public void EngineRefusesToLoad_APolicyWithAnUnparseableCondition()
        {
            // Why the cleanup exists.
            Assert.Throws<PolicyValidationException>(() =>
                PolicySerializer.DeserializeFromJson(SsnPolicy(("REDACT", TypeStartsWith))));
        }

        [Theory]
        [InlineData(TypeStartsWith)]
        [InlineData(BadNumber)]
        [InlineData("token == ")]
        [InlineData("token startswith \"a\" or token == \"b\"")]
        public void UnparseableCondition_IsRemoved_AndThePolicyLoads(string condition)
        {
            string json = LegacyConditions.RemoveUnparseable(SsnPolicy(("REDACT", condition)), out var removed);

            Assert.Equal(new[] { "SSN: " + condition }, removed);
            JsonObject strategy = SsnStrategies(json)[0]!.AsObject();
            Assert.False(strategy.ContainsKey("condition"));
            Assert.Equal("REDACT", (string?)strategy["strategy"]);
            Assert.NotNull(PolicySerializer.DeserializeFromJson(json));
        }

        [Fact]
        public void RemovedCondition_StrategyStillAppliesToEveryMatch()
        {
            string json = LegacyConditions.RemoveUnparseable(SsnPolicy(("REDACT", TypeStartsWith)), out _);

            Assert.Equal("SSN {{{REDACTED-ssn}}} and {{{REDACTED-ssn}}}.", Redact(json, "SSN 123-45-6789 and 212-34-5678."));
        }

        [Theory]
        [InlineData("confidence > 0.5")]
        [InlineData("token == \"123-45-6789\"")]
        [InlineData("context != \"ctx\"")]
        [InlineData("token startswith \"1\" and confidence >= 0.5")]
        [InlineData("type == \"ssn\"")]
        public void ParseableCondition_IsKept_AndTheTextIsUnchanged(string condition)
        {
            string original = SsnPolicy(("REDACT", condition));

            string json = LegacyConditions.RemoveUnparseable(original, out var removed);

            Assert.Empty(removed);
            Assert.Same(original, json);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void BlankCondition_IsLeftAlone_BecauseItMeansAlways(string condition)
        {
            string original = SsnPolicy(("REDACT", condition));

            string json = LegacyConditions.RemoveUnparseable(original, out var removed);

            Assert.Empty(removed);
            Assert.Same(original, json);
            Assert.Equal("SSN {{{REDACTED-ssn}}}.", Redact(json, "SSN 123-45-6789."));
        }

        [Fact]
        public void OnlyTheBadConditions_AreRemoved_AmongSeveralStrategies()
        {
            string original = SsnPolicy(
                ("MASK", "confidence > 0.5"),
                ("REDACT", TypeStartsWith),
                ("LAST_4", null),
                ("REDACT", BadNumber));

            string json = LegacyConditions.RemoveUnparseable(original, out var removed);

            Assert.Equal(new[] { "SSN: " + TypeStartsWith, "SSN: " + BadNumber }, removed);
            JsonArray strategies = SsnStrategies(json);
            Assert.Equal(4, strategies.Count);
            Assert.Equal("confidence > 0.5", (string?)strategies[0]!["condition"]);
            Assert.Equal(new[] { "MASK", "REDACT", "LAST_4", "REDACT" }, strategies.Select(s => (string?)s!["strategy"]));
            Assert.All(strategies.Skip(1), s => Assert.False(s!.AsObject().ContainsKey("condition")));
            Assert.NotNull(PolicySerializer.DeserializeFromJson(json));
        }

        [Fact]
        public void ConditionsInEveryFilter_AndInListFilters_AreChecked()
        {
            var policy = new PhileasPolicy
            {
                Identifiers = new Identifiers
                {
                    EmailAddress = new EmailAddress { Strategies = new() { new() { Strategy = "REDACT", Condition = "confidence == 0.1" } } },
                    CustomDictionaries = new List<CustomDictionary>
                    {
                        new()
                        {
                            Terms = new List<string> { "Zephyrous" },
                            Strategies = new() { new() { Strategy = "REDACT", Condition = "confidence == 0.2" } }
                        }
                    }
                }
            };
            string original = PolicySerializer.SerializeToJson(policy)
                .Replace("confidence == 0.1", "type startswith \\u0022E\\u0022")
                .Replace("confidence == 0.2", BadNumber);
            Assert.Throws<PolicyValidationException>(() => PolicySerializer.DeserializeFromJson(original));

            string json = LegacyConditions.RemoveUnparseable(original, out var removed);

            Assert.Equal(2, removed.Count);
            Assert.Contains("Email Address: type startswith \"E\"", removed);
            Assert.Contains("Custom: " + BadNumber, removed);
            PhileasPolicy loaded = PolicySerializer.DeserializeFromJson(json);
            Assert.Null(loaded.Identifiers.EmailAddress!.Strategies![0].Condition);
            Assert.Null(loaded.Identifiers.CustomDictionaries![0].Strategies![0].Condition);
            Assert.Equal("Zephyrous", loaded.Identifiers.CustomDictionaries[0].Terms![0]);
        }

        [Fact]
        public void EveryFilter_HasItsConditionsChecked()
        {
            // Builds one strategy per filter and confirms the cleanup reaches it, whatever its JSON name.
            foreach ((string name, PropertyInfo property) in FilterCatalog.Discover())
            {
                object filter = Activator.CreateInstance(property.PropertyType)!;
                PropertyInfo strategiesProperty = property.PropertyType.GetProperty("Strategies")!;
                var strategies = (IList)Activator.CreateInstance(strategiesProperty.PropertyType)!;
                var strategy = (AbstractFilterStrategy)Activator.CreateInstance(strategiesProperty.PropertyType.GetGenericArguments()[0])!;
                strategy.Strategy = "REDACT";
                strategy.Condition = "confidence == 0.25";
                strategies.Add(strategy);
                strategiesProperty.SetValue(filter, strategies);
                var identifiers = new Identifiers();
                property.SetValue(identifiers, filter);
                string original = PolicySerializer.SerializeToJson(new PhileasPolicy { Name = "p", Identifiers = identifiers })
                    .Replace("confidence == 0.25", BadNumber);

                string json = LegacyConditions.RemoveUnparseable(original, out var removed);

                Assert.True(removed.Count == 1, name + " condition was not found");
                Assert.EndsWith(": " + BadNumber, removed[0]);
                Assert.Null(((AbstractFilterStrategy)((IList)strategiesProperty.GetValue(property.GetValue(
                    PolicySerializer.DeserializeFromJson(json).Identifiers)!)!)[0]!).Condition);
            }
        }

        [Fact]
        public void EverythingElseInThePolicy_IsPreserved()
        {
            string original = new JsonObject
            {
                ["config"] = new JsonObject { ["splitting"] = new JsonObject { ["enabled"] = true, ["threshold"] = 5000 } },
                ["identifiers"] = new JsonObject
                {
                    ["ssn"] = new JsonObject
                    {
                        ["enabled"] = true,
                        ["ssnFilterStrategies"] = new JsonArray(new JsonObject
                        {
                            ["strategy"] = "STATIC_REPLACE",
                            ["staticReplacement"] = "[ID]",
                            ["condition"] = TypeStartsWith
                        })
                    }
                }
            }.ToJsonString();

            string json = LegacyConditions.RemoveUnparseable(original, out _);

            JsonNode expected = JsonNode.Parse(original)!;
            expected["identifiers"]!["ssn"]!["ssnFilterStrategies"]![0]!.AsObject().Remove("condition");
            Assert.True(JsonNode.DeepEquals(expected, JsonNode.Parse(json)));
            Assert.Equal("SSN [ID].", Redact(json, "SSN 123-45-6789."));
        }

        [Theory]
        [InlineData("")]
        [InlineData("not json")]
        [InlineData("{\"identifiers\":")]
        public void TextThatIsNotJson_IsReturnedUnchanged(string text)
        {
            Assert.Same(text, LegacyConditions.RemoveUnparseable(text, out var removed));
            Assert.Empty(removed);
        }

        [Theory]
        [InlineData("{}")]
        [InlineData("[]")]
        [InlineData("null")]
        [InlineData("{\"identifiers\":{\"ssn\":{\"ssnFilterStrategies\":[{\"strategy\":\"REDACT\",\"condition\":42}]}}}")]
        [InlineData("{\"identifiers\":{\"ssn\":{\"ssnFilterStrategies\":[{\"strategy\":\"REDACT\",\"condition\":null}]}}}")]
        [InlineData("{\"identifiers\":{\"ssn\":{\"ssnFilterStrategies\":[\"REDACT\", 1, null]}}}")]
        [InlineData("{\"identifiers\":{\"ssn\":{\"ssnFilterStrategies\":{\"condition\":\"type startswith \\\"S\\\"\"}}}}")]
        [InlineData("{\"notes\":{\"condition\":\"type startswith \\\"S\\\"\"}}")]
        public void ShapesWithNoStrategyCondition_AreLeftAlone(string json)
        {
            Assert.Same(json, LegacyConditions.RemoveUnparseable(json, out var removed));
            Assert.Empty(removed);
        }
    }
}
