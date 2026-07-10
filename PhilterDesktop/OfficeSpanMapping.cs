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

using Phileas.Services.Office;
using PhilterData;

namespace PhilterDesktop
{
    /// <summary>
    /// Maps between the library's persistence-free <see cref="OfficeRedactionSpan"/> (returned by, and
    /// consumed by, the Word/Excel redactors in <c>Phileas.Services.Office</c>) and Desktop's stored
    /// <see cref="RedactionSpanEntity"/> (a LiteDB entity). The redactors moved into the portable library,
    /// which cannot depend on LiteDB, so the app translates at the call boundary.
    /// </summary>
    internal static class OfficeSpanMapping
    {
        /// <summary>Projects a library span onto a fresh stored entity (Id/VersionId left at their defaults).</summary>
        public static RedactionSpanEntity ToEntity(OfficeRedactionSpan s) => new()
        {
            Order = s.Order,
            ParagraphIndex = s.ParagraphIndex,
            CharacterStart = s.CharacterStart,
            CharacterEnd = s.CharacterEnd,
            Text = s.Text,
            Replacement = s.Replacement,
            Classification = s.Classification,
            FilterType = s.FilterType,
            Confidence = s.Confidence,
            Pattern = s.Pattern,
            Window = s.Window.Count > 0 ? new List<string>(s.Window) : new List<string>(),
        };

        public static List<RedactionSpanEntity> ToEntities(IEnumerable<OfficeRedactionSpan> spans) =>
            spans.Select(ToEntity).ToList();

        /// <summary>Projects a stored entity onto a library span for re-applying by position.</summary>
        public static OfficeRedactionSpan ToOfficeSpan(RedactionSpanEntity e) => new()
        {
            Order = e.Order,
            ParagraphIndex = e.ParagraphIndex,
            CharacterStart = e.CharacterStart,
            CharacterEnd = e.CharacterEnd,
            Text = e.Text,
            Replacement = e.Replacement,
            Classification = e.Classification,
            FilterType = e.FilterType,
            Confidence = e.Confidence,
            Pattern = e.Pattern,
            Window = e.Window.Count > 0 ? new List<string>(e.Window) : new List<string>(),
        };

        public static List<OfficeRedactionSpan> ToOfficeSpans(IEnumerable<RedactionSpanEntity> spans) =>
            spans.Select(ToOfficeSpan).ToList();
    }
}
