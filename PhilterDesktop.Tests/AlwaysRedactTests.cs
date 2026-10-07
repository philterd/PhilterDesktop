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
using PhilterDesktop.PolicyEditing;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Verifies the policy-level "always redact" contract the Policy Editor's Always Redact list
    /// relies on: a literal term is redacted even though no built-in filter would detect it.
    /// </summary>
    public sealed class AlwaysRedactTests
    {
        // Built the way the Policy Editor stores the list, so the test covers what ships.
        private static PhileasPolicy PolicyWithAlwaysRedact(params string[] terms)
        {
            var policy = new PhileasPolicy { Name = "p", Identifiers = new Identifiers() };
            AlwaysRedactPolicy.SetTerms(policy, terms);
            return policy;
        }

        [Fact]
        public void AlwaysRedactTerm_IsRedacted_EvenWithoutAFilter()
        {
            var policy = PolicyWithAlwaysRedact("Voldemort");

            var result = new FilterService().Filter(policy, "ctx", 0, "Tell Voldemort the news.");

            Assert.DoesNotContain("Voldemort", result.FilteredText);
            Assert.NotEmpty(result.Spans);
        }

        [Fact]
        public void AlwaysRedact_IsCaseInsensitive()
        {
            var policy = PolicyWithAlwaysRedact("voldemort");

            var result = new FilterService().Filter(policy, "ctx", 0, "Tell VOLDEMORT the news.");

            Assert.DoesNotContain("VOLDEMORT", result.FilteredText);
        }

        [Fact]
        public void WithoutAlwaysRedact_TermRemains()
        {
            var policy = new PhileasPolicy { Name = "p", Identifiers = new Identifiers() };

            var result = new FilterService().Filter(policy, "ctx", 0, "Tell Voldemort the news.");

            Assert.Contains("Voldemort", result.FilteredText);
        }
    }
}
