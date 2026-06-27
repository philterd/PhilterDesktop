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
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Tests for <see cref="EmailRedactor"/> and the email path through <see cref="RedactionService"/>:
    /// .eml round-trips, the Subject / address headers / body get redacted, the output is a valid email,
    /// and an edited span set re-applies (the Modify Redaction path).
    /// </summary>
    public sealed class EmailRedactorTests : IDisposable
    {
        private readonly string _tempDir;

        public EmailRedactorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "philter-email-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        }

        private const string SampleEml =
            "From: John Doe <john@example.com>\r\n" +
            "To: Jane Roe <jane@example.com>\r\n" +
            "Cc: Sam Poe <sam@example.com>\r\n" +
            "Subject: Reach me at secret@example.com\r\n" +
            "Date: Tue, 01 Apr 2025 10:00:00 +0000\r\n" +
            "Content-Type: text/plain; charset=utf-8\r\n" +
            "\r\n" +
            "Please email secret@example.com or call 555-123-4567. Thanks.\r\n";

        private string WriteEml(string name = "sample.eml")
        {
            string path = Path.Combine(_tempDir, name);
            File.WriteAllText(path, SampleEml);
            return path;
        }

        [Fact]
        public async Task RedactFileAsync_Eml_RedactsBodyAndSubject()
        {
            string input = WriteEml();
            string output = Path.Combine(_tempDir, "out.eml");

            await RedactionService.RedactFileAsync(input, output, EmailPolicy(), "ctx");

            string result = await File.ReadAllTextAsync(output);
            // Body PII gone.
            Assert.DoesNotContain("secret@example.com", result);
            Assert.DoesNotContain("555-123-4567", result);
            // Subject carried the email too — it must be gone there as well.
            // (The Subject line is the only place "Reach me at" appears.)
            string subjectLine = result.Split("\r\n", StringSplitOptions.None)
                .FirstOrDefault(l => l.StartsWith("Subject:", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
            Assert.DoesNotContain("secret@example.com", subjectLine);
            // Non-sensitive body text is preserved.
            Assert.Contains("Thanks.", result);
        }

        [Fact]
        public async Task RedactFileAsync_Eml_RedactsEmailsInAddressHeaders()
        {
            string input = WriteEml();
            string output = Path.Combine(_tempDir, "out.eml");

            await RedactionService.RedactFileAsync(input, output, EmailPolicy(), "ctx");

            string result = await File.ReadAllTextAsync(output);
            // The address-header email addresses are detected by the EmailAddress filter and removed.
            Assert.DoesNotContain("john@example.com", result);
            Assert.DoesNotContain("jane@example.com", result);
            Assert.DoesNotContain("sam@example.com", result);
        }

        [Fact]
        public async Task RedactFileAsync_Eml_OutputIsStillAParseableEmail()
        {
            string input = WriteEml();
            string output = Path.Combine(_tempDir, "out.eml");

            await RedactionService.RedactFileAsync(input, output, EmailPolicy(), "ctx");

            // The output round-trips through the email loader and keeps its structure.
            using var stream = File.OpenRead(output);
            MimeKit.MimeMessage message = MimeKit.MimeMessage.Load(stream);
            Assert.False(string.IsNullOrEmpty(message.Subject));
            Assert.NotNull(message.TextBody);
            Assert.Contains("Thanks.", message.TextBody);
        }

        [Fact]
        public async Task RedactFileAsync_Eml_DisabledFilters_LeaveMessageUnchanged()
        {
            string input = WriteEml();
            string output = Path.Combine(_tempDir, "out.eml");

            // Empty policy: nothing is detected, so nothing should be removed.
            await RedactionService.RedactFileAsync(input, output, new PhileasPolicy { Name = "noop" }, "ctx");

            string result = await File.ReadAllTextAsync(output);
            Assert.Contains("secret@example.com", result);
            Assert.Contains("555-123-4567", result);
        }

        [Fact]
        public void ApplySpans_Eml_ReappliesEditedSpanSet()
        {
            string input = WriteEml();
            string firstPass = Path.Combine(_tempDir, "first.eml");

            // Capture the spans the engine would apply.
            List<RedactionSpanEntity> spans = EmailRedactor.Redact(
                input, firstPass, text => new FilterServiceShim().Filter(text));

            Assert.NotEmpty(spans);

            // Re-apply that exact span set to the original (the Modify Redaction path).
            string reapplied = Path.Combine(_tempDir, "reapplied.eml");
            EmailRedactor.ApplySpans(input, reapplied, spans);

            string result = File.ReadAllText(reapplied);
            Assert.DoesNotContain("secret@example.com", result);
            Assert.DoesNotContain("555-123-4567", result);
        }

        [Fact]
        public async Task RedactFileAsync_Eml_RedactsHtmlBody()
        {
            string input = Path.Combine(_tempDir, "html.eml");
            var message = new MimeKit.MimeMessage();
            message.From.Add(new MimeKit.MailboxAddress("George Banks", "george@fake.com"));
            message.To.Add(new MimeKit.MailboxAddress("Mary Johnson", "mary@example.org"));
            message.Subject = "HTML message";
            message.Body = new MimeKit.BodyBuilder
            {
                HtmlBody = "<html><body><p>Reach me at " +
                           "<a href=\"mailto:secret@example.com\">secret@example.com</a> or call 555-123-4567.</p></body></html>"
            }.ToMessageBody();
            using (var fs = File.Create(input)) { message.WriteTo(fs); }

            string output = Path.Combine(_tempDir, "html-out.eml");
            await RedactionService.RedactFileAsync(input, output, EmailPolicy(), "ctx");

            // Assert on the decoded HTML body (the raw .eml may be quoted-printable encoded).
            using var stream = File.OpenRead(output);
            MimeKit.MimeMessage loaded = MimeKit.MimeMessage.Load(stream);
            string html = loaded.HtmlBody ?? string.Empty;
            Assert.DoesNotContain("secret@example.com", html);
            Assert.DoesNotContain("555-123-4567", html);
            // The HTML markup is preserved (only the detected text inside it was replaced).
            Assert.Contains("<p", html);
        }

        [Fact]
        public async Task RedactFileAsync_Eml_LeavesAttachmentsUntouched()
        {
            string input = Path.Combine(_tempDir, "withatt.eml");
            var message = new MimeKit.MimeMessage();
            message.From.Add(new MimeKit.MailboxAddress("George Banks", "george@fake.com"));
            message.To.Add(new MimeKit.MailboxAddress("Mary Johnson", "mary@example.org"));
            message.Subject = "See attachment";
            var builder = new MimeKit.BodyBuilder { TextBody = "Body has secret@example.com in it." };
            builder.Attachments.Add("notes.txt",
                System.Text.Encoding.UTF8.GetBytes("Attachment SSN 123-45-6789 must survive unredacted."));
            message.Body = builder.ToMessageBody();
            using (var fs = File.Create(input)) { message.WriteTo(fs); }

            // A policy that *would* redact the SSN if attachments were inspected.
            var policy = new PhileasPolicy
            {
                Name = "att",
                Identifiers = new Identifiers { EmailAddress = new EmailAddress(), Ssn = new Ssn() }
            };

            string output = Path.Combine(_tempDir, "withatt-out.eml");
            await RedactionService.RedactFileAsync(input, output, policy, "ctx");

            using var stream = File.OpenRead(output);
            MimeKit.MimeMessage loaded = MimeKit.MimeMessage.Load(stream);

            // The message body was redacted...
            Assert.DoesNotContain("secret@example.com", loaded.TextBody ?? string.Empty);

            // ...but the attachment is preserved and its contents are left exactly as they were.
            MimeKit.MimePart attachment = loaded.Attachments.OfType<MimeKit.MimePart>().Single();
            Assert.Equal("notes.txt", attachment.FileName);
            Assert.NotNull(attachment.Content);
            using var ms = new MemoryStream();
            attachment.Content!.DecodeTo(ms);
            string attachmentText = System.Text.Encoding.UTF8.GetString(ms.ToArray());
            Assert.Contains("123-45-6789", attachmentText);
        }

        [Fact]
        public async Task RedactFileAsync_Msg_ReadsOutlookFormatAndWritesRedactedEml()
        {
            string input = WriteMsg("sample.msg");
            // The pipeline names .msg output as .eml; mirror that here.
            string output = Path.Combine(_tempDir, "out.eml");

            await RedactionService.RedactFileAsync(input, output, EmailPolicy(), "ctx");

            Assert.True(File.Exists(output));
            string result = await File.ReadAllTextAsync(output);
            Assert.DoesNotContain("secret@example.com", result);
            Assert.DoesNotContain("555-123-4567", result);
            Assert.Contains("Thanks.", result);

            // It is a real, parseable .eml.
            using var stream = File.OpenRead(output);
            MimeKit.MimeMessage message = MimeKit.MimeMessage.Load(stream);
            Assert.Contains("Thanks.", message.TextBody ?? string.Empty);
        }

        // Builds a minimal real Outlook .msg fixture with MsgKit (test-only dependency).
        private string WriteMsg(string name)
        {
            string path = Path.Combine(_tempDir, name);
            using var email = new MsgKit.Email(
                new MsgKit.Sender("john@example.com", "John Doe"),
                "Reach me at secret@example.com")
            {
                BodyText = "Please email secret@example.com or call 555-123-4567. Thanks."
            };
            email.Recipients.AddTo("jane@example.com", "Jane Roe");
            email.Recipients.AddCc("sam@example.com", "Sam Poe");
            email.Save(path);
            return path;
        }

        [Fact]
        public void ReadFields_ReturnsLabeledFields_WithBodyFlag()
        {
            string input = WriteEml();
            List<(string Label, string Text, bool IsBody)> fields = EmailRedactor.ReadFields(input);

            List<string> labels = fields.Select(f => f.Label).ToList();
            Assert.Contains("Subject", labels);
            Assert.Contains("From", labels);
            Assert.Contains("To", labels);
            Assert.Contains("Body (text)", labels);
            Assert.Contains(fields, f => f.Text.Contains("Thanks", StringComparison.Ordinal));

            // Body parts are flagged as bodies (position-meaningful); headers are not.
            Assert.True(fields.Single(f => f.Label == "Body (text)").IsBody);
            Assert.False(fields.Single(f => f.Label == "Subject").IsBody);
            Assert.False(fields.Single(f => f.Label == "To").IsBody);
        }

        [Fact]
        public void ManualBodySelection_AppliesOnSave_RedactsBodyOnly()
        {
            string input = WriteEml();
            string output = Path.Combine(_tempDir, "manual.eml");

            List<(string Label, string Text, bool IsBody)> fields = EmailRedactor.ReadFields(input);
            int[] bodyIndices = fields.Select((f, i) => (f, i)).Where(x => x.f.IsBody).Select(x => x.i).ToArray();
            Assert.NotEmpty(bodyIndices);

            List<string> bodyTexts = bodyIndices.Select(i => fields[i].Text).ToList();
            // Select "Thanks" — a word the detector would not flag — to prove a manual redaction.
            int idx = bodyTexts[0].IndexOf("Thanks", StringComparison.Ordinal);
            Assert.True(idx >= 0);

            List<RedactionSpanEntity> spans = ManualRedaction.FromParagraphSelection(bodyTexts, idx, "Thanks".Length, 2);
            foreach (RedactionSpanEntity s in spans)
            {
                s.ParagraphIndex = bodyIndices[s.ParagraphIndex]; // body-part index -> field index
            }

            EmailRedactor.ApplySpans(input, output, spans);

            List<(string Label, string Text, bool IsBody)> outFields = EmailRedactor.ReadFields(output);
            Assert.DoesNotContain("Thanks", outFields.Single(f => f.IsBody).Text);                 // body redacted
            Assert.Contains("secret@example.com", outFields.Single(f => f.Label == "Subject").Text); // header untouched
        }

        [Fact]
        public void Detect_FindsFieldIndexedSpans_WithoutWriting()
        {
            string input = WriteEml();
            string before = File.ReadAllText(input);
            var fs = new Phileas.Services.FilterService();
            var policy = EmailPolicy();

            List<RedactionSpanEntity> spans = EmailRedactor.Detect(input, t => fs.Filter(policy, "ctx", 0, t));

            Assert.NotEmpty(spans);
            Assert.All(spans, s => Assert.True(s.ParagraphIndex >= 0)); // field-indexed
            Assert.Contains(spans, s => s.FilterType == "EmailAddress");
            Assert.Equal(before, File.ReadAllText(input)); // read-only: source unchanged
        }

        private static PhileasPolicy EmailPolicy() => new()
        {
            Name = "email",
            Identifiers = new Identifiers
            {
                EmailAddress = new EmailAddress(),
                PhoneNumber = new PhoneNumber()
            }
        };

        // A tiny real-filter shim so ApplySpans gets genuine engine spans without a policy dance.
        private sealed class FilterServiceShim
        {
            private readonly Phileas.Services.FilterService _fs = new();
            private readonly PhileasPolicy _policy = EmailPolicy();
            public Phileas.Model.TextFilterResult Filter(string text) => _fs.Filter(_policy, "ctx", 0, text);
        }
    }
}
