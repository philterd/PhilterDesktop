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
using Phileas.Model;
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using PhilterData;
using PhilterDesktop;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The overlapping-window streamer must redact a large .txt exactly as the whole-string path would,
    /// including entities that straddle window seams. Tests force many tiny windows over small
    /// inputs so the boundary logic is exercised deterministically. Emails are surrounded by spaces so
    /// each is a discrete token at a known position (the detector treats adjacent non-space text as part
    /// of the address).
    /// </summary>
    public sealed class TextStreamRedactorTests : IDisposable
    {
        private static readonly PhileasPolicy EmailPolicy = new()
        {
            Name = "email",
            Identifiers = new Identifiers { EmailAddress = new EmailAddress() }
        };

        private readonly FilterService _filterService = new();
        private readonly string _dir;

        public TextStreamRedactorTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-txtstream-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private Func<string, TextFilterResult> Filter => t => _filterService.Filter(EmailPolicy, "ctx", 0, t);

        private string Write(string name, string content, Encoding? encoding = null)
        {
            string path = Path.Combine(_dir, name);
            File.WriteAllText(path, content, encoding ?? new UTF8Encoding(false));
            return path;
        }

        // Streams with deliberately tiny windows (40-char core, 30-char overlap) to force many seams.
        private List<Span> StreamRedact(string input, string output, Encoding? encoding = null) =>
            TextStreamRedactor.Redact(input, output, Filter, encoding ?? new UTF8Encoding(false),
                chunkChars: 40, overlapChars: 30);

        private string WholeStringRedact(string content) => Filter(content).FilteredText;

        // Spaces don't merge into an email, so the address is a discrete token at index `pad`.
        private static string Padded(int pad, string email, int trailing) =>
            new string(' ', pad) + email + new string(' ', trailing);

        [Fact]
        public void Redact_ManyEmailsAcrossManyChunks_MatchesWholeStringRedaction()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 12; i++)
            {
                sb.Append("some filler text user").Append(i).Append("@example.com and more words. ");
            }
            string content = sb.ToString();
            string input = Write("many.txt", content);
            string output = Path.Combine(_dir, "many_out.txt");

            List<Span> spans = StreamRedact(input, output);

            Assert.Equal(WholeStringRedact(content), File.ReadAllText(output));
            Assert.DoesNotContain("@example.com", File.ReadAllText(output));
            Assert.Equal(12, spans.Count);
        }

        [Theory]
        [InlineData(25)] // straddles the first 40-char seam
        [InlineData(30)]
        [InlineData(38)]
        [InlineData(40)] // starts exactly at the seam
        [InlineData(45)] // starts just past the horizon -> committed in the next window
        [InlineData(79)] // straddles the second seam
        public void Redact_EmailAtEveryBoundaryPosition_IsRedacted(int emailStart)
        {
            string email = "boundary@example.com";
            string content = Padded(emailStart, email, 60);
            string input = Write($"b{emailStart}.txt", content);
            string output = Path.Combine(_dir, $"b{emailStart}_out.txt");

            List<Span> spans = StreamRedact(input, output);

            string result = File.ReadAllText(output);
            Assert.DoesNotContain(email, result);
            Assert.Equal(WholeStringRedact(content), result);
            Assert.Single(spans);
        }

        [Fact]
        public void Redact_BackToBackEmailsAcrossSeam_BothRedacted()
        {
            // Two space-separated emails positioned so the seam falls within them.
            string content = new string(' ', 33) + "one@example.com two@example.com" + new string(' ', 20);
            string input = Write("adjacent.txt", content);
            string output = Path.Combine(_dir, "adjacent_out.txt");

            List<Span> spans = StreamRedact(input, output);

            string result = File.ReadAllText(output);
            Assert.DoesNotContain("@example.com", result);
            Assert.Equal(WholeStringRedact(content), result);
            Assert.Equal(2, spans.Count);
        }

        [Fact]
        public void Redact_GlobalSpanOffsets_PointAtTheEmailInTheOriginal()
        {
            string email = "target@example.com";
            string content = Padded(50, email, 50); // email at index 50
            string input = Write("offsets.txt", content);
            string output = Path.Combine(_dir, "offsets_out.txt");

            List<Span> spans = StreamRedact(input, output);

            Span span = Assert.Single(spans);
            Assert.Equal(50, span.CharacterStart);                 // whole-file coordinate
            Assert.Equal(50 + email.Length, span.CharacterEnd);
            Assert.Equal(email, content.Substring(span.CharacterStart, span.CharacterEnd - span.CharacterStart));
            Assert.Equal(email, span.Text);
        }

        [Fact]
        public void Redact_LongUnbrokenRun_NoWhitespaceToSplitOn_StillRedactsCorrectly()
        {
            // A window boundary lands inside a long run with no whitespace, so the boundary-nudge finds
            // nothing and falls back to the raw split — redaction must still be correct.
            string content = new string('x', 200) + " contact user@example.com after.";
            string input = Write("nospace.txt", content);
            string output = Path.Combine(_dir, "nospace_out.txt");

            List<Span> spans = StreamRedact(input, output);

            Assert.Equal(WholeStringRedact(content), File.ReadAllText(output));
            Assert.DoesNotContain("user@example.com", File.ReadAllText(output));
            Assert.Single(spans);
        }

        [Fact]
        public void Redact_NoEntities_OutputEqualsInput()
        {
            string content = string.Concat(Enumerable.Repeat("just some plain text with no PII here. ", 20));
            string input = Write("plain.txt", content);
            string output = Path.Combine(_dir, "plain_out.txt");

            List<Span> spans = StreamRedact(input, output);

            Assert.Empty(spans);
            Assert.Equal(content, File.ReadAllText(output));
        }

        [Fact]
        public void Redact_PreservesNewlinesAndExactText()
        {
            string content = "line1 keep@example.com\r\nline2\nline3 also@example.com\r\n" + new string(' ', 90) + "end";
            string input = Write("lines.txt", content);
            string output = Path.Combine(_dir, "lines_out.txt");

            StreamRedact(input, output);

            string result = File.ReadAllText(output);
            Assert.Equal(WholeStringRedact(content), result);
            Assert.Contains("\r\n", result);          // CRLF preserved
            Assert.Contains("\nline3", result);        // lone LF preserved
        }

        [Fact]
        public void Redact_PreservesEncodingAndBom()
        {
            var utf16 = new UnicodeEncoding(bigEndian: false, byteOrderMark: true);
            string content = Padded(45, "boundary@example.com", 45);
            string input = Write("utf16.txt", content, utf16);
            string output = Path.Combine(_dir, "utf16_out.txt");

            StreamRedact(input, output, utf16);

            byte[] bytes = File.ReadAllBytes(output);
            Assert.Equal(new byte[] { 0xFF, 0xFE }, bytes[..2]);   // UTF-16 LE BOM preserved
            string decoded = File.ReadAllText(output);             // auto-detect
            Assert.DoesNotContain("boundary@example.com", decoded);
            Assert.Equal(WholeStringRedact(content), decoded);
        }

        [Fact]
        public void Redact_EmptyFile_ProducesEmptyOutput()
        {
            string input = Write("empty.txt", string.Empty);
            string output = Path.Combine(_dir, "empty_out.txt");

            List<Span> spans = StreamRedact(input, output);

            Assert.Empty(spans);
            Assert.True(File.Exists(output));
            Assert.Equal(string.Empty, File.ReadAllText(output));
        }

        [Fact]
        public void Redact_LeavesNoTempFileBehind()
        {
            string content = Padded(100, "x@example.com", 5);
            string input = Write("temp.txt", content);
            string output = Path.Combine(_dir, "temp_out.txt");

            StreamRedact(input, output);

            Assert.True(File.Exists(output));
            Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));       // temp cleaned up / committed
            Assert.Empty(Directory.GetFiles(_dir, "*.redacting-*"));
        }

        [Fact]
        public async Task RedactFileAsync_LargeTxt_UsesStreamingPath_AndRedacts()
        {
            // A file over the 50 MB streaming threshold must be redacted (via the streaming path) end to end.
            string input = Path.Combine(_dir, "huge.txt");
            string email = "needle@example.com";
            await using (var w = new StreamWriter(input, append: false, new UTF8Encoding(false)))
            {
                string filler = new string('a', 1024 * 1024) + "\n"; // 1 MB + newline (no '@', so no match)
                for (int i = 0; i < 55; i++) // ~55 MB, over the threshold
                {
                    await w.WriteAsync(filler);
                }
                await w.WriteAsync(" " + email + " "); // discrete PII near the very end, past many seams
            }
            string output = Path.Combine(_dir, "huge_out.txt");

            List<RedactionSpanEntity> spans = await RedactionService.RedactFileAsync(
                input, output, EmailPolicy, "ctx", new SettingsEntity(), _filterService);

            Assert.Contains(spans, s => s.Text == email);
            Assert.DoesNotContain(email, File.ReadAllText(output));
        }
    }
}
