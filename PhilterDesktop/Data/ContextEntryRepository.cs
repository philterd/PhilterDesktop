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

        private const string ContextTokenIndexName = "ctx_token_unique";

        // Delimiter between Context and Token in the composite key: a control char (SOH, U+0001) that
        // never appears in real context names or tokens, so "ab" + "c" can't collide with "a" + "bc".
        private const char KeyDelimiter = (char)1;

        // Composite (Context, Token) key. LiteDB indexes use the database's (case-insensitive) collation,
        // matching how FindEntry compares Context/Token, so the unique constraint and the lookup agree.
        private static readonly BsonExpression ContextTokenKey =
            BsonExpression.Create("$.Context + '" + KeyDelimiter + "' + $.Token");

        protected override void ConfigureIndexes()
        {
            // Single-field indexes for context lookups (FindByContext / DeleteAllByContext / CountByContext).
            EnsureIndex(x => x.Context, unique: false);
            EnsureIndex(x => x.Token, unique: false);

            // A (Context, Token) pair must map to exactly one replacement even when the GUI and CLI redact
            // against the shared database at the same time. A unique composite index makes the database
            // reject the duplicate insert from a cross-process race — which the in-process write lock in
            // LiteDbContextService can't cover. See UpsertEntry for how the losing writer recovers.
            EnsureUniqueContextTokenIndex();
        }

        private void EnsureUniqueContextTokenIndex()
        {
            try
            {
                EnsureIndex(ContextTokenIndexName, ContextTokenKey, unique: true);
            }
            catch (LiteException)
            {
                // A database written before this index existed may already hold duplicate (Context, Token)
                // rows (the very bug this fixes), which blocks creating the unique index. Remove them
                // (keeping the most recently written) and try once more.
                RemoveDuplicateContextTokenRows();
                try
                {
                    EnsureIndex(ContextTokenIndexName, ContextTokenKey, unique: true);
                }
                catch (LiteException)
                {
                    // Extremely unlikely collation edge: don't let index setup keep the app from opening
                    // its database. Falls back to the in-process-lock behavior for this database.
                }
            }
        }

        // Keeps the most recently written row per (Context, Token) and deletes the rest, so the unique
        // index can be built. Grouping is case-insensitive to match LiteDB's index collation.
        private void RemoveDuplicateContextTokenRows()
        {
            foreach (IGrouping<string, ContextEntryEntity> group in GetAll()
                         .GroupBy(e => e.Context + KeyDelimiter + e.Token, StringComparer.OrdinalIgnoreCase))
            {
                foreach (ContextEntryEntity duplicate in group
                             .OrderByDescending(e => e.UpdatedAtUtc)
                             .ThenBy(e => e.Id.ToString(), StringComparer.Ordinal)
                             .Skip(1))
                {
                    Delete(duplicate.Id);
                }
            }
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
        /// the same replacement across documents and app restarts. Safe under concurrent GUI/CLI writers:
        /// the unique composite index rejects a duplicate insert from a cross-process race, and the losing
        /// writer falls back to updating the row that won — so there's still exactly one row.
        /// </summary>
        public void UpsertEntry(string contextName, string token, string replacement)
        {
            DateTime now = _utcNow();
            ContextEntryEntity? existing = FindEntry(contextName, token);
            if (existing is null)
            {
                try
                {
                    Insert(new ContextEntryEntity
                    {
                        Context = contextName,
                        Token = token,
                        Replacement = replacement,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    });
                    return;
                }
                catch (LiteException ex) when (IsDuplicateKey(ex))
                {
                    // Another process inserted this (context, token) between the find above and this insert.
                    // Re-read and fall through to update it, so there's still exactly one row (no duplicate).
                    existing = FindEntry(contextName, token);
                    if (existing is null)
                    {
                        throw; // the row must exist after a duplicate-key error; if not, surface it
                    }
                }
            }

            existing.Replacement = replacement;
            existing.UpdatedAtUtc = now; // CreatedAtUtc is preserved across updates
            Update(existing);
        }

        private static bool IsDuplicateKey(LiteException ex) =>
            ex.ErrorCode == 110 // LiteException.INDEX_DUPLICATE_KEY
            || ex.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
    }
}
