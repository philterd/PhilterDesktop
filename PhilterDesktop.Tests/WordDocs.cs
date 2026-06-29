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

        // --- Comments (issue #480) -----------------------------------------------------------------

        /// <summary>Creates a .docx with one Word comment (one paragraph) plus the given body paragraphs.</summary>
        public static void CreateWithComment(string path, string commentText, params string[] bodyParagraphs)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            MainDocumentPart main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body());
            Body body = main.Document.Body!;
            foreach (string text in bodyParagraphs)
            {
                body.AppendChild(Para(text));
            }

            WordprocessingCommentsPart commentsPart = main.AddNewPart<WordprocessingCommentsPart>();
            commentsPart.Comments = new Comments(
                new Comment(Para(commentText)) { Id = "1", Author = "Reviewer", Initials = "R" });
        }

        /// <summary>The text of each Word comment, in order (empty if there is no comments part).</summary>
        public static string[] CommentTexts(string path)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(path, false);
            WordprocessingCommentsPart? part = doc.MainDocumentPart!.WordprocessingCommentsPart;
            return part?.Comments is null
                ? Array.Empty<string>()
                : part.Comments.Elements<Comment>().Select(c => c.InnerText).ToArray();
        }

        /// <summary>Whether the document still has a comments part with at least one comment.</summary>
        public static bool HasComments(string path)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(path, false);
            WordprocessingCommentsPart? part = doc.MainDocumentPart!.WordprocessingCommentsPart;
            return part?.Comments?.Elements<Comment>().Any() == true;
        }

        // --- Threaded comments (issue #507) --------------------------------------------------------

        private const string ThreadedRelType = "http://schemas.microsoft.com/office/2018/08/relationships/threadedComment";
        private const string ThreadedContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.threadedcomments+xml";

        /// <summary>
        /// Creates a .docx with a classic comment (<paramref name="classicText"/>) and the modern
        /// threaded-comment duplicate store (<paramref name="threadedText"/> in word/threadedComments.xml).
        /// </summary>
        public static void CreateWithThreadedComment(string path, string threadedText, string classicText, params string[] bodyParagraphs)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            MainDocumentPart main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body());
            Body body = main.Document.Body!;
            foreach (string text in bodyParagraphs)
            {
                body.AppendChild(Para(text));
            }

            WordprocessingCommentsPart commentsPart = main.AddNewPart<WordprocessingCommentsPart>();
            commentsPart.Comments = new Comments(
                new Comment(Para(classicText)) { Id = "1", Author = "Reviewer", Initials = "R" });

            ExtendedPart threaded = main.AddExtendedPart(ThreadedRelType, ThreadedContentType, "xml");
            string xml =
                "<w15:threadedComments xmlns:w15=\"http://schemas.microsoft.com/office/word/2015/wml\">" +
                "<w15:threadedComment w15:id=\"1\" w15:paraId=\"00000001\" w15:dateUtc=\"2024-01-01T00:00:00Z\">" +
                $"<w15:text>{Escape(threadedText)}</w15:text></w15:threadedComment></w15:threadedComments>";
            using Stream s = threaded.GetStream(FileMode.Create, FileAccess.Write);
            using var w = new StreamWriter(s, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            w.Write(xml);
        }

        /// <summary>
        /// Creates a .docx that models a real Word comment thread: each message in
        /// <paramref name="messages"/> becomes both a classic comment (word/comments.xml) and a
        /// threaded comment (word/threadedComments.xml), with the companion commentsExtended part.
        /// </summary>
        public static void CreateWithThreadedThread(string path, string[] messages, params string[] bodyParagraphs)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            MainDocumentPart main = doc.AddMainDocumentPart();
            main.Document = new Document(new Body());
            Body body = main.Document.Body!;
            foreach (string text in bodyParagraphs)
            {
                body.AppendChild(Para(text));
            }

            var comments = new Comments();
            for (int i = 0; i < messages.Length; i++)
            {
                comments.AppendChild(new Comment(Para(messages[i])) { Id = (i + 1).ToString(), Author = "Reviewer", Initials = "R" });
            }
            main.AddNewPart<WordprocessingCommentsPart>().Comments = comments;

            // commentsExtended (threading metadata, no text) — present in real threaded docs.
            WordprocessingCommentsExPart exPart = main.AddNewPart<WordprocessingCommentsExPart>();
            using (Stream es = exPart.GetStream(FileMode.Create, FileAccess.Write))
            using (var ew = new StreamWriter(es, new UTF8Encoding(false)))
            {
                ew.Write("<w15:commentsEx xmlns:w15=\"http://schemas.microsoft.com/office/word/2015/wml\">" +
                         string.Concat(Enumerable.Range(0, messages.Length).Select(i =>
                             $"<w15:commentEx w15:paraId=\"{i + 1:00000000}\" w15:done=\"0\"/>")) +
                         "</w15:commentsEx>");
            }

            // threadedComments duplicate (the PII copy this issue is about).
            ExtendedPart threaded = main.AddExtendedPart(ThreadedRelType, ThreadedContentType, "xml");
            string threads = string.Concat(Enumerable.Range(0, messages.Length).Select(i =>
                $"<w15:threadedComment w15:id=\"{i + 1}\" w15:paraId=\"{i + 1:00000000}\" w15:dateUtc=\"2024-01-01T00:00:00Z\"" +
                (i == 0 ? "" : " w15:parentId=\"1\"") + $"><w15:text>{Escape(messages[i])}</w15:text></w15:threadedComment>"));
            using (Stream s = threaded.GetStream(FileMode.Create, FileAccess.Write))
            using (var w = new StreamWriter(s, new UTF8Encoding(false)))
            {
                w.Write("<w15:threadedComments xmlns:w15=\"http://schemas.microsoft.com/office/word/2015/wml\">" + threads + "</w15:threadedComments>");
            }
        }

        /// <summary>Whether the document still has a Word threaded-comments part.</summary>
        public static bool HasThreadedComments(string path)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(path, false);
            return ThreadedPart(doc.MainDocumentPart!) is not null;
        }

        /// <summary>Whether the document still has the commentsExtended / commentsIds companion parts.</summary>
        public static bool HasCommentCompanionParts(string path)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(path, false);
            MainDocumentPart main = doc.MainDocumentPart!;
            return main.GetPartsOfType<WordprocessingCommentsExPart>().Any()
                || main.GetPartsOfType<WordprocessingCommentsIdsPart>().Any();
        }

        /// <summary>True if any part in the package contains <paramref name="value"/> (strong leak check).</summary>
        public static bool AnyPartContains(string path, string value)
        {
            using WordprocessingDocument doc = WordprocessingDocument.Open(path, false);
            var seen = new HashSet<string>();
            return AnyPartContains(doc.MainDocumentPart!, value, seen);
        }

        private static bool AnyPartContains(OpenXmlPart part, string value, HashSet<string> seen)
        {
            if (!seen.Add(part.Uri.OriginalString))
            {
                return false;
            }
            using (Stream s = part.GetStream(FileMode.Open, FileAccess.Read))
            using (var r = new StreamReader(s))
            {
                if (r.ReadToEnd().Contains(value, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return part.Parts.Any(child => AnyPartContains(child.OpenXmlPart, value, seen));
        }

        private static OpenXmlPart? ThreadedPart(MainDocumentPart main) =>
            main.Parts.Select(p => p.OpenXmlPart).FirstOrDefault(p =>
                p.ContentType.Contains("threadedcomment", StringComparison.OrdinalIgnoreCase)
                || p.Uri.OriginalString.EndsWith("threadedComments.xml", StringComparison.OrdinalIgnoreCase));

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
