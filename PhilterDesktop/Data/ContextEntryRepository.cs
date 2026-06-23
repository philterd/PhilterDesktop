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
    /// Repository for managing <see cref="ContextEntryEntity"/> instances in LiteDB.
    /// Includes indexes on Context field for efficient filtering.
    /// </summary>
    public sealed class ContextEntryRepository : LiteDbRepository<ContextEntryEntity>
    {
        public ContextEntryRepository(LiteDatabase database) 
            : base(database, "contextentries")
        {
        }

        protected override void ConfigureIndexes()
        {
            // Index on Context for fast lookup by context name
            EnsureIndex(x => x.Context, unique: false);
            
            // Index on Token for fast lookup
            EnsureIndex(x => x.Token, unique: false);
        }

        /// <summary>
        /// Finds all context entries for a given context name.
        /// </summary>
        public IEnumerable<ContextEntryEntity> FindByContext(string contextName)
        {
            ArgumentException.ThrowIfNullOrEmpty(contextName);
            return Find(x => x.Context == contextName);
        }

        /// <summary>
        /// Deletes all context entries for a given context name.
        /// </summary>
        /// <param name="contextName">The name of the context to empty.</param>
        /// <returns>The number of entries deleted.</returns>
        public int DeleteAllByContext(string contextName)
        {
            ArgumentException.ThrowIfNullOrEmpty(contextName);
            return DeleteWhere(x => x.Context == contextName);
        }

        /// <summary>
        /// Counts the number of entries for a given context name.
        /// </summary>
        public int CountByContext(string contextName)
        {
            ArgumentException.ThrowIfNullOrEmpty(contextName);
            return Find(x => x.Context == contextName).Count();
        }
    }
}