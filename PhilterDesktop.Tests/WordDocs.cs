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

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Minimal Open XML SDK helpers for the Word tests: create .docx fixtures and read their text
    /// back, replacing the previous Xceed-based helpers (no license required).
    /// </summary>
    internal static class WordDocs
    {
        /// <summary>Creates a .docx with one body paragraph per supplied string.</summary>
        public static void Create(string path, params string[] paragraphs)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            MainDocumentPart main = doc.AddMainDocumentPart();
            var body = new Body();
            foreach (string text in paragraphs)
            {
                body.AppendChild(Para(text));
            }
            main.Document = new Document(body);
        }

        /// <summary>Creates a .docx with a (default/odd) header and footer plus body paragraphs.</summary>
        public static void CreateWithHeaderFooter(string path, string headerText, string footerText, params string[] bodyParagraphs)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            MainDocumentPart main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body());
            Body body = main.Document.Body!;

            foreach (string text in bodyParagraphs)
            {
                body.AppendChild(Para(text));
            }

            HeaderPart headerPart = main.AddNewPart<HeaderPart>();
            headerPart.Header = new Header(Para(headerText));
            string headerId = main.GetIdOfPart(headerPart);

            FooterPart footerPart = main.AddNewPart<FooterPart>();
            footerPart.Footer = new Footer(Para(footerText));
            string footerId = main.GetIdOfPart(footerPart);

            body.AppendChild(new SectionProperties(
                new HeaderReference { Type = HeaderFooterValues.Default, Id = headerId },
                new FooterReference { Type = HeaderFooterValues.Default, Id = footerId }));
        }

        /// <summary>The body paragraphs' text, in order.</summary>
        public static string[] BodyParagraphs(string path)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(path, false);
            return doc.MainDocumentPart!.Document.Body!
                .Elements<Paragraph>()
                .Select(p => p.InnerText)
                .ToArray();
        }

        /// <summary>All body text concatenated and trimmed (matches the old DocX helper).</summary>
        public static string AllBodyText(string path) => string.Concat(BodyParagraphs(path)).Trim();

        /// <summary>All header parts' text concatenated.</summary>
        public static string HeadersText(string path)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(path, false);
            return string.Concat(doc.MainDocumentPart!.HeaderParts
                .SelectMany(h => h.Header.Descendants<Paragraph>())
                .Select(p => p.InnerText));
        }

        /// <summary>All footer parts' text concatenated.</summary>
        public static string FootersText(string path)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(path, false);
            return string.Concat(doc.MainDocumentPart!.FooterParts
                .SelectMany(f => f.Footer.Descendants<Paragraph>())
                .Select(p => p.InnerText));
        }

        private static Paragraph Para(string text) =>
            new(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
    }
}
