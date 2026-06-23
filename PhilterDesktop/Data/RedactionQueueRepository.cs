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
}