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
    /// Repository for managing <see cref="ContextEntity"/> instances in LiteDB.
    /// Includes indexes on Name and CreatedAt fields.
    /// </summary>
    public sealed class ContextRepository : LiteDbRepository<ContextEntity>
    {
        public ContextRepository(LiteDatabase database)
            : base(database, "contexts")
        {
        }

        protected override void ConfigureIndexes()
        {
            // Index on Name for fast lookup by policy name
            EnsureIndex(x => x.Name, unique: false);

            // Index on CreatedAt for sorting and date-based queries
            EnsureIndex(x => x.CreatedAt, unique: false);
        }

        /// <summary>
        /// Finds a context by its exact name.
        /// </summary>
        public ContextEntity? FindByName(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            return FindOne(x => x.Name == name);
        }

        /// <summary>
        /// Returns all contexts ordered by creation date (newest first).
        /// </summary>
        public IEnumerable<ContextEntity> GetAllOrderedByDate()
        {
            return GetAll().OrderByDescending(x => x.CreatedAt);
        }
    }

    /// <summary>
    /// Repository for managing <see cref="PolicyEntity"/> instances in LiteDB.
    /// Includes indexes on Name and CreatedAt fields.
    /// </summary>
    public sealed class PolicyRepository : LiteDbRepository<PolicyEntity>
    {
        public PolicyRepository(LiteDatabase database)
            : base(database, "policies")
        {
        }

        protected override void ConfigureIndexes()
        {
            // Index on Name for fast lookup by policy name
            EnsureIndex(x => x.Name, unique: false);

            // Index on CreatedAt for sorting and date-based queries
            EnsureIndex(x => x.CreatedAt, unique: false);
        }

        /// <summary>
        /// Finds a policy by its exact name.
        /// </summary>
        public PolicyEntity? FindByName(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            return FindOne(x => x.Name == name);
        }

        /// <summary>
        /// Returns all policies ordered by creation date (newest first).
        /// </summary>
        public IEnumerable<PolicyEntity> GetAllOrderedByDate()
        {
            return GetAll().OrderByDescending(x => x.CreatedAt);
        }
    }

    /// <summary>
    /// Repository for managing application settings in LiteDB.
    /// </summary>
    public class RedactionQueueRepository : LiteDbRepository<RedactionQueueEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsRepository"/> class with a shared database.
        /// </summary>
        /// <param name="database">The shared LiteDatabase instance.</param>
        public RedactionQueueRepository(LiteDatabase database) : base(database, "redaction_queue")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsRepository"/> class with a database path.
        /// </summary>
        /// <param name="databasePath">The path to the LiteDB database file.</param>
        public RedactionQueueRepository(string databasePath) : base(databasePath, "redaction_queue")
        {
        }

    }

    /// <summary>Repository for redaction spans (one-to-many under a version).</summary>
    public class RedactionSpanRepository : LiteDbRepository<RedactionSpanEntity>
    {
        public RedactionSpanRepository(LiteDatabase database) : base(database, "redaction_spans")
        {
        }

        public RedactionSpanRepository(string databasePath) : base(databasePath, "redaction_spans")
        {
        }

        protected override void ConfigureIndexes()
        {
            EnsureIndex(x => x.VersionId);
        }

        /// <summary>Returns a version's spans in their stored order.</summary>
        public IReadOnlyList<RedactionSpanEntity> GetForVersion(ObjectId versionId) =>
            Find(x => x.VersionId == versionId)
                .OrderBy(x => x.Order)
                .ToList();

        public int DeleteForVersion(ObjectId versionId) =>
            DeleteWhere(x => x.VersionId == versionId);
    }

    /// <summary>Repository for redaction versions (one-to-many under a document).</summary>
    public class RedactionVersionRepository : LiteDbRepository<RedactionVersionEntity>
    {
        public RedactionVersionRepository(LiteDatabase database) : base(database, "redaction_versions")
        {
        }

        public RedactionVersionRepository(string databasePath) : base(databasePath, "redaction_versions")
        {
        }

        protected override void ConfigureIndexes()
        {
            EnsureIndex(x => x.DocumentId);
        }

        /// <summary>Returns a document's versions, oldest first.</summary>
        public IReadOnlyList<RedactionVersionEntity> GetForDocument(ObjectId documentId) =>
            Find(x => x.DocumentId == documentId)
                .OrderBy(x => x.Version)
                .ToList();

        /// <summary>The next version number to assign for a document (1 if none exist).</summary>
        public int NextVersionNumber(ObjectId documentId)
        {
            var versions = Find(x => x.DocumentId == documentId).ToList();
            return versions.Count == 0 ? 1 : versions.Max(v => v.Version) + 1;
        }

        public int DeleteForDocument(ObjectId documentId) =>
            DeleteWhere(x => x.DocumentId == documentId);
    }

    /// <summary>
    /// Repository for managing application settings in LiteDB.
    /// </summary>
    public class SettingsRepository : LiteDbRepository<SettingsEntity>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsRepository"/> class with a shared database.
        /// </summary>
        /// <param name="database">The shared LiteDatabase instance.</param>
        public SettingsRepository(LiteDatabase database) : base(database, "settings")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsRepository"/> class with a database path.
        /// </summary>
        /// <param name="databasePath">The path to the LiteDB database file.</param>
        public SettingsRepository(string databasePath) : base(databasePath, "settings")
        {
        }

        /// <summary>
        /// Gets the application settings. If no settings exist, creates and returns default settings.
        /// </summary>
        /// <returns>The application settings entity.</returns>
        public SettingsEntity GetSettings()
        {
            // Settings should always have ID = 1 (singleton pattern)
            var settings = GetById(1);

            if (settings == null)
            {
                // Create default settings
                settings = new SettingsEntity
                {
                    Id = 1,
                    OutputToOriginalLocation = true,
                    CustomOutputFolder = string.Empty,
                    LastModified = DateTime.UtcNow
                };
                Insert(settings);
            }

            return settings;
        }

        /// <summary>
        /// Saves the application settings.
        /// </summary>
        /// <param name="settings">The settings entity to save.</param>
        public void SaveSettings(SettingsEntity settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            settings.Id = 1; // Ensure we're always updating the singleton record
            settings.LastModified = DateTime.UtcNow;
            Upsert(settings);
        }
    }

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

        /// <summary>
        /// Counts a folder's entries whose <see cref="WatchedFolderLogEntity.Level"/> matches any of the
        /// given levels (case-insensitive) — used to surface a failure/warning indicator on the
        /// watched-folder list. Returns 0 when no levels are supplied.
        /// </summary>
        public int CountByLevels(ObjectId folderId, params string[] levels)
        {
            if (levels is null || levels.Length == 0)
            {
                return 0;
            }
            var wanted = new HashSet<string>(levels, StringComparer.OrdinalIgnoreCase);
            return Find(x => x.FolderId == folderId).Count(e => wanted.Contains(e.Level));
        }

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

    /// <summary>
    /// Repository for managing watched folders in LiteDB.
    /// </summary>
    public class WatchedFolderRepository : LiteDbRepository<WatchedFolderEntity>
    {
        /// <summary>
        /// Initializes a new instance with a shared database.
        /// </summary>
        /// <param name="database">The shared LiteDatabase instance.</param>
        public WatchedFolderRepository(LiteDatabase database) : base(database, "watched_folders")
        {
        }

        /// <summary>
        /// Initializes a new instance with a database path.
        /// </summary>
        /// <param name="databasePath">The path to the LiteDB database file.</param>
        public WatchedFolderRepository(string databasePath) : base(databasePath, "watched_folders")
        {
        }
    }
}
