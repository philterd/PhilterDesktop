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
using Xceed.Words.NET;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Integration tests that redact the sample documents in <c>test-documents/</c> end-to-end
    /// through <see cref="RedactionService"/> and compare the redacted output to the expected
    /// result. The .txt and .docx samples contain identical text, so they must redact identically.
    /// </summary>
    public sealed class SampleDocumentRedactionTests : IClassFixture<XceedLicenseFixture>, IDisposable
    {
        // The samples contain: "...an email george@fake.com and SSN 123-45-6789."
        private const string Email = "george@fake.com";
        private const string Ssn = "123-45-6789";
        private const string ExpectedRedacted =
            "This is a sample document with an email {{{REDACTED-email-address}}} and SSN {{{REDACTED-ssn}}}.";

        private static readonly string SamplesDir = Path.Combine(AppContext.BaseDirectory, "test-documents");

        private readonly XceedLicenseFixture _license;
        private readonly string _tempDir;

        public SampleDocumentRedactionTests(XceedLicenseFixture license)
        {
            _license = license;
            _tempDir = Path.Combine(Path.GetTempPath(), "philter-sample-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        }

        private static readonly string[] Names = { "John", "Smith", "Mary", "Johnson" };

        private static PhileasPolicy SamplePolicy() => new()
        {
            Name = "sample",
            Identifiers = new Identifiers { EmailAddress = new EmailAddress(), Ssn = new Ssn() }
        };

        private static PhileasPolicy NamePolicy() => new()
        {
            Name = "names",
            Identifiers = new Identifiers { FirstName = new FirstName(), Surname = new Surname() }
        };

        [Fact]
        public async Task TextSample2_RedactsPersonNames()
        {
            string input = Path.Combine(SamplesDir, "test2.txt");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "test2_redacted.txt");

            await RedactionService.RedactFileAsync(input, output, NamePolicy(), "ctx");

            string result = await File.ReadAllTextAsync(output);
            foreach (string name in Names)
            {
                Assert.DoesNotContain(name, result);
            }
            Assert.Contains("Patient", result);
            Assert.Contains("REDACTED", result);
        }

        [SkippableFact]
        public async Task WordSample2_RedactsPersonNames()
        {
            Skip.IfNot(_license.HasLicense, "Xceed license not configured.");

            string input = Path.Combine(SamplesDir, "test2.docx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "test2_redacted.docx");

            await RedactionService.RedactFileAsync(input, output, NamePolicy(), "ctx");

            string result = ReadDocxText(output);
            foreach (string name in Names)
            {
                Assert.DoesNotContain(name, result);
            }
            Assert.Contains("Patient", result);
        }

        [Fact]
        public async Task TextSample_RedactsToExpectedResult()
        {
            string input = Path.Combine(SamplesDir, "test1.txt");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "test1_redacted.txt");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            string result = File.ReadAllText(output).Trim();
            Assert.Equal(ExpectedRedacted, result);
            Assert.DoesNotContain(Email, result);
            Assert.DoesNotContain(Ssn, result);
        }

        [SkippableFact]
        public async Task WordSample_RedactsToExpectedResult()
        {
            Skip.IfNot(_license.HasLicense, "Xceed license not configured.");

            string input = Path.Combine(SamplesDir, "test1.docx");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "test1_redacted.docx");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            string result = ReadDocxText(output);
            Assert.Equal(ExpectedRedacted, result);
            Assert.DoesNotContain(Email, result);
            Assert.DoesNotContain(Ssn, result);

            // The original sample must be untouched.
            Assert.Contains(Ssn, ReadDocxText(input));
        }

        [SkippableFact]
        public async Task TextAndWordSamples_RedactToSameText()
        {
            Skip.IfNot(_license.HasLicense, "Xceed license not configured.");

            string txtOut = Path.Combine(_tempDir, "t.txt");
            string docxOut = Path.Combine(_tempDir, "t.docx");
            await RedactionService.RedactFileAsync(Path.Combine(SamplesDir, "test1.txt"), txtOut, SamplePolicy(), "ctx");
            await RedactionService.RedactFileAsync(Path.Combine(SamplesDir, "test1.docx"), docxOut, SamplePolicy(), "ctx");

            Assert.Equal(File.ReadAllText(txtOut).Trim(), ReadDocxText(docxOut));
        }

        [Fact]
        public async Task PdfSample_RedactsToImageBasedPdf()
        {
            string input = Path.Combine(SamplesDir, "test1.pdf");
            Assert.True(File.Exists(input), $"Sample not found: {input}");
            string output = Path.Combine(_tempDir, "test1_redacted.pdf");

            await RedactionService.RedactFileAsync(input, output, SamplePolicy(), "ctx");

            byte[] bytes = await File.ReadAllBytesAsync(output);
            Assert.True(bytes.Length > 0);
            Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));

            // Pages are rasterized, so the original PII has no recoverable text layer.
            string asBytes = System.Text.Encoding.Latin1.GetString(bytes);
            Assert.DoesNotContain(Email, asBytes);
            Assert.DoesNotContain(Ssn, asBytes);
        }

        private static string ReadDocxText(string path)
        {
            using DocX doc = DocX.Load(path);
            return string.Concat(doc.Paragraphs.Select(p => p.Text)).Trim();
        }
    }
}
