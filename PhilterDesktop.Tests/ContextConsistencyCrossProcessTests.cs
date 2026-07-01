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
    /// <summary>
    /// The context store keeps RANDOM_REPLACE mappings consistent across the GUI and CLI redacting the
    /// shared database at the same time. The in-process write lock can't cover two processes, so a unique
    /// composite (Context, Token) index guarantees a single row per pair. These simulate two "processes"
    /// with two separate LiteDB connections to one file (#546).
    /// </summary>
    public sealed class ContextConsistencyCrossProcessTests : IDisposable
    {
        private static readonly DateTime T0 = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private readonly string _path;

        public ContextConsistencyCrossProcessTests()
        {
            _path = Path.Combine(Path.GetTempPath(), "philter-ctx-xproc-" + Guid.NewGuid().ToString("N") + ".db");
        }

        public void Dispose()
        {
            try { File.Delete(_path); } catch { /* best effort */ }
        }

        private LiteDatabase Shared() => new($"Filename={_path};Connection=Shared");

        private static ContextEntryEntity Entry(string token, string replacement, DateTime updated) => new()
        {
            Context = "ctx",
            Token = token,
            Replacement = replacement,
            CreatedAtUtc = T0,
            UpdatedAtUtc = updated
        };

        [Fact]
        public void UniqueIndex_RejectsDuplicateContextTokenInsert_FromAnotherConnection()
        {
            using LiteDatabase dbA = Shared();
            using LiteDatabase dbB = Shared();
            var repoA = new ContextEntryRepository(dbA);
            var repoB = new ContextEntryRepository(dbB);

            repoA.Insert(Entry("tok", "A", T0));

            // A second raw insert of the same (context, token) from the other connection is rejected.
            Exception? ex = Record.Exception(() => repoB.Insert(Entry("tok", "B", T0)));
            Assert.IsType<LiteException>(ex);
        }

        [Fact]
        public void UniqueIndex_IsCaseInsensitive_MatchingLookup()
        {
            using LiteDatabase db = Shared();
            var repo = new ContextEntryRepository(db);

            repo.Insert(Entry("Smith", "A", T0));

            // FindEntry is case-insensitive (LiteDB collation); the unique index must agree, so "smith"
            // is treated as the same key and a raw insert is rejected.
            Exception? ex = Record.Exception(() => repo.Insert(Entry("smith", "B", T0)));
            Assert.IsType<LiteException>(ex);
        }

        [Fact]
        public void CrossProcess_ConcurrentPutSameToken_ResultsInExactlyOneRow()
        {
            using LiteDatabase dbA = Shared();
            using LiteDatabase dbB = Shared();
            // Two services with independent in-process locks == two processes with no shared lock.
            var svcA = new LiteDbContextService(new ContextEntryRepository(dbA));
            var svcB = new LiteDbContextService(new ContextEntryRepository(dbB));

            Parallel.For(0, 100, i =>
            {
                LiteDbContextService svc = (i % 2 == 0) ? svcA : svcB;
                svc.Put("ctx", "token", "val-" + (i % 2));
            });

            // Without the unique index the two connections would race the find-then-insert and leave
            // duplicate rows; the index collapses them to exactly one.
            using LiteDatabase dbC = Shared();
            Assert.Equal(1, new ContextEntryRepository(dbC).CountByContext("ctx"));
        }

        [Fact]
        public void CrossProcess_ConcurrentPutManyTokens_OneRowEach()
        {
            using LiteDatabase dbA = Shared();
            using LiteDatabase dbB = Shared();
            var svcA = new LiteDbContextService(new ContextEntryRepository(dbA));
            var svcB = new LiteDbContextService(new ContextEntryRepository(dbB));

            // Both "processes" write the same 50 tokens; each pair must end up as a single row.
            Parallel.For(0, 50, i =>
            {
                svcA.Put("ctx", "token-" + i, "a");
                svcB.Put("ctx", "token-" + i, "b");
            });

            using LiteDatabase dbC = Shared();
            Assert.Equal(50, new ContextEntryRepository(dbC).CountByContext("ctx"));
        }

        [Fact]
        public void OpeningRepository_DeduplicatesPreExistingRows_KeepingMostRecentlyWritten()
        {
            // Seed duplicate (ctx, tok) rows BEFORE the unique index exists, by writing to the raw
            // collection (a repository would create the index). This models a database from before the fix.
            using (var raw = new LiteDatabase($"Filename={_path};"))
            {
                ILiteCollection<ContextEntryEntity> col = raw.GetCollection<ContextEntryEntity>("contextentries");
                col.Insert(Entry("tok", "old", T0));
                col.Insert(Entry("tok", "new", T0.AddMinutes(5)));
            }

            // Opening the repository runs the migration: the unique index can't be created over the
            // duplicates, so they're deduped (keeping the newest) and the index is then built.
            using LiteDatabase db = new($"Filename={_path};");
            var repo = new ContextEntryRepository(db);

            Assert.Equal(1, repo.CountByContext("ctx"));
            Assert.Equal("new", repo.FindEntry("ctx", "tok")!.Replacement);
        }
    }
}
