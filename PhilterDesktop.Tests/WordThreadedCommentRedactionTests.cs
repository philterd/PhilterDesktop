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
    /// Word 2019/365 mirrors comment text into word/threadedComments.xml, which neither the redaction
    /// nor the delete path touched — so that duplicate could ship a comment's PII (philterd-website
    /// issue #507). The fix drops the threaded duplicate during redaction, leaving the redacted classic
    /// comments. These pin that the duplicate is gone and no copy of the PII survives.
    /// </summary>
    public sealed class WordThreadedCommentRedactionTests : IDisposable
    {
        private static readonly PhileasPolicy EmailPolicy = new()
        {
            Name = "email",
            Identifiers = new Phileas.Policy.Identifiers { EmailAddress = new Phileas.Policy.Filters.EmailAddress() }
        };

        private static Func<string, TextFilterResult> Filter =>
            t => new FilterService().Filter(EmailPolicy, "ctx", 0, t);

        // Catches two unrelated PII types so the tests don't lean on a single detector.
        private static readonly PhileasPolicy EmailSsnPolicy = new()
        {
            Name = "es",
            Identifiers = new Phileas.Policy.Identifiers
            {
                EmailAddress = new Phileas.Policy.Filters.EmailAddress(),
                Ssn = new Phileas.Policy.Filters.Ssn()
            }
        };

        private static Func<string, TextFilterResult> EsFilter =>
            t => new FilterService().Filter(EmailSsnPolicy, "ctx", 0, t);

        private readonly string _dir;

        public WordThreadedCommentRedactionTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-threaded-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private string NewPath(string name) => Path.Combine(_dir, name);

        [Fact]
        public void Redact_RemovesThreadedDuplicate_AndLeavesNoPii()
        {
            string input = NewPath("in.docx");
            string output = NewPath("out.docx");
            WordDocs.CreateWithThreadedComment(input, "threaded threaded@example.com", "classic classic@example.com", "body plain");

            // Sanity: the fixture really has the threaded duplicate and the PII before redaction.
            Assert.True(WordDocs.HasThreadedComments(input));
            Assert.True(WordDocs.AnyPartContains(input, "threaded@example.com"));

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.False(WordDocs.HasThreadedComments(output), "the threaded-comments duplicate must be removed");
            Assert.False(WordDocs.AnyPartContains(output, "@example.com"), "no copy of the PII may survive in any part");
            Assert.True(CanOpen(output));
        }

        [Fact]
        public void Redact_KeepsRedactedClassicComment()
        {
            string input = NewPath("in.docx");
            string output = NewPath("out.docx");
            WordDocs.CreateWithThreadedComment(input, "threaded@example.com", "ping classic@example.com", "body");

            WordDocumentRedactor.Redact(input, output, Filter);

            // The canonical comment is preserved and redacted; the threaded copy is gone.
            Assert.True(WordDocs.HasComments(output));
            Assert.Contains(WordDocs.CommentTexts(output), c => c.Contains("REDACTED"));
            Assert.False(WordDocs.HasThreadedComments(output));
        }

        [Fact]
        public void ApplySpans_RemovesThreadedDuplicate()
        {
            string input = NewPath("in.docx");
            string output = NewPath("out.docx");
            WordDocs.CreateWithThreadedComment(input, "threaded@example.com", "classic@example.com", "body");

            List<RedactionSpanEntity> spans = WordDocumentRedactor.Detect(input, Filter);
            WordDocumentRedactor.ApplySpans(input, output, spans, highlight: false);

            Assert.False(WordDocs.HasThreadedComments(output));
            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
        }

        [Fact]
        public void Redact_DocumentWithoutThreadedComments_IsUnaffected()
        {
            // A classic-only document (no threaded part) must redact normally and keep its comment.
            string input = NewPath("in.docx");
            string output = NewPath("out.docx");
            WordDocs.CreateWithComment(input, "ping classic@example.com", "body");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.True(WordDocs.HasComments(output));
            Assert.False(WordDocs.HasThreadedComments(output));
            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
        }

        [Fact]
        public async Task RedactFileAsync_RemoveCommentsOff_RemovesThreadedDuplicate_KeepsRedactedComment()
        {
            string input = NewPath("svc_keep_in.docx");
            string output = NewPath("svc_keep_out.docx");
            WordDocs.CreateWithThreadedComment(input, "threaded@example.com", "classic@example.com", "body");

            var settings = new SettingsEntity { ScrubWordComments = false, ScrubDocumentMetadata = false };
            await RedactionService.RedactFileAsync(input, output, EmailPolicy, "ctx", settings, new FilterService());

            Assert.True(WordDocs.HasComments(output), "classic comment kept when removal is off");
            Assert.False(WordDocs.HasThreadedComments(output), "threaded duplicate removed");
            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
        }

        [Fact]
        public async Task RedactFileAsync_RemoveCommentsOn_RemovesEverything_NoPii()
        {
            string input = NewPath("svc_del_in.docx");
            string output = NewPath("svc_del_out.docx");
            WordDocs.CreateWithThreadedComment(input, "threaded@example.com", "classic@example.com", "body");

            var settings = new SettingsEntity { ScrubWordComments = true };
            await RedactionService.RedactFileAsync(input, output, EmailPolicy, "ctx", settings, new FilterService());

            Assert.False(WordDocs.HasComments(output), "classic comment deleted when removal is on");
            Assert.False(WordDocs.HasThreadedComments(output), "threaded duplicate removed");
            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
        }

        [Fact]
        public void Redact_MultiMessageThread_AllMessagesRedacted_NoPii()
        {
            string input = NewPath("thread_in.docx");
            string output = NewPath("thread_out.docx");
            string[] messages =
            {
                "root alice@example.com",
                "reply bob@example.com",
                "reply carol@example.com"
            };
            WordDocs.CreateWithThreadedThread(input, messages, "body plain");

            Assert.True(WordDocs.HasThreadedComments(input));
            foreach (string m in messages)
            {
                Assert.True(WordDocs.AnyPartContains(input, m.Split(' ')[1]), $"fixture should contain {m}");
            }

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.False(WordDocs.HasThreadedComments(output));
            Assert.False(WordDocs.HasCommentCompanionParts(output), "commentsExtended/commentsIds must be removed too");
            Assert.False(WordDocs.AnyPartContains(output, "@example.com"), "no reply's PII may survive in any part");
            // The three classic comments are kept and each redacted.
            Assert.Equal(3, WordDocs.CommentTexts(output).Length);
            Assert.All(WordDocs.CommentTexts(output), c => Assert.Contains("REDACTED", c));
        }

        [Fact]
        public void Redact_SsnInClassicAndThreaded_IsRemoved()
        {
            string input = NewPath("ssn_in.docx");
            string output = NewPath("ssn_out.docx");
            // Both are valid SSNs (areas 123 and 078) so the engine actually detects them.
            WordDocs.CreateWithThreadedComment(input, "threaded ssn 123-45-6789", "classic ssn 078-05-1120", "body");

            Assert.True(WordDocs.AnyPartContains(input, "123-45-6789"));
            Assert.True(WordDocs.AnyPartContains(input, "078-05-1120"));

            WordDocumentRedactor.Redact(input, output, EsFilter);

            Assert.False(WordDocs.AnyPartContains(output, "123-45-6789"), "threaded-copy SSN must not survive");
            Assert.False(WordDocs.AnyPartContains(output, "078-05-1120"), "classic-comment SSN must be redacted");
            Assert.False(WordDocs.HasThreadedComments(output));
        }

        [Fact]
        public void Redact_PiiOnlyInComment_BodyClean_IsRemoved()
        {
            // The body has no PII at all — the only PII is in the comments, so this proves redaction
            // doesn't depend on the body containing the same text.
            string input = NewPath("only_in.docx");
            string output = NewPath("only_out.docx");
            WordDocs.CreateWithThreadedComment(input, "threaded only@example.com", "classic only2@example.com", "nothing sensitive here");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
            Assert.False(WordDocs.HasThreadedComments(output));
        }

        [Fact]
        public void Redact_TwiceIsIdempotent_StillNoPii()
        {
            string input = NewPath("idem_in.docx");
            string once = NewPath("idem_once.docx");
            string twice = NewPath("idem_twice.docx");
            WordDocs.CreateWithThreadedComment(input, "threaded@example.com", "classic@example.com", "body");

            WordDocumentRedactor.Redact(input, once, Filter);
            WordDocumentRedactor.Redact(once, twice, Filter);

            Assert.False(WordDocs.AnyPartContains(twice, "@example.com"));
            Assert.False(WordDocs.HasThreadedComments(twice));
            Assert.True(CanOpen(twice));
        }

        [Fact]
        public async Task RedactFileAsync_MultiMessageThread_RemoveOff_NoPii()
        {
            string input = NewPath("svc_thread_in.docx");
            string output = NewPath("svc_thread_out.docx");
            WordDocs.CreateWithThreadedThread(input, new[] { "root r1@example.com", "reply r2@example.com" }, "body");

            var settings = new SettingsEntity { ScrubWordComments = false, ScrubDocumentMetadata = false };
            await RedactionService.RedactFileAsync(input, output, EmailPolicy, "ctx", settings, new FilterService());

            Assert.True(WordDocs.HasComments(output));
            Assert.False(WordDocs.HasThreadedComments(output));
            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
        }

        private static bool CanOpen(string path)
        {
            try
            {
                using WordprocessingDocument doc = WordprocessingDocument.Open(path, false);
                return doc.MainDocumentPart?.Document?.Body is not null;
            }
            catch
            {
                return false;
            }
        }
    }
}
