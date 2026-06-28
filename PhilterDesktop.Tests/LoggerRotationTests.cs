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
using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Tests the log-rotation helper that keeps <c>application.log</c> from growing without bound.
    /// </summary>
    public sealed class LoggerRotationTests : IDisposable
    {
        private readonly string _dir;
        private readonly string _log;

        public LoggerRotationTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-log-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _log = Path.Combine(_dir, "application.log");
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private void WriteBytes(string path, int count) =>
            File.WriteAllBytes(path, Encoding.ASCII.GetBytes(new string('x', count)));

        [Fact]
        public void RotateLogIfNeeded_UnderLimit_LeavesFileAlone()
        {
            WriteBytes(_log, 100);
            Logger.RotateLogIfNeeded(_log, maxBytes: 1000, maxBackups: 3);

            Assert.True(File.Exists(_log));
            Assert.False(File.Exists(_log + ".1"));
        }

        [Fact]
        public void RotateLogIfNeeded_AtOrOverLimit_RollsToDotOne_AndStartsFresh()
        {
            WriteBytes(_log, 1000);
            Logger.RotateLogIfNeeded(_log, maxBytes: 1000, maxBackups: 3);

            // The current log is moved aside; a fresh one is created on the next write (not here).
            Assert.False(File.Exists(_log));
            Assert.True(File.Exists(_log + ".1"));
            Assert.Equal(1000, new FileInfo(_log + ".1").Length);
        }

        [Fact]
        public void RotateLogIfNeeded_ShiftsBackups_AndDropsOldest()
        {
            // Existing backups .1 and .2, plus an over-limit current log; with maxBackups=2 the oldest
            // (.2) must be discarded, not promoted to .3.
            WriteBytes(_log, 1000);
            File.WriteAllText(_log + ".1", "first-backup");
            File.WriteAllText(_log + ".2", "second-backup");

            Logger.RotateLogIfNeeded(_log, maxBytes: 1000, maxBackups: 2);

            Assert.False(File.Exists(_log));
            Assert.False(File.Exists(_log + ".3"));            // never exceeds maxBackups
            Assert.Equal("first-backup", File.ReadAllText(_log + ".2"));  // .1 -> .2
            Assert.Equal(1000, new FileInfo(_log + ".1").Length);        // log -> .1
        }

        [Fact]
        public void RotateLogIfNeeded_NoFile_DoesNothing()
        {
            Logger.RotateLogIfNeeded(_log, maxBytes: 10, maxBackups: 3); // must not throw
            Assert.False(File.Exists(_log));
        }
    }
}
