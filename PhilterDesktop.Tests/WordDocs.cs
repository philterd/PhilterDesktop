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

using System.Text;
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
            return doc.MainDocumentPart!.Document!.Body!
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
                .SelectMany(h => h.Header!.Descendants<Paragraph>())
                .Select(p => p.InnerText));
        }

        /// <summary>All footer parts' text concatenated.</summary>
        public static string FootersText(string path)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(path, false);
            return string.Concat(doc.MainDocumentPart!.FooterParts
                .SelectMany(f => f.Footer!.Descendants<Paragraph>())
                .Select(p => p.InnerText));
        }

        private static Paragraph Para(string text) =>
            new(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));

        // --- Raw-XML fixtures for drawings / text boxes (issue #481) -----------------------------
        //
        // Text boxes and drawings can't be authored conveniently through the strongly-typed DOM, so
        // these write document.xml directly with every namespace declared at the root.

        private const string DocNamespaces =
            "xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" " +
            "xmlns:wp=\"http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing\" " +
            "xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" " +
            "xmlns:wps=\"http://schemas.microsoft.com/office/word/2010/wordprocessingShape\" " +
            "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" " +
            "xmlns:v=\"urn:schemas-microsoft-com:vml\" " +
            "xmlns:w10=\"urn:schemas-microsoft-com:office:word\" " +
            "xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\"";

        /// <summary>Creates a .docx whose body is exactly the supplied raw WordprocessingML.</summary>
        public static void CreateRaw(string path, string bodyInnerXml)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            MainDocumentPart main = doc.AddMainDocumentPart();
            string xml = $"<w:document {DocNamespaces}><w:body>{bodyInnerXml}</w:body></w:document>";
            using Stream stream = main.GetStream(FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            writer.Write(xml);
        }

        /// <summary>A simple body paragraph (raw XML).</summary>
        public static string ParaXml(string text) =>
            $"<w:p><w:r><w:t xml:space=\"preserve\">{Escape(text)}</w:t></w:r></w:p>";

        /// <summary>
        /// A body paragraph that anchors a modern (DrawingML) text box containing <paramref name="boxText"/>,
        /// optionally with the paragraph's own leading/trailing text around the drawing.
        /// </summary>
        public static string TextBoxParaXml(string boxText, string? before = null, string? after = null)
        {
            string lead = before is null ? "" : $"<w:r><w:t xml:space=\"preserve\">{Escape(before)}</w:t></w:r>";
            string trail = after is null ? "" : $"<w:r><w:t xml:space=\"preserve\">{Escape(after)}</w:t></w:r>";
            return "<w:p>" + lead +
                   "<w:r><w:drawing><wp:inline><a:graphic><a:graphicData " +
                   "uri=\"http://schemas.microsoft.com/office/word/2010/wordprocessingShape\">" +
                   "<wps:wsp><wps:txbx><w:txbxContent>" +
                   $"<w:p><w:r><w:t xml:space=\"preserve\">{Escape(boxText)}</w:t></w:r></w:p>" +
                   "</w:txbxContent></wps:txbx><wps:bodyPr/></wps:wsp>" +
                   "</a:graphicData></a:graphic></wp:inline></w:drawing></w:r>" +
                   trail + "</w:p>";
        }

        /// <summary>A body paragraph that anchors a legacy (VML) text box containing <paramref name="boxText"/>.</summary>
        public static string VmlTextBoxParaXml(string boxText) =>
            "<w:p><w:r><w:pict><v:shape><v:textbox><w:txbxContent>" +
            $"<w:p><w:r><w:t xml:space=\"preserve\">{Escape(boxText)}</w:t></w:r></w:p>" +
            "</w:txbxContent></v:textbox></v:shape></w:pict></w:r></w:p>";

        /// <summary>A body paragraph that contains a non-text drawing (a shape) plus optional caption text.</summary>
        public static string DrawingParaXml(string? caption = null)
        {
            string cap = caption is null ? "" : $"<w:r><w:t xml:space=\"preserve\">{Escape(caption)}</w:t></w:r>";
            return "<w:p>" + cap +
                   "<w:r><w:drawing><wp:inline><a:graphic><a:graphicData " +
                   "uri=\"http://schemas.microsoft.com/office/word/2010/wordprocessingShape\">" +
                   "<wps:wsp><wps:bodyPr/></wps:wsp>" +
                   "</a:graphicData></a:graphic></wp:inline></w:drawing></w:r></w:p>";
        }

        /// <summary>Text of every text box (txbxContent paragraph) in the document, in order.</summary>
        public static string[] TextBoxTexts(string path)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(path, false);
            return doc.MainDocumentPart!.Document!.Body!
                .Descendants<TextBoxContent>()
                .SelectMany(t => t.Descendants<Paragraph>())
                .Select(p => p.InnerText)
                .ToArray();
        }

        /// <summary>Whether the document still contains any drawing / picture markup.</summary>
        public static bool HasDrawingOrPicture(string path)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(path, false);
            Body body = doc.MainDocumentPart!.Document!.Body!;
            return body.Descendants<Drawing>().Any() || body.Descendants<Picture>().Any();
        }

        /// <summary>The full raw document.xml (for substring / occurrence assertions).</summary>
        public static string DocumentXml(string path)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(path, false);
            return doc.MainDocumentPart!.Document!.OuterXml;
        }

        /// <summary>Counts non-overlapping occurrences of <paramref name="value"/> in the document.xml.</summary>
        public static int Occurrences(string path, string value)
        {
            string xml = DocumentXml(path);
            int count = 0, index = 0;
            while ((index = xml.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }
            return count;
        }

        private static string Escape(string text) =>
            text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
