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
}
