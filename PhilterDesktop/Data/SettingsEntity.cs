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
    /// Entity representing application settings stored in LiteDB.
    /// </summary>
    public class SettingsEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the settings record.
        /// </summary>
        [BsonId]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether redacted files should be output to their original location.
        /// If false, files will be output to a custom folder specified by <see cref="CustomOutputFolder"/>.
        /// </summary>
        public bool OutputToOriginalLocation { get; set; } = true;

        /// <summary>
        /// Gets or sets the custom output folder path for redacted files.
        /// Only used when <see cref="OutputToOriginalLocation"/> is false.
        /// </summary>
        public string CustomOutputFolder { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether logging is enabled.
        /// </summary>
        public bool LoggingEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets the date and time when the settings were last modified.
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }
}