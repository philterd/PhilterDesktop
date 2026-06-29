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
    }
}
