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
using Phileas.Policy.Filters;
using Phileas.Policy.Filters.Strategies;
using Phileas.Services;
using PhilterDesktop;
using PhilterDesktop.PolicyEditing;
using Xunit;

namespace PhilterDesktop.Tests
{
    public class PolicyWizardTests
    {
        [Fact]
        public void BuildJson_ProducesSchemaValidPolicy()
        {
            string json = PolicyWizard.BuildJson("Wizard Test",
                new[] { "Ssn", "EmailAddress" }, includeNames: true,
                PolicyWizard.ReplacementStyle.LabeledMarker);

            Assert.True(PolicyValidator.Validate(json).IsValid);
        }

        [Fact]
        public void BuildJson_EnablesChosenFilters_AndRedacts()
        {
            string json = PolicyWizard.BuildJson("w", new[] { "Ssn" }, includeNames: false,
                PolicyWizard.ReplacementStyle.LabeledMarker);

            var policy = PolicySerializer.DeserializeFromJson(json);
            Assert.NotNull(policy.Identifiers.Ssn);
            Assert.Null(policy.Identifiers.EmailAddress); // not chosen

            PhEyeModel.Prepare(policy);
            var result = new FilterService().Filter(policy, "ctx", 0, "ssn 123-45-6789");
            Assert.DoesNotContain("123-45-6789", result.FilteredText);
        }

        [Fact]
        public void BuildJson_FixedWordStyle_UsesStaticReplacement()
        {
            string json = PolicyWizard.BuildJson("w", new[] { "Ssn" }, includeNames: false,
                PolicyWizard.ReplacementStyle.FixedWord);

            var policy = PolicySerializer.DeserializeFromJson(json);
            AbstractFilterStrategy s = policy.Identifiers.Ssn!.Strategies![0];
            Assert.Equal(AbstractFilterStrategy.StaticReplace, s.Strategy);
            Assert.Equal(PolicyWizard.FixedWordValue, s.StaticReplacement);
        }

        [Fact]
        public void BuildJson_IncludeNames_AddsPhEye()
        {
            string json = PolicyWizard.BuildJson("w", Array.Empty<string>(), includeNames: true,
                PolicyWizard.ReplacementStyle.LabeledMarker);

            var policy = PolicySerializer.DeserializeFromJson(json);
            Assert.NotNull(policy.Identifiers.PhEyes);
            Assert.NotEmpty(policy.Identifiers.PhEyes!);
        }

        [Fact]
        public void BaselineFrom_Template_ReflectsItsFilters()
        {
            (HashSet<string> props, bool names) = PolicyWizard.BaselineFrom("common-pii");

            Assert.Contains("Ssn", props);
            Assert.Contains("EmailAddress", props);
            Assert.True(names); // common-pii includes on-device name detection
        }

        [Fact]
        public void BaselineFrom_UnknownId_IsEmpty()
        {
            (HashSet<string> props, bool names) = PolicyWizard.BaselineFrom("does-not-exist");
            Assert.Empty(props);
            Assert.False(names);
        }

        [Fact]
        public void Catalog_Grouped_IncludesKnownFiltersWithoutDuplicates()
        {
            var all = FilterCatalog.Grouped().SelectMany(g => g).Select(f => f.Property).ToList();
            Assert.Contains("Ssn", all);
            Assert.Contains("EmailAddress", all);
            Assert.Equal(all.Count, all.Distinct().Count()); // each filter appears once
        }
    }
}
