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

using System.Text.Json;
using System.Text.Json.Serialization;
using PhilterData;

namespace PhilterDesktop
{
    /// <summary>
    /// Builds a human- and machine-readable JSON "explanation" of a redaction: for each item the
    /// engine removed, what it was, why (the filter that matched, its confidence, the pattern, and the
    /// surrounding context window), and where. Phileas has no separate <c>explain</c> call — every
    /// detection it returns already carries this detail, which we persist on each
    /// <see cref="RedactionSpanEntity"/> — so this just projects the stored spans into a document.
    ///
    /// IMPORTANT: the explanation contains the <b>original (unredacted) detected text</b> and its
    /// surrounding context, so the resulting file is as sensitive as the source document. Callers must
    /// treat it accordingly (warn the user, don't write it somewhere unexpected).
    /// </summary>
    internal static class RedactionExplanation
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>Builds the explanation JSON for one redaction <paramref name="version"/>.</summary>
        public static string ToJson(
            RedactionVersionEntity version,
            IReadOnlyList<RedactionSpanEntity> spans,
            string toolVersion,
            DateTimeOffset generatedAt)
        {
            var ordered = spans.OrderBy(s => s.Order).ToList();

            var document = new ExplanationDocument
            {
                Tool = "Philter Desktop",
                ToolVersion = toolVersion,
                GeneratedUtc = generatedAt.ToUniversalTime(),
                Source = version.SourcePath,
                RedactedOutput = version.OutputPath,
                FileType = version.FileType,
                Policy = version.Policy,
                Context = version.Context,
                Version = version.Version,
                RedactedUtc = DateTime.SpecifyKind(version.CreatedAt, DateTimeKind.Utc),
                DetectionCount = ordered.Count,
                Detections = ordered.Select(ToDetection).ToList()
            };

            return JsonSerializer.Serialize(document, Options);
        }

        private static Detection ToDetection(RedactionSpanEntity s) => new()
        {
            Order = s.Order,
            FilterType = NullIfEmpty(s.FilterType),
            Classification = NullIfEmpty(s.Classification),
            Confidence = s.UserAdded ? null : s.Confidence,
            Text = s.Text,
            Replacement = s.Replacement,
            Pattern = NullIfEmpty(s.Pattern),
            Window = s.Window is { Count: > 0 } ? s.Window : null,
            UserAdded = s.UserAdded ? true : null,
            Location = ToLocation(s)
        };

        private static Location ToLocation(RedactionSpanEntity s)
        {
            // PDF spans locate by page + bounding box; everything else by character offsets.
            if (s.PageNumber > 0)
            {
                return new Location
                {
                    PageNumber = s.PageNumber,
                    LowerLeftX = s.LowerLeftX,
                    LowerLeftY = s.LowerLeftY,
                    UpperRightX = s.UpperRightX,
                    UpperRightY = s.UpperRightY
                };
            }

            return new Location
            {
                ParagraphIndex = s.ParagraphIndex >= 0 ? s.ParagraphIndex : null,
                CharacterStart = s.CharacterStart,
                CharacterEnd = s.CharacterEnd
            };
        }

        private static string? NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;

        // --- JSON shape (camelCase via the serializer options) ---

        private sealed class ExplanationDocument
        {
            public string Tool { get; init; } = string.Empty;
            public string ToolVersion { get; init; } = string.Empty;
            public DateTimeOffset GeneratedUtc { get; init; }
            public string Source { get; init; } = string.Empty;
            public string RedactedOutput { get; init; } = string.Empty;
            public string FileType { get; init; } = string.Empty;
            public string Policy { get; init; } = string.Empty;
            public string Context { get; init; } = string.Empty;
            public int Version { get; init; }
            public DateTime RedactedUtc { get; init; }
            public int DetectionCount { get; init; }
            public List<Detection> Detections { get; init; } = new();
        }

        private sealed class Detection
        {
            public int Order { get; init; }
            public string? FilterType { get; init; }
            public string? Classification { get; init; }
            public double? Confidence { get; init; }
            public string Text { get; init; } = string.Empty;
            public string Replacement { get; init; } = string.Empty;
            public string? Pattern { get; init; }
            public List<string>? Window { get; init; }
            public bool? UserAdded { get; init; }
            public Location Location { get; init; } = new();
        }

        private sealed class Location
        {
            public int? ParagraphIndex { get; init; }
            public int? CharacterStart { get; init; }
            public int? CharacterEnd { get; init; }
            public int? PageNumber { get; init; }
            public double? LowerLeftX { get; init; }
            public double? LowerLeftY { get; init; }
            public double? UpperRightX { get; init; }
            public double? UpperRightY { get; init; }
        }
    }
}
