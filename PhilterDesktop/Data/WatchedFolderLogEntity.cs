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
    /// A single activity entry for a watched folder: when a file was found, redacted (and where),
    /// skipped, or failed.
    /// </summary>
    public class WatchedFolderLogEntity
    {
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();

        /// <summary>The <see cref="WatchedFolderEntity.Id"/> this entry belongs to.</summary>
        public ObjectId FolderId { get; set; } = ObjectId.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>"Info" or "Error".</summary>
        public string Level { get; set; } = "Info";

        public string Message { get; set; } = string.Empty;
    }
}
