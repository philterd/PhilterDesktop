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
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Tests for the extracted <see cref="RedactionService"/> — output-path resolution and
    /// file-type redaction dispatch (the core pipeline that used to live in MainForm).
    /// </summary>
    public sealed class RedactionServiceTests : IClassFixture<XceedLicenseFixture>, IDisposable
    {
        private readonly XceedLicenseFixture _license;
        private readonly string _tempDir;

        public RedactionServiceTests(XceedLicenseFixture license)
        {
            _license = license;
            _tempDir = Path.Combine(Path.GetTempPath(), "philter-redact-svc-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        }

        [Theory]
        [InlineData("report.txt", true)]
        [InlineData("report.docx", true)]
        [InlineData("report.DOCX", true)]
        [InlineData("report.pdf", true)]
        [InlineData("report.doc", false)]   // legacy binary Word is not supported
        [InlineData("report", false)]
        public void IsSupported_RecognizesRedactableTypes(string name, bool expected)
        {
            Assert.Equal(expected, RedactionService.IsSupported(name));
        }

        [Fact]
        public void GetOutputPath_OriginalLocation_AddsSuffixKeepsExtension()
        {
            var settings = new SettingsEntity { OutputToOriginalLocation = true };
            string input = Path.Combine(_tempDir, "report.txt");

            string output = RedactionService.GetOutputPath(input, settings);

            Assert.Equal(Path.Combine(_tempDir, "report_redacted.txt"), output);
        }

        [Fact]
        public void GetOutputPath_CustomFolder_UsesThatFolder()
        {
            string custom = Path.Combine(_tempDir, "out");
            var settings = new SettingsEntity { OutputToOriginalLocation = false, CustomOutputFolder = custom };
            string input = Path.Combine(_tempDir, "in", "memo.docx");

            string output = RedactionService.GetOutputPath(input, settings);

            Assert.Equal(Path.Combine(custom, "memo_redacted.docx"), output);
        }

        [Fact]
        public async Task RedactFileAsync_TextFile_RedactsEnabledTypes()
        {
            string input = Path.Combine(_tempDir, "in.txt");
            string output = Path.Combine(_tempDir, "out.txt");
            await File.WriteAllTextAsync(input, "Email john@example.com phone 555-123-4567 ssn 123-45-6789.");

            var policy = EditorStylePolicy();

            await RedactionService.RedactFileAsync(input, output, policy, "ctx");

            string result = await File.ReadAllTextAsync(output);
            Assert.DoesNotContain("john@example.com", result);
            Assert.DoesNotContain("555-123-4567", result);
            Assert.DoesNotContain("123-45-6789", result);
        }

        [SkippableFact]
        public async Task RedactFileAsync_Docx_RedactsViaWordRedactor()
        {
            Skip.IfNot(_license.HasLicense, "Xceed license not configured.");

            string input = Path.Combine(_tempDir, "in.docx");
            string output = Path.Combine(_tempDir, "out.docx");
            using (var doc = Xceed.Words.NET.DocX.Create(input))
            {
                doc.InsertParagraph("ssn 123-45-6789 here.");
                doc.Save();
            }

            await RedactionService.RedactFileAsync(input, output, EditorStylePolicy(), "ctx");

            using var redacted = Xceed.Words.NET.DocX.Load(output);
            string text = string.Join("\n", redacted.Paragraphs.Select(p => p.Text));
            Assert.DoesNotContain("123-45-6789", text);
        }

        private static PhileasPolicy EditorStylePolicy() => new()
        {
            Name = "svc",
            Identifiers = new Identifiers
            {
                Ssn = new Ssn(),
                EmailAddress = new EmailAddress(),
                PhoneNumber = new PhoneNumber()
            }
        };
    }
}
