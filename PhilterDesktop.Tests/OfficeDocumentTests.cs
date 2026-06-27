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
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    public sealed class OfficeDocumentTests : IDisposable
    {
        private readonly string _tempDir;

        public OfficeDocumentTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "philter-office-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        }

        private static readonly byte[] Ole2 = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

        [Fact]
        public void Inspect_RealDocx_IsOk()
        {
            string path = Path.Combine(_tempDir, "real.docx");
            WordDocs.Create(path, "Hello world.");
            Assert.Equal(OfficeFileState.Ok, OfficeDocument.Inspect(path));
        }

        [Fact]
        public void Inspect_RealXlsx_IsOk()
        {
            string path = Path.Combine(_tempDir, "real.xlsx");
            SpreadsheetTestHelper.CreateXlsx(path, new[] { new string?[] { "A", "B" } });
            Assert.Equal(OfficeFileState.Ok, OfficeDocument.Inspect(path));
        }

        [Fact]
        public void Inspect_Ole2CompoundFile_IsPasswordProtected()
        {
            // A password-protected .xlsx/.docx is an OLE2 compound file, not a ZIP.
            string path = Path.Combine(_tempDir, "locked.xlsx");
            File.WriteAllBytes(path, Ole2.Concat(new byte[64]).ToArray());
            Assert.Equal(OfficeFileState.PasswordProtected, OfficeDocument.Inspect(path));
        }

        [Fact]
        public void Inspect_RandomBytes_IsNotReadable()
        {
            string path = Path.Combine(_tempDir, "junk.docx");
            File.WriteAllText(path, "this is not an office file at all");
            Assert.Equal(OfficeFileState.NotReadable, OfficeDocument.Inspect(path));
        }

        [Fact]
        public async Task RedactFileAsync_PasswordProtectedXlsx_ThrowsFriendlyError()
        {
            string input = Path.Combine(_tempDir, "secret.xlsx");
            File.WriteAllBytes(input, Ole2.Concat(new byte[64]).ToArray());
            string output = Path.Combine(_tempDir, "out.xlsx");

            DocumentLoadException ex = await Assert.ThrowsAsync<DocumentLoadException>(() =>
                RedactionService.RedactFileAsync(input, output, new PhileasPolicy { Name = "p" }, "ctx"));

            Assert.Contains("password-protected", ex.Message);
            Assert.Contains("Excel", ex.Message);          // app-specific guidance
            Assert.Contains("secret.xlsx", ex.Message);
        }

        [Fact]
        public async Task RedactFileAsync_CorruptDocx_ThrowsFriendlyError()
        {
            string input = Path.Combine(_tempDir, "broken.docx");
            File.WriteAllText(input, "definitely not a docx");
            string output = Path.Combine(_tempDir, "out.docx");

            DocumentLoadException ex = await Assert.ThrowsAsync<DocumentLoadException>(() =>
                RedactionService.RedactFileAsync(input, output, new PhileasPolicy { Name = "p" }, "ctx"));

            Assert.Contains("could not be opened", ex.Message);
            Assert.Contains("Word", ex.Message);
        }

        [Fact]
        public void UserError_Describe_ReturnsDocumentLoadMessageVerbatim()
        {
            var ex = new DocumentLoadException("This file is password-protected.");
            Assert.Equal("This file is password-protected.", UserError.Describe(ex, @"C:\x\secret.xlsx", writing: true));
        }
    }
}
