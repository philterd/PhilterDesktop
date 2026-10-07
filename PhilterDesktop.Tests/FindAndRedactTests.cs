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
    public class FindAndRedactTests
    {
        private static string Filter(IReadOnlyList<string> terms, string text) =>
            new FilterService().Filter(FindAndRedact.BuildPolicy(terms), "ctx", 0, text).FilteredText;

        [Fact]
        public void BuildPolicy_RedactsTheGivenTerms_RegardlessOfType()
        {
            // No built-in filter targets "Bluebird"/"Falcon" — only the ad-hoc terms should remove them.
            string result = Filter(new[] { "Bluebird", "Falcon" }, "Operation Bluebird used the Falcon route.");

            Assert.DoesNotContain("Bluebird", result);
            Assert.DoesNotContain("Falcon", result);
        }

        [Fact]
        public void BuildPolicy_NoTerms_LeavesTextUnchanged()
        {
            const string text = "Nothing here should change.";
            Assert.Equal(text, Filter(System.Array.Empty<string>(), text));
        }

        // Terms next to a line break or tab must still be found (a custom dictionary misses them on
        // Phileas 1.6.0, so Find & Redact uses a regex instead).
        [Theory]
        [InlineData("Project\nBluebird")]
        [InlineData("Project\r\nBluebird")]
        [InlineData("Name:\tBluebird")]
        [InlineData("Bluebird\nfollows")]
        public void Term_NextToALineBreakOrTab_IsRedacted(string text) =>
            Assert.DoesNotContain("Bluebird", Filter(new[] { "Bluebird" }, text));

        [Theory]
        [InlineData("operation BLUEBIRD launched.")]
        [InlineData("(Bluebird).")]
        [InlineData("\"bluebird\"")]
        public void Term_IgnoresCase_AndNeighboringPunctuation(string text) =>
            Assert.DoesNotContain("bluebird", Filter(new[] { "Bluebird" }, text), StringComparison.OrdinalIgnoreCase);

        // Whole words only, as before: other words that contain the term are left alone.
        [Theory]
        [InlineData("Bluebirds flew.")]
        [InlineData("MyBluebird app")]
        public void Term_DoesNotMatchInsideOtherWords(string text) =>
            Assert.Equal(text, Filter(new[] { "Bluebird" }, text));

        [Fact]
        public void MultiWordTerm_IsRedactedAsAPhrase()
        {
            string result = Filter(new[] { "John Smith" }, "Meet John Smith today, not John Doe.");
            Assert.DoesNotContain("John Smith", result);
            Assert.Contains("John Doe", result);
        }

        // Regex metacharacters in a term are literal text.
        [Theory]
        [InlineData("ACME-42", "Ref ACME-42 filed.", "ACME-42")]
        [InlineData("j.doe@example.com", "Mail j.doe@example.com now.", "j.doe@example.com")]
        [InlineData("Acme (US)", "Client Acme (US) signed.", "Acme (US)")]
        public void TermWithSpecialCharacters_IsMatchedLiterally(string term, string text, string sensitive) =>
            Assert.DoesNotContain(sensitive, Filter(new[] { term }, text));

        [Fact]
        public void TermWithSpecialCharacters_DoesNotMatchAsAPattern()
        {
            const string text = "Value axb stays.";
            Assert.Equal(text, Filter(new[] { "a.b" }, text)); // "." is not "any character"
        }

        [Fact]
        public void Wildcard_MatchesWordVariants_AsInAlwaysRedact()
        {
            string result = Filter(new[] { "bluebird*" }, "Bluebirds and bluebird-ish things.");
            Assert.DoesNotContain("Bluebirds", result);
        }

        [Theory]
        [InlineData("*")]
        [InlineData("**")]
        public void TermWithNoLiteralText_IsIgnored_RatherThanMatchingEverything(string term)
        {
            const string text = "Everything here should stay.";
            Assert.Equal(text, Filter(new[] { term }, text));
            Assert.Null(FindAndRedact.BuildPolicy(new[] { term }).Identifiers.CustomIdentifiers);
        }

        [Fact]
        public void BuildPolicy_UsesOneCustomIdentifier_NotTheDeprecatedDictionaryKey()
        {
            var policy = FindAndRedact.BuildPolicy(new[] { "Bluebird", "Falcon" });

            var identifier = Assert.Single(policy.Identifiers.CustomIdentifiers!);
            Assert.Equal(FindAndRedact.Classification, identifier.Classification);
            Assert.False(identifier.CaseSensitive);
            Assert.True(identifier.Enabled);
            Assert.True(policy.Identifiers.CustomDictionaries is null or { Count: 0 });
            Assert.DoesNotContain("\"dictionary\"", PolicySerializer.SerializeToJson(policy));
        }

        [Fact]
        public void BuildPolicy_IsValidAgainstThePolicySchema()
        {
            string json = PolicySerializer.SerializeToJson(FindAndRedact.BuildPolicy(new[] { "Bluebird" }));
            PolicyValidationResult result = PolicyValidator.Validate(json);
            Assert.True(result.IsValid, string.Join(" | ", result.Errors));
        }

        // End to end through the same call the Find & Redact window makes.
        [Fact]
        public async Task RedactFile_RemovesTermsOnSeparateLines_AndKeepsTheLineBreaks()
        {
            string dir = Path.Combine(Path.GetTempPath(), "far-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string input = Path.Combine(dir, "notes.txt");
                string output = Path.Combine(dir, "notes-out.txt");
                File.WriteAllText(input, "Status report\nBluebird is on track.\nThanks,\nFalcon\n");

                var spans = await RedactionService.RedactFileAsync(input, output, FindAndRedact.BuildPolicy(new[] { "Bluebird", "Falcon" }), string.Empty);

                string result = File.ReadAllText(output);
                Assert.DoesNotContain("Bluebird", result);
                Assert.DoesNotContain("Falcon", result);
                Assert.StartsWith("Status report\n", result);
                Assert.Equal(4, result.Count(c => c == '\n'));
                Assert.Equal(2, spans.Count);
            }
            finally
            {
                try { Directory.Delete(dir, true); } catch { /* best effort */ }
            }
        }
    }
}
