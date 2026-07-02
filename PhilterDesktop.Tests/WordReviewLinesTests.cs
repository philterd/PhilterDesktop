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
using Phileas.Services;
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The .docx View Diff builds both sides from <see cref="WordDocumentRedactor.ReadReviewLines"/>,
    /// which — unlike ReadParagraphs — also includes DrawingML (shape/SmartArt/chart) text, so the diff
    /// no longer understates redactions by hiding drawing changes.
    /// </summary>
    public sealed class WordReviewLinesTests : IDisposable
    {
        private static readonly PhileasPolicy EmailPolicy = new()
        {
            Name = "email",
            Identifiers = new Phileas.Policy.Identifiers { EmailAddress = new Phileas.Policy.Filters.EmailAddress() }
        };

        private readonly string _dir;

        public WordReviewLinesTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-review-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private string NewPath(string name) => Path.Combine(_dir, name);

        [Fact]
        public void ReadReviewLines_IncludesChartText_WhichReadParagraphsOmits()
        {
            string input = NewPath("chart.docx");
            WordDocs.CreateWithChart(input, WordDocs.AParagraph("Sales chart@example.com"), "body plain");

            List<string> review = WordDocumentRedactor.ReadReviewLines(input);
            List<string> paragraphs = WordDocumentRedactor.ReadParagraphs(input);

            Assert.Contains(review, l => l.Contains("chart@example.com"));            // drawing text is in the review lines
            Assert.DoesNotContain(paragraphs, l => l.Contains("chart@example.com"));  // ...but not in ReadParagraphs (the bug)
            Assert.Contains(review, l => l.Contains("body plain"));                   // body text still present
        }

        [Fact]
        public void ReadReviewLines_IncludesSmartArtNodeText()
        {
            string input = NewPath("sa.docx");
            WordDocs.CreateWithSmartArt(input, WordDocs.AParagraph("node smart@example.com"), "body");

            Assert.Contains(WordDocumentRedactor.ReadReviewLines(input), l => l.Contains("smart@example.com"));
        }

        [Fact]
        public void ReadReviewLines_NoDrawings_MatchesReadParagraphs()
        {
            string input = NewPath("plain.docx");
            WordDocs.Create(input, "just body a@example.com");

            Assert.Equal(WordDocumentRedactor.ReadParagraphs(input), WordDocumentRedactor.ReadReviewLines(input));
        }

        [Fact]
        public async Task ReadReviewLines_ReflectsDrawingRedaction_SourceVsOutput()
        {
            // The end-to-end scenario the diff relies on: after redacting, the "before" review lines show
            // the shape/chart PII and the "after" lines show it redacted — so View Diff surfaces it.
            string input = NewPath("in.docx");
            string output = NewPath("out.docx");
            WordDocs.CreateWithChart(input, WordDocs.AParagraph("chart c@example.com"), "body b@example.com");

            await RedactionService.RedactFileAsync(input, output, EmailPolicy, "ctx", new SettingsEntity(), new FilterService());

            List<string> before = WordDocumentRedactor.ReadReviewLines(input);
            List<string> after = WordDocumentRedactor.ReadReviewLines(output);

            Assert.Contains(before, l => l.Contains("c@example.com"));       // chart PII on the before side
            Assert.DoesNotContain(after, l => l.Contains("c@example.com"));  // gone on the after side
            Assert.Contains(after, l => l.Contains("REDACTED"));            // shown as a redaction
            Assert.Equal(before.Count, after.Count);                        // structure preserved -> diff aligns
        }

        [Fact]
        public async Task ReadReviewLines_AlsoShowsBodyRedaction_AlongsideDrawing()
        {
            string input = NewPath("both.docx");
            string output = NewPath("both-out.docx");
            WordDocs.CreateWithChart(input, WordDocs.AParagraph("chart c@example.com"), "body b@example.com");

            await RedactionService.RedactFileAsync(input, output, EmailPolicy, "ctx", new SettingsEntity(), new FilterService());

            List<string> after = WordDocumentRedactor.ReadReviewLines(output);
            Assert.DoesNotContain(after, l => l.Contains("b@example.com")); // body redaction shown too
            Assert.DoesNotContain(after, l => l.Contains("c@example.com")); // chart redaction shown too
        }
    }
}
