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
using Phileas.Services;
using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Guards against the "default policy is a silent no-op" regression: a fresh user's default
    /// policy must actually redact common, structured PII. (Names rely on the bundled PhEye model,
    /// which isn't present in the test environment, so they're not asserted here.)
    /// </summary>
    public class DefaultPolicyTests
    {
        [Fact]
        public void Json_IsNotEmpty()
        {
            string json = DefaultPolicy.Json();
            Assert.False(string.IsNullOrWhiteSpace(json));
            Assert.NotEqual("{}", json.Trim());
        }

        [Fact]
        public void DefaultPolicy_RedactsCommonStructuredPii()
        {
            var policy = PolicySerializer.DeserializeFromJson(DefaultPolicy.Json());

            // Mirror the real redaction flow: Prepare() injects the on-device model path, or strips
            // PhEye when no model is present (as in this test environment). Without it, the PhEye
            // filter would attempt a remote call.
            PhEyeModel.Prepare(policy);

            var filterService = new FilterService();

            const string sample =
                "Email john@example.com phone 555-123-4567 ssn 123-45-6789 card 4111 1111 1111 1111.";

            var result = filterService.Filter(policy, "ctx", 0, sample);

            Assert.NotEqual(sample, result.FilteredText);
            Assert.DoesNotContain("john@example.com", result.FilteredText);
            Assert.DoesNotContain("555-123-4567", result.FilteredText);
            Assert.DoesNotContain("123-45-6789", result.FilteredText);
            Assert.DoesNotContain("4111 1111 1111 1111", result.FilteredText);
        }

        [Fact]
        public void DefaultPolicy_IncludesDateFilter_WithOnlyValidDates()
        {
            var policy = PolicySerializer.DeserializeFromJson(DefaultPolicy.Json());
            Assert.NotNull(policy.Identifiers.Date);
            Assert.True(policy.Identifiers.Date!.OnlyValidDates); // valid calendar dates only, to limit noise
        }

        [Fact]
        public void DefaultPolicy_RedactsDateOfBirth()
        {
            var policy = PolicySerializer.DeserializeFromJson(DefaultPolicy.Json());
            PhEyeModel.Prepare(policy);

            var result = new FilterService().Filter(policy, "ctx", 0, "Patient DOB: 03/14/1981 on file.");

            Assert.DoesNotContain("03/14/1981", result.FilteredText);
        }

        [Fact]
        public void DefaultPolicy_DoesNotRedactIncidentalNonDateNumbers()
        {
            // Enabling Date must not start redacting number/slash text that isn't a real calendar date.
            var policy = PolicySerializer.DeserializeFromJson(DefaultPolicy.Json());
            PhEyeModel.Prepare(policy);

            const string sample = "The mix ratio was 3/4 and it is page 12 of 34.";
            var result = new FilterService().Filter(policy, "ctx", 0, sample);

            Assert.Equal(sample, result.FilteredText);
        }
    }
}
