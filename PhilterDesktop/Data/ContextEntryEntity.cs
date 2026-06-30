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
    public class ContextEntryEntity
    {
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();
        public string Token { get; set; } = string.Empty;
        public string Replacement { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;

        /// <summary>UTC time the mapping was first stored. Default for entries written before this field existed.</summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>UTC time the mapping's replacement was last written (equals <see cref="CreatedAtUtc"/> until first updated).</summary>
        public DateTime UpdatedAtUtc { get; set; }
    }

}
