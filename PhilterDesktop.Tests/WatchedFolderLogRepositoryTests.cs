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
using Xunit;

namespace PhilterDesktop.Tests
{
    public sealed class WatchedFolderLogRepositoryTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly LiteDatabase _db;
        private readonly WatchedFolderLogRepository _repo;

        public WatchedFolderLogRepositoryTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), "philter-folderlog-" + Guid.NewGuid().ToString("N") + ".db");
            _db = new LiteDatabase(_dbPath);
            _repo = new WatchedFolderLogRepository(_db);
        }

        public void Dispose()
        {
            _db.Dispose();
            try { File.Delete(_dbPath); } catch { /* best effort */ }
        }

        [Fact]
        public void Append_And_GetForFolder_NewestFirst_ScopedToFolder()
        {
            var folderA = ObjectId.NewObjectId();
            var folderB = ObjectId.NewObjectId();

            _repo.Append(folderA, "Info", "first");
            _repo.Append(folderA, "Info", "second");
            _repo.Append(folderB, "Error", "other folder");

            var a = _repo.GetForFolder(folderA);
            Assert.Equal(2, a.Count);
            Assert.Equal("second", a[0].Message); // newest first
            Assert.Equal("first", a[1].Message);

            var b = _repo.GetForFolder(folderB);
            Assert.Single(b);
            Assert.Equal("other folder", b[0].Message);
        }

        [Fact]
        public void DeleteForFolder_RemovesOnlyThatFolder()
        {
            var folderA = ObjectId.NewObjectId();
            var folderB = ObjectId.NewObjectId();
            _repo.Append(folderA, "Info", "a");
            _repo.Append(folderB, "Info", "b");

            _repo.DeleteForFolder(folderA);

            Assert.Empty(_repo.GetForFolder(folderA));
            Assert.Single(_repo.GetForFolder(folderB));
        }

        [Fact]
        public void Append_RetainsAllEntries_NoCap()
        {
            var folder = ObjectId.NewObjectId();
            const int total = 750;
            for (int i = 0; i < total; i++)
            {
                _repo.Append(folder, "Info", "entry " + i);
            }

            var entries = _repo.GetForFolder(folder);
            Assert.Equal(total, entries.Count); // nothing is capped/dropped
            // The newest entry is first (Id breaks ties when timestamps are equal).
            Assert.Equal("entry " + (total - 1), entries[0].Message);
        }

        [Fact]
        public void DeleteOlderThan_RemovesOnlyOldEntries()
        {
            var folder = ObjectId.NewObjectId();

            // An old entry (40 days) and a fresh entry.
            _repo.Insert(new WatchedFolderLogEntity
            {
                FolderId = folder,
                Level = "Info",
                Message = "old",
                Timestamp = DateTime.UtcNow.AddDays(-40)
            });
            _repo.Append(folder, "Info", "fresh");

            int removed = _repo.DeleteOlderThan(DateTime.UtcNow.AddDays(-WatchedFolderLogRepository.RetentionDays));

            Assert.Equal(1, removed);
            var remaining = _repo.GetForFolder(folder);
            Assert.Single(remaining);
            Assert.Equal("fresh", remaining[0].Message);
        }

        [Fact]
        public void PruneOldEntries_UsesThirtyDayRetention()
        {
            Assert.Equal(30, WatchedFolderLogRepository.RetentionDays);

            var folder = ObjectId.NewObjectId();
            _repo.Insert(new WatchedFolderLogEntity
            {
                FolderId = folder,
                Level = "Info",
                Message = "ancient",
                Timestamp = DateTime.UtcNow.AddDays(-31)
            });
            _repo.Append(folder, "Info", "recent");

            _repo.PruneOldEntries();

            var remaining = _repo.GetForFolder(folder);
            Assert.Single(remaining);
            Assert.Equal("recent", remaining[0].Message);
        }
    }
}
