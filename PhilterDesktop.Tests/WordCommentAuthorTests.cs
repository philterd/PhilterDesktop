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
using Phileas.Model;
using Phileas.Services;
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// A kept comment still carried the reviewer's identity (the <c>w:author</c>/<c>w:initials</c>
    /// attributes and word/people.xml display names), which only the delete path removed
    ///. These pin that the "remove document metadata" scrub anonymizes
    /// comment authors with consistent pseudonyms and drops the people part, while comment text
    /// redaction is unaffected.
    /// </summary>
    public sealed class WordCommentAuthorTests : IDisposable
    {
        private static readonly PhileasPolicy EmailPolicy = new()
        {
            Name = "email",
            Identifiers = new Phileas.Policy.Identifiers { EmailAddress = new Phileas.Policy.Filters.EmailAddress() }
        };

        private static Func<string, TextFilterResult> Filter =>
            t => new FilterService().Filter(EmailPolicy, "ctx", 0, t);

        private readonly string _dir;

        public WordCommentAuthorTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-cauthor-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private string NewPath(string name) => Path.Combine(_dir, name);

        // Mirrors WordScrubOptions.Metadata via the public ScrubDocx overload.
        private static void ScrubMetadata(string path) =>
            DocumentMetadata.ScrubDocx(path, WordScrubOptions.Metadata);

        // --- Direct scrub behavior -----------------------------------------------------------------

        [Fact]
        public void ScrubMetadata_AnonymizesCommentAuthor_NoNameLeaksAnywhere()
        {
            string path = NewPath("a.docx");
            WordDocs.CreateWithAuthoredComments(path, new[] { ("John Smith", "looks good") }, "body");

            Assert.True(WordDocs.AnyPartContains(path, "John Smith")); // sanity: fixture has the name

            ScrubMetadata(path);

            Assert.DoesNotContain("John Smith", WordDocs.CommentAuthors(path));
            Assert.False(WordDocs.AnyPartContains(path, "John Smith"), "the author name must not survive in any part");
        }

        [Fact]
        public void ScrubMetadata_RemovesPeoplePart()
        {
            string path = NewPath("b.docx");
            WordDocs.CreateWithAuthoredComments(path, new[] { ("Jane Doe", "note") }, "body");
            Assert.True(WordDocs.HasPeoplePart(path));

            ScrubMetadata(path);

            Assert.False(WordDocs.HasPeoplePart(path), "people.xml (reviewer identities) must be removed");
        }

        [Fact]
        public void ScrubMetadata_UsesConsistentPseudonyms()
        {
            string path = NewPath("c.docx");
            WordDocs.CreateWithAuthoredComments(path, new[]
            {
                ("Alice Anderson", "first"),
                ("Bob Brown", "second"),
                ("Alice Anderson", "third")   // same author as the first
            }, "body");

            ScrubMetadata(path);

            string[] authors = WordDocs.CommentAuthors(path);
            Assert.Equal(3, authors.Length);
            Assert.Equal(authors[0], authors[2]);            // same original author -> same pseudonym
            Assert.NotEqual(authors[0], authors[1]);          // different authors -> different pseudonyms
            Assert.All(authors, a => Assert.StartsWith("Reviewer ", a));
            Assert.False(WordDocs.AnyPartContains(path, "Alice Anderson"));
            Assert.False(WordDocs.AnyPartContains(path, "Bob Brown"));
        }

        [Fact]
        public void ScrubMetadata_AnonymizesInitials()
        {
            string path = NewPath("d.docx");
            WordDocs.CreateWithAuthoredComments(path, new[] { ("Quentin Xavier", "hi") }, "body");

            ScrubMetadata(path);

            Assert.DoesNotContain("Q", WordDocs.CommentInitials(path)); // original initial gone
            Assert.All(WordDocs.CommentInitials(path), i => Assert.StartsWith("R", i));
        }

        [Fact]
        public void ScrubMetadata_PreservesCommentText()
        {
            string path = NewPath("e.docx");
            WordDocs.CreateWithAuthoredComments(path, new[] { ("John Smith", "keep this text") }, "body");

            ScrubMetadata(path);

            Assert.Contains(WordDocs.CommentTexts(path), c => c.Contains("keep this text"));
        }

        [Fact]
        public void ScrubWithoutMetadataFlag_LeavesAuthorsUnchanged()
        {
            // Author anonymization is gated on the metadata option; with it off, authors are untouched.
            string path = NewPath("f.docx");
            WordDocs.CreateWithAuthoredComments(path, new[] { ("John Smith", "note") }, "body");

            DocumentMetadata.ScrubDocx(path, WordScrubOptions.HiddenText); // some other flag, not Metadata

            Assert.Contains("John Smith", WordDocs.CommentAuthors(path));
        }

        // --- Integration through RedactionService --------------------------------------------------

        [Fact]
        public async Task RedactFileAsync_KeepComments_MetadataOn_RedactsTextAndAnonymizesAuthor()
        {
            string input = NewPath("svc_in.docx");
            string output = NewPath("svc_out.docx");
            WordDocs.CreateWithAuthoredComments(input, new[] { ("John Smith", "ping me@example.com") }, "body");

            var settings = new SettingsEntity { ScrubWordComments = false, ScrubDocumentMetadata = true };
            await RedactionService.RedactFileAsync(input, output, EmailPolicy, "ctx", settings, new FilterService());

            Assert.True(WordDocs.HasComments(output), "comment kept when removal is off");
            Assert.False(WordDocs.AnyPartContains(output, "John Smith"), "author name must be gone");
            Assert.False(WordDocs.AnyPartContains(output, "@example.com"), "comment text PII must be redacted");
            Assert.False(WordDocs.HasPeoplePart(output));
        }

        [Fact]
        public async Task RedactFileAsync_RemoveComments_DeletesCommentsAndAuthors()
        {
            string input = NewPath("svc_del_in.docx");
            string output = NewPath("svc_del_out.docx");
            WordDocs.CreateWithAuthoredComments(input, new[] { ("John Smith", "ping me@example.com") }, "body");

            var settings = new SettingsEntity { ScrubWordComments = true, ScrubDocumentMetadata = true };
            await RedactionService.RedactFileAsync(input, output, EmailPolicy, "ctx", settings, new FilterService());

            Assert.False(WordDocs.HasComments(output));
            Assert.False(WordDocs.HasPeoplePart(output));
            Assert.False(WordDocs.AnyPartContains(output, "John Smith"));
            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
        }

        [Fact]
        public async Task RedactFileAsync_KeepComments_MetadataOff_AuthorRemains_ByDesign()
        {
            // Documents the gating: opting out of metadata removal keeps authorship (consistent with how
            // document-level author/last-modified-by are handled).
            string input = NewPath("svc_off_in.docx");
            string output = NewPath("svc_off_out.docx");
            WordDocs.CreateWithAuthoredComments(input, new[] { ("John Smith", "ping me@example.com") }, "body");

            var settings = new SettingsEntity { ScrubWordComments = false, ScrubDocumentMetadata = false };
            await RedactionService.RedactFileAsync(input, output, EmailPolicy, "ctx", settings, new FilterService());

            Assert.Contains("John Smith", WordDocs.CommentAuthors(output)); // author kept (metadata off)
            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));   // but text PII still redacted
        }

        [Fact]
        public void ScrubMetadata_DocumentStaysValid()
        {
            string path = NewPath("valid.docx");
            WordDocs.CreateWithAuthoredComments(path, new[] { ("John Smith", "note") }, "body");

            ScrubMetadata(path);

            using WordprocessingDocument doc = WordprocessingDocument.Open(path, false);
            Assert.NotNull(doc.MainDocumentPart?.Document?.Body);
        }
    }
}
