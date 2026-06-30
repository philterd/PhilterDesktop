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

using Phileas.Services;
using PhilterData;

namespace PhilterDesktop
{
    /// <summary>
    /// Durable <see cref="IContextService"/> backed by the application's (encrypted) LiteDB database.
    /// Phileas calls this to keep RANDOM_REPLACE replacements consistent — the same token maps to the
    /// same replacement within a context. Storing the mappings in the database (instead of the default
    /// in-memory store) makes them persist across documents and app restarts and keeps the redaction
    /// engine from accumulating those mappings in memory over a long-running session.
    /// </summary>
    internal sealed class LiteDbContextService : IContextService
    {
        private readonly ContextEntryRepository _entries;

        // Serializes the read-modify-write in Put so two threads (e.g. concurrent watched-folder
        // redactions) can't both insert a mapping for the same token. LiteDB itself is thread-safe.
        private readonly object _writeLock = new();

        public LiteDbContextService(ContextEntryRepository entries) => _entries = entries;

        /// <inheritdoc />
        public string? Get(string contextName, string token) => _entries.FindEntry(contextName, token)?.Replacement;

        /// <inheritdoc />
        public void Put(string contextName, string token, string replacement)
        {
            lock (_writeLock)
            {
                _entries.UpsertEntry(contextName, token, replacement);
            }
        }
    }
}
