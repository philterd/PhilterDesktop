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
    /// Tests the rule that each watched folder must have a unique output directory (so two folders can't
    /// write same-named redacted files into one directory and overwrite each other).
    /// </summary>
    public sealed class WatchedFolderValidationTests
    {
        private static WatchedFolderEntity Folder(string output) =>
            new() { FolderPath = @"C:\in\" + Guid.NewGuid().ToString("N"), OutputFolder = output };

        [Fact]
        public void OutputConflict_SameOutput_DifferentFolder_IsConflict()
        {
            var existing = new[] { Folder(@"C:\out") };
            WatchedFolderEntity? conflict = WatchedFolderValidation.OutputConflict(existing, @"C:\out", ObjectId.NewObjectId());
            Assert.NotNull(conflict);
        }

        [Fact]
        public void OutputConflict_UniqueOutput_IsNoConflict()
        {
            var existing = new[] { Folder(@"C:\out\a") };
            Assert.Null(WatchedFolderValidation.OutputConflict(existing, @"C:\out\b", ObjectId.NewObjectId()));
        }

        [Fact]
        public void OutputConflict_EmptyOutput_IsNoConflict()
        {
            // An empty output folder means "write beside the source", which can't collide across folders.
            var existing = new[] { Folder(string.Empty) };
            Assert.Null(WatchedFolderValidation.OutputConflict(existing, string.Empty, ObjectId.NewObjectId()));
        }

        [Fact]
        public void OutputConflict_EditingSameFolder_IsNotSelfConflict()
        {
            var self = Folder(@"C:\out");
            // Re-saving the same folder (its own Id) with the same output must not conflict with itself.
            Assert.Null(WatchedFolderValidation.OutputConflict(new[] { self }, @"C:\out", self.Id));
        }

        [Fact]
        public void OutputConflict_IgnoresCaseAndTrailingSeparator()
        {
            var existing = new[] { Folder(@"C:\Out") };
            Assert.NotNull(WatchedFolderValidation.OutputConflict(existing, @"c:\out\", ObjectId.NewObjectId()));
        }

        // --- output must not overlap a watched folder (redaction-loop guard) ---

        private static WatchedFolderEntity WatchedAt(string folderPath) =>
            new() { FolderPath = folderPath, OutputFolder = @"C:\somewhere\" + Guid.NewGuid().ToString("N") };

        [Fact]
        public void OutputOverlaps_OutputIsAnotherWatchedFolder_IsOverlap()
        {
            var existing = new[] { WatchedAt(@"C:\watch2") };
            Assert.NotNull(WatchedFolderValidation.OutputOverlapsWatchedFolder(existing, @"C:\watch2", ObjectId.NewObjectId()));
        }

        [Fact]
        public void OutputOverlaps_OutputNestedInsideWatchedFolder_IsOverlap()
        {
            var existing = new[] { WatchedAt(@"C:\watch2") };
            Assert.NotNull(WatchedFolderValidation.OutputOverlapsWatchedFolder(existing, @"C:\watch2\sub\deep", ObjectId.NewObjectId()));
        }

        [Fact]
        public void OutputOverlaps_OutputContainsAWatchedFolder_IsOverlap()
        {
            // The watched folder is nested inside the candidate output -> redacted files could still be watched.
            var existing = new[] { WatchedAt(@"C:\out\inner") };
            Assert.NotNull(WatchedFolderValidation.OutputOverlapsWatchedFolder(existing, @"C:\out", ObjectId.NewObjectId()));
        }

        [Fact]
        public void OutputOverlaps_Unrelated_IsNoOverlap()
        {
            var existing = new[] { WatchedAt(@"C:\watch2") };
            Assert.Null(WatchedFolderValidation.OutputOverlapsWatchedFolder(existing, @"C:\totally\elsewhere", ObjectId.NewObjectId()));
        }

        [Fact]
        public void OutputOverlaps_ExcludesSelf()
        {
            var self = new WatchedFolderEntity { FolderPath = @"C:\watch1", OutputFolder = @"C:\out1" };
            // A folder never conflicts with its own watched path via this cross-folder check.
            Assert.Null(WatchedFolderValidation.OutputOverlapsWatchedFolder(new[] { self }, @"C:\watch1", self.Id));
        }

        [Fact]
        public void OutputOverlaps_EmptyOutput_IsNoOverlap()
        {
            var existing = new[] { WatchedAt(@"C:\watch2") };
            Assert.Null(WatchedFolderValidation.OutputOverlapsWatchedFolder(existing, string.Empty, ObjectId.NewObjectId()));
        }
    }
}
