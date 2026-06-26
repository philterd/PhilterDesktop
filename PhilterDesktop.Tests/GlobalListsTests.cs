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
        public void Apply_AddsManagedDictionaryAndIgnoredSet()
        {
            var policy = new Policy { Identifiers = new Identifiers() };
            var settings = new SettingsEntity { GlobalAlwaysRedact = "Acme Corp", GlobalAlwaysIgnore = "Public Co" };

            GlobalLists.Apply(policy, settings);

            Assert.Contains(policy.Identifiers.Dictionaries!, d => d.Name == GlobalLists.AlwaysRedactName && d.Terms!.Contains("Acme Corp"));
            Assert.Contains(policy.Ignored!, i => i.Name == GlobalLists.AlwaysIgnoreName && i.Terms!.Contains("Public Co"));
        }

        [Fact]
        public void Apply_EmptyLists_AddsNothing()
        {
            var policy = new Policy { Identifiers = new Identifiers() };
            GlobalLists.Apply(policy, new SettingsEntity());

            Assert.True(policy.Identifiers.Dictionaries is null || policy.Identifiers.Dictionaries.Count == 0);
            Assert.True(policy.Ignored is null || policy.Ignored.Count == 0);
        }

        [Fact]
        public void ApplyTerms_IsIdempotent_ReplacesOwnEntries()
        {
            var policy = new Policy { Identifiers = new Identifiers() };
            GlobalLists.ApplyTerms(policy, new[] { "One" }, new[] { "Two" });
            GlobalLists.ApplyTerms(policy, new[] { "Three" }, new[] { "Four" });

            // Only one managed dictionary / ignored set, reflecting the latest terms.
            Assert.Single(policy.Identifiers.Dictionaries!, d => d.Name == GlobalLists.AlwaysRedactName);
            Assert.Single(policy.Ignored!, i => i.Name == GlobalLists.AlwaysIgnoreName);
            Assert.Contains("Three", policy.Identifiers.Dictionaries!.First(d => d.Name == GlobalLists.AlwaysRedactName).Terms!);
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
    }
}
