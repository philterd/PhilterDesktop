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
    /// A single redacted span belonging to a <see cref="RedactionVersionEntity"/>. Carries enough
    /// location detail to re-apply it to the source for every supported file type: character offsets
    /// (.txt and, with <see cref="ParagraphIndex"/>, .docx) and page/coordinates (.pdf).
    /// </summary>
    public class RedactionSpanEntity
    {
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();

        /// <summary>The <see cref="RedactionVersionEntity.Id"/> this span belongs to.</summary>
        public ObjectId VersionId { get; set; } = ObjectId.Empty;

        /// <summary>Stable display/apply order within the version.</summary>
        public int Order { get; set; }

        /// <summary>The original (detected) text. For user-added spans this is the term to redact.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>The replacement text written in place of <see cref="Text"/>.</summary>
        public string Replacement { get; set; } = string.Empty;

        /// <summary>Filter type / label (e.g. "email-address", "ssn"), informational.</summary>
        public string Classification { get; set; } = string.Empty;

        /// <summary>True for a span the user added by hand (located by term search when re-applied).</summary>
        public bool UserAdded { get; set; }

        // --- Explanation detail (why the engine flagged this) ---------------------------------------
        // Populated from the engine's Span when a detection is captured. Empty/zero for user-added
        // spans. Used by the "Export Explanation (JSON)" feature.

        /// <summary>The engine filter that matched (e.g. "EMAIL_ADDRESS", "SSN").</summary>
        public string FilterType { get; set; } = string.Empty;

        /// <summary>The engine's confidence in this detection (0–1).</summary>
        public double Confidence { get; set; }

        /// <summary>The rule/regex pattern that matched, when the filter is pattern-based.</summary>
        public string Pattern { get; set; } = string.Empty;

        /// <summary>The surrounding tokens (context window) the engine considered, when available.</summary>
        public List<string> Window { get; set; } = new();

        // --- Text location (.txt and .docx) ---
        public int CharacterStart { get; set; }
        public int CharacterEnd { get; set; }

        /// <summary>For .docx: index into the document's canonical paragraph enumeration; -1 otherwise.</summary>
        public int ParagraphIndex { get; set; } = -1;

        // --- PDF location ---
        public int PageNumber { get; set; }
        public double LowerLeftX { get; set; }
        public double LowerLeftY { get; set; }
        public double UpperRightX { get; set; }
        public double UpperRightY { get; set; }
    }
}
