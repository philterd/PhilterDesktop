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

using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using PhilterDesktop;
using SkiaSharp;
using UglyToad.PdfPig;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Verifies that redacting a PDF does not leak the source document's metadata (author, title,
    /// subject, keywords) into the redacted output.
    /// </summary>
    public sealed class PdfMetadataTests : IDisposable
    {
        private readonly string _dir;

        public PdfMetadataTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-pdfmeta-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private static void CreatePdfWithMetadata(string path, string body)
        {
            var metadata = new SKDocumentPdfMetadata
            {
                Author = "Jane Author",
                Title = "Secret Title",
                Subject = "Confidential Subject",
                Keywords = "secret,internal"
            };
            using var ws = new SKFileWStream(path);
            using var document = SKDocument.CreatePdf(ws, metadata);
            SKCanvas canvas = document.BeginPage(612, 792);
            using var font = new SKFont(SKTypeface.Default, 12f);
            using var paint = new SKPaint { Color = SKColors.Black, IsAntialias = true };
            canvas.DrawText(body, 50, 60, font, paint);
            document.EndPage();
            document.Close();
        }

        private static (string? Author, string? Title, string? Subject, string? Keywords) ReadMetadata(string path)
        {
            using PdfDocument doc = PdfDocument.Open(path);
            return (doc.Information.Author, doc.Information.Title, doc.Information.Subject, doc.Information.Keywords);
        }

        [Fact]
        public async Task RedactedPdf_DoesNotCarryOriginalMetadata()
        {
            string input = Path.Combine(_dir, "in.pdf");
            string output = Path.Combine(_dir, "out.pdf");
            CreatePdfWithMetadata(input, "Contact george@fake.com about this matter.");

            // Sanity: the source really does carry the metadata we expect to be gone afterward.
            var before = ReadMetadata(input);
            Assert.Equal("Jane Author", before.Author);
            Assert.Equal("Secret Title", before.Title);

            var policy = new PhileasPolicy { Name = "p", Identifiers = new Identifiers { EmailAddress = new EmailAddress() } };
            await RedactionService.RedactFileAsync(input, output, policy, "ctx", new FilterService());

            var after = ReadMetadata(output);
            Assert.True(string.IsNullOrEmpty(after.Author), $"author leaked: {after.Author}");
            Assert.True(string.IsNullOrEmpty(after.Title), $"title leaked: {after.Title}");
            Assert.True(string.IsNullOrEmpty(after.Subject), $"subject leaked: {after.Subject}");
            Assert.True(string.IsNullOrEmpty(after.Keywords), $"keywords leaked: {after.Keywords}");
        }
    }
}
