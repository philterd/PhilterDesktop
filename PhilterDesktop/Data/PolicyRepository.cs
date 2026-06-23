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
}