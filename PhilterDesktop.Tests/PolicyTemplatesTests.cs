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
    public class PolicyTemplatesTests
    {
        // Yield template ids (strings) so the public test signatures don't expose the internal type.
        public static IEnumerable<object[]> TemplateIds =>
            PolicyTemplates.All.Select(t => new object[] { t.Id });

        [Fact]
        public void All_HasTemplates()
        {
            Assert.NotEmpty(PolicyTemplates.All);
            Assert.Contains(PolicyTemplates.All, t => t.Id == "common-pii");
        }

        [Theory]
        [MemberData(nameof(TemplateIds))]
        public void Template_ProducesValidJsonForPhiSqlSchema(string id)
        {
            string json = PolicyTemplates.All.First(t => t.Id == id).BuildJson();
            PolicyValidationResult result = PolicyValidator.Validate(json);
            Assert.True(result.IsValid,
                $"Template '{id}' failed schema validation: {string.Join(" | ", result.Errors)}");
        }

        [Theory]
        [MemberData(nameof(TemplateIds))]
        public void Template_RedactsCommonStructuredPii(string id)
        {
            string json = PolicyTemplates.All.First(t => t.Id == id).BuildJson();
            var policy = PolicySerializer.DeserializeFromJson(json);
            PhEyeModel.Prepare(policy); // strips PhEye in the test env (no model), like the real flow
            var fs = new FilterService();

            // Every template includes SSN; verify it actually redacts.
            const string sample = "Defendant SSN 123-45-6789 on file.";
            var result = fs.Filter(policy, "ctx", 0, sample);

            Assert.DoesNotContain("123-45-6789", result.FilteredText);
        }

        [Fact]
        public void Disclaimer_IsPresent()
        {
            Assert.Contains("starting points", PolicyTemplates.Disclaimer);
            Assert.Contains("not", PolicyTemplates.Disclaimer);
        }
    }
}
