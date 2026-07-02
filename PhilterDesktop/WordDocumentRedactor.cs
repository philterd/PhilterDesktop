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
using A = DocumentFormat.OpenXml.Drawing;

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

        // A PII-bearing hyperlink target is rewritten to this. ".invalid" is reserved (RFC 2606) so it
        // never resolves; the relationship keeps its id, so the (already redacted) link text stays valid.
        private const string RedactedHyperlinkTarget = "https://redacted.invalid/";
        private const string HyperlinkClassification = "hyperlink-target";

        /// <summary>
        /// Returns the text of each redactable paragraph (body, then headers/footers) in the canonical
        /// order used for redaction — i.e. index <c>i</c> here is <see cref="RedactionSpanEntity.ParagraphIndex"/>
        /// <c>i</c>. Read-only; does not modify the file. Used for the preview workspace.
        /// </summary>
        public static List<string> ReadParagraphs(string inputPath)
        {
            using WordprocessingDocument document = WordprocessingDocument.Open(inputPath, isEditable: false);
            return EnumerateTargets(document).Select(OwnText).ToList();
        }

        /// <summary>
        /// Returns every readable line of a .docx for a before/after review diff: the redactable
        /// paragraphs (body, headers/footers, notes, comments — as <see cref="ReadParagraphs"/>) plus the
        /// DrawingML text of shapes, SmartArt, and chart labels, which redaction also rewrites. Unlike
        /// <see cref="ReadParagraphs"/>, this is <b>not</b> paragraph-index aligned — it exists so the diff
        /// doesn't understate redactions by hiding shape/SmartArt/chart changes. Read-only.
        /// </summary>
        public static List<string> ReadReviewLines(string inputPath)
        {
            using WordprocessingDocument document = WordprocessingDocument.Open(inputPath, isEditable: false);
            var lines = EnumerateTargets(document).Select(OwnText).ToList();
            lines.AddRange(ReadDrawingParagraphText(document));
            return lines;
        }

        // The concatenated text of each DrawingML paragraph across every part, in the same AllParts walk
        // order RedactDrawingText uses — redaction preserves the drawing structure (it flattens each
        // paragraph's text into its first run without removing paragraphs), so source and output align.
        private static IEnumerable<string> ReadDrawingParagraphText(WordprocessingDocument document)
        {
            MainDocumentPart? main = document.MainDocumentPart;
            if (main is null)
            {
                yield break;
            }

            foreach (OpenXmlPart part in AllParts(main))
            {
                OpenXmlElement? root;
                try
                {
                    root = part.RootElement; // may throw on binary/VML/untyped parts — skip those
                }
                catch
                {
                    continue;
                }
                if (root is null)
                {
                    continue;
                }
                foreach (A.Paragraph paragraph in root.Descendants<A.Paragraph>())
                {
                    List<A.Text> texts = paragraph.Descendants<A.Text>().ToList();
                    if (texts.Count > 0)
                    {
                        yield return string.Concat(texts.Select(t => t.Text));
                    }
                }
            }
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
                string original = OwnText(paragraph);
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
            // partial file.
            var captured = new List<RedactionSpanEntity>();
            int order = 0;
            int paragraphIndex = 0;

            using MemoryStream buffer = SafeOutput.ReadToEditableStream(inputPath);
            using WordprocessingDocument document = WordprocessingDocument.Open(buffer, isEditable: true);

            foreach (Paragraph paragraph in EnumerateTargets(document).ToList())
            {
                string original = OwnText(paragraph);
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

                        ApplyRangesToParagraph(paragraph, original, ranges, highlight);

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

            captured.AddRange(RedactDrawingText(document, filter, ref order));
            captured.AddRange(RedactHyperlinkTargets(document, filter, ref order));
            RemoveThreadedCommentDuplicate(document);
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
        public static void ApplySpans(string inputPath, string outputPath, IReadOnlyList<RedactionSpanEntity> spans, bool highlight,
            Func<string, TextFilterResult>? drawingFilter = null)
        {
            Dictionary<int, List<RedactionSpanEntity>> byParagraph = spans
                .GroupBy(s => s.ParagraphIndex)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Apply in memory, then write once (see Redact).
            using MemoryStream buffer = SafeOutput.ReadToEditableStream(inputPath);
            using WordprocessingDocument document = WordprocessingDocument.Open(buffer, isEditable: true);

            int paragraphIndex = 0;
            foreach (Paragraph paragraph in EnumerateTargets(document).ToList())
            {
                string original = OwnText(paragraph);
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
                    ApplyRangesToParagraph(paragraph, original, resolved, highlight);
                }
                paragraphIndex++;
            }

            if (drawingFilter is not null)
            {
                // Re-render path: re-redact drawing/hyperlink text via the filter (stored spans re-apply the
                // body by position). The captured drawing spans are already in the stored history, so the
                // returns are discarded here.
                int order = 0;
                RedactDrawingText(document, drawingFilter, ref order);
                RedactHyperlinkTargets(document, drawingFilter, ref order);
            }
            RemoveThreadedCommentDuplicate(document);
            document.Save(); // flush into the buffer
            SafeOutput.Write(outputPath, buffer.ToArray());
        }

        /// <summary>
        /// Canonical, stable order of redactable paragraphs: body (including tables), then header and
        /// footer parts in First, Even, Default(Odd) order, then footnotes, endnotes, and comments. The
        /// order must match between <see cref="Redact"/> and <see cref="ApplySpans"/> so a stored
        /// paragraph index re-applies.
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

            // Footnotes then endnotes: their paragraphs carry body-like text, so they must be filtered
            // like any other paragraph — otherwise PII in a note ships in the output.
            if (main.FootnotesPart?.Footnotes is Footnotes footnotes)
            {
                foreach (Paragraph p in footnotes.Descendants<Paragraph>())
                {
                    yield return p;
                }
            }
            if (main.EndnotesPart?.Endnotes is Endnotes endnotes)
            {
                foreach (Paragraph p in endnotes.Descendants<Paragraph>())
                {
                    yield return p;
                }
            }

            // Comment text: redact it too, so PII in a comment isn't shipped when the user keeps
            // comments (they're only deleted separately, by the "remove comments" scrub). Appended last
            // so body/header/footer paragraph indices stay stable for stored-span re-apply.
            if (main.WordprocessingCommentsPart?.Comments is Comments comments)
            {
                foreach (Paragraph p in comments.Descendants<Paragraph>())
                {
                    yield return p;
                }
            }
        }

        // Redacts DrawingML text (<a:t> runs in shapes, SmartArt, and charts) — which is not made of
        // WordprocessingML <w:p> paragraphs and so is missed by the body/notes enumeration. Each DrawingML
        // paragraph's runs are concatenated, filtered, and (when changed) flattened into its first run so
        // PII in a shape/SmartArt/chart label doesn't survive. Walks every part of the package so text in
        // the main document, headers/footers, notes, chart parts, and SmartArt data is covered. Returns the
        // redactions it made so they're recorded in the report/explanation like any other span.
        private static List<RedactionSpanEntity> RedactDrawingText(
            WordprocessingDocument document, Func<string, TextFilterResult> filter, ref int order)
        {
            var captured = new List<RedactionSpanEntity>();
            MainDocumentPart? main = document.MainDocumentPart;
            if (main is null)
            {
                return captured;
            }

            foreach (OpenXmlPart part in AllParts(main))
            {
                OpenXmlElement? root;
                try
                {
                    // Accessing RootElement parses the part; some parts (binary, VML, or a version the
                    // SDK can't type) throw — skip them rather than fail the whole redaction.
                    root = part.RootElement;
                }
                catch
                {
                    continue;
                }
                if (root is null)
                {
                    continue;
                }
                foreach (A.Paragraph paragraph in root.Descendants<A.Paragraph>().ToList())
                {
                    RedactDrawingParagraph(paragraph, filter, ref order, captured);
                }
            }
            return captured;
        }

        private static void RedactDrawingParagraph(
            A.Paragraph paragraph, Func<string, TextFilterResult> filter, ref int order, List<RedactionSpanEntity> captured)
        {
            List<A.Text> texts = paragraph.Descendants<A.Text>().ToList();
            if (texts.Count == 0)
            {
                return;
            }

            string original = string.Concat(texts.Select(t => t.Text));
            if (string.IsNullOrEmpty(original))
            {
                return;
            }

            TextFilterResult result = filter(original);
            if (string.Equals(result.FilteredText, original, StringComparison.Ordinal))
            {
                return;
            }

            // Flatten the (possibly multi-run) text into the first run, clearing the rest — the same
            // approach the WordprocessingML rebuild uses; the run structure (and the drawing) is preserved.
            texts[0].Text = result.FilteredText;
            for (int i = 1; i < texts.Count; i++)
            {
                texts[i].Text = string.Empty;
            }

            // Record each detection so shape/SmartArt/chart redactions appear in the report count, the
            // "What was removed" table, and the explanation export. ParagraphIndex -1: not a body paragraph.
            foreach (Span s in result.Spans
                         .Where(s => s.CharacterStart >= 0 && s.CharacterEnd <= original.Length && s.CharacterEnd > s.CharacterStart)
                         .OrderBy(s => s.CharacterStart))
            {
                var entity = new RedactionSpanEntity
                {
                    Order = order++,
                    ParagraphIndex = -1,
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

        // Redacts hyperlink URL *targets* — the external addresses stored as relationships and referenced
        // by w:hyperlink r:id / HYPERLINK fields (e.g. mailto:john@x.com, an intranet URL carrying an id,
        // a file:// path). The visible link text is redacted with the body, but the target lives in the
        // part's relationships and would otherwise ship intact. Each external target is run through the
        // policy filter; any target the policy flags is rewritten to a neutral placeholder (keeping the
        // relationship id so the document stays valid). Benign targets are left untouched. Every part is
        // walked, so links in the body, headers/footers, notes, and comments are all covered.
        private static List<RedactionSpanEntity> RedactHyperlinkTargets(WordprocessingDocument document, Func<string, TextFilterResult> filter, ref int order)
        {
            var captured = new List<RedactionSpanEntity>();
            MainDocumentPart? main = document.MainDocumentPart;
            if (main is null)
            {
                return captured;
            }

            foreach (OpenXmlPart part in AllParts(main))
            {
                // Snapshot: we delete/re-add relationships on this part inside the loop.
                foreach (HyperlinkRelationship rel in part.HyperlinkRelationships.ToList())
                {
                    if (!rel.IsExternal)
                    {
                        continue; // internal anchors (#bookmark) carry no external address
                    }

                    string target = rel.Uri?.ToString() ?? string.Empty;
                    if (string.IsNullOrEmpty(target))
                    {
                        continue;
                    }

                    TextFilterResult result = filter(target);
                    if (result.Spans.Count == 0)
                    {
                        continue; // policy found nothing in this target -> keep the link intact
                    }

                    string id = rel.Id;
                    part.DeleteReferenceRelationship(rel);
                    part.AddHyperlinkRelationship(new Uri(RedactedHyperlinkTarget, UriKind.Absolute), isExternal: true, id);

                    var entity = new RedactionSpanEntity
                    {
                        Order = order++,
                        ParagraphIndex = -1, // a hyperlink target, not a paragraph offset
                        CharacterStart = 0,
                        CharacterEnd = target.Length,
                        Text = target,
                        Replacement = RedactedHyperlinkTarget,
                        Classification = HyperlinkClassification,
                    };
                    SpanExplanation.Populate(entity, result.Spans[0]);
                    captured.Add(entity);
                }
            }

            return captured;
        }

        // Every distinct part reachable from the main document part (itself included), breadth-first.
        private static IEnumerable<OpenXmlPart> AllParts(MainDocumentPart main)
        {
            var seen = new HashSet<OpenXmlPart> { main };
            var queue = new Queue<OpenXmlPart>();
            queue.Enqueue(main);
            while (queue.Count > 0)
            {
                OpenXmlPart part = queue.Dequeue();
                yield return part;
                foreach (IdPartPair child in part.Parts)
                {
                    if (seen.Add(child.OpenXmlPart))
                    {
                        queue.Enqueue(child.OpenXmlPart);
                    }
                }
            }
        }

        // Word 2019/365 mirrors comment text into word/threadedComments.xml (plus commentsExtended /
        // commentsIds). We redact the canonical comments part (see EnumerateTargets), so drop the
        // threaded duplicate and its companions to (a) stop the duplicate copy of the text from
        // shipping and (b) leave a clean classic-comments document. The redacted comments part remains
        // (or is deleted by the "remove comments" scrub). No PII lives in the companion parts.
        private static void RemoveThreadedCommentDuplicate(WordprocessingDocument document)
        {
            MainDocumentPart? main = document.MainDocumentPart;
            if (main is null)
            {
                return;
            }

            List<OpenXmlPart> threaded = main.Parts.Select(p => p.OpenXmlPart).Where(IsThreadedCommentsPart).ToList();
            if (threaded.Count == 0)
            {
                return; // nothing threaded to downgrade; leave older comment structures untouched
            }

            foreach (OpenXmlPart part in threaded)
            {
                main.DeletePart(part);
            }
            foreach (WordprocessingCommentsExPart part in main.GetPartsOfType<WordprocessingCommentsExPart>().ToList())
            {
                main.DeletePart(part);
            }
            foreach (WordprocessingCommentsIdsPart part in main.GetPartsOfType<WordprocessingCommentsIdsPart>().ToList())
            {
                main.DeletePart(part);
            }
        }

        private static bool IsThreadedCommentsPart(OpenXmlPart part) =>
            part.ContentType.Contains("threadedcomment", StringComparison.OrdinalIgnoreCase)
            || part.Uri.OriginalString.EndsWith("threadedComments.xml", StringComparison.OrdinalIgnoreCase);

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

        // The paragraph's own visible text, excluding any text inside a nested text box (which is a
        // separate redaction unit). For a simple paragraph this equals InnerText.
        private static string OwnText(Paragraph paragraph) =>
            string.Concat(OwnTexts(paragraph).Select(t => t.Text));

        private static IEnumerable<Text> OwnTexts(Paragraph paragraph) =>
            paragraph.Descendants<Text>().Where(t => BelongsDirectlyTo(paragraph, t));

        // Runs holding the paragraph's own text (a direct <w:t>), excluding drawing runs and any run
        // inside a nested text box.
        private static List<Run> OwnTextRuns(Paragraph paragraph) =>
            paragraph.Descendants<Run>()
                .Where(r => BelongsDirectlyTo(paragraph, r)
                            && r.Elements<Text>().Any()
                            && !r.Descendants<Drawing>().Any()
                            && !r.Descendants<Picture>().Any())
                .ToList();

        // True when <paramref name="element"/>'s nearest ancestor paragraph is <paramref name="owner"/>
        // — i.e. the element is part of this paragraph's own content, not a nested text-box paragraph.
        // (Ancestors() yields nearest-first, so the first paragraph ancestor is the immediate one.)
        private static bool BelongsDirectlyTo(Paragraph owner, OpenXmlElement element) =>
            ReferenceEquals(element.Ancestors<Paragraph>().FirstOrDefault(), owner);

        // A paragraph that contains a drawing, picture, or text box must not be wiped and rebuilt:
        // doing so would delete the drawing. Such paragraphs are redacted run-by-run instead.
        private static bool ContainsDrawing(Paragraph paragraph) =>
            paragraph.Descendants<Drawing>().Any()
            || paragraph.Descendants<Picture>().Any()
            || paragraph.Descendants<Paragraph>().Any(); // a nested paragraph means text-box content

        private static void ApplyRangesToParagraph(Paragraph paragraph, string ownText, IReadOnlyCollection<ReplacementRange> ranges, bool highlight)
        {
            if (ranges.Count == 0)
            {
                return;
            }
            if (ContainsDrawing(paragraph))
            {
                RebuildComplexParagraph(paragraph, ownText, ranges, highlight);
            }
            else
            {
                RebuildParagraph(paragraph, ownText, ranges, highlight);
            }
        }

        // Empties the paragraph (keeping its properties), then refills it: plain runs for the kept
        // text and a highlighted run for each replacement. Only used for simple paragraphs (no drawing).
        private static void RebuildParagraph(Paragraph paragraph, string original, IEnumerable<ReplacementRange> ranges, bool highlight)
        {
            ParagraphProperties? properties = paragraph.GetFirstChild<ParagraphProperties>();
            paragraph.RemoveAllChildren();
            if (properties is not null)
            {
                paragraph.AppendChild(properties); // must precede the runs
            }
            foreach (Run run in BuildRuns(original, ranges, highlight))
            {
                paragraph.AppendChild(run);
            }
        }

        // Redacts a paragraph that contains a drawing / text box / picture without destroying it: only
        // the paragraph's own text runs are replaced; the drawing and any text-box content are left in
        // place (the text-box's inner paragraphs are redacted as their own units).
        private static void RebuildComplexParagraph(Paragraph paragraph, string ownText, IEnumerable<ReplacementRange> ranges, bool highlight)
        {
            List<Run> ownRuns = OwnTextRuns(paragraph);
            if (ownRuns.Count == 0)
            {
                return; // nothing of the paragraph's own to redact (e.g. a drawing-only paragraph)
            }

            Run anchor = ownRuns[0];
            OpenXmlElement parent = anchor.Parent!;
            foreach (Run run in BuildRuns(ownText, ranges, highlight))
            {
                parent.InsertBefore(run, anchor);
            }
            foreach (Run run in ownRuns)
            {
                run.Remove();
            }
        }

        // Splits text into plain runs for kept spans and a (optionally highlighted) run per replacement.
        private static IEnumerable<Run> BuildRuns(string text, IEnumerable<ReplacementRange> ranges, bool highlight)
        {
            var runs = new List<Run>();
            int last = 0;
            foreach (ReplacementRange range in ranges.OrderBy(r => r.Start))
            {
                if (range.Start < last || range.End > text.Length)
                {
                    continue;
                }
                if (range.Start > last)
                {
                    runs.Add(MakeRun(text.Substring(last, range.Start - last), highlight: false));
                }
                runs.Add(MakeRun(range.Replacement ?? string.Empty, highlight));
                last = range.End;
            }
            if (last < text.Length)
            {
                runs.Add(MakeRun(text.Substring(last), highlight: false));
            }
            return runs;
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
