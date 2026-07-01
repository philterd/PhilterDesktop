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
using Phileas.Policy;
using Phileas.Policy.Filters;
using PhilterData;
using PhilterDesktop;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// A redacted .txt must keep the source's byte encoding and BOM (a UTF-16 or UTF-8-with-BOM input was
    /// previously re-encoded to UTF-8 no-BOM). Covers the redact path and the Modify re-apply path, plus
    /// the shared encoding detector.
    /// </summary>
    public sealed class TextEncodingPreservationTests : IDisposable
    {
        private readonly string _dir;

        public TextEncodingPreservationTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-enc-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private static PhileasPolicy EmailPolicy() => new()
        {
            Name = "p",
            Identifiers = new Identifiers { EmailAddress = new EmailAddress() }
        };

        // Non-ASCII ("é") proves the bytes are decoded/re-encoded in the right encoding, not just the BOM.
        private const string Content = "Café contact a@b.com today.";

        public static IEnumerable<object[]> Encodings => new[]
        {
            new object[] { "utf8-nobom", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false) },
            new object[] { "utf8-bom", new UTF8Encoding(encoderShouldEmitUTF8Identifier: true) },
            new object[] { "utf16-le", new UnicodeEncoding(bigEndian: false, byteOrderMark: true) },
            new object[] { "utf16-be", new UnicodeEncoding(bigEndian: true, byteOrderMark: true) },
        };

        [Theory]
        [MemberData(nameof(Encodings))]
        public async Task Redact_Text_PreservesEncodingAndBom(string name, Encoding encoding)
        {
            string input = Path.Combine(_dir, name + "-in.txt");
            string output = Path.Combine(_dir, name + "-out.txt");
            await File.WriteAllTextAsync(input, Content, encoding);

            await RedactionService.RedactFileAsync(input, output, EmailPolicy(), "ctx");

            AssertPreambleMatches(encoding, output);

            // Auto-detecting the output (strips the BOM) proves the bytes decode correctly in their
            // encoding: the accented text survives intact and the email is gone.
            string decoded = await File.ReadAllTextAsync(output);
            Assert.Contains("Café", decoded);
            Assert.DoesNotContain("a@b.com", decoded);
        }

        [Theory]
        [MemberData(nameof(Encodings))]
        public async Task ApplySpans_Text_PreservesEncodingAndBom(string name, Encoding encoding)
        {
            string input = Path.Combine(_dir, name + "-src.txt");
            await File.WriteAllTextAsync(input, Content, encoding);

            var spans = await RedactionService.RedactFileAsync(input, Path.Combine(_dir, name + "-v1.txt"), EmailPolicy(), "ctx");
            string output = Path.Combine(_dir, name + "-v2.txt");
            await RedactionService.ApplySpansAsync(input, output, ".txt", highlight: false, spans);

            AssertPreambleMatches(encoding, output);
            string decoded = await File.ReadAllTextAsync(output);
            Assert.Contains("Café", decoded);
            Assert.DoesNotContain("a@b.com", decoded);
        }

        [Fact]
        public async Task Redact_Utf8NoBom_DoesNotGainABom()
        {
            string input = Path.Combine(_dir, "plain-in.txt");
            string output = Path.Combine(_dir, "plain-out.txt");
            await File.WriteAllTextAsync(input, Content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            await RedactionService.RedactFileAsync(input, output, EmailPolicy(), "ctx");

            byte[] bytes = await File.ReadAllBytesAsync(output);
            Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
                "a UTF-8 no-BOM source must not gain a BOM");
        }

        [Theory]
        [MemberData(nameof(Encodings))]
        public void Detector_IdentifiesEncodingFromBom(string name, Encoding encoding)
        {
            string path = Path.Combine(_dir, name + "-detect.txt");
            File.WriteAllText(path, Content, encoding);

            Encoding detected = TextEncodingDetector.Detect(path);

            // The detector returns an encoding whose preamble matches what was written.
            Assert.Equal(encoding.GetPreamble(), detected.GetPreamble());
        }

        [Fact]
        public void Detector_UnreadableOrMissing_FallsBackToUtf8NoBom()
        {
            Encoding detected = TextEncodingDetector.Detect(Path.Combine(_dir, "does-not-exist.txt"));
            Assert.Empty(detected.GetPreamble()); // UTF-8 no BOM
        }

        [Fact]
        public async Task SafeOutput_WriteTextAsync_WithEncoding_RoundTrips()
        {
            var encoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: true);
            string path = Path.Combine(_dir, "safeout.txt");

            await SafeOutput.WriteTextAsync(path, "Café", encoding);

            byte[] bytes = await File.ReadAllBytesAsync(path);
            Assert.Equal(new byte[] { 0xFF, 0xFE }, bytes[..2]); // UTF-16 LE BOM
            Assert.Equal("Café", await File.ReadAllTextAsync(path)); // auto-detect strips the BOM
        }

        private static void AssertPreambleMatches(Encoding encoding, string path)
        {
            byte[] expected = encoding.GetPreamble();
            byte[] bytes = File.ReadAllBytes(path);
            Assert.True(bytes.Length >= expected.Length, "output shorter than the expected preamble");
            Assert.Equal(expected, bytes[..expected.Length]);
        }
    }
}
