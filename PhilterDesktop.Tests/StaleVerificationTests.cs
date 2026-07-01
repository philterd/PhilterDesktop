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

using PhilterData;
using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// A Modify-Redaction re-redact creates a new, unverified output; the document's stored verification
    /// verdict must be cleared so a report for the new version isn't stamped with the old "verified"
    /// result (#560).
    /// </summary>
    public sealed class StaleVerificationTests
    {
        private static readonly DateTimeOffset Generated = new(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);

        private static RedactionVersionEntity Version() => new()
        {
            Version = 2,
            SourcePath = @"C:\cases\letter.txt",
            OutputPath = @"C:\cases\letter_redacted-draft_2.txt", // the NEW output
            FileType = ".txt",
            Policy = "Legal",
            Context = "matter-42",
            CreatedAt = new DateTime(2026, 6, 30, 11, 30, 0, DateTimeKind.Utc)
        };

        private static List<RedactionSpanEntity> Spans() => new()
        {
            new RedactionSpanEntity { Order = 0, FilterType = "SSN", Text = "123-45-6789", Replacement = "{{REDACTED}}", CharacterStart = 0, CharacterEnd = 11 }
        };

        [Fact]
        public void ResetVerification_ClearsAllVerificationFields()
        {
            var entity = new RedactionQueueEntity
            {
                VerificationStatus = "Clean",
                VerificationFindingCount = 3,
                VerificationCheckedAt = DateTime.UtcNow
            };

            MainForm.ResetVerification(entity);

            Assert.Equal("NotRun", entity.VerificationStatus);
            Assert.Equal(0, entity.VerificationFindingCount);
            Assert.Null(entity.VerificationCheckedAt);
        }

        [Fact]
        public void Report_ForNewVersion_DropsTheStaleVerifiedVerdict_AfterReset()
        {
            // v1 was verified clean; the verdict lives on the queue entity.
            var entity = new RedactionQueueEntity
            {
                VerificationStatus = "Clean",
                VerificationFindingCount = 0,
                VerificationCheckedAt = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc)
            };
            var options = new RedactionReportOptions();

            // Before the fix: a report for the new version would carry the stale "verified" verdict.
            RedactionReportModel stale = RedactionReport.Build(
                Version(), Spans(), "1.0.0", Generated, "src", "out", options,
                entity.VerificationStatus, entity.VerificationFindingCount, entity.VerificationCheckedAt);
            Assert.NotNull(stale.VerificationSummary);
            Assert.Contains("no detectable PII", stale.VerificationSummary!);

            // After a modify re-redact, the verdict is reset, so the report for the new (unverified)
            // output makes no verification claim.
            MainForm.ResetVerification(entity);
            RedactionReportModel afterReset = RedactionReport.Build(
                Version(), Spans(), "1.0.0", Generated, "src", "out", options,
                entity.VerificationStatus, entity.VerificationFindingCount, entity.VerificationCheckedAt);

            Assert.Null(afterReset.VerificationSummary);
            Assert.DoesNotContain("Verified", RedactionReport.ToHtml(afterReset));
        }

        [Fact]
        public void VerificationDisplay_NotRun_AfterReset_NotShownAsClean()
        {
            // The same reset also drives the queue column / View Details, which must not read "Clean".
            var entity = new RedactionQueueEntity { VerificationStatus = "Clean", VerificationFindingCount = 0 };
            MainForm.ResetVerification(entity);
            Assert.NotEqual("Clean", entity.VerificationStatus);
        }
    }
}
