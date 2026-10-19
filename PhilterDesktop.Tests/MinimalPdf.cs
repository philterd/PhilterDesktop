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

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Builds tiny, hand-authored PDFs for tests — PdfPig can read these but can't write annotations or
    /// form fields, so we emit the raw object/xref structure ourselves (ASCII only, so byte offsets are
    /// simple). The cross-reference offsets are computed so PdfPig parses without brute-force recovery.
    /// </summary>
    internal static class MinimalPdf
    {
        private const string Helvetica = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>";

        /// <summary>A 1-page PDF with visible body text only (no annotations or form fields).</summary>
        public static byte[] PlainText(string bodyText) => Build(new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            ContentStream($"BT /F1 12 Tf 72 700 Td ({EscapeString(bodyText)}) Tj ET"),
            Helvetica
        }, root: 1);

        /// <summary>
        /// A 1-page PDF whose body text is <paramref name="bodyText"/>, with a <c>FreeText</c> annotation
        /// carrying <paramref name="annotationText"/> and an AcroForm text field whose value is
        /// <paramref name="formFieldValue"/>.
        /// </summary>
        public static byte[] WithAnnotationAndFormField(string bodyText, string annotationText, string formFieldValue) => Build(new[]
        {
            "<< /Type /Catalog /Pages 2 0 R /AcroForm << /Fields [5 0 R] >> >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 7 0 R >> >> /Annots [6 0 R 5 0 R] >>",
            ContentStream($"BT /F1 12 Tf 72 700 Td ({EscapeString(bodyText)}) Tj ET"),
            $"<< /Type /Annot /Subtype /Widget /FT /Tx /T (field1) /V ({EscapeString(formFieldValue)}) /Rect [72 600 300 620] /P 3 0 R >>",
            $"<< /Type /Annot /Subtype /FreeText /Contents ({EscapeString(annotationText)}) /Rect [72 650 300 670] /P 3 0 R >>",
            Helvetica
        }, root: 1);

        /// <summary>
        /// A PDF with one page per entry. A page whose <c>Unmapped</c> flag is set draws its text with an
        /// Identity-H font that has no <c>/ToUnicode</c>, as glyph IDs offset by 29 (a standard TrueType
        /// glyph order), so it extracts as gibberish like "3DWLHQW" just as print-driver output does.
        /// </summary>
        public static byte[] Pages(params (string Text, bool Unmapped)[] pages)
        {
            // Objects: 1 catalog, 2 pages, 3 Helvetica, 4 Type0, 5 CIDFont, 6 descriptor, then page + content pairs.
            const int firstPage = 7;
            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                $"<< /Type /Pages /Kids [{string.Join(" ", pages.Select((_, i) => $"{firstPage + 2 * i} 0 R"))}] /Count {pages.Length} >>",
                Helvetica,
                "<< /Type /Font /Subtype /Type0 /BaseFont /ABCDEF+Arial /Encoding /Identity-H /DescendantFonts [5 0 R] >>",
                "<< /Type /Font /Subtype /CIDFontType2 /BaseFont /ABCDEF+Arial /CIDSystemInfo << /Registry (Adobe) /Ordering (Identity) /Supplement 0 >> /FontDescriptor 6 0 R /DW 500 /CIDToGIDMap /Identity >>",
                "<< /Type /FontDescriptor /FontName /ABCDEF+Arial /Flags 32 /FontBBox [0 -200 1000 900] /ItalicAngle 0 /Ascent 900 /Descent -200 /CapHeight 700 /StemV 80 >>"
            };
            for (int i = 0; i < pages.Length; i++)
            {
                objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents {firstPage + 2 * i + 1} 0 R /Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> >>");
                objects.Add(ContentStream(pages[i].Unmapped ? GlyphIdLines(pages[i].Text) : TextLines("/F1", pages[i].Text)));
            }
            return Build(objects.ToArray(), root: 1);
        }

        // One Tj per line, 14pt apart, in a simple font.
        private static string TextLines(string font, string text) =>
            $"BT {font} 12 Tf 14 TL 72 740 Td " +
            string.Join(" T* ", text.Split('\n').Select(l => $"({EscapeString(l)}) Tj")) + " ET";

        // As TextLines, but each character written as a 2-byte glyph ID (code point - 29) in the unmapped font.
        private static string GlyphIdLines(string text) =>
            "BT /F2 12 Tf 14 TL 72 740 Td " +
            string.Join(" T* ", text.Split('\n').Select(l => $"<{string.Concat(l.Select(c => (c - 29).ToString("X4")))}> Tj")) + " ET";

        private static string ContentStream(string content) =>
            $"<< /Length {Encoding.Latin1.GetByteCount(content)} >>\nstream\n{content}\nendstream";

        // Assembles the objects into a valid PDF with a correct xref table. Objects are numbered 1..N in
        // array order; `root` is the object number of the catalog.
        private static byte[] Build(string[] objects, int root)
        {
            using var stream = new MemoryStream();
            void Write(string s) => stream.Write(Encoding.Latin1.GetBytes(s));

            Write("%PDF-1.7\n");
            var offsets = new long[objects.Length + 1];
            for (int i = 0; i < objects.Length; i++)
            {
                offsets[i + 1] = stream.Position;
                Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }

            long xrefStart = stream.Position;
            Write($"xref\n0 {objects.Length + 1}\n");
            Write("0000000000 65535 f \n");
            for (int i = 1; i <= objects.Length; i++)
            {
                Write($"{offsets[i]:D10} 00000 n \n");
            }
            Write($"trailer\n<< /Size {objects.Length + 1} /Root {root} 0 R >>\nstartxref\n{xrefStart}\n%%EOF");

            return stream.ToArray();
        }

        // Escapes a PDF literal string's special characters.
        private static string EscapeString(string text) =>
            text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }
}
