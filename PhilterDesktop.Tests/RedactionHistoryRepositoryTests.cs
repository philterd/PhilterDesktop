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
    public sealed class RedactionHistoryRepositoryTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly LiteDatabase _db;

        public RedactionHistoryRepositoryTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), "philter-history-" + Guid.NewGuid().ToString("N") + ".db");
            _db = new LiteDatabase(_dbPath);
        }

        public void Dispose()
        {
            _db.Dispose();
            try { File.Delete(_dbPath); } catch { /* best effort */ }
        }

        [Fact]
        public void Versions_OrderedAndNumbered_PerDocument()
        {
            var repo = new RedactionVersionRepository(_db);
            var docA = ObjectId.NewObjectId();
            var docB = ObjectId.NewObjectId();

            Assert.Equal(1, repo.NextVersionNumber(docA));
            repo.Insert(new RedactionVersionEntity { DocumentId = docA, Version = 1 });
            repo.Insert(new RedactionVersionEntity { DocumentId = docA, Version = 2 });
            repo.Insert(new RedactionVersionEntity { DocumentId = docB, Version = 1 });

            Assert.Equal(3, repo.NextVersionNumber(docA));
            var a = repo.GetForDocument(docA);
            Assert.Equal(2, a.Count);
            Assert.Equal(1, a[0].Version);
            Assert.Equal(2, a[1].Version);
            Assert.Single(repo.GetForDocument(docB));
        }

        [Fact]
        public void Spans_ScopedToVersion_AndDeletable()
        {
            var repo = new RedactionSpanRepository(_db);
            var v1 = ObjectId.NewObjectId();
            var v2 = ObjectId.NewObjectId();

            repo.Insert(new RedactionSpanEntity { VersionId = v1, Order = 1, Text = "b" });
            repo.Insert(new RedactionSpanEntity { VersionId = v1, Order = 0, Text = "a" });
            repo.Insert(new RedactionSpanEntity { VersionId = v2, Order = 0, Text = "other" });

            var spans = repo.GetForVersion(v1);
            Assert.Equal(2, spans.Count);
            Assert.Equal("a", spans[0].Text); // ordered by Order
            Assert.Equal("b", spans[1].Text);

            Assert.Equal(2, repo.DeleteForVersion(v1));
            Assert.Empty(repo.GetForVersion(v1));
            Assert.Single(repo.GetForVersion(v2));
        }
    }
}
