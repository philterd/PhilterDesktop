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
    public sealed class RedactionHistoryTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly LiteDatabase _db;
        private readonly RedactionQueueRepository _queue;
        private readonly RedactionVersionRepository _versions;
        private readonly RedactionSpanRepository _spans;

        public RedactionHistoryTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), "philter-history-" + Guid.NewGuid().ToString("N") + ".db");
            _db = new LiteDatabase(_dbPath);
            _queue = new RedactionQueueRepository(_db);
            _versions = new RedactionVersionRepository(_db);
            _spans = new RedactionSpanRepository(_db);
        }

        public void Dispose()
        {
            _db.Dispose();
            try { File.Delete(_dbPath); } catch { /* best effort */ }
        }

        [Fact]
        public void ClearAll_RemovesVersionsSpansAndCompletedItems_KeepsActiveItems()
        {
            // Two completed documents (each with a version + spans), one pending, one processing.
            var done1 = new RedactionQueueEntity { Name = "a.txt", Status = "Completed" };
            var done2 = new RedactionQueueEntity { Name = "b.txt", Status = "Completed" };
            var pending = new RedactionQueueEntity { Name = "c.txt", Status = "Pending" };
            var processing = new RedactionQueueEntity { Name = "d.txt", Status = "Processing" };
            _queue.Insert(done1);
            _queue.Insert(done2);
            _queue.Insert(pending);
            _queue.Insert(processing);

            foreach (RedactionQueueEntity doc in new[] { done1, done2 })
            {
                var version = new RedactionVersionEntity { DocumentId = doc.Id, Version = 1, SourcePath = doc.Name };
                _versions.Insert(version);
                _spans.InsertBulk(new[]
                {
                    new RedactionSpanEntity { VersionId = version.Id, Text = "secret", Replacement = "[X]" }
                });
            }

            Assert.Equal(2, _versions.Count());
            Assert.Equal(2, _spans.Count());
            Assert.Equal(4, _queue.Count());

            RedactionHistory.ClearAll(_spans, _versions, _queue);

            // History wiped entirely.
            Assert.Equal(0, _versions.Count());
            Assert.Equal(0, _spans.Count());

            // Completed items removed; pending + processing kept.
            List<RedactionQueueEntity> remaining = _queue.GetAll().ToList();
            Assert.Equal(2, remaining.Count);
            Assert.Contains(remaining, x => x.Status == "Pending");
            Assert.Contains(remaining, x => x.Status == "Processing");
            Assert.DoesNotContain(remaining, x => x.Status == "Completed");
        }

        // --- SaveVersionWithSpans atomicity (#547) ------------------------------------------------

        private static RedactionVersionEntity NewVersion(string src = "a.txt") =>
            new() { DocumentId = ObjectId.NewObjectId(), Version = 1, SourcePath = src, OutputPath = src + ".red" };

        [Fact]
        public void SaveVersionWithSpans_PersistsVersionAndSpansAtomically()
        {
            RedactionVersionEntity version = NewVersion();
            var spans = new List<RedactionSpanEntity>
            {
                new() { VersionId = version.Id, Text = "alice@example.com", Replacement = "[E]", Order = 0 },
                new() { VersionId = version.Id, Text = "078-05-1120", Replacement = "[S]", Order = 1 },
            };

            RedactionHistory.SaveVersionWithSpans(_db, _versions, _spans, version, spans);

            Assert.NotNull(_versions.GetById(version.Id));
            Assert.Equal(2, _spans.Find(x => x.VersionId == version.Id).Count());
        }

        [Fact]
        public void SaveVersionWithSpans_NoSpans_PersistsVersionOnly()
        {
            RedactionVersionEntity version = NewVersion();

            RedactionHistory.SaveVersionWithSpans(_db, _versions, _spans, version, new List<RedactionSpanEntity>());

            Assert.NotNull(_versions.GetById(version.Id));
            Assert.Equal(0, _spans.Count());
        }

        [Fact]
        public void SaveVersionWithSpans_RollsBackVersion_WhenSpanWriteFails()
        {
            RedactionVersionEntity version = NewVersion();
            ObjectId dupId = ObjectId.NewObjectId();
            var spans = new List<RedactionSpanEntity>
            {
                new() { Id = dupId, VersionId = version.Id, Text = "a", Order = 0 },
                new() { Id = dupId, VersionId = version.Id, Text = "b", Order = 1 }, // duplicate _id -> bulk insert throws
            };

            Assert.ThrowsAny<Exception>(() =>
                RedactionHistory.SaveVersionWithSpans(_db, _versions, _spans, version, spans));

            // The whole save is rolled back: no orphaned span-less version, and no partial spans.
            Assert.Null(_versions.GetById(version.Id));
            Assert.Equal(0, _spans.Count());
        }

        [Fact]
        public void SaveVersionWithSpans_FailureDoesNotAffectPriorVersions()
        {
            // A previously saved version stays intact when a later save fails and rolls back.
            RedactionVersionEntity good = NewVersion("good.txt");
            RedactionHistory.SaveVersionWithSpans(_db, _versions, _spans, good,
                new List<RedactionSpanEntity> { new() { VersionId = good.Id, Text = "x", Order = 0 } });

            RedactionVersionEntity bad = NewVersion("bad.txt");
            ObjectId dupId = ObjectId.NewObjectId();
            var dupSpans = new List<RedactionSpanEntity>
            {
                new() { Id = dupId, VersionId = bad.Id, Order = 0 },
                new() { Id = dupId, VersionId = bad.Id, Order = 1 },
            };
            Assert.ThrowsAny<Exception>(() =>
                RedactionHistory.SaveVersionWithSpans(_db, _versions, _spans, bad, dupSpans));

            Assert.NotNull(_versions.GetById(good.Id));                 // earlier version survives
            Assert.Null(_versions.GetById(bad.Id));                     // failed one rolled back
            Assert.Equal(1, _spans.Find(x => x.VersionId == good.Id).Count());
        }
    }
}
