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

using System.IO.Compression;
using Phileas.Model;
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Tests Word (.docx) redaction via the Open XML SDK implementation. No license required, so
    /// these run unconditionally.
    /// </summary>
    public sealed class WordDocumentRedactorTests : IDisposable
    {
        private static readonly PhileasPolicy EmailPolicy = new()
        {
            Name = "email",
            Identifiers = new Identifiers { EmailAddress = new EmailAddress() }
        };

        // Real filter producing spans; emails redact to {{{REDACTED-email-address}}}.
        private static Func<string, TextFilterResult> Filter =>
            t => new FilterService().Filter(EmailPolicy, "ctx", 0, t);

        private readonly string _tempDir;

        public WordDocumentRedactorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "philter-word-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        }

        private string NewPath(string name) => Path.Combine(_tempDir, name);

        private string CreateDocx(params string[] paragraphs)
        {
            string path = NewPath("in_" + Guid.NewGuid().ToString("N") + ".docx");
            WordDocs.Create(path, paragraphs);
            return path;
        }

        // Regression test for the bug we hit: emptying a changed paragraph must not delete the
        // paragraph itself, so the rebuild is preserved and the paragraph count is unchanged.
        [Fact]
        public void Redact_ReplacesChangedParagraphs_WithoutDeletingThem()
        {
            string input = CreateDocx("keep one", "email aaa@example.com here", "email bbb@example.com too", "keep four");
            int originalCount = WordDocs.BodyParagraphs(input).Length;
            string output = NewPath("out.docx");

            WordDocumentRedactor.Redact(input, output, Filter);

            string[] result = WordDocs.BodyParagraphs(output);
            Assert.Equal(originalCount, result.Length); // no paragraph deleted
            Assert.Contains("keep one", result);
            Assert.Contains("keep four", result);
            Assert.Contains("email {{{REDACTED-email-address}}} here", result);
            Assert.DoesNotContain(result, p => p.Contains("@example.com"));
        }

        [Fact]
        public void Redact_UnchangedText_LeavesParagraphsIdentical()
        {
            string input = CreateDocx("alpha", "beta", "gamma"); // no PII
            string output = NewPath("out.docx");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.Equal(WordDocs.BodyParagraphs(input), WordDocs.BodyParagraphs(output));
        }

        [Fact]
        public void Redact_RedactsHeadersAndFooters()
        {
            string input = NewPath("hf.docx");
            WordDocs.CreateWithHeaderFooter(input, "header hh@example.com", "footer ff@example.com", "body bb@example.com");
            string output = NewPath("hf_out.docx");

            WordDocumentRedactor.Redact(input, output, Filter);

            string body = WordDocs.AllBodyText(output);
            string header = WordDocs.HeadersText(output);
            string footer = WordDocs.FootersText(output);

            Assert.DoesNotContain("@example.com", body + header + footer);
            Assert.Contains("REDACTED", header);
            Assert.Contains("REDACTED", footer);
            Assert.Contains("REDACTED", body);
        }

        [Fact]
        public void Redact_LeavesInputFileUnchanged()
        {
            string input = CreateDocx("contact data@example.com");
            string output = NewPath("out.docx");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.Contains(WordDocs.BodyParagraphs(input), p => p.Contains("@example.com"));
        }

        [Fact]
        public void Redact_WithHighlight_HighlightsTheReplacement()
        {
            string input = CreateDocx("email zzz@example.com end");
            string output = NewPath("out.docx");

            WordDocumentRedactor.Redact(input, output, Filter, highlight: true);

            Assert.Contains("email {{{REDACTED-email-address}}} end", WordDocs.BodyParagraphs(output));

            // The replacement run carries Word's native yellow highlight.
            string xml = ReadDocumentXml(output);
            Assert.Contains("highlight", xml);
            Assert.Contains("yellow", xml);
        }

        private static string ReadDocumentXml(string docxPath)
        {
            using ZipArchive zip = ZipFile.OpenRead(docxPath);
            ZipArchiveEntry entry = zip.GetEntry("word/document.xml")!;
            using var reader = new StreamReader(entry.Open());
            return reader.ReadToEnd();
        }
    }
}
