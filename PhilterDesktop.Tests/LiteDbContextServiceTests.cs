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
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The durable, LiteDB-backed <see cref="IContextService"/> keeps RANDOM_REPLACE replacements
    /// consistent across documents and app restarts (replacing the non-durable in-memory store).
    /// </summary>
    public sealed class LiteDbContextServiceTests : IDisposable
    {
        private readonly string _dbPath;

        public LiteDbContextServiceTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), "ctxsvc-" + Guid.NewGuid().ToString("N") + ".db");
        }

        public void Dispose()
        {
            try { File.Delete(_dbPath); } catch { /* best effort */ }
        }

        private LiteDbContextService Service(LiteDatabase db) => new(new ContextEntryRepository(db));

        [Fact]
        public void Get_ReturnsNull_WhenAbsent()
        {
            using var db = new LiteDatabase(_dbPath);
            Assert.Null(Service(db).Get("ctx", "token"));
        }

        [Fact]
        public void PutThenGet_ReturnsReplacement()
        {
            using var db = new LiteDatabase(_dbPath);
            var svc = Service(db);
            svc.Put("ctx", "123-45-6789", "REPL-1");
            Assert.Equal("REPL-1", svc.Get("ctx", "123-45-6789"));
        }

        [Fact]
        public void Put_Overwrites_ExistingValue_WithoutDuplicating()
        {
            using var db = new LiteDatabase(_dbPath);
            var svc = Service(db);
            svc.Put("ctx", "token", "first");
            svc.Put("ctx", "token", "second");

            Assert.Equal("second", svc.Get("ctx", "token"));
            Assert.Equal(1, new ContextEntryRepository(db).CountByContext("ctx")); // updated, not duplicated
        }

        [Fact]
        public void Get_IsolatesValuesByContext()
        {
            using var db = new LiteDatabase(_dbPath);
            var svc = Service(db);
            svc.Put("ctx1", "token", "value1");
            svc.Put("ctx2", "token", "value2");

            Assert.Equal("value1", svc.Get("ctx1", "token"));
            Assert.Equal("value2", svc.Get("ctx2", "token"));
        }

        [Fact]
        public void Get_ReturnsNull_ForTokenStoredOnlyInAnotherContext()
        {
            using var db = new LiteDatabase(_dbPath);
            var svc = Service(db);
            svc.Put("ctx1", "token", "value1");

            Assert.Null(svc.Get("ctx2", "token")); // same token, different context => miss
        }

        [Fact]
        public void Get_SelectsTheCorrectToken_AmongManyInTheSameContext()
        {
            // Exercises the "&& Token ==" half of the lookup: several tokens coexist in one context and
            // each must resolve to its own replacement (not a neighbour's).
            using var db = new LiteDatabase(_dbPath);
            var svc = Service(db);
            for (int i = 0; i < 25; i++)
            {
                svc.Put("ctx", "token-" + i, "value-" + i);
            }

            Assert.Equal("value-0", svc.Get("ctx", "token-0"));
            Assert.Equal("value-7", svc.Get("ctx", "token-7"));
            Assert.Equal("value-24", svc.Get("ctx", "token-24"));
            Assert.Null(svc.Get("ctx", "token-25"));
            Assert.Equal(25, new ContextEntryRepository(db).CountByContext("ctx"));
        }

        [Fact]
        public void Put_OverwritingOneContext_DoesNotAffectAnother()
        {
            using var db = new LiteDatabase(_dbPath);
            var svc = Service(db);
            svc.Put("ctx1", "token", "first");
            svc.Put("ctx2", "token", "other");
            svc.Put("ctx1", "token", "second"); // overwrite only ctx1

            Assert.Equal("second", svc.Get("ctx1", "token"));
            Assert.Equal("other", svc.Get("ctx2", "token"));
        }

        [Theory]
        [InlineData("O'Brien, \"Pat\"")]
        [InlineData("a$.b[0] == c")]            // looks like a LiteDB/BSON expression
        [InlineData("line1\nline2\twith\ttabs")]
        [InlineData("emoji 🙂 and accents éàü")]
        [InlineData("123-45-6789")]
        public void PutThenGet_RoundTripsTokensWithSpecialCharacters(string token)
        {
            using var db = new LiteDatabase(_dbPath);
            var svc = Service(db);
            svc.Put("ctx", token, "REPL");

            Assert.Equal("REPL", svc.Get("ctx", token));
        }

        [Fact]
        public void Mappings_PersistAcrossReopeningTheDatabase()
        {
            // Durability: write with one database handle, close it, reopen the same file, and the mapping
            // is still there — i.e. it survives an app restart.
            using (var db = new LiteDatabase(_dbPath))
            {
                Service(db).Put("ctx", "token", "durable");
            }
            using (var db2 = new LiteDatabase(_dbPath))
            {
                Assert.Equal("durable", Service(db2).Get("ctx", "token"));
            }
        }

        [Fact]
        public void DeletingContextEntries_RemovesThem()
        {
            using var db = new LiteDatabase(_dbPath);
            var repo = new ContextEntryRepository(db);
            var svc = new LiteDbContextService(repo);
            svc.Put("ctx", "token", "value");

            repo.DeleteAllByContext("ctx"); // what the Contexts form does when a context is deleted

            Assert.Null(svc.Get("ctx", "token"));
            Assert.Equal(0, repo.CountByContext("ctx"));
        }

        [Fact]
        public void ConcurrentPut_OfSameToken_KeepsASingleEntry()
        {
            using var db = new LiteDatabase(_dbPath);
            var svc = Service(db);

            Parallel.For(0, 50, _ => svc.Put("ctx", "token", "value"));

            Assert.Equal("value", svc.Get("ctx", "token"));
            Assert.Equal(1, new ContextEntryRepository(db).CountByContext("ctx"));
        }

        [Fact]
        public void Tokens_AreMatchedCaseInsensitively()
        {
            // Pins behavior: LiteDB's default collation is case-insensitive, so a token differing only by
            // case resolves to the same entry (the second Put updates the first rather than adding a new
            // mapping). This guards against an accidental collation change that would silently split a
            // token's consistent replacement in two.
            using var db = new LiteDatabase(_dbPath);
            var svc = Service(db);
            svc.Put("ctx", "Smith", "A");
            svc.Put("ctx", "smith", "B");

            Assert.Equal("B", svc.Get("ctx", "Smith"));
            Assert.Equal("B", svc.Get("ctx", "smith"));
            Assert.Equal(1, new ContextEntryRepository(db).CountByContext("ctx"));
        }

        [Fact]
        public void ConcurrentPut_OfDistinctTokens_PersistsEveryWrite()
        {
            // Distinct tokens written from many threads (e.g. parallel watched-folder redactions) must
            // all survive — the write lock serializes the read-modify-write so none are lost.
            using var db = new LiteDatabase(_dbPath);
            var svc = Service(db);

            Parallel.For(0, 200, i => svc.Put("ctx", "token-" + i, "value-" + i));

            Assert.Equal(200, new ContextEntryRepository(db).CountByContext("ctx"));
            Assert.Equal("value-0", svc.Get("ctx", "token-0"));
            Assert.Equal("value-199", svc.Get("ctx", "token-199"));
        }

        [Fact]
        public void Put_StampsCreatedAndUpdated_OnFirstWrite()
        {
            var clock = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            using var db = new LiteDatabase(_dbPath);
            var repo = new ContextEntryRepository(db, () => clock);
            new LiteDbContextService(repo).Put("ctx", "token", "value");

            ContextEntryEntity entry = repo.FindEntry("ctx", "token")!;
            Assert.Equal(clock, entry.CreatedAtUtc);
            Assert.Equal(clock, entry.UpdatedAtUtc); // equal until first update
            Assert.Equal(DateTimeKind.Utc, entry.CreatedAtUtc.Kind); // normalized back to UTC on read
            Assert.Equal(DateTimeKind.Utc, entry.UpdatedAtUtc.Kind);
        }

        [Fact]
        public void Put_AdvancesUpdatedAt_ButPreservesCreatedAt_OnOverwrite()
        {
            var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            using var db = new LiteDatabase(_dbPath);
            var repo = new ContextEntryRepository(db, () => now); // mutable closure variable
            var svc = new LiteDbContextService(repo);

            svc.Put("ctx", "token", "first");
            DateTime created = repo.FindEntry("ctx", "token")!.CreatedAtUtc;

            now = now.AddMinutes(30); // time passes
            svc.Put("ctx", "token", "second");

            ContextEntryEntity entry = repo.FindEntry("ctx", "token")!;
            Assert.Equal("second", entry.Replacement);
            Assert.Equal(created, entry.CreatedAtUtc);                          // creation time unchanged
            Assert.Equal(now, entry.UpdatedAtUtc);                             // moved to the new write time
            Assert.True(entry.UpdatedAtUtc > entry.CreatedAtUtc);
        }

        [Fact]
        public void Timestamps_SurviveReopeningTheDatabase()
        {
            var clock = new DateTime(2026, 5, 6, 7, 8, 9, DateTimeKind.Utc);
            using (var db = new LiteDatabase(_dbPath))
            {
                new LiteDbContextService(new ContextEntryRepository(db, () => clock)).Put("ctx", "token", "value");
            }
            using (var db2 = new LiteDatabase(_dbPath))
            {
                ContextEntryEntity entry = new ContextEntryRepository(db2).FindEntry("ctx", "token")!;
                Assert.Equal(clock, entry.CreatedAtUtc);
                Assert.Equal(clock, entry.UpdatedAtUtc);
            }
        }

        [Fact]
        public void FilterService_WithDurableService_GivesConsistentReplacements_AndPersistsThem()
        {
            var policy = new PhileasPolicy
            {
                Name = "test",
                Identifiers = new Identifiers
                {
                    Ssn = new Ssn
                    {
                        Strategies = new List<Phileas.Policy.Filters.Strategies.SsnFilterStrategy>
                        {
                            new()
                            {
                                Strategy = Phileas.Policy.Filters.Strategies.AbstractFilterStrategy.RandomReplace,
                                ReplacementScope = Phileas.Policy.Filters.Strategies.AbstractFilterStrategy.ReplacementScopeContext
                            }
                        }
                    }
                }
            };

            using var db = new LiteDatabase(_dbPath);
            var fs = new FilterService(Service(db));

            var r1 = fs.Filter(policy, "ctx", 0, "SSN: 123-45-6789");
            var r2 = fs.Filter(policy, "ctx", 0, "SSN: 123-45-6789");

            Assert.NotEmpty(r1.Spans);
            Assert.Equal(r1.Spans[0].Replacement, r2.Spans[0].Replacement); // consistent across calls
            Assert.Equal(1, new ContextEntryRepository(db).CountByContext("ctx")); // stored durably
        }
    }
}
