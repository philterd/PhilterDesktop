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
    /// A folder that Philter Desktop continuously monitors for new .txt/.docx/.pdf files.
    /// Each new file is redacted with the configured policy/context and written to
    /// <see cref="OutputFolder"/>.
    /// </summary>
    public class WatchedFolderEntity
    {
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();

        /// <summary>The folder being monitored for new files.</summary>
        public string FolderPath { get; set; } = string.Empty;

        /// <summary>Name of the policy to redact with.</summary>
        public string Policy { get; set; } = string.Empty;

        /// <summary>Name of the redaction context to use.</summary>
        public string Context { get; set; } = string.Empty;

        /// <summary>When true, redacted replacements in .docx output are highlighted.</summary>
        public bool Highlight { get; set; }

        /// <summary>When true, a tray notification is shown when a file in this folder is redacted.</summary>
        public bool Notify { get; set; }

        /// <summary>The folder redacted output is written to.</summary>
        public string OutputFolder { get; set; } = string.Empty;

        /// <summary>
        /// When true, subfolders are watched too and the redacted output mirrors the source
        /// subfolder structure under <see cref="OutputFolder"/>.
        /// </summary>
        public bool IncludeSubfolders { get; set; }

        /// <summary>
        /// File extensions (lowercase, with the leading dot, e.g. ".pdf") this folder redacts.
        /// An empty list means all supported types.
        /// </summary>
        public List<string> FileTypes { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
