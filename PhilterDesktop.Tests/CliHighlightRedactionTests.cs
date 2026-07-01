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

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The CLI's <c>--highlight</c> flag flows into the same <see cref="RedactionService.RedactFileAsync"/>
    /// (SettingsEntity) overload the command line calls. These guard that overload so that highlight is
    /// applied to Word (.docx) output when requested and not otherwise (#539).
    /// </summary>
    public sealed class CliHighlightRedactionTests : IDisposable
    {
        private static readonly PhileasPolicy EmailPolicy = new()
        {
            Name = "email",
            Identifiers = new Identifiers { EmailAddress = new EmailAddress() }
        };

        private readonly string _dir;

        public CliHighlightRedactionTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-cli-hl-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        [Fact]
        public async Task RedactFileAsync_HighlightTrue_HighlightsDocxReplacements()
        {
            string input = Path.Combine(_dir, "in.docx");
            string output = Path.Combine(_dir, "hl.docx");
            WordDocs.Create(input, "Contact a@b.com for details.");

            await RedactionService.RedactFileAsync(input, output, EmailPolicy, "ctx",
                new SettingsEntity(), new FilterService(), highlight: true);

            Assert.False(WordDocs.AnyPartContains(output, "a@b.com")); // redacted
            Assert.True(HasHighlight(output), "replacements should be highlighted when --highlight is set");
        }

        [Fact]
        public async Task RedactFileAsync_HighlightFalse_LeavesDocxUnhighlighted()
        {
            string input = Path.Combine(_dir, "in.docx");
            string output = Path.Combine(_dir, "plain.docx");
            WordDocs.Create(input, "Contact a@b.com for details.");

            await RedactionService.RedactFileAsync(input, output, EmailPolicy, "ctx",
                new SettingsEntity(), new FilterService(), highlight: false);

            Assert.False(WordDocs.AnyPartContains(output, "a@b.com")); // still redacted
            Assert.False(HasHighlight(output), "no highlight without the flag (default)");
        }

        private static bool HasHighlight(string path)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(path, false);
            return doc.MainDocumentPart!.Document!.Body!.Descendants<Highlight>().Any();
        }
    }
}
