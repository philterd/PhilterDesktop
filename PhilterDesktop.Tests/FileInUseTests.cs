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
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Redacting a file another program has open (as Word does) fails with a plain message naming that
    /// file, and writes nothing.
    /// </summary>
    public sealed class FileInUseTests : IDisposable
    {
        private readonly string _tempDir;

        public FileInUseTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "philter-inuse-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        }

        private static PhileasPolicy SsnPolicy() => new() { Identifiers = new Identifiers { Ssn = new Ssn() } };

        // Word keeps an open document for writing and lets others read only.
        private static FileStream OpenLikeWord(string path) =>
            new(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);

        private string Docx(string name)
        {
            string path = Path.Combine(_tempDir, name);
            WordDocs.Create(path, "My SSN is 123-45-6789.");
            return path;
        }

        private string Text(string name)
        {
            string path = Path.Combine(_tempDir, name);
            File.WriteAllText(path, "My SSN is 123-45-6789.");
            return path;
        }

        [Fact]
        public void Unopened_File_Passes() =>
            RedactionService.EnsureInputNotInUse(Text("free.txt"));

        [Fact]
        public void File_OpenOnlyForReading_Passes()
        {
            string path = Text("reader.txt");
            using var reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            RedactionService.EnsureInputNotInUse(path);
        }

        [Fact]
        public void File_OpenForWriting_IsReportedAsInUse()
        {
            string path = Docx("My SSN is 123.docx");
            using FileStream word = OpenLikeWord(path);

            DocumentLoadException ex = Assert.Throws<DocumentLoadException>(() => RedactionService.EnsureInputNotInUse(path));

            Assert.Equal("\"My SSN is 123.docx\" is open in another program (such as Microsoft Word), so it can't be redacted. " +
                         "Close it and try again.", ex.Message);
        }

        [Fact]
        public void File_OpenExclusively_IsReportedAsInUse()
        {
            string path = Text("exclusive.txt");
            using var exclusive = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);

            Assert.Throws<DocumentLoadException>(() => RedactionService.EnsureInputNotInUse(path));
        }

        [Fact]
        public void MissingFile_IsNotReportedAsInUse()
        {
            // Other failures keep their own exception (and message).
            Assert.Throws<FileNotFoundException>(() => RedactionService.EnsureInputNotInUse(Path.Combine(_tempDir, "gone.txt")));
        }

        [Theory]
        [InlineData("open.docx")]
        [InlineData("open.txt")]
        public async Task RedactFileAsync_OpenFile_FailsWithTheMessage_AndWritesNothing(string name)
        {
            string input = name.EndsWith(".docx") ? Docx(name) : Text(name);
            string output = Path.Combine(_tempDir, "out" + Path.GetExtension(name));
            using FileStream word = OpenLikeWord(input);

            DocumentLoadException ex = await Assert.ThrowsAsync<DocumentLoadException>(() =>
                RedactionService.RedactFileAsync(input, output, SsnPolicy(), "ctx"));

            Assert.StartsWith("\"" + name + "\" is open in another program", ex.Message);
            Assert.False(File.Exists(output));
        }

        [Fact]
        public async Task RedactFileAsync_Succeeds_OnceTheFileIsClosed()
        {
            string input = Text("later.txt");
            string output = Path.Combine(_tempDir, "later-out.txt");
            using (OpenLikeWord(input))
            {
                await Assert.ThrowsAsync<DocumentLoadException>(() => RedactionService.RedactFileAsync(input, output, SsnPolicy(), "ctx"));
            }

            await RedactionService.RedactFileAsync(input, output, SsnPolicy(), "ctx");

            Assert.DoesNotContain("123-45-6789", File.ReadAllText(output));
        }

        [Fact]
        public async Task QueueFailure_ShowsTheMessage_NotTheOutputFile()
        {
            string input = Docx("queued.docx");
            string output = Path.Combine(_tempDir, "queued_redacted-draft.docx");
            using FileStream word = OpenLikeWord(input);
            Exception ex = await Assert.ThrowsAsync<DocumentLoadException>(() => RedactionService.RedactFileAsync(input, output, SsnPolicy(), "ctx"));

            string shown = QueueProcessor.DescribeFailure(QueueRedactionResult.Failed(ex.Message, ex), output);

            Assert.Equal(ex.Message, shown);
            Assert.Contains("queued.docx", shown);
            Assert.DoesNotContain("redacted-draft", shown);
            Assert.DoesNotContain("—", shown);
        }

        [Fact]
        public void IsFileInUse_MatchesSharingAndLockViolations_Only()
        {
            Assert.True(UserError.IsFileInUse(new IOException("x", unchecked((int)0x80070020))));
            Assert.True(UserError.IsFileInUse(new IOException("x", unchecked((int)0x80070021))));
            Assert.False(UserError.IsFileInUse(new IOException("x", unchecked((int)0x80070070)))); // disk full
            Assert.False(UserError.IsFileInUse(new IOException("x")));
        }
    }
}
