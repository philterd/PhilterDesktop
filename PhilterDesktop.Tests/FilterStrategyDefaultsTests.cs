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
using PhilterDesktop.PolicyEditing;
using Xunit;

namespace PhilterDesktop.Tests
{
    public class FilterStrategyDefaultsTests
    {
        [Fact]
        public void MaterializeMissing_AddsDefaultRedactStrategy_WhenEnabledFilterHasNone()
        {
            var policy = new Policy { Identifiers = new Identifiers { Ssn = new Ssn() } };

            FilterStrategyDefaults.MaterializeMissing(policy);

            Assert.NotNull(policy.Identifiers.Ssn!.Strategies);
            SsnFilterStrategy s = Assert.Single(policy.Identifiers.Ssn.Strategies!);
            Assert.Equal("REDACT", s.Strategy);
            Assert.Equal("{{{REDACTED-%t}}}", s.RedactionFormat);
        }

        [Fact]
        public void MaterializeMissing_LeavesExistingStrategiesUntouched()
        {
            var policy = new Policy
            {
                Identifiers = new Identifiers
                {
                    Ssn = new Ssn { Strategies = new List<SsnFilterStrategy> { new() { Strategy = "MASK" } } }
                }
            };

            FilterStrategyDefaults.MaterializeMissing(policy);

            SsnFilterStrategy s = Assert.Single(policy.Identifiers.Ssn!.Strategies!);
            Assert.Equal("MASK", s.Strategy);
        }

        [Fact]
        public void MaterializeMissing_IgnoresDisabledFilters()
        {
            var policy = new Policy { Identifiers = new Identifiers() }; // Ssn is null (disabled)

            FilterStrategyDefaults.MaterializeMissing(policy);

            Assert.Null(policy.Identifiers.Ssn);
        }

        [Fact]
        public void MaterializeMissing_PreservesRedactionBehavior()
        {
            // The materialized default must redact exactly like the implicit default did.
            var policy = PolicySerializer.DeserializeFromJson("{\"identifiers\":{\"ssn\":{}}}");
            FilterStrategyDefaults.MaterializeMissing(policy);

            var result = new FilterService().Filter(policy, "ctx", 0, "ssn 123-45-6789 end");

            Assert.DoesNotContain("123-45-6789", result.FilteredText);
        }

        [Fact]
        public void MaterializeMissing_AddsDefaultStrategy_ToPhEyeNameFilter()
        {
            var policy = new Policy
            {
                Identifiers = new Identifiers { PhEyes = new List<PhEye> { new() } }
            };

            FilterStrategyDefaults.MaterializeMissing(policy);

            PhEyeFilterStrategy s = Assert.Single(policy.Identifiers.PhEyes![0].Strategies!);
            Assert.Equal("REDACT", s.Strategy);
            Assert.Equal("{{{REDACTED-%t}}}", s.RedactionFormat);
        }

        [Fact]
        public void MaterializeMissing_NullPolicy_DoesNotThrow()
        {
            FilterStrategyDefaults.MaterializeMissing(null);
        }
    }
}
