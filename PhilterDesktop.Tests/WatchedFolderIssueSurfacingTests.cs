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

using LiteDB;
using PhilterData;
using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// A watched-folder failure must be visible even when notifications and application logging are both
    /// off: every failure is written to the folder's (database-backed) activity log, the list
    /// surfaces an error/warning count from it, and error/warning entries always reach the application log.
    /// </summary>
    public sealed class WatchedFolderIssueSurfacingTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly LiteDatabase _db;

        public WatchedFolderIssueSurfacingTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), "philter-wflog-" + Guid.NewGuid().ToString("N") + ".db");
            _db = new LiteDatabase(_dbPath);
        }

        public void Dispose()
        {
            _db.Dispose();
            try { File.Delete(_dbPath); } catch { /* best effort */ }
        }

        // --- #3: warnings/errors always reach the application log, info respects the toggle -----------

        [Theory]
        [InlineData(true, false, true)]   // warning + logging off -> still logged
        [InlineData(true, true, true)]    // warning + logging on  -> logged
        [InlineData(false, true, true)]   // info    + logging on  -> logged
        [InlineData(false, false, false)] // info    + logging off -> suppressed
        public void ShouldWriteToGlobalLog_AlwaysLogsWarnings_GatesInfo(bool warning, bool loggingEnabled, bool expected)
        {
            Assert.Equal(expected, FolderWatcherService.ShouldWriteToGlobalLog(warning, loggingEnabled));
        }

        // --- #1: the activity log (in the database) always records, and counts drive the indicator ----

        [Fact]
        public void ActivityLog_CountsErrorsAndWarningsPerFolder()
        {
            var repo = new WatchedFolderLogRepository(_db);
            ObjectId folderA = ObjectId.NewObjectId();
            ObjectId folderB = ObjectId.NewObjectId();

            repo.Append(folderA, "Error", "Unknown policy, skipped: a.txt");
            repo.Append(folderA, "Error", "Failed to redact: b.pdf");
            repo.Append(folderA, "Warning", "Redacted without name detection: c.docx");
            repo.Append(folderA, "Info", "Redacted: d.txt");
            repo.Append(folderB, "Error", "Exceeds size limit: big.txt");

            Assert.Equal(2, repo.CountByLevels(folderA, "Error"));
            Assert.Equal(1, repo.CountByLevels(folderA, "Warning"));
            Assert.Equal(3, repo.CountByLevels(folderA, "Error", "Warning"));
            Assert.Equal(1, repo.CountByLevels(folderB, "Error"));
            Assert.Equal(0, repo.CountByLevels(folderB, "Warning"));
            Assert.Equal(0, repo.CountByLevels(folderA)); // no levels -> 0
        }

        [Fact]
        public void CountByLevels_IsCaseInsensitive()
        {
            var repo = new WatchedFolderLogRepository(_db);
            ObjectId folder = ObjectId.NewObjectId();
            repo.Append(folder, "Error", "x");

            Assert.Equal(1, repo.CountByLevels(folder, "error"));
            Assert.Equal(1, repo.CountByLevels(folder, "ERROR"));
        }

        [Fact]
        public void Severity_PrioritizesErrorsThenWarnings()
        {
            Assert.Equal(WatchedFolderSeverity.None, WatchedFolderIssueSummary.Severity(0, 0));
            Assert.Equal(WatchedFolderSeverity.Warning, WatchedFolderIssueSummary.Severity(0, 3));
            Assert.Equal(WatchedFolderSeverity.Error, WatchedFolderIssueSummary.Severity(2, 0));
            Assert.Equal(WatchedFolderSeverity.Error, WatchedFolderIssueSummary.Severity(2, 3)); // errors dominate
        }

        [Theory]
        [InlineData(0, 0, "None")]
        [InlineData(1, 0, "1 error")]
        [InlineData(2, 0, "2 errors")]
        [InlineData(0, 1, "1 warning")]
        [InlineData(2, 1, "2 errors, 1 warning")]
        public void Describe_ProducesReadableCellText(int errors, int warnings, string expected)
        {
            Assert.Equal(expected, WatchedFolderIssueSummary.Describe(errors, warnings));
        }

        [Fact]
        public void Tooltip_IsNullWhenClean_AndExplainsWhenNot()
        {
            Assert.Null(WatchedFolderIssueSummary.Tooltip(0, 0));
            Assert.Contains("not redacted", WatchedFolderIssueSummary.Tooltip(1, 0));
            Assert.Contains("View Log", WatchedFolderIssueSummary.Tooltip(0, 2));
        }
    }
}
