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
    /// One redaction of a document: the initial automatic redaction is version 1, and each manual
    /// modification (add/edit/remove spans) creates a new version. A version owns a set of
    /// <see cref="RedactionSpanEntity"/> (one-to-many) and points at the output file it produced.
    /// It also carries everything needed to re-apply against the original source.
    /// </summary>
    public class RedactionVersionEntity
    {
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();

        /// <summary>The <see cref="RedactionQueueEntity.Id"/> this version belongs to.</summary>
        public ObjectId DocumentId { get; set; } = ObjectId.Empty;

        /// <summary>1-based version number within the document.</summary>
        public int Version { get; set; }

        /// <summary>Original (source) document path the spans apply to.</summary>
        public string SourcePath { get; set; } = string.Empty;

        /// <summary>The redacted output file this version produced.</summary>
        public string OutputPath { get; set; } = string.Empty;

        /// <summary>Source extension (".txt", ".docx", ".pdf").</summary>
        public string FileType { get; set; } = string.Empty;

        public string Policy { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;

        /// <summary>When true, .docx replacements are highlighted.</summary>
        public bool Highlight { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
