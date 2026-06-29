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
    /// Deleting a policy that a watched folder uses would orphan the folder (it then silently fails
    /// every file). The policy editor uses these helpers to find affected folders and reassign them to
    /// the default policy (philterd-website issue #491).
    /// </summary>
    public sealed class WatchedFolderPolicyGuardTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly LiteDatabase _db;
        private readonly WatchedFolderRepository _repo;

        public WatchedFolderPolicyGuardTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), "wfguard-" + Guid.NewGuid().ToString("N") + ".db");
            _db = new LiteDatabase(_dbPath);
            _repo = new WatchedFolderRepository(_db);
        }

        public void Dispose()
        {
            _db.Dispose();
            try { File.Delete(_dbPath); } catch { /* best effort */ }
        }

        private void Add(string folderPath, string policy) =>
            _repo.Insert(new WatchedFolderEntity { FolderPath = folderPath, Policy = policy, Context = "default" });

        [Fact]
        public void FoldersUsing_ReturnsOnlyMatchingFolders()
        {
            Add(@"C:\medical", "medical");
            Add(@"C:\finance", "finance");
            Add(@"C:\more-medical", "medical");

            List<WatchedFolderEntity> using_ = WatchedFolderPolicyGuard.FoldersUsing(_repo, "medical");

            Assert.Equal(2, using_.Count);
            Assert.All(using_, f => Assert.Equal("medical", f.Policy));
        }

        [Fact]
        public void FoldersUsing_IsCaseInsensitive()
        {
            Add(@"C:\a", "Medical");

            Assert.Single(WatchedFolderPolicyGuard.FoldersUsing(_repo, "medical"));
        }

        [Fact]
        public void FoldersUsing_NoMatch_ReturnsEmpty()
        {
            Add(@"C:\a", "finance");

            Assert.Empty(WatchedFolderPolicyGuard.FoldersUsing(_repo, "medical"));
        }

        [Fact]
        public void FoldersUsing_EmptyRepository_ReturnsEmpty()
        {
            Assert.Empty(WatchedFolderPolicyGuard.FoldersUsing(_repo, "anything"));
        }

        [Fact]
        public void ReassignToDefault_SwitchesOnlyTheGivenFolders_AndPersists()
        {
            Add(@"C:\medical", "medical");
            Add(@"C:\finance", "finance");

            List<WatchedFolderEntity> using_ = WatchedFolderPolicyGuard.FoldersUsing(_repo, "medical");
            WatchedFolderPolicyGuard.ReassignToDefault(_repo, using_);

            // Reloaded from the database: the medical folder is now default, finance is untouched.
            List<WatchedFolderEntity> all = _repo.GetAll().ToList();
            Assert.Equal("default", all.Single(f => f.FolderPath == @"C:\medical").Policy);
            Assert.Equal("finance", all.Single(f => f.FolderPath == @"C:\finance").Policy);
        }

        [Fact]
        public void ReassignToDefault_LeavesNoFolderReferencingTheDeletedPolicy()
        {
            Add(@"C:\a", "medical");
            Add(@"C:\b", "medical");

            WatchedFolderPolicyGuard.ReassignToDefault(_repo, WatchedFolderPolicyGuard.FoldersUsing(_repo, "medical"));

            Assert.Empty(WatchedFolderPolicyGuard.FoldersUsing(_repo, "medical"));
            Assert.Equal(2, WatchedFolderPolicyGuard.FoldersUsing(_repo, "default").Count);
        }

        [Fact]
        public void ReassignToDefault_EmptyList_IsNoOp()
        {
            Add(@"C:\a", "finance");

            WatchedFolderPolicyGuard.ReassignToDefault(_repo, new List<WatchedFolderEntity>());

            Assert.Equal("finance", _repo.GetAll().Single().Policy);
        }
    }
}
