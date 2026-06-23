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

using Phileas.Model;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace PhilterDesktop
{
    /// <summary>
    /// Redacts Microsoft Word (.docx) documents using Xceed Words for .NET.
    ///
    /// Each paragraph's text is run through the supplied filter and, when the filter
    /// changes it, the paragraph's text is replaced. Working at the paragraph level
    /// preserves the document's structure (paragraphs, tables, lists). Inline run
    /// formatting within a changed paragraph is not preserved, which is expected for
    /// redaction since the original text is being removed.
    ///
    /// When <c>highlight</c> is set, the paragraph is rebuilt from the filter's spans so
    /// each replacement run can be given Word's native highlight, making redactions
    /// visually obvious.
    /// </summary>
    internal static class WordDocumentRedactor
    {
        private const Highlight ReplacementHighlight = Highlight.yellow;

        /// <summary>
        /// Loads <paramref name="inputPath"/>, redacts its text with <paramref name="filter"/>,
        /// and writes the result to <paramref name="outputPath"/>. The input file is left
        /// untouched. When <paramref name="highlight"/> is true, replacement text is highlighted.
        /// </summary>
        public static void Redact(string inputPath, string outputPath, Func<string, TextFilterResult> filter, bool highlight = false)
        {
            using DocX document = DocX.Load(inputPath);

            // Body (Document.Paragraphs includes paragraphs inside tables).
            RedactParagraphs(document.Paragraphs, filter, highlight);

            // Headers and footers (first / even / odd may each be null).
            RedactContainer(document.Headers?.First, filter, highlight);
            RedactContainer(document.Headers?.Even, filter, highlight);
            RedactContainer(document.Headers?.Odd, filter, highlight);
            RedactContainer(document.Footers?.First, filter, highlight);
            RedactContainer(document.Footers?.Even, filter, highlight);
            RedactContainer(document.Footers?.Odd, filter, highlight);

            document.SaveAs(outputPath);
        }

        private static void RedactContainer(Container? container, Func<string, TextFilterResult> filter, bool highlight)
        {
            if (container != null)
            {
                RedactParagraphs(container.Paragraphs, filter, highlight);
            }
        }

        private static void RedactParagraphs(IEnumerable<Paragraph> paragraphs, Func<string, TextFilterResult> filter, bool highlight)
        {
            foreach (Paragraph paragraph in paragraphs)
            {
                string original = paragraph.Text;
                if (string.IsNullOrEmpty(original))
                {
                    continue;
                }

                TextFilterResult result = filter(original);
                if (string.Equals(result.FilteredText, original, StringComparison.Ordinal))
                {
                    continue;
                }

                // Remove all existing text, then re-add. The 4-arg overload is
                // (index, count, trackChanges, removeEmptyParagraph); removeEmptyParagraph:false
                // keeps the (now empty) paragraph so Append can refill it.
                paragraph.RemoveText(0, original.Length, false, false);

                if (!highlight)
                {
                    if (!string.IsNullOrEmpty(result.FilteredText))
                    {
                        paragraph.Append(result.FilteredText);
                    }
                    continue;
                }

                // Rebuild from spans so only the replacements are highlighted.
                int last = 0;
                foreach (Span span in result.Spans.OrderBy(s => s.CharacterStart))
                {
                    if (span.CharacterStart < last || span.CharacterEnd > original.Length)
                    {
                        continue; // defensive: skip out-of-range/overlapping spans
                    }
                    if (span.CharacterStart > last)
                    {
                        paragraph.Append(original.Substring(last, span.CharacterStart - last));
                    }
                    paragraph.Append(span.Replacement ?? string.Empty).Highlight(ReplacementHighlight);
                    last = span.CharacterEnd;
                }
                if (last < original.Length)
                {
                    paragraph.Append(original.Substring(last));
                }
            }
        }
    }
}
