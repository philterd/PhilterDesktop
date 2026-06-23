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
    /// </summary>
    internal static class WordDocumentRedactor
    {
        /// <summary>
        /// Loads <paramref name="inputPath"/>, redacts its text with
        /// <paramref name="filterText"/>, and writes the result to
        /// <paramref name="outputPath"/>. The input file is left untouched.
        /// </summary>
        public static void Redact(string inputPath, string outputPath, Func<string, string> filterText)
        {
            using DocX document = DocX.Load(inputPath);

            // Body (Document.Paragraphs includes paragraphs inside tables).
            RedactParagraphs(document.Paragraphs, filterText);

            // Headers and footers (first / even / odd may each be null).
            RedactContainer(document.Headers?.First, filterText);
            RedactContainer(document.Headers?.Even, filterText);
            RedactContainer(document.Headers?.Odd, filterText);
            RedactContainer(document.Footers?.First, filterText);
            RedactContainer(document.Footers?.Even, filterText);
            RedactContainer(document.Footers?.Odd, filterText);

            document.SaveAs(outputPath);
        }

        private static void RedactContainer(Container? container, Func<string, string> filterText)
        {
            if (container != null)
            {
                RedactParagraphs(container.Paragraphs, filterText);
            }
        }

        private static void RedactParagraphs(IEnumerable<Paragraph> paragraphs, Func<string, string> filterText)
        {
            foreach (Paragraph paragraph in paragraphs)
            {
                string original = paragraph.Text;
                if (string.IsNullOrEmpty(original))
                {
                    continue;
                }

                string filtered = filterText(original);
                if (string.Equals(filtered, original, StringComparison.Ordinal))
                {
                    continue;
                }

                // Remove all existing text, then append the redacted text. The 4-arg
                // overload is (index, count, trackChanges, removeEmptyParagraph); we
                // pass removeEmptyParagraph: false so the paragraph survives being
                // emptied and Append can refill it (the 2-arg overload removes it).
                paragraph.RemoveText(0, original.Length, false, false);
                if (!string.IsNullOrEmpty(filtered))
                {
                    paragraph.Append(filtered);
                }
            }
        }
    }
}
