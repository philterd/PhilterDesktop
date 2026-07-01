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
using PhilterData;
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
        public void Verify_Csv_CleanAfterRedaction()
        {
            string source = Path_("src.csv");
            File.WriteAllText(source, "Name,Email\r\nAlice,alice@example.com\r\nBob,bob@example.com\r\n");
            string output = Path_("src_redacted.csv");
            PhileasPolicy policy = EmailPolicy();
            CsvRedactor.Redact(source, output, t => _fs.Filter(policy, "ctx", 0, t));

            VerificationOutcome outcome = RedactionVerifier.Verify(output, policy, "ctx", _fs);

            Assert.Equal(VerificationStatus.Clean, outcome.Status);
        }

        // The residual maps to its cell (per-field), not to a whole-file offset (the blob behavior this
        // replaced, which yielded ParagraphIndex -1 and a file-wide CharacterStart).
        [Fact]
        public void Verify_Csv_Residual_IsMappedToCellOrdinal_NotFileOffset()
        {
            string output = Path_("residual.csv");
            File.WriteAllText(output, "Name,Email\r\nAlice,leftover@example.com\r\n");

            VerificationOutcome outcome = RedactionVerifier.Verify(output, EmailPolicy(), "ctx", _fs);

            Assert.Equal(VerificationStatus.ResidualsFound, outcome.Status);
            RedactionSpanEntity r = Assert.Single(outcome.Residuals);
            Assert.Equal(3, r.ParagraphIndex);   // 4th field, not -1
            Assert.Equal(0, r.CharacterStart);   // offset within the cell, not the file
            Assert.Contains("leftover@example.com", r.Text);
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
        public void Verify_DoesNotReportTheRedactionsOwnReplacements()
        {
            // Simulates a RANDOM_REPLACE stand-in (a PII-shaped value the redactor inserted): it must not
            // be reported as residual PII when it's in the known-replacements set.
            string output = Path_("standin.txt");
            File.WriteAllText(output, "Contact standin@example.com about this.");

            var known = new HashSet<string> { "standin@example.com" };
            VerificationOutcome outcome = RedactionVerifier.Verify(output, EmailPolicy(), "ctx", _fs, known);

            Assert.Equal(VerificationStatus.Clean, outcome.Status);
            Assert.Empty(outcome.Residuals);
        }

        [Fact]
        public void Verify_StillReportsGenuineResiduals_AlongsideOwnReplacements()
        {
            // One real leftover email plus one inserted stand-in: only the genuine residual is reported.
            string output = Path_("mixed.txt");
            File.WriteAllText(output, "Real leftover@example.com and standin standin@example.com.");

            var known = new HashSet<string> { "standin@example.com" };
            VerificationOutcome outcome = RedactionVerifier.Verify(output, EmailPolicy(), "ctx", _fs, known);

            Assert.Equal(VerificationStatus.ResidualsFound, outcome.Status);
            Assert.Contains(outcome.Residuals, r => r.Text == "leftover@example.com");
            Assert.DoesNotContain(outcome.Residuals, r => r.Text == "standin@example.com");
        }

        [Fact]
        public void ReplacementsOf_ReturnsDistinctNonEmptyReplacements()
        {
            var spans = new List<RedactionSpanEntity>
            {
                new() { Replacement = "AAA" },
                new() { Replacement = "AAA" }, // duplicate
                new() { Replacement = "BBB" },
                new() { Replacement = "" },     // empty -> excluded
            };

            IReadOnlySet<string> set = RedactionVerifier.ReplacementsOf(spans);

            Assert.Equal(2, set.Count);
            Assert.Contains("AAA", set);
            Assert.Contains("BBB", set);
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

        // --- #543: RTF verification carries a fidelity caveat so "clean" doesn't overstate integrity ----

        private static PhileasPolicy EmailSsnPolicy() =>
            PolicySerializer.DeserializeFromJson("{\"identifiers\":{\"emailAddress\":{},\"ssn\":{}}}");

        [Fact]
        public void Verify_Rtf_SourceHadHeaderFooter_IsCleanButCarriesFidelityNote()
        {
            // Source has a header (dropped by RTF redaction); the redacted body has no residual PII.
            string source = Path_("hdr-source.rtf");
            File.WriteAllText(source, @"{\rtf1\ansi{\header Letterhead records@example.com}\f0 Plain body no pii}");
            string output = Path_("hdr-out.rtf");
            File.WriteAllText(output, @"{\rtf1\ansi\f0 Plain body no pii}"); // body-only, as the redactor would write

            VerificationOutcome outcome = RedactionVerifier.Verify(output, EmailPolicy(), "ctx", _fs, sourcePath: source);

            Assert.Equal(VerificationStatus.Clean, outcome.Status); // no detectable PII in the body
            Assert.NotNull(outcome.FidelityNote);                   // but "clean" is qualified for RTF
            Assert.Contains(".docx", outcome.FidelityNote!);
        }

        [Fact]
        public void Verify_Rtf_BodyOnlySource_HasNoFidelityNote()
        {
            string source = Path_("body-source.rtf");
            File.WriteAllText(source, @"{\rtf1\ansi\f0 Plain body no pii}");
            string output = Path_("body-out.rtf");
            File.WriteAllText(output, @"{\rtf1\ansi\f0 Plain body no pii}");

            VerificationOutcome outcome = RedactionVerifier.Verify(output, EmailPolicy(), "ctx", _fs, sourcePath: source);

            Assert.Equal(VerificationStatus.Clean, outcome.Status);
            Assert.Null(outcome.FidelityNote); // nothing was dropped -> no caveat
        }

        [Fact]
        public void Verify_Rtf_NoSourcePath_HasNoFidelityNote()
        {
            string output = Path_("nosource-out.rtf");
            File.WriteAllText(output, @"{\rtf1\ansi\f0 Plain body no pii}");

            VerificationOutcome outcome = RedactionVerifier.Verify(output, EmailPolicy(), "ctx", _fs); // no sourcePath

            Assert.Equal(VerificationStatus.Clean, outcome.Status);
            Assert.Null(outcome.FidelityNote); // can't assess fidelity without the source -> no caveat
        }

        [Fact]
        public void Verify_Rtf_WithResidualAndDroppedSource_FlagsResidual_AndCarriesFidelityNote()
        {
            string source = Path_("both-source.rtf");
            File.WriteAllText(source, @"{\rtf1\ansi{\footer foot@example.com}\f0 Body}");
            string output = Path_("both-out.rtf");
            File.WriteAllText(output, @"{\rtf1\ansi\f0 leftover@example.com in the body}");

            VerificationOutcome outcome = RedactionVerifier.Verify(output, EmailPolicy(), "ctx", _fs, sourcePath: source);

            Assert.Equal(VerificationStatus.ResidualsFound, outcome.Status); // real residual still reported
            Assert.NotNull(outcome.FidelityNote);                            // and the caveat rides along
        }

        [Fact]
        public async Task Verify_Rtf_HeaderFooterSample_CleanBody_ButFidelityCaveat()
        {
            // Integration: redact the real header/footer sample, then verify — the body's PII is redacted
            // (clean) but the result is honestly qualified because the source had headers/footers (#543).
            string input = Path.Combine(AppContext.BaseDirectory, "test-documents", "header-footer.rtf");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path_("hf_redacted.rtf");

            PhileasPolicy policy = EmailSsnPolicy();
            await RedactionService.RedactFileAsync(input, output, policy, "ctx");

            VerificationOutcome outcome = RedactionVerifier.Verify(output, policy, "ctx", _fs, sourcePath: input);

            Assert.Equal(VerificationStatus.Clean, outcome.Status); // body email/SSN redacted -> no residual
            Assert.NotNull(outcome.FidelityNote);                   // header/footer weren't re-scanned
            Assert.Contains(".docx", outcome.FidelityNote!);
        }
    }
}
