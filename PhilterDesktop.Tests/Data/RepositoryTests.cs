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

namespace PhilterDesktop.Tests.Data
{
    /// <summary>
    /// Integration tests for the LiteDB repositories against a real (temp-file) database:
    /// the generic CRUD base plus each repository's custom queries.
    /// </summary>
    public sealed class RepositoryTests : IDisposable
    {
        private readonly string _path;
        private readonly LiteDatabase _db;

        public RepositoryTests()
        {
            _path = Path.Combine(Path.GetTempPath(), "philter-repo-tests-" + Guid.NewGuid().ToString("N") + ".db");
            _db = new LiteDatabase(_path);
        }

        public void Dispose()
        {
            _db.Dispose();
            try { File.Delete(_path); } catch { /* best effort */ }
        }

        [Fact]
        public void Crud_InsertGetUpdateDelete_Works()
        {
            var repo = new PolicyRepository(_db);
            var entity = new PolicyEntity { Name = "p1", Json = "{}" };

            repo.Insert(entity);
            Assert.Equal(1, repo.Count());

            PolicyEntity? fetched = repo.GetById(entity.Id);
            Assert.NotNull(fetched);
            Assert.Equal("p1", fetched!.Name);

            fetched.Name = "p1-renamed";
            Assert.True(repo.Update(fetched));
            Assert.Equal("p1-renamed", repo.GetById(entity.Id)!.Name);

            Assert.True(repo.Delete(entity.Id));
            Assert.Equal(0, repo.Count());
        }

        [Fact]
        public void PolicyRepository_FindByName()
        {
            var repo = new PolicyRepository(_db);
            repo.Insert(new PolicyEntity { Name = "alpha", Json = "{}" });
            repo.Insert(new PolicyEntity { Name = "beta", Json = "{}" });

            Assert.NotNull(repo.FindByName("beta"));
            Assert.Equal("beta", repo.FindByName("beta")!.Name);
            Assert.Null(repo.FindByName("missing"));
        }

        [Fact]
        public void ContextRepository_FindByName()
        {
            var repo = new ContextRepository(_db);
            repo.Insert(new ContextEntity { Name = "default" });

            Assert.NotNull(repo.FindByName("default"));
            Assert.Null(repo.FindByName("nope"));
        }

        [Fact]
        public void ContextEntryRepository_FindCountDeleteByContext()
        {
            var repo = new ContextEntryRepository(_db);
            repo.Insert(new ContextEntryEntity { Context = "c1", Token = "t1", Replacement = "r1" });
            repo.Insert(new ContextEntryEntity { Context = "c1", Token = "t2", Replacement = "r2" });
            repo.Insert(new ContextEntryEntity { Context = "c2", Token = "t3", Replacement = "r3" });

            Assert.Equal(2, repo.CountByContext("c1"));
            Assert.Single(repo.FindByContext("c2"));

            int deleted = repo.DeleteAllByContext("c1");
            Assert.Equal(2, deleted);
            Assert.Equal(0, repo.CountByContext("c1"));
            Assert.Equal(1, repo.CountByContext("c2"));
        }

        [Fact]
        public void SettingsRepository_GetSettings_CreatesDefaultsThenPersists()
        {
            var repo = new SettingsRepository(_db);

            SettingsEntity defaults = repo.GetSettings();
            Assert.True(defaults.OutputToOriginalLocation);
            Assert.Equal(1, defaults.Id);

            defaults.OutputToOriginalLocation = false;
            defaults.CustomOutputFolder = @"C:\out";
            defaults.LoggingEnabled = true;
            repo.SaveSettings(defaults);

            SettingsEntity reloaded = repo.GetSettings();
            Assert.False(reloaded.OutputToOriginalLocation);
            Assert.Equal(@"C:\out", reloaded.CustomOutputFolder);
            Assert.True(reloaded.LoggingEnabled);
            Assert.Equal(1, repo.Count()); // singleton, not duplicated
        }

        [Fact]
        public void RedactionQueueRepository_FilterByStatus_AndDeleteWhere()
        {
            var repo = new RedactionQueueRepository(_db);
            repo.Insert(new RedactionQueueEntity { Name = "a", Status = "Pending" });
            repo.Insert(new RedactionQueueEntity { Name = "b", Status = "Pending" });
            repo.Insert(new RedactionQueueEntity { Name = "c", Status = "Completed" });

            Assert.Equal(2, repo.Find(x => x.Status == "Pending").Count());

            int removed = repo.DeleteWhere(x => x.Status == "Completed");
            Assert.Equal(1, removed);
            Assert.Equal(2, repo.Count());
        }

        [Fact]
        public void NullArguments_AreRejected()
        {
            var repo = new PolicyRepository(_db);
            Assert.Throws<ArgumentNullException>(() => repo.Insert(null!));
            Assert.Throws<ArgumentException>(() => repo.FindByName(""));
        }
    }
}
