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

using Phileas.Model;
using Phileas.Services;
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Comments were only ever <em>deleted</em> (when "remove comments" was on) and never filtered, so
    /// a kept comment shipped its PII (philterd-website issue #480). These pin that comment text is now
    /// redacted like any other text, while the comment itself is preserved when not being deleted.
    /// </summary>
    public sealed class WordCommentRedactionTests : IDisposable
    {
        private static readonly PhileasPolicy EmailPolicy = new()
        {
            Name = "email",
            Identifiers = new Phileas.Policy.Identifiers { EmailAddress = new Phileas.Policy.Filters.EmailAddress() }
        };

        private static Func<string, TextFilterResult> Filter =>
            t => new FilterService().Filter(EmailPolicy, "ctx", 0, t);

        private readonly string _dir;

        public WordCommentRedactionTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-comment-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private string NewPath(string name) => Path.Combine(_dir, name);

        // --- Direct redactor behavior --------------------------------------------------------------

        [Fact]
        public void Redact_RedactsCommentText_AndKeepsTheComment()
        {
            string input = NewPath("in.docx");
            string output = NewPath("out.docx");
            WordDocs.CreateWithComment(input, "ping reviewer@example.com about this", "body text");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.True(WordDocs.HasComments(output), "the comment must be preserved (not deleted)");
            Assert.DoesNotContain("@example.com", WordDocs.DocumentXml(output));
            Assert.Contains(WordDocs.CommentTexts(output), c => c.Contains("REDACTED"));
        }

        [Fact]
        public void Redact_CommentWithoutPii_IsUnchanged()
        {
            string input = NewPath("in.docx");
            string output = NewPath("out.docx");
            WordDocs.CreateWithComment(input, "looks good to me", "body text");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.Equal(new[] { "looks good to me" }, WordDocs.CommentTexts(output));
        }

        [Fact]
        public void Detect_IncludesCommentText()
        {
            string input = NewPath("in.docx");
            WordDocs.CreateWithComment(input, "see reviewer@example.com", "body has body@example.com");

            List<RedactionSpanEntity> spans = WordDocumentRedactor.Detect(input, Filter);

            Assert.Contains(spans, s => s.Text == "reviewer@example.com");
            Assert.Contains(spans, s => s.Text == "body@example.com");
        }

        [Fact]
        public void ReadParagraphs_IncludesCommentText()
        {
            string input = NewPath("in.docx");
            WordDocs.CreateWithComment(input, "comment line", "body line");

            List<string> paragraphs = WordDocumentRedactor.ReadParagraphs(input);

            Assert.Contains("body line", paragraphs);
            Assert.Contains("comment line", paragraphs);
        }

        [Fact]
        public void Redact_KeepsBodyParagraphIndexesStable_CommentsAppendedLast()
        {
            // The comment is enumerated after the body, so the body e-mail keeps paragraph index 0.
            string input = NewPath("in.docx");
            WordDocs.CreateWithComment(input, "note note@example.com", "body body@example.com");

            List<RedactionSpanEntity> spans = WordDocumentRedactor.Detect(input, Filter);

            RedactionSpanEntity bodySpan = Assert.Single(spans, s => s.Text == "body@example.com");
            RedactionSpanEntity commentSpan = Assert.Single(spans, s => s.Text == "note@example.com");
            Assert.Equal(0, bodySpan.ParagraphIndex);
            Assert.True(commentSpan.ParagraphIndex > bodySpan.ParagraphIndex);
        }

        [Fact]
        public void ApplySpans_RedactsCommentText()
        {
            string input = NewPath("in.docx");
            string output = NewPath("out.docx");
            WordDocs.CreateWithComment(input, "call reviewer@example.com", "body text");

            List<RedactionSpanEntity> spans = WordDocumentRedactor.Detect(input, Filter);
            WordDocumentRedactor.ApplySpans(input, output, spans, highlight: false);

            Assert.True(WordDocs.HasComments(output));
            Assert.DoesNotContain("@example.com", WordDocs.DocumentXml(output));
        }

        // --- Integration through RedactionService --------------------------------------------------

        [Fact]
        public async Task RedactFileAsync_RemoveCommentsOff_RedactsCommentAndKeepsIt()
        {
            string input = NewPath("svc_keep_in.docx");
            string output = NewPath("svc_keep_out.docx");
            WordDocs.CreateWithComment(input, "ping reviewer@example.com", "body plain");

            var settings = new SettingsEntity { ScrubWordComments = false, ScrubDocumentMetadata = false };
            await RedactionService.RedactFileAsync(input, output, EmailPolicy, "ctx", settings, new FilterService());

            Assert.True(WordDocs.HasComments(output), "comment must be kept when removal is off");
            Assert.DoesNotContain("@example.com", WordDocs.DocumentXml(output));
        }

        [Fact]
        public async Task RedactFileAsync_RemoveCommentsOn_DeletesComment_NoPii()
        {
            string input = NewPath("svc_del_in.docx");
            string output = NewPath("svc_del_out.docx");
            WordDocs.CreateWithComment(input, "ping reviewer@example.com", "body plain");

            var settings = new SettingsEntity { ScrubWordComments = true };
            await RedactionService.RedactFileAsync(input, output, EmailPolicy, "ctx", settings, new FilterService());

            Assert.False(WordDocs.HasComments(output), "comment must be removed when removal is on");
            Assert.DoesNotContain("@example.com", WordDocs.DocumentXml(output));
        }
    }
}
