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
    /// Tests the report content core: counts by type, the privacy guarantee (no original text), the
    /// optional detail table, and HTML rendering. PDF rendering is covered separately.
    /// </summary>
    public sealed class RedactionReportTests
    {
        private static readonly DateTimeOffset Generated = new(2026, 6, 27, 12, 0, 0, TimeSpan.Zero);

        private static RedactionVersionEntity Version() => new()
        {
            Version = 1,
            SourcePath = @"C:\cases\letter.txt",
            OutputPath = @"C:\cases\letter_redacted-draft.txt",
            FileType = ".txt",
            Policy = "Legal",
            Context = "matter-42",
            CreatedAt = new DateTime(2026, 6, 27, 11, 30, 0, DateTimeKind.Utc)
        };

        private static List<RedactionSpanEntity> Spans() => new()
        {
            new RedactionSpanEntity { Order = 0, FilterType = "EMAIL_ADDRESS", Text = "john@example.com", Replacement = "{{REDACTED}}", CharacterStart = 5, CharacterEnd = 21 },
            new RedactionSpanEntity { Order = 1, FilterType = "EMAIL_ADDRESS", Text = "jane@example.com", Replacement = "{{REDACTED}}", CharacterStart = 30, CharacterEnd = 46 },
            new RedactionSpanEntity { Order = 2, FilterType = "SSN", Text = "123-45-6789", Replacement = "{{REDACTED}}", CharacterStart = 60, CharacterEnd = 71 },
            new RedactionSpanEntity { Order = 3, UserAdded = true, Text = "Project Bluebird", Replacement = "{{REDACTED}}", CharacterStart = 80, CharacterEnd = 96 }
        };

        private static RedactionReportModel Build(RedactionReportOptions options) =>
            RedactionReport.Build(Version(), Spans(), "1.0.0", Generated, "aaaa", "bbbb", options);

        [Fact]
        public void Build_CountsByType_OrderedByCountThenName()
        {
            RedactionReportModel m = Build(new RedactionReportOptions());

            Assert.Equal(4, m.TotalRedactions);
            // Email (2) first, then Ssn (1) and User-added (1) alphabetically.
            Assert.Equal("Email Address", m.CountsByType[0].Type);
            Assert.Equal(2, m.CountsByType[0].Count);
            Assert.Contains(("Ssn", 1), m.CountsByType);
            Assert.Contains(("User-added", 1), m.CountsByType);
        }

        [Fact]
        public void Build_WithoutDetail_HasNoDetailRows()
        {
            RedactionReportModel m = Build(new RedactionReportOptions { IncludeDetailTable = false });
            Assert.Empty(m.Details);
        }

        [Fact]
        public void Build_WithDetail_HasRowPerSpan_ButNoOriginalText()
        {
            RedactionReportModel m = Build(new RedactionReportOptions { IncludeDetailTable = true });

            Assert.Equal(4, m.Details.Count);
            // Detail carries type/location/replacement — never the original detected text.
            foreach (RedactionReportRow row in m.Details)
            {
                Assert.DoesNotContain("example.com", row.Type + row.Location + row.Replacement);
                Assert.DoesNotContain("Bluebird", row.Type + row.Location + row.Replacement);
                Assert.DoesNotContain("123-45-6789", row.Type + row.Location + row.Replacement);
            }
        }

        // #536: the ParagraphIndex doubles as a spreadsheet cell / email field ordinal, so it must be
        // labeled for what it is instead of always "Section N".
        [Theory]
        [InlineData(".xlsx", "Cell 3")]
        [InlineData(".csv", "Cell 3")]
        [InlineData(".XLSX", "Cell 3")] // case-insensitive
        [InlineData(".eml", "Field 3")]
        [InlineData(".msg", "Field 3")]
        [InlineData(".docx", "Section 3")]
        [InlineData(".txt", "Section 3")]
        [InlineData(".rtf", "Section 3")]
        [InlineData(null, "Section 3")]
        public void LocationOf_LabelsOrdinalByFileType(string? fileType, string expected)
        {
            var span = new RedactionSpanEntity { ParagraphIndex = 2 }; // ordinal 2 -> "… 3"
            Assert.Equal(expected, RedactionReport.LocationOf(span, fileType));
        }

        [Fact]
        public void LocationOf_PageAndCharacterRange_AreUnaffectedByFileType()
        {
            Assert.Equal("Page 5", RedactionReport.LocationOf(new RedactionSpanEntity { PageNumber = 5 }, ".pdf"));
            Assert.Equal("Characters 10-20",
                RedactionReport.LocationOf(new RedactionSpanEntity { ParagraphIndex = -1, CharacterStart = 10, CharacterEnd = 20 }, ".txt"));
        }

        [Fact]
        public void Build_SpreadsheetDetail_LabelsLocationAsCell()
        {
            var version = new RedactionVersionEntity { Version = 1, FileType = ".xlsx", Policy = "p", Context = "c" };
            var spans = new List<RedactionSpanEntity>
            {
                new() { Order = 0, FilterType = "EMAIL_ADDRESS", ParagraphIndex = 4, Text = "a@b.com", Replacement = "{{R}}" }
            };

            RedactionReportModel m = RedactionReport.Build(
                version, spans, "1.0.0", Generated, "h1", "h2", new RedactionReportOptions { IncludeDetailTable = true });

            Assert.Equal("Cell 5", m.Details[0].Location);
        }

        [Fact]
        public void Build_MachineInfo_OnlyWhenRequested()
        {
            Assert.Null(Build(new RedactionReportOptions { IncludeMachineInfo = false }).MachineName);
            Assert.NotNull(Build(new RedactionReportOptions { IncludeMachineInfo = true }).MachineName);
        }

        [Fact]
        public void ToHtml_IncludesMetadataAndCounts_AndDraftReminder()
        {
            string html = RedactionReport.ToHtml(Build(new RedactionReportOptions()));

            Assert.Contains("Redaction Report", html);
            Assert.Contains("letter.txt", html);
            Assert.Contains("Legal", html);
            Assert.Contains("matter-42", html);
            Assert.Contains("aaaa", html);  // source hash
            Assert.Contains("bbbb", html);  // output hash
            Assert.Contains("Email Address", html);
            Assert.Contains("4 redactions", html);
            Assert.Contains(RedactionReport.DraftReminder, html);
        }

        [Fact]
        public void ToHtml_NeverContainsOriginalText_EvenWithDetailTable()
        {
            string html = RedactionReport.ToHtml(Build(new RedactionReportOptions { IncludeDetailTable = true }));

            Assert.DoesNotContain("john@example.com", html);
            Assert.DoesNotContain("jane@example.com", html);
            Assert.DoesNotContain("123-45-6789", html);
            Assert.DoesNotContain("Bluebird", html);
        }

        [Fact]
        public void Build_VerificationSummary_RendersWhenProvided_OmittedWhenNotRun()
        {
            RedactionReportModel notRun = Build(new RedactionReportOptions());
            Assert.Null(notRun.VerificationSummary);

            RedactionReportModel clean = RedactionReport.Build(
                Version(), Spans(), "1.0.0", Generated, "h1", "h2", new RedactionReportOptions(),
                verificationStatus: "Clean", verificationCount: 0, verificationCheckedAt: new DateTime(2026, 6, 27, 0, 0, 0, DateTimeKind.Utc));
            Assert.NotNull(clean.VerificationSummary);
            Assert.Contains("no detectable PII", clean.VerificationSummary!);
            Assert.Contains(clean.VerificationSummary!, RedactionReport.ToHtml(clean));

            RedactionReportModel residual = RedactionReport.Build(
                Version(), Spans(), "1.0.0", Generated, "h1", "h2", new RedactionReportOptions(),
                verificationStatus: "ResidualsFound", verificationCount: 2);
            Assert.Contains("2 possible items may remain", residual.VerificationSummary!);
        }

        [Fact]
        public void Build_VerificationSummary_NamesNotChecked_WarnsNamesMayRemain()
        {
            RedactionReportModel m = RedactionReport.Build(
                Version(), Spans(), "1.0.0", Generated, "h1", "h2", new RedactionReportOptions(),
                verificationStatus: "NamesNotChecked");

            Assert.NotNull(m.VerificationSummary);
            Assert.Contains("name detection was unavailable", m.VerificationSummary!, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(m.VerificationSummary!, RedactionReport.ToHtml(m)); // rendered in the report
        }

        [Fact]
        public void ToHtml_EncodesValues_NoRawAngleBrackets_FromData()
        {
            var version = Version();
            version.Policy = "<script>alert(1)</script>";
            string html = RedactionReport.ToHtml(
                RedactionReport.Build(version, Spans(), "1.0.0", Generated, "h1", "h2", new RedactionReportOptions()));

            Assert.DoesNotContain("<script>alert(1)</script>", html);
            Assert.Contains("&lt;script&gt;", html);
        }
    }
}
