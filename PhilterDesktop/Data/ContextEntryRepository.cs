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
        private readonly Func<DateTime> _utcNow;

        public ContextEntryRepository(LiteDatabase database)
            : this(database, () => DateTime.UtcNow)
        {
        }

        // Test seam: lets tests supply a deterministic clock so timestamp behavior can be asserted exactly.
        internal ContextEntryRepository(LiteDatabase database, Func<DateTime> utcNow)
            : base(database, "contextentries")
        {
            _utcNow = utcNow;
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

        /// <summary>The entry for a (context, token) pair, or null if none is stored yet.</summary>
        public ContextEntryEntity? FindEntry(string contextName, string token)
        {
            ContextEntryEntity? entry = FindOne(x => x.Context == contextName && x.Token == token);
            if (entry is not null)
            {
                // LiteDB stores DateTimes in UTC but returns them as Local kind; normalize back to UTC so
                // the CreatedAtUtc/UpdatedAtUtc contract holds for callers (same instant, correct Kind).
                entry.CreatedAtUtc = entry.CreatedAtUtc.ToUniversalTime();
                entry.UpdatedAtUtc = entry.UpdatedAtUtc.ToUniversalTime();
            }
            return entry;
        }

        /// <summary>
        /// Stores (or updates) the replacement for a (context, token) pair, so the same token maps to
        /// the same replacement across documents and app restarts.
        /// </summary>
        public void UpsertEntry(string contextName, string token, string replacement)
        {
            DateTime now = _utcNow();
            ContextEntryEntity? existing = FindEntry(contextName, token);
            if (existing is null)
            {
                Insert(new ContextEntryEntity
                {
                    Context = contextName,
                    Token = token,
                    Replacement = replacement,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });
            }
            else
            {
                existing.Replacement = replacement;
                existing.UpdatedAtUtc = now; // CreatedAtUtc is preserved across updates
                Update(existing);
            }
        }
    }
}