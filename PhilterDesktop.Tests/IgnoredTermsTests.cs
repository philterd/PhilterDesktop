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
using Phileas.Services;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Verifies the policy-level "always ignore" contract the Policy Editor's Ignore List relies on:
    /// a detected entity whose text matches an ignored term is left unredacted.
    /// </summary>
    public sealed class IgnoredTermsTests
    {
        private static PhileasPolicy PolicyWithIgnored(IEnumerable<string> terms, bool caseSensitive) => new()
        {
            Name = "p",
            Identifiers = new Identifiers { EmailAddress = new EmailAddress() },
            Ignored = new List<Ignored>
            {
                new() { Name = "ignored-terms", Terms = terms.ToList(), Files = new List<string>(), CaseSensitive = caseSensitive }
            }
        };

        [Fact]
        public void IgnoredTerm_IsLeftIntact_OthersStillRedacted()
        {
            var policy = PolicyWithIgnored(new[] { "keep@example.com" }, caseSensitive: false);

            var result = new FilterService().Filter(policy, "ctx", 0,
                "Contact keep@example.com or drop@example.com.");

            Assert.Contains("keep@example.com", result.FilteredText);     // ignored
            Assert.DoesNotContain("drop@example.com", result.FilteredText); // still redacted
        }

        [Fact]
        public void IgnoredTerm_IsCaseInsensitive_ByDefault()
        {
            var policy = PolicyWithIgnored(new[] { "KEEP@EXAMPLE.COM" }, caseSensitive: false);

            var result = new FilterService().Filter(policy, "ctx", 0, "Email keep@example.com here.");

            Assert.Contains("keep@example.com", result.FilteredText);
        }

        [Fact]
        public void NoIgnoredTerms_EverythingRedacted()
        {
            var policy = new PhileasPolicy
            {
                Name = "p",
                Identifiers = new Identifiers { EmailAddress = new EmailAddress() }
            };

            var result = new FilterService().Filter(policy, "ctx", 0, "Email a@example.com here.");

            Assert.DoesNotContain("a@example.com", result.FilteredText);
        }
    }
}
