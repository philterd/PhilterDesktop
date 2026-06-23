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
