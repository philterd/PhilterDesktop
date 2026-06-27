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

using System.Text;
using PhilterData;
using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Smoke tests for PDF rendering: produces a valid, non-trivial PDF and survives a large detail
    /// table (which forces page breaks). Content correctness is covered by RedactionReportTests.
    /// </summary>
    public sealed class RedactionReportPdfTests
    {
        private static RedactionReportModel Model(int spanCount, bool detail)
        {
            var version = new RedactionVersionEntity
            {
                Version = 1,
                SourcePath = @"C:\cases\big.txt",
                OutputPath = @"C:\cases\big_redacted-draft.txt",
                FileType = ".txt",
                Policy = "Legal",
                Context = "default",
                CreatedAt = new DateTime(2026, 6, 27, 11, 30, 0, DateTimeKind.Utc)
            };
            var spans = new List<RedactionSpanEntity>();
            for (int i = 0; i < spanCount; i++)
            {
                spans.Add(new RedactionSpanEntity
                {
                    Order = i,
                    FilterType = i % 2 == 0 ? "EMAIL_ADDRESS" : "PHONE_NUMBER",
                    Text = "sensitive-" + i,
                    Replacement = "{{REDACTED}}",
                    CharacterStart = i,
                    CharacterEnd = i + 5
                });
            }
            return RedactionReport.Build(version, spans, "1.0.0", DateTimeOffset.UnixEpoch, "h1", "h2",
                new RedactionReportOptions { IncludeDetailTable = detail });
        }

        [Fact]
        public void ToPdfBytes_ProducesValidPdf()
        {
            byte[] pdf = RedactionReportPdf.ToPdfBytes(Model(spanCount: 5, detail: true));

            Assert.True(pdf.Length > 1000, "PDF should be non-trivial");
            Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4)); // PDF magic header
        }

        [Fact]
        public void ToPdfBytes_ManyRedactions_PagesWithoutThrowing()
        {
            // 150 detail rows exceeds one page, exercising the page-break path.
            byte[] pdf = RedactionReportPdf.ToPdfBytes(Model(spanCount: 150, detail: true));

            Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
            Assert.True(pdf.Length > 2000);
        }

        [Fact]
        public void Write_CreatesFileOnDisk()
        {
            string path = Path.Combine(Path.GetTempPath(), "philter-report-" + Guid.NewGuid().ToString("N") + ".pdf");
            try
            {
                RedactionReportPdf.Write(Model(spanCount: 3, detail: false), path);
                Assert.True(File.Exists(path));
                Assert.True(new FileInfo(path).Length > 1000);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
