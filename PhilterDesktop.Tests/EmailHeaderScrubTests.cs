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
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Verifies the "Remove technical email headers" option: identifying headers (originating IP, mail
    /// client, server-hop trail, X-*/ARC-*) are stripped from a redacted <c>.eml</c> when on, and kept
    /// when off (so the scrub is genuinely gated by the setting). The visible fields are preserved.
    /// </summary>
    public sealed class EmailHeaderScrubTests : IDisposable
    {
        private readonly string _dir;
        public EmailHeaderScrubTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-emlhdr-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }
        public void Dispose() { try { Directory.Delete(_dir, true); } catch { /* best effort */ } }

        private const string SampleEml =
            "Received: from mail.evil.example (10.1.2.3) by mx.example.com; Tue, 01 Apr 2025 10:00:00 +0000\r\n" +
            "Return-Path: <alice@example.com>\r\n" +
            "X-Originating-IP: [203.0.113.7]\r\n" +
            "X-Mailer: SecretMailer 9.9\r\n" +
            "User-Agent: Thunderbird 115\r\n" +
            "ARC-Authentication-Results: i=1; mx.example.com\r\n" +
            "From: Alice <alice@example.com>\r\n" +
            "To: Bob <bob@example.com>\r\n" +
            "Subject: Case notes\r\n" +
            "Date: Tue, 01 Apr 2025 10:00:00 +0000\r\n" +
            "Content-Type: text/plain; charset=utf-8\r\n" +
            "\r\n" +
            "Patient SSN 123-45-6789.\r\n";

        private string WriteEml()
        {
            string path = Path.Combine(_dir, "in.eml");
            File.WriteAllText(path, SampleEml);
            return path;
        }

        private static PhileasPolicy SsnPolicy() => new()
        {
            Name = "ssn",
            Identifiers = new Identifiers { Ssn = new Ssn() }
        };

        [Fact]
        public async Task ScrubEnabled_RemovesTechnicalHeaders_KeepsVisibleFields_AndRedactsPii()
        {
            string output = Path.Combine(_dir, "out.eml");
            await RedactionService.RedactFileAsync(WriteEml(), output, SsnPolicy(), "ctx",
                scrubEmailHeaders: true);

            string eml = await File.ReadAllTextAsync(output);

            // Identifying technical metadata is gone.
            Assert.DoesNotContain("203.0.113.7", eml);
            Assert.DoesNotContain("10.1.2.3", eml);
            Assert.DoesNotContain("SecretMailer", eml);
            Assert.DoesNotContain("Thunderbird", eml);
            Assert.DoesNotContain("X-Originating-IP", eml);
            Assert.DoesNotContain("X-Mailer", eml);
            Assert.DoesNotContain("User-Agent", eml);
            Assert.DoesNotContain("Received:", eml);
            Assert.DoesNotContain("ARC-", eml);

            // The visible fields remain (and the body PII is redacted).
            Assert.Contains("Subject: Case notes", eml);
            Assert.Contains("alice@example.com", eml); // From kept (no email filter in this policy)
            Assert.DoesNotContain("123-45-6789", eml);
        }

        [Fact]
        public async Task ScrubDisabled_KeepsTechnicalHeaders()
        {
            string output = Path.Combine(_dir, "out-keep.eml");
            await RedactionService.RedactFileAsync(WriteEml(), output, SsnPolicy(), "ctx",
                scrubEmailHeaders: false);

            string eml = await File.ReadAllTextAsync(output);
            Assert.Contains("X-Originating-IP", eml);
            Assert.Contains("203.0.113.7", eml);
            Assert.DoesNotContain("123-45-6789", eml); // PII still redacted regardless
        }

        // An .eml carrying the common identity/delivery headers that aren't part of the scanned
        // From/To/Cc field set — these would otherwise pass through unredacted.
        private const string SampleEmlWithIdentityHeaders =
            "Sender: Secretary <secretary@example.com>\r\n" +
            "Reply-To: replies@example.com\r\n" +
            "From: Alice <alice@example.com>\r\n" +
            "To: Bob <bob@example.com>\r\n" +
            "Cc: Carol <carol@example.com>\r\n" +
            "Bcc: Hidden Person <hidden@example.com>\r\n" +
            "Resent-To: resent@example.com\r\n" +
            "Subject: Case notes\r\n" +
            "Date: Tue, 01 Apr 2025 10:00:00 +0000\r\n" +
            "Content-Type: text/plain; charset=utf-8\r\n" +
            "\r\n" +
            "Patient SSN 123-45-6789.\r\n";

        private string WriteIdentityEml()
        {
            string path = Path.Combine(_dir, "identity.eml");
            File.WriteAllText(path, SampleEmlWithIdentityHeaders);
            return path;
        }

        [Fact]
        public async Task RemoveCommonHeadersEnabled_StripsBccReplyToSenderResent_KeepsToCcFrom()
        {
            string output = Path.Combine(_dir, "out-common.eml");
            await RedactionService.RedactFileAsync(WriteIdentityEml(), output, SsnPolicy(), "ctx",
                removeCommonEmailHeaders: true);

            string eml = await File.ReadAllTextAsync(output);

            // The blind-copy recipient and the other identity headers must be gone.
            Assert.DoesNotContain("hidden@example.com", eml);
            Assert.DoesNotContain("Bcc:", eml);
            Assert.DoesNotContain("replies@example.com", eml);
            Assert.DoesNotContain("Reply-To:", eml);
            Assert.DoesNotContain("secretary@example.com", eml);
            Assert.DoesNotContain("Sender:", eml);
            Assert.DoesNotContain("resent@example.com", eml);
            Assert.DoesNotContain("Resent-To", eml);

            // The visible recipient fields stay, and body PII is still redacted.
            Assert.Contains("bob@example.com", eml);   // To
            Assert.Contains("carol@example.com", eml); // Cc
            Assert.Contains("alice@example.com", eml); // From
            Assert.DoesNotContain("123-45-6789", eml);
        }

        [Fact]
        public async Task RemoveCommonHeadersDisabled_KeepsBcc()
        {
            string output = Path.Combine(_dir, "out-common-keep.eml");
            await RedactionService.RedactFileAsync(WriteIdentityEml(), output, SsnPolicy(), "ctx",
                removeCommonEmailHeaders: false);

            string eml = await File.ReadAllTextAsync(output);
            Assert.Contains("hidden@example.com", eml); // gated by the setting
        }

        [Fact]
        public async Task RemoveDateHeaderEnabled_DropsDate_KeepsOtherFields_AndRedactsPii()
        {
            string output = Path.Combine(_dir, "out-nodate.eml");
            await RedactionService.RedactFileAsync(WriteEml(), output, SsnPolicy(), "ctx",
                removeEmailDateHeader: true);

            string eml = await File.ReadAllTextAsync(output);

            // The Date header is gone outright, no detector involved. (The identical timestamp in the
            // Received header survives here because technical-header scrubbing isn't enabled in this test.)
            Assert.DoesNotContain("Date:", eml);

            // Everything else is preserved and body PII is still redacted.
            Assert.Contains("Subject: Case notes", eml);
            Assert.Contains("alice@example.com", eml);
            Assert.DoesNotContain("123-45-6789", eml);
        }

        [Fact]
        public async Task RemoveDateHeaderDisabled_KeepsDate()
        {
            string output = Path.Combine(_dir, "out-date-keep.eml");
            await RedactionService.RedactFileAsync(WriteEml(), output, SsnPolicy(), "ctx",
                removeEmailDateHeader: false);

            string eml = await File.ReadAllTextAsync(output);
            Assert.Contains("Date:", eml);        // gated by the setting
            Assert.Contains("01 Apr 2025", eml);
            Assert.DoesNotContain("123-45-6789", eml); // PII still redacted regardless
        }

        [Fact]
        public async Task RemoveDateHeaderDefault_KeepsDate()
        {
            // Default (option off) leaves the Date header in place.
            string output = Path.Combine(_dir, "out-date-default.eml");
            await RedactionService.RedactFileAsync(WriteEml(), output, SsnPolicy(), "ctx");

            string eml = await File.ReadAllTextAsync(output);
            Assert.Contains("Date:", eml);
        }

        // A multipart/mixed .eml with a plain-text body and a PDF attachment whose filename itself carries
        // PII ("john_smith_ssn.pdf"). "U0VDUkVU..." decodes to "SECRET-ATTACHMENT-BODY".
        private const string SampleEmlWithAttachment =
            "From: Alice <alice@example.com>\r\n" +
            "To: Bob <bob@example.com>\r\n" +
            "Subject: With attachment\r\n" +
            "Date: Tue, 01 Apr 2025 10:00:00 +0000\r\n" +
            "MIME-Version: 1.0\r\n" +
            "Content-Type: multipart/mixed; boundary=\"BND\"\r\n" +
            "\r\n" +
            "--BND\r\n" +
            "Content-Type: text/plain; charset=utf-8\r\n" +
            "\r\n" +
            "Patient SSN 123-45-6789.\r\n" +
            "--BND\r\n" +
            "Content-Type: application/pdf; name=\"john_smith_ssn.pdf\"\r\n" +
            "Content-Disposition: attachment; filename=\"john_smith_ssn.pdf\"\r\n" +
            "Content-Transfer-Encoding: base64\r\n" +
            "\r\n" +
            "U0VDUkVULUFUVEFDSE1FTlQtQk9EWQ==\r\n" +
            "--BND--\r\n";

        private string WriteAttachmentEml()
        {
            string path = Path.Combine(_dir, "attachment.eml");
            File.WriteAllText(path, SampleEmlWithAttachment);
            return path;
        }

        [Fact]
        public async Task RemoveAttachmentsEnabled_DropsAttachment_KeepsBody_AndRedactsPii()
        {
            string output = Path.Combine(_dir, "out-noattach.eml");
            await RedactionService.RedactFileAsync(WriteAttachmentEml(), output, SsnPolicy(), "ctx",
                removeEmailAttachments: true);

            string eml = await File.ReadAllTextAsync(output);
            // The attachment (content and its PII-bearing filename) is gone.
            Assert.DoesNotContain("john_smith_ssn.pdf", eml);
            Assert.DoesNotContain("U0VDUkVU", eml);
            Assert.DoesNotContain("Content-Disposition: attachment", eml);

            // The message body survives and its PII is still redacted.
            using var stream = File.OpenRead(output);
            MimeKit.MimeMessage message = MimeKit.MimeMessage.Load(stream);
            Assert.Empty(message.Attachments);
            Assert.Contains("REDACTED", message.TextBody ?? string.Empty);
            Assert.DoesNotContain("123-45-6789", message.TextBody ?? string.Empty);
        }

        [Fact]
        public async Task RemoveAttachmentsDisabled_KeepsAttachment()
        {
            string output = Path.Combine(_dir, "out-attach-keep.eml");
            await RedactionService.RedactFileAsync(WriteAttachmentEml(), output, SsnPolicy(), "ctx",
                removeEmailAttachments: false);

            string eml = await File.ReadAllTextAsync(output);
            Assert.Contains("john_smith_ssn.pdf", eml);      // gated by the setting
            Assert.DoesNotContain("123-45-6789", eml);       // body PII still redacted
        }

        [Fact]
        public async Task RemoveAttachmentsDefault_KeepsAttachment()
        {
            // Default (option off) leaves attachments in place.
            string output = Path.Combine(_dir, "out-attach-default.eml");
            await RedactionService.RedactFileAsync(WriteAttachmentEml(), output, SsnPolicy(), "ctx");

            string eml = await File.ReadAllTextAsync(output);
            Assert.Contains("john_smith_ssn.pdf", eml);
        }

        // An .eml that forwards another message as a message/rfc822 part. The nested message has its own
        // identifying headers (Bcc, Received) and a body with PII.
        private const string SampleEmlWithNestedMessage =
            "From: Alice <alice@example.com>\r\n" +
            "To: Bob <bob@example.com>\r\n" +
            "Subject: FW: notes\r\n" +
            "Date: Tue, 01 Apr 2025 10:00:00 +0000\r\n" +
            "MIME-Version: 1.0\r\n" +
            "Content-Type: multipart/mixed; boundary=\"BND\"\r\n" +
            "\r\n" +
            "--BND\r\n" +
            "Content-Type: text/plain; charset=utf-8\r\n" +
            "\r\n" +
            "Forwarding the note below.\r\n" +
            "--BND\r\n" +
            "Content-Type: message/rfc822\r\n" +
            "\r\n" +
            "From: Carol <carol@example.com>\r\n" +
            "To: Dave <dave@example.com>\r\n" +
            "Bcc: Secret <secret-inner@example.com>\r\n" +
            "Subject: notes\r\n" +
            "Date: Mon, 31 Mar 2025 09:00:00 +0000\r\n" +
            "Received: from mail.inner (10.1.2.3) by mx; Mon, 31 Mar 2025 09:00:00 +0000\r\n" +
            "Content-Type: text/plain; charset=utf-8\r\n" +
            "\r\n" +
            "Patient SSN 123-45-6789.\r\n" +
            "--BND--\r\n";

        private string WriteNestedEml()
        {
            string path = Path.Combine(_dir, "nested.eml");
            File.WriteAllText(path, SampleEmlWithNestedMessage);
            return path;
        }

        [Fact]
        public async Task NestedMessage_RedactsInnerBody()
        {
            // The forwarded message's body PII is redacted (its parts are enumerated recursively).
            string output = Path.Combine(_dir, "nested-body.eml");
            await RedactionService.RedactFileAsync(WriteNestedEml(), output, SsnPolicy(), "ctx");

            Assert.DoesNotContain("123-45-6789", await File.ReadAllTextAsync(output));
        }

        [Fact]
        public async Task NestedMessage_ScrubEnabled_RemovesInnerTechnicalHeaders()
        {
            string output = Path.Combine(_dir, "nested-scrub.eml");
            await RedactionService.RedactFileAsync(WriteNestedEml(), output, SsnPolicy(), "ctx",
                scrubEmailHeaders: true);

            Assert.DoesNotContain("10.1.2.3", await File.ReadAllTextAsync(output)); // nested Received IP
        }

        [Fact]
        public async Task NestedMessage_RemoveCommonEnabled_RemovesInnerBcc()
        {
            string output = Path.Combine(_dir, "nested-bcc.eml");
            await RedactionService.RedactFileAsync(WriteNestedEml(), output, SsnPolicy(), "ctx",
                removeCommonEmailHeaders: true);

            Assert.DoesNotContain("secret-inner@example.com", await File.ReadAllTextAsync(output)); // nested Bcc
        }

        [Fact]
        public async Task NestedMessage_ScrubDisabled_KeepsInnerHeaders()
        {
            // Gated by the settings: with the scrubs off the nested identity/technical headers remain.
            string output = Path.Combine(_dir, "nested-keep.eml");
            await RedactionService.RedactFileAsync(WriteNestedEml(), output, SsnPolicy(), "ctx");

            string eml = await File.ReadAllTextAsync(output);
            Assert.Contains("10.1.2.3", eml);
            Assert.Contains("secret-inner@example.com", eml);
        }
    }
}
