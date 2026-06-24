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
        /// Loads <paramref name="inputPath"/>, redacts its text with <paramref name="filter"/>, writes
        /// the result to <paramref name="outputPath"/>, and returns the applied spans (paragraph-indexed).
        /// The input file is left untouched.
        /// </summary>
        public static List<RedactionSpanEntity> Redact(string inputPath, string outputPath, Func<string, TextFilterResult> filter, bool highlight = false)
        {
            // Work on a copy so the original is never modified; all other document parts are preserved.
            File.Copy(inputPath, outputPath, overwrite: true);

            var captured = new List<RedactionSpanEntity>();
            int order = 0;
            int paragraphIndex = 0;

            using WordprocessingDocument document = WordprocessingDocument.Open(outputPath, isEditable: true);

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

                        RebuildParagraph(paragraph, original,
                            spans.Select(s => new ReplacementRange(s.CharacterStart, s.CharacterEnd, s.Replacement ?? string.Empty)),
                            highlight);

                        foreach (Span s in spans)
                        {
                            captured.Add(new RedactionSpanEntity
                            {
                                Order = order++,
                                ParagraphIndex = paragraphIndex,
                                CharacterStart = s.CharacterStart,
                                CharacterEnd = s.CharacterEnd,
                                Text = original.Substring(s.CharacterStart, s.CharacterEnd - s.CharacterStart),
                                Replacement = s.Replacement ?? string.Empty,
                                Classification = s.Classification ?? string.Empty
                            });
                        }
                    }
                }
                paragraphIndex++;
            }

            // Changes flush on dispose (AutoSave is on by default for the editable package).
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
            File.Copy(inputPath, outputPath, overwrite: true);

            Dictionary<int, List<RedactionSpanEntity>> byParagraph = spans
                .GroupBy(s => s.ParagraphIndex)
                .ToDictionary(g => g.Key, g => g.ToList());

            using WordprocessingDocument document = WordprocessingDocument.Open(outputPath, isEditable: true);

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
