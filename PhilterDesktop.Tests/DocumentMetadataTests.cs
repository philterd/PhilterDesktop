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

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Packaging;
using Phileas.Policy;
using Phileas.Services;
using PhilterData;
using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    public sealed class DocumentMetadataTests : IDisposable
    {
        private readonly string _dir;

        public DocumentMetadataTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-meta-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

#pragma warning disable OOXML0001
        private static void StampMetadata(string path)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(path, isEditable: true);
            doc.PackageProperties.Creator = "Jane Author";
            doc.PackageProperties.Title = "Confidential Title";
            doc.PackageProperties.LastModifiedBy = "Editor Bob";
            doc.PackageProperties.Keywords = "secret, internal";

            ExtendedFilePropertiesPart ext = doc.AddNewPart<ExtendedFilePropertiesPart>();
            ext.Properties = new DocumentFormat.OpenXml.ExtendedProperties.Properties();
            ext.Properties.Company = new Company("Acme Corp");
            ext.Properties.Manager = new Manager("Big Boss");
            ext.Properties.Save();

            CustomFilePropertiesPart custom = doc.AddNewPart<CustomFilePropertiesPart>();
            custom.Properties = new DocumentFormat.OpenXml.CustomProperties.Properties();
            custom.Properties.Save();
        }

        private static (string Creator, string Title, string LastModifiedBy, bool HasCompany, bool HasCustomPart) ReadMetadata(string path)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(path, isEditable: false);
            return (
                doc.PackageProperties.Creator ?? string.Empty,
                doc.PackageProperties.Title ?? string.Empty,
                doc.PackageProperties.LastModifiedBy ?? string.Empty,
                doc.ExtendedFilePropertiesPart?.Properties?.Company is not null,
                doc.CustomFilePropertiesPart is not null);
        }
#pragma warning restore OOXML0001

        [Fact]
        public void ScrubDocx_ClearsCoreExtendedAndCustomProperties_PreservesBody()
        {
            string path = Path.Combine(_dir, "doc.docx");
            WordDocs.Create(path, "Important body content stays.");
            StampMetadata(path);

            DocumentMetadata.ScrubDocx(path);

            var meta = ReadMetadata(path);
            Assert.Equal(string.Empty, meta.Creator);
            Assert.Equal(string.Empty, meta.Title);
            Assert.Equal(string.Empty, meta.LastModifiedBy);
            Assert.False(meta.HasCompany);     // extended Company removed
            Assert.False(meta.HasCustomPart);  // custom properties part removed

            // The visible document content must be untouched.
            Assert.Contains("Important body content stays.", WordDocs.AllBodyText(path));
        }

        [Fact]
        public async Task RedactFileAsync_ScrubMetadataTrue_RemovesAuthor()
        {
            string input = Path.Combine(_dir, "in.docx");
            string output = Path.Combine(_dir, "out.docx");
            WordDocs.Create(input, "Email alice@example.com here.");
            StampMetadata(input);

            Policy policy = PolicySerializer.DeserializeFromJson("{\"identifiers\":{\"emailAddress\":{}}}");
            await RedactionService.RedactFileAsync(input, output, policy, "ctx", new FilterService(), wordScrub: WordScrubOptions.Metadata);

            var meta = ReadMetadata(output);
            Assert.Equal(string.Empty, meta.Creator);
            Assert.False(meta.HasCompany);
            Assert.DoesNotContain("alice@example.com", WordDocs.AllBodyText(output)); // redaction still happened
        }

        // Builds a .docx containing a tracked insertion + deletion, a comment, and a hidden-text run.
        // Authored from raw XML so the real w:ins/w:del run containers exist exactly as Word writes them.
        private const string W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

        private static void CreateRichDocx(string path)
        {
            string documentXml =
                $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                $"<w:document xmlns:w=\"{W}\"><w:body>" +
                "<w:p>" +
                "<w:r><w:t xml:space=\"preserve\">Visible start. </w:t></w:r>" +
                "<w:ins w:id=\"2\" w:author=\"Alice\"><w:r><w:t xml:space=\"preserve\">inserted-text </w:t></w:r></w:ins>" +
                "<w:del w:id=\"3\" w:author=\"Alice\"><w:r><w:delText xml:space=\"preserve\">deleted-text </w:delText></w:r></w:del>" +
                "<w:r><w:t>visible end.</w:t></w:r>" +
                "</w:p>" +
                "<w:p>" +
                "<w:commentRangeStart w:id=\"1\"/>" +
                "<w:r><w:t>commented text</w:t></w:r>" +
                "<w:commentRangeEnd w:id=\"1\"/>" +
                "<w:r><w:commentReference w:id=\"1\"/></w:r>" +
                "</w:p>" +
                "<w:p>" +
                "<w:r><w:t xml:space=\"preserve\">before </w:t></w:r>" +
                "<w:r><w:rPr><w:vanish/></w:rPr><w:t>HIDDEN-SECRET</w:t></w:r>" +
                "<w:r><w:t xml:space=\"preserve\"> after</w:t></w:r>" +
                "</w:p>" +
                "</w:body></w:document>";

            string commentsXml =
                $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                $"<w:comments xmlns:w=\"{W}\">" +
                "<w:comment w:id=\"1\" w:author=\"Reviewer\" w:initials=\"R\">" +
                "<w:p><w:r><w:t>a reviewer comment</w:t></w:r></w:p>" +
                "</w:comment></w:comments>";

            using WordprocessingDocument doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            MainDocumentPart main = doc.AddMainDocumentPart();
            WritePart(main, documentXml);
            WordprocessingCommentsPart commentsPart = main.AddNewPart<WordprocessingCommentsPart>();
            WritePart(commentsPart, commentsXml);
        }

        private static void WritePart(OpenXmlPart part, string xml)
        {
            using Stream stream = part.GetStream(FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false));
            writer.Write(xml);
        }

        // True if any descendant of the document body has the given WordprocessingML local name.
        private static bool AnyW(string path, string local)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(path, isEditable: false);
            OpenXmlElement? root = doc.MainDocumentPart?.Document;
            return root is not null && root.Descendants().Any(e => e.LocalName == local && e.NamespaceUri == W);
        }

        [Fact]
        public void ScrubDocx_Comments_RemovesCommentsAndMarkup_KeepsOtherVectors()
        {
            string path = Path.Combine(_dir, "comments.docx");
            CreateRichDocx(path);

            DocumentMetadata.ScrubDocx(path, WordScrubOptions.Comments);

            using (var doc = WordprocessingDocument.Open(path, false))
            {
                Assert.Null(doc.MainDocumentPart!.WordprocessingCommentsPart);
            }
            Assert.False(AnyW(path, "commentRangeStart"));
            Assert.False(AnyW(path, "commentReference"));
            // Comments-only must not touch tracked changes or hidden text.
            Assert.True(AnyW(path, "ins"));
            Assert.True(AnyW(path, "vanish"));
            Assert.Contains("commented text", WordDocs.AllBodyText(path)); // the commented words remain
        }

        [Fact]
        public void ScrubDocx_TrackedChanges_AcceptsInsertions_DropsDeletions()
        {
            string path = Path.Combine(_dir, "revisions.docx");
            CreateRichDocx(path);

            DocumentMetadata.ScrubDocx(path, WordScrubOptions.TrackedChanges);

            Assert.False(AnyW(path, "ins"));
            Assert.False(AnyW(path, "del"));
            string text = WordDocs.AllBodyText(path);
            Assert.Contains("inserted-text", text);     // insertion accepted (kept)
            Assert.Contains("visible end.", text);
            Assert.DoesNotContain("deleted-text", text); // deletion accepted (removed)
        }

        [Fact]
        public void ScrubDocx_HiddenText_RemovesHiddenRun_KeepsVisible()
        {
            string path = Path.Combine(_dir, "hidden.docx");
            CreateRichDocx(path);

            DocumentMetadata.ScrubDocx(path, WordScrubOptions.HiddenText);

            Assert.False(AnyW(path, "vanish"));
            string text = WordDocs.AllBodyText(path);
            Assert.DoesNotContain("HIDDEN-SECRET", text);
            Assert.Contains("before", text);
            Assert.Contains("after", text);
        }

        [Fact]
        public async Task RedactFileAsync_WithAllWordScrub_RemovesEveryVector_ThroughThePipeline()
        {
            // End-to-end: a rich document goes through the real redaction service with every Word scrub
            // option on, and the written output has comments, tracked changes, and hidden text gone.
            string input = Path.Combine(_dir, "rich-in.docx");
            string output = Path.Combine(_dir, "rich-out.docx");
            CreateRichDocx(input);
            StampMetadata(input);

            WordScrubOptions all = WordScrubOptions.Metadata | WordScrubOptions.Comments
                | WordScrubOptions.TrackedChanges | WordScrubOptions.HiddenText;
            await RedactionService.RedactFileAsync(
                input, output, PolicySerializer.DeserializeFromJson("{}"), "ctx", new FilterService(), wordScrub: all);

            using (var doc = WordprocessingDocument.Open(output, false))
            {
                Assert.Null(doc.MainDocumentPart!.WordprocessingCommentsPart);
            }
            Assert.False(AnyW(output, "commentReference"));
            Assert.False(AnyW(output, "ins"));
            Assert.False(AnyW(output, "del"));
            Assert.False(AnyW(output, "vanish"));

            var meta = ReadMetadata(output);
            Assert.Equal(string.Empty, meta.Creator);

            string text = WordDocs.AllBodyText(output);
            Assert.Contains("inserted-text", text);
            Assert.DoesNotContain("deleted-text", text);
            Assert.DoesNotContain("HIDDEN-SECRET", text);
        }

        [Fact]
        public void OptionsFor_MapsSettingsToFlags()
        {
            var all = new SettingsEntity
            {
                ScrubDocumentMetadata = true,
                ScrubWordComments = true,
                ScrubWordTrackedChanges = true,
                ScrubWordHiddenText = true
            };
            Assert.Equal(
                WordScrubOptions.Metadata | WordScrubOptions.Comments | WordScrubOptions.TrackedChanges | WordScrubOptions.HiddenText,
                DocumentMetadata.OptionsFor(all));

            var none = new SettingsEntity
            {
                ScrubDocumentMetadata = false,
                ScrubWordComments = false,
                ScrubWordTrackedChanges = false,
                ScrubWordHiddenText = false
            };
            Assert.Equal(WordScrubOptions.None, DocumentMetadata.OptionsFor(none));

            var commentsOnly = new SettingsEntity
            {
                ScrubDocumentMetadata = false,
                ScrubWordComments = true,
                ScrubWordTrackedChanges = false,
                ScrubWordHiddenText = false
            };
            Assert.Equal(WordScrubOptions.Comments, DocumentMetadata.OptionsFor(commentsOnly));
        }

        [Fact]
        public async Task RedactFileAsync_ScrubMetadataFalse_KeepsAuthor()
        {
            string input = Path.Combine(_dir, "in2.docx");
            string output = Path.Combine(_dir, "out2.docx");
            WordDocs.Create(input, "Plain content.");
            StampMetadata(input);

            Policy policy = PolicySerializer.DeserializeFromJson("{}");
            await RedactionService.RedactFileAsync(input, output, policy, "ctx", new FilterService(), wordScrub: WordScrubOptions.None);

            var meta = ReadMetadata(output);
            Assert.Equal("Jane Author", meta.Creator); // metadata preserved when scrubbing is off
        }
    }
}
