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

namespace PhilterData
{
    /// <summary>
    /// Stores per-watched-folder activity entries. There is no size cap; entries are pruned by age
    /// (see <see cref="RetentionDays"/>) and removed when a folder is removed or its log is cleared.
    /// </summary>
    public class WatchedFolderLogRepository : LiteDbRepository<WatchedFolderLogEntity>
    {
        /// <summary>Entries older than this many days are removed automatically.</summary>
        public const int RetentionDays = 30;

        public WatchedFolderLogRepository(LiteDatabase database) : base(database, "watched_folder_logs")
        {
        }

        public WatchedFolderLogRepository(string databasePath) : base(databasePath, "watched_folder_logs")
        {
        }

        protected override void ConfigureIndexes()
        {
            EnsureIndex(x => x.FolderId);
        }

        /// <summary>Returns a folder's entries, newest first. Id breaks ties between equal timestamps.</summary>
        public IReadOnlyList<WatchedFolderLogEntity> GetForFolder(ObjectId folderId) =>
            Find(x => x.FolderId == folderId)
                .OrderByDescending(x => x.Timestamp)
                .ThenByDescending(x => x.Id)
                .ToList();

        /// <summary>Deletes all entries for a folder.</summary>
        public int DeleteForFolder(ObjectId folderId) =>
            DeleteWhere(x => x.FolderId == folderId);

        /// <summary>Deletes entries older than <paramref name="cutoffUtc"/>. Returns the number removed.</summary>
        public int DeleteOlderThan(DateTime cutoffUtc) =>
            DeleteWhere(x => x.Timestamp < cutoffUtc);

        /// <summary>Removes entries older than <see cref="RetentionDays"/> days. Returns the number removed.</summary>
        public int PruneOldEntries() =>
            DeleteOlderThan(DateTime.UtcNow.AddDays(-RetentionDays));

        /// <summary>Appends an entry for a folder.</summary>
        public void Append(ObjectId folderId, string level, string message)
        {
            Insert(new WatchedFolderLogEntity
            {
                FolderId = folderId,
                Level = level,
                Message = message,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
