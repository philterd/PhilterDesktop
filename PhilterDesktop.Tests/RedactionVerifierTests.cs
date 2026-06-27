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
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Tests the post-redaction verification pass across formats: it reads the written output, reports
    /// residual PII when present, reports clean when a redacted output is actually clean, and surfaces an
    /// error for a missing file.
    /// </summary>
    public sealed class RedactionVerifierTests : IDisposable
    {
        private readonly string _dir;
        private readonly FilterService _fs = new();

        public RedactionVerifierTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-verify-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private static PhileasPolicy EmailPolicy() =>
            PolicySerializer.DeserializeFromJson("{\"identifiers\":{\"emailAddress\":{}}}");

        private string Path_(string name) => Path.Combine(_dir, name);

        [Fact]
        public void Verify_Txt_NoPii_IsClean()
        {
            string output = Path_("clean.txt");
            File.WriteAllText(output, "Nothing sensitive here at all.");

            VerificationOutcome outcome = RedactionVerifier.Verify(output, EmailPolicy(), "ctx", _fs);

            Assert.Equal(VerificationStatus.Clean, outcome.Status);
            Assert.Empty(outcome.Residuals);
        }

        [Fact]
        public void Verify_Txt_WithResidualEmail_IsFlagged()
        {
            string output = Path_("leak.txt");
            File.WriteAllText(output, "Contact leftover@example.com about this.");

            VerificationOutcome outcome = RedactionVerifier.Verify(output, EmailPolicy(), "ctx", _fs);

            Assert.Equal(VerificationStatus.ResidualsFound, outcome.Status);
            Assert.Contains(outcome.Residuals, r => r.Text.Contains("leftover@example.com"));
        }

        [Fact]
        public void Verify_Csv_WithResidualEmail_IsFlagged()
        {
            string output = Path_("people.csv");
            File.WriteAllText(output, "Name,Email\r\nAlice,alice@example.com\r\n");

            VerificationOutcome outcome = RedactionVerifier.Verify(output, EmailPolicy(), "ctx", _fs);

            Assert.Equal(VerificationStatus.ResidualsFound, outcome.Status);
        }

        [Fact]
        public void Verify_Docx_CleanAfterRedaction()
        {
            string source = Path_("note.docx");
            string output = Path_("note_redacted.docx");
            WordDocs.Create(source, "Please email alice@example.com today.");

            PhileasPolicy policy = EmailPolicy();
            WordDocumentRedactor.Redact(source, output, t => _fs.Filter(policy, "ctx", 0, t));

            // Verifying the actual written output proves the email is gone — the core of the feature.
            VerificationOutcome outcome = RedactionVerifier.Verify(output, policy, "ctx", _fs);

            Assert.Equal(VerificationStatus.Clean, outcome.Status);
        }

        [Fact]
        public void Verify_Docx_WithResidual_IsFlagged()
        {
            string output = Path_("raw.docx");
            WordDocs.Create(output, "Reach me at bob@example.com.");

            VerificationOutcome outcome = RedactionVerifier.Verify(output, EmailPolicy(), "ctx", _fs);

            Assert.Equal(VerificationStatus.ResidualsFound, outcome.Status);
            Assert.Contains(outcome.Residuals, r => r.Text.Contains("bob@example.com"));
        }

        [Fact]
        public void Verify_Xlsx_WithResidualEmail_IsFlagged()
        {
            string output = Path_("book.xlsx");
            SpreadsheetTestHelper.CreateXlsx(output, new List<string?[]>
            {
                new string?[] { "Name", "Email" },
                new string?[] { "Carol", "carol@example.com" }
            });

            VerificationOutcome outcome = RedactionVerifier.Verify(output, EmailPolicy(), "ctx", _fs);

            Assert.Equal(VerificationStatus.ResidualsFound, outcome.Status);
        }

        [Fact]
        public void BroadPolicy_FindsTypesTheRedactionPolicyMissed()
        {
            // The output still contains a phone number. The narrow (email-only) policy can't see it, so
            // it verifies "clean"; the broad policy enables phone detection and flags it.
            string output = Path_("missed.txt");
            File.WriteAllText(output, "No emails here, but call 555-123-4567 anytime.");

            VerificationOutcome narrow = RedactionVerifier.Verify(output, EmailPolicy(), "ctx", _fs);
            VerificationOutcome broad = RedactionVerifier.Verify(output, VerificationPolicy.Broad(), "ctx", _fs);

            Assert.Equal(VerificationStatus.Clean, narrow.Status);
            Assert.Equal(VerificationStatus.ResidualsFound, broad.Status);
            Assert.Contains(broad.Residuals, r => r.Text.Contains("555-123-4567"));
        }

        [Fact]
        public void BroadPolicy_EnablesManyDetectors()
        {
            // Sanity check the broad policy really turns on a wide set (not just one or two filters).
            PhileasPolicy broad = VerificationPolicy.Broad();
            int enabled = typeof(Phileas.Policy.Identifiers)
                .GetProperties()
                .Count(p => typeof(Phileas.Policy.Filters.AbstractPolicyFilter).IsAssignableFrom(p.PropertyType)
                            && p.GetValue(broad.Identifiers) is not null);
            Assert.True(enabled >= 10, $"expected a broad set of detectors, got {enabled}");
        }

        [Fact]
        public void Verify_MissingFile_ReturnsError()
        {
            VerificationOutcome outcome = RedactionVerifier.Verify(Path_("gone.txt"), EmailPolicy(), "ctx", _fs);

            Assert.Equal(VerificationStatus.Error, outcome.Status);
            Assert.False(string.IsNullOrWhiteSpace(outcome.Error));
        }
    }
}
