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

using Phileas.Services.Pdf;
using Windows.Media.Ocr;

namespace PhilterDesktop
{
    /// <summary>
    /// Detects PDF pages whose text layer can't be read: fonts with no Unicode mapping (e.g. Identity-H
    /// subsets without <c>/ToUnicode</c>) extract as glyph IDs, which look normal on screen but read as
    /// gibberish like <c>3DWLHQW 1DPH</c>. Detection finds nothing on such pages, so they must be OCR'd
    /// or flagged rather than passed through as if checked.
    /// </summary>
    internal static class PdfTextLayerQuality
    {
        // Below this much text a page is too short to judge either way.
        internal const int MinCharsToJudge = 40;

        // Control characters, U+FFFD, and private-use code points are what unmapped glyphs come out as.
        internal const double MaxBadCharFraction = 0.05;

        // Real text in Latin-script languages is ~35-45% vowels, all-caps included; glyph IDs are far lower.
        internal const double MinVowelFraction = 0.2;

        /// <summary>
        /// True when <paramref name="text"/> looks like unmapped glyph IDs rather than real characters.
        /// Short text and text with too few Latin letters to judge (e.g. other scripts) return false.
        /// </summary>
        public static bool LooksUnreadable(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            int counted = 0, bad = 0, letters = 0, vowels = 0;
            foreach (char c in text)
            {
                if (c is '\r' or '\n' or '\t' or ' ')
                {
                    continue;
                }
                counted++;
                if (char.IsControl(c) || c == '�' || (c >= '' && c <= ''))
                {
                    bad++;
                }
                else if (char.IsAsciiLetter(c))
                {
                    letters++;
                    if ("aeiouyAEIOUY".Contains(c))
                    {
                        vowels++;
                    }
                }
            }

            if (counted < MinCharsToJudge)
            {
                return false;
            }
            if ((double)bad / counted >= MaxBadCharFraction)
            {
                return true;
            }
            return letters >= MinCharsToJudge && (double)vowels / letters < MinVowelFraction;
        }

        /// <summary>1-based numbers of the pages in <paramref name="lines"/> whose text is unreadable.</summary>
        public static IReadOnlyList<int> UnreadablePages(IEnumerable<PdfLine> lines) =>
            lines.GroupBy(l => l.PageNumber)
                .Where(g => LooksUnreadable(string.Join("\n", g.Select(l => l.Text))))
                .Select(g => g.Key)
                .OrderBy(p => p)
                .ToList();

        /// <summary>
        /// 1-based numbers of the pages of <paramref name="document"/> whose text is unreadable. An
        /// unreadable document returns none (the redaction path reports that failure itself).
        /// </summary>
        public static IReadOnlyList<int> UnreadablePages(byte[] document)
        {
            try
            {
                return UnreadablePages(new PdfTextExtractor().GetLines(document));
            }
            catch
            {
                return Array.Empty<int>();
            }
        }

        /// <summary>
        /// The warning for a PDF with unreadable pages that OCR won't cover, or null when there are none
        /// or OCR is on and available (those pages are then OCR'd instead).
        /// </summary>
        public static string? WarningFor(byte[] document, bool ocrScannedPdfs) =>
            ocrScannedPdfs && OcrEngine.TryCreateFromUserProfileLanguages() is not null
                ? null
                : Warning(UnreadablePages(document), ocrScannedPdfs);

        /// <summary>As <see cref="WarningFor(byte[], bool)"/>, for a file; non-PDF and unreadable files return null.</summary>
        public static string? WarningFor(string inputPath, bool ocrScannedPdfs)
        {
            if (!string.Equals(Path.GetExtension(inputPath), ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            try
            {
                return WarningFor(File.ReadAllBytes(inputPath), ocrScannedPdfs);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>The warning text for <paramref name="pages"/>, or null when it's empty.</summary>
        internal static string? Warning(IReadOnlyList<int> pages, bool ocrScannedPdfs)
        {
            if (pages.Count == 0)
            {
                return null;
            }
            string which = pages.Count == 1 ? $"Page {pages[0]}" : $"Pages {FormatPages(pages)}";
            string fix = ocrScannedPdfs
                ? "OCR is on, but no OCR language is installed in Windows. Add one, or review those pages by hand."
                : "Turn on OCR for scanned PDFs in Settings, or review those pages by hand.";
            return $"{which} of this PDF {(pages.Count == 1 ? "has" : "have")} text that can't be read (its fonts " +
                   $"have no character mapping), so {(pages.Count == 1 ? "it was" : "they were")} not checked for " +
                   $"sensitive information. {fix}";
        }

        /// <summary>Formats sorted page numbers with ranges collapsed, e.g. "2, 11-18, 21".</summary>
        internal static string FormatPages(IReadOnlyList<int> pages)
        {
            var parts = new List<string>();
            int i = 0;
            while (i < pages.Count)
            {
                int start = pages[i];
                int end = start;
                while (i + 1 < pages.Count && pages[i + 1] == end + 1)
                {
                    end = pages[++i];
                }
                parts.Add(start == end ? $"{start}" : $"{start}-{end}");
                i++;
            }
            return string.Join(", ", parts);
        }
    }
}
