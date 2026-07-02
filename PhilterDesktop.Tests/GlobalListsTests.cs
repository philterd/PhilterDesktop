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
using PhilterData;
using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    public class GlobalListsTests
    {
        [Fact]
        public void ParseTerms_TrimsBlanksAndDeduplicates()
        {
            var terms = GlobalLists.ParseTerms("Acme\r\n  Acme \n\n Beta \nACME");
            Assert.Equal(new[] { "Acme", "Beta" }, terms);
        }

        [Fact]
        public void Apply_AddsManagedIdentifierAndIgnoredSet()
        {
            var policy = new Policy { Identifiers = new Identifiers() };
            var settings = new SettingsEntity { GlobalAlwaysRedact = "Acme Corp", GlobalAlwaysIgnore = "Public Co" };

            GlobalLists.Apply(policy, settings);

            Assert.Contains(policy.Identifiers.CustomIdentifiers!, i => i.Classification == GlobalLists.AlwaysRedactName && i.Pattern.Contains("Acme"));
            Assert.Contains(policy.Ignored!, i => i.Name == GlobalLists.AlwaysIgnoreName && i.Terms!.Contains("Public Co"));
        }

        [Fact]
        public void Apply_EmptyLists_AddsNothing()
        {
            var policy = new Policy { Identifiers = new Identifiers() };
            GlobalLists.Apply(policy, new SettingsEntity());

            Assert.True(policy.Identifiers.CustomIdentifiers is null || policy.Identifiers.CustomIdentifiers.Count == 0);
            Assert.True(policy.Ignored is null || policy.Ignored.Count == 0);
        }

        [Fact]
        public void ApplyTerms_IsIdempotent_ReplacesOwnEntries()
        {
            var policy = new Policy { Identifiers = new Identifiers() };
            GlobalLists.ApplyTerms(policy, new[] { "One" }, new[] { "Two" });
            GlobalLists.ApplyTerms(policy, new[] { "Three" }, new[] { "Four" });

            // Only one managed always-redact identifier / ignored set, reflecting the latest terms.
            var identifier = Assert.Single(policy.Identifiers.CustomIdentifiers!, i => i.Classification == GlobalLists.AlwaysRedactName);
            Assert.Single(policy.Ignored!, i => i.Name == GlobalLists.AlwaysIgnoreName);
            Assert.Contains("Three", identifier.Pattern);
            Assert.DoesNotContain("One", identifier.Pattern);
        }

        [Fact]
        public void AppliedGlobalLists_AffectRedaction()
        {
            // Always-redact a custom term, and always-ignore something a filter would otherwise remove.
            var policy = PolicySerializer.DeserializeFromJson("{\"identifiers\":{\"emailAddress\":{}}}");
            GlobalLists.ApplyTerms(policy, alwaysRedact: new[] { "Projity" }, alwaysIgnore: new[] { "keep@example.com" });

            var fs = new FilterService();

            // The custom always-redact term is removed even though no built-in filter targets it.
            string redacted = fs.Filter(policy, "ctx", 0, "Codename Projity is internal.").FilteredText;
            Assert.DoesNotContain("Projity", redacted);

            // The always-ignore email survives even though the email filter is on.
            string ignored = fs.Filter(policy, "ctx", 0, "Reach us at keep@example.com please.").FilteredText;
            Assert.Contains("keep@example.com", ignored);
        }

        // ---- whole-word matching + '*' wildcard opt-in ----

        private static string Redact(string text, params string[] alwaysRedact)
        {
            var policy = new Policy { Identifiers = new Identifiers() };
            GlobalLists.ApplyTerms(policy, alwaysRedact, Array.Empty<string>());
            return new FilterService().Filter(policy, "ctx", 0, text).FilteredText;
        }

        [Fact]
        public void PlainTerm_MatchesWholeWord_EvenWhenGluedByPunctuation()
        {
            string r = Redact("Contact Acme,Inc today.", "Acme");
            Assert.DoesNotContain("Acme", r); // caught even though a comma joins it to "Inc"
            Assert.Contains("Inc", r);        // the rest of the token is left alone
        }

        [Fact]
        public void PlainTerm_DoesNotMatchInsideAWord()
        {
            Assert.Contains("category", Redact("the category list", "cat")); // 'cat' isn't a whole word here
            Assert.Contains("banana", Redact("a banana here", "a"));         // 'a' doesn't nuke letters in words
        }

        [Fact]
        public void Wildcard_Contains_MatchesInsideWords()
        {
            string r = Redact("see acmecorp and myacme now", "*acme*");
            Assert.DoesNotContain("acmecorp", r);
            Assert.DoesNotContain("myacme", r);
            Assert.Contains("see", r);
            Assert.Contains("now", r);
        }

        [Fact]
        public void Wildcard_Prefix_MatchesWordStartOnly()
        {
            string r = Redact("acmecorp vs myacme", "acme*");
            Assert.DoesNotContain("acmecorp", r); // starts with acme
            Assert.Contains("myacme", r);         // does not start with acme
        }

        [Fact]
        public void Wildcard_Suffix_MatchesWordEndOnly()
        {
            string r = Redact("myacme vs acmecorp", "*acme");
            Assert.DoesNotContain("myacme", r);  // ends with acme
            Assert.Contains("acmecorp", r);      // does not end with acme
        }

        [Fact]
        public void Matching_IsCaseInsensitive()
        {
            Assert.DoesNotContain("ACME", Redact("the ACME brand", "acme"));
        }

        [Fact]
        public void BareWildcard_IsIgnored_AndRedactsNothing()
        {
            var policy = new Policy { Identifiers = new Identifiers() };
            GlobalLists.ApplyTerms(policy, new[] { "*" }, Array.Empty<string>());

            Assert.Null(policy.Identifiers.CustomIdentifiers); // no identifier added for a content-less term
            Assert.Equal("ordinary words here",
                new FilterService().Filter(policy, "ctx", 0, "ordinary words here").FilteredText);
        }

        [Theory]
        [InlineData("acme", @"(?<!\w)acme(?!\w)")]
        [InlineData("acme*", @"(?<!\w)acme\w*(?!\w)")]
        [InlineData("*acme", @"(?<!\w)\w*acme(?!\w)")]
        [InlineData("*acme*", @"(?<!\w)\w*acme\w*(?!\w)")]
        public void TermToPattern_BuildsWordBoundedRegex(string term, string expected)
        {
            Assert.Equal(expected, GlobalLists.TermToPattern(term));
        }

        [Theory]
        [InlineData("")]
        [InlineData("*")]
        [InlineData("**")]
        public void TermToPattern_NoLiteralContent_ReturnsNull(string term)
        {
            Assert.Null(GlobalLists.TermToPattern(term));
        }
    }
}
