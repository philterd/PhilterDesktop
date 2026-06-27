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
    public class RedactionQueueEntity
    {
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string Policy { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;

        /// <summary>
        /// Plain-language reason the most recent redaction attempt failed; empty when not failed.
        /// Persisted so the user can see why a "Failed" row failed after any toast has gone.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>When true, redacted replacements in .docx output are highlighted.</summary>
        public bool Highlight { get; set; }

        /// <summary>
        /// For spreadsheets (.xlsx/.csv): 1-based column indices whose every data cell should be
        /// redacted in full (the column header is preserved). Empty for all other documents and for
        /// spreadsheets queued without column selection.
        /// </summary>
        public List<int> FullyRedactedColumns { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
