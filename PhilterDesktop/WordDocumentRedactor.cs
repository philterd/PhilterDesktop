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

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Phileas.Model;
using PhilterData;

namespace PhilterDesktop
{
    /// <summary>
    /// Redacts Microsoft Word (.docx) documents using the open-source Open XML SDK
    /// (<c>DocumentFormat.OpenXml</c>) — no commercial dependency or license key.
    ///
    /// Each paragraph's text is run through the supplied filter and, when the filter changes it, the
    /// paragraph is rebuilt from the resulting spans. Working at the paragraph level preserves the
    /// document's structure. The applied spans are captured (with a stable paragraph index) so they
    /// can be stored and later re-applied via <see cref="ApplySpans"/>.
    ///
    /// Note: rebuilding a <em>changed</em> paragraph flattens its inline run formatting (and any
    /// hyperlinks/fields collapse to plain text), since the visible text is what gets redacted.
    /// Unchanged paragraphs are left exactly as they were.
    /// </summary>
    internal static class WordDocumentRedactor
    {
        private const string DefaultReplacement = "{{{REDACTED-custom}}}";

        /// <summary>
        /// Returns the text of each redactable paragraph (body, then headers/footers) in the canonical
        /// order used for redaction — i.e. index <c>i</c> here is <see cref="RedactionSpanEntity.ParagraphIndex"/>
        /// <c>i</c>. Read-only; does not modify the file. Used for the preview workspace.
        /// </summary>
        public static List<string> ReadParagraphs(string inputPath)
        {
            using WordprocessingDocument document = WordprocessingDocument.Open(inputPath, isEditable: false);
            return EnumerateTargets(document).Select(p => p.InnerText).ToList();
        }

        /// <summary>
        /// Detects redactions for <paramref name="inputPath"/> using <paramref name="filter"/> without
        /// writing anything, returning the spans (paragraph-indexed) — the same set <see cref="Redact"/>
        /// would apply. Used for the preview workspace.
        /// </summary>
        public static List<RedactionSpanEntity> Detect(string inputPath, Func<string, TextFilterResult> filter)
        {
            using WordprocessingDocument document = WordprocessingDocument.Open(inputPath, isEditable: false);

            var captured = new List<RedactionSpanEntity>();
            int order = 0;
            int paragraphIndex = 0;
            foreach (Paragraph paragraph in EnumerateTargets(document))
            {
                string original = paragraph.InnerText;
                if (!string.IsNullOrEmpty(original))
                {
                    foreach (Span s in filter(original).Spans
                        .Where(s => s.CharacterStart >= 0 && s.CharacterEnd <= original.Length && s.CharacterEnd > s.CharacterStart)
                        .OrderBy(s => s.CharacterStart))
                    {
                        var entity = new RedactionSpanEntity
                        {
                            Order = order++,
                            ParagraphIndex = paragraphIndex,
                            CharacterStart = s.CharacterStart,
                            CharacterEnd = s.CharacterEnd,
                            Text = original.Substring(s.CharacterStart, s.CharacterEnd - s.CharacterStart),
                            Replacement = s.Replacement ?? string.Empty,
                            Classification = s.Classification ?? string.Empty
                        };
                        SpanExplanation.Populate(entity, s);
                        captured.Add(entity);
                    }
                }
                paragraphIndex++;
            }
            return captured;
        }

        /// <summary>
        /// Loads <paramref name="inputPath"/>, redacts its text with <paramref name="filter"/>, writes
        /// the result to <paramref name="outputPath"/>, and returns the applied spans (paragraph-indexed).
        /// The input file is left untouched.
        /// </summary>
        public static List<RedactionSpanEntity> Redact(string inputPath, string outputPath, Func<string, TextFilterResult> filter, bool highlight = false)
        {
            // Redact in memory, then write the output once so a failure never leaves the original or a
            // partial file (issue #483).
            var captured = new List<RedactionSpanEntity>();
            int order = 0;
            int paragraphIndex = 0;

            using MemoryStream buffer = SafeOutput.ReadToEditableStream(inputPath);
            using WordprocessingDocument document = WordprocessingDocument.Open(buffer, isEditable: true);

            foreach (Paragraph paragraph in EnumerateTargets(document).ToList())
            {
                string original = paragraph.InnerText;
                if (!string.IsNullOrEmpty(original))
                {
                    TextFilterResult result = filter(original);
                    if (!string.Equals(result.FilteredText, original, StringComparison.Ordinal))
                    {
                        List<Span> spans = result.Spans
                            .Where(s => s.CharacterStart >= 0 && s.CharacterEnd <= original.Length && s.CharacterEnd > s.CharacterStart)
                            .OrderBy(s => s.CharacterStart)
                            .ToList();

                        // Resolve overlaps before rebuilding (parity with ApplySpans): an unresolved
                        // overlapping range would be silently skipped while rebuilding, which could leave
                        // original text behind. The engine doesn't emit overlaps today; this is defensive.
                        List<ReplacementRange> ranges = RedactionSpanMath.ResolveNonOverlapping(
                            spans.Select(s => new ReplacementRange(s.CharacterStart, s.CharacterEnd, s.Replacement ?? string.Empty)));

                        RebuildParagraph(paragraph, original, ranges, highlight);

                        foreach (Span s in spans)
                        {
                            var entity = new RedactionSpanEntity
                            {
                                Order = order++,
                                ParagraphIndex = paragraphIndex,
                                CharacterStart = s.CharacterStart,
                                CharacterEnd = s.CharacterEnd,
                                Text = original.Substring(s.CharacterStart, s.CharacterEnd - s.CharacterStart),
                                Replacement = s.Replacement ?? string.Empty,
                                Classification = s.Classification ?? string.Empty
                            };
                            SpanExplanation.Populate(entity, s);
                            captured.Add(entity);
                        }
                    }
                }
                paragraphIndex++;
            }

            document.Save(); // flush into the buffer
            SafeOutput.Write(outputPath, buffer.ToArray());
            return captured;
        }

        /// <summary>
        /// Applies an explicit set of spans to <paramref name="inputPath"/>, writing to
        /// <paramref name="outputPath"/>. Every span — detected or user-added — is applied by
        /// <b>position</b>: its <see cref="RedactionSpanEntity.ParagraphIndex"/> plus character
        /// start/stop offsets within that paragraph.
        /// </summary>
        public static void ApplySpans(string inputPath, string outputPath, IReadOnlyList<RedactionSpanEntity> spans, bool highlight)
        {
            Dictionary<int, List<RedactionSpanEntity>> byParagraph = spans
                .GroupBy(s => s.ParagraphIndex)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Apply in memory, then write once (see Redact — issue #483).
            using MemoryStream buffer = SafeOutput.ReadToEditableStream(inputPath);
            using WordprocessingDocument document = WordprocessingDocument.Open(buffer, isEditable: true);

            int paragraphIndex = 0;
            foreach (Paragraph paragraph in EnumerateTargets(document).ToList())
            {
                string original = paragraph.InnerText;
                if (!string.IsNullOrEmpty(original) && byParagraph.TryGetValue(paragraphIndex, out List<RedactionSpanEntity>? paragraphSpans))
                {
                    var ranges = new List<ReplacementRange>();
                    foreach (RedactionSpanEntity s in paragraphSpans)
                    {
                        if (s.CharacterStart >= 0 && s.CharacterEnd <= original.Length && s.CharacterEnd > s.CharacterStart)
                        {
                            string repl = string.IsNullOrEmpty(s.Replacement) ? DefaultReplacement : s.Replacement;
                            ranges.Add(new ReplacementRange(s.CharacterStart, s.CharacterEnd, repl));
                        }
                    }

                    List<ReplacementRange> resolved = RedactionSpanMath.ResolveNonOverlapping(ranges);
                    if (resolved.Count > 0)
                    {
                        RebuildParagraph(paragraph, original, resolved, highlight);
                    }
                }
                paragraphIndex++;
            }

            document.Save(); // flush into the buffer
            SafeOutput.Write(outputPath, buffer.ToArray());
        }

        /// <summary>
        /// Canonical, stable order of redactable paragraphs: body (including tables), then header
        /// parts and footer parts in First, Even, Default(Odd) order. The order must match between
        /// <see cref="Redact"/> and <see cref="ApplySpans"/> so a stored paragraph index re-applies.
        /// </summary>
        private static IEnumerable<Paragraph> EnumerateTargets(WordprocessingDocument document)
        {
            MainDocumentPart? main = document.MainDocumentPart;
            if (main is null)
            {
                yield break;
            }

            Body? body = main.Document?.Body;
            if (body is not null)
            {
                foreach (Paragraph p in body.Descendants<Paragraph>())
                {
                    yield return p;
                }
            }

            foreach (OpenXmlPart part in OrderedHeaderFooterParts(main))
            {
                if (part.RootElement is OpenXmlElement root)
                {
                    foreach (Paragraph p in root.Descendants<Paragraph>())
                    {
                        yield return p;
                    }
                }
            }
        }

        // Resolves the document's header then footer parts via the section properties' references,
        // ordered First, Even, Default(Odd), de-duplicated (a part may be referenced by many sections).
        private static IEnumerable<OpenXmlPart> OrderedHeaderFooterParts(MainDocumentPart main)
        {
            List<SectionProperties> sectPrs = main.Document?.Body?.Descendants<SectionProperties>().ToList()
                                              ?? new List<SectionProperties>();
            var seen = new HashSet<OpenXmlPart>();

            foreach (HeaderReference reference in sectPrs.SelectMany(s => s.Elements<HeaderReference>()).OrderBy(r => Rank(r.Type)))
            {
                if (reference.Id?.Value is string id && TryGetPart(main, id, out OpenXmlPart part) && seen.Add(part))
                {
                    yield return part;
                }
            }

            foreach (FooterReference reference in sectPrs.SelectMany(s => s.Elements<FooterReference>()).OrderBy(r => Rank(r.Type)))
            {
                if (reference.Id?.Value is string id && TryGetPart(main, id, out OpenXmlPart part) && seen.Add(part))
                {
                    yield return part;
                }
            }
        }

        private static int Rank(EnumValue<HeaderFooterValues>? type)
        {
            if (type is null)
            {
                return 3;
            }
            if (type.Value == HeaderFooterValues.First)
            {
                return 0;
            }
            return type.Value == HeaderFooterValues.Even ? 1 : 2; // Default == Odd
        }

        private static bool TryGetPart(MainDocumentPart main, string id, out OpenXmlPart part)
        {
            try
            {
                part = main.GetPartById(id);
                return true;
            }
            catch
            {
                part = null!;
                return false;
            }
        }

        // Empties the paragraph (keeping its properties), then refills it: plain runs for the kept
        // text and a highlighted run for each replacement.
        private static void RebuildParagraph(Paragraph paragraph, string original, IEnumerable<ReplacementRange> ranges, bool highlight)
        {
            ParagraphProperties? properties = paragraph.GetFirstChild<ParagraphProperties>();
            paragraph.RemoveAllChildren();
            if (properties is not null)
            {
                paragraph.AppendChild(properties); // must precede the runs
            }

            int last = 0;
            foreach (ReplacementRange range in ranges.OrderBy(r => r.Start))
            {
                if (range.Start < last || range.End > original.Length)
                {
                    continue;
                }
                if (range.Start > last)
                {
                    paragraph.AppendChild(MakeRun(original.Substring(last, range.Start - last), highlight: false));
                }
                paragraph.AppendChild(MakeRun(range.Replacement ?? string.Empty, highlight));
                last = range.End;
            }
            if (last < original.Length)
            {
                paragraph.AppendChild(MakeRun(original.Substring(last), highlight: false));
            }
        }

        private static Run MakeRun(string text, bool highlight)
        {
            var run = new Run();
            if (highlight)
            {
                run.RunProperties = new RunProperties(new Highlight { Val = HighlightColorValues.Yellow });
            }
            // Preserve leading/trailing whitespace so spacing around replacements is kept.
            run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            return run;
        }
    }
}
