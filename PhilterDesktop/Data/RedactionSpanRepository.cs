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
}
