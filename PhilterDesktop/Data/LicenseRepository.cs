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
    /// <summary>The stored work-use license token (the singleton record, Id 1).</summary>
    public class LicenseEntity
    {
        public int Id { get; set; } = 1;

        /// <summary>The signed token from account.philterd.ai, as received.</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>When the token was last obtained or refreshed.</summary>
        public DateTime LastRefreshUtc { get; set; }
    }

    /// <summary>
    /// Keeps the license token in its own collection of the encrypted database, apart from the settings
    /// record so the Settings dialog's save can never overwrite a refresh made while it was open.
    /// </summary>
    public class LicenseRepository : LiteDbRepository<LicenseEntity>
    {
        public LicenseRepository(LiteDatabase database) : base(database, "license")
        {
        }

        /// <summary>The stored license, with its refresh time back in UTC (LiteDB returns dates as local time).</summary>
        public LicenseEntity? Get()
        {
            LicenseEntity? entity = GetById(1);
            if (entity is not null)
            {
                entity.LastRefreshUtc = entity.LastRefreshUtc.Kind == DateTimeKind.Utc
                    ? entity.LastRefreshUtc
                    : entity.LastRefreshUtc.ToUniversalTime();
            }
            return entity;
        }

        public void Save(string token, DateTime refreshedUtc) =>
            Upsert(new LicenseEntity { Id = 1, Token = token, LastRefreshUtc = refreshedUtc });

        public void Clear() => Delete(1);
    }
}
