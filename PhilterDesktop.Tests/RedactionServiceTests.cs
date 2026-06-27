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
    public sealed class RedactionServiceTests : IDisposable
    {
        private readonly string _tempDir;

        public RedactionServiceTests()
        {
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
        [InlineData("report.rtf", true)]
        [InlineData("report.RTF", true)]
        [InlineData("message.eml", true)]
        [InlineData("message.msg", true)]
        [InlineData("message.MSG", true)]
        [InlineData("report.doc", false)]   // legacy binary Word is not supported
        [InlineData("report", false)]
        public void IsSupported_RecognizesRedactableTypes(string name, bool expected)
        {
            Assert.Equal(expected, RedactionService.IsSupported(name));
        }

        [Theory]
        [InlineData("memo.txt", ".txt")]
        [InlineData("memo.docx", ".docx")]
        [InlineData("memo.pdf", ".pdf")]
        [InlineData("memo.rtf", ".rtf")]   // RTF keeps its own extension
        [InlineData("memo.eml", ".eml")]
        [InlineData("memo.msg", ".eml")]   // .msg is read but written back as .eml
        [InlineData("memo.MSG", ".eml")]
        public void OutputExtension_MapsMsgToEml(string name, string expected)
        {
            Assert.Equal(expected, RedactionService.OutputExtension(name));
        }

        [Theory]
        [InlineData("book.xlsx", true)]
        [InlineData("data.csv", true)]
        [InlineData("note.eml", true)]
        [InlineData("note.msg", true)]
        [InlineData(".CSV", true)]               // bare extension, case-insensitive
        [InlineData("memo.docx", false)]         // paragraph-addressed (Modify can add by hand)
        [InlineData("report.pdf", false)]
        [InlineData("notes.txt", false)]
        [InlineData("letter.rtf", false)]        // whole-text offsets
        public void UsesOrdinalSpanAddressing_IdentifiesCellAndFieldFormats(string input, bool expected)
        {
            Assert.Equal(expected, RedactionService.UsesOrdinalSpanAddressing(input));
        }

        [Fact]
        public void GetOutputPath_MsgInput_ProducesEmlOutput()
        {
            var settings = new SettingsEntity { OutputToOriginalLocation = true };
            string input = Path.Combine(_tempDir, "memo.msg");

            string output = RedactionService.GetOutputPath(input, settings);

            Assert.Equal(Path.Combine(_tempDir, "memo_redacted-draft.eml"), output);
        }

        [Fact]
        public void GetOutputPath_OriginalLocation_AddsSuffixKeepsExtension()
        {
            var settings = new SettingsEntity { OutputToOriginalLocation = true };
            string input = Path.Combine(_tempDir, "report.txt");

            string output = RedactionService.GetOutputPath(input, settings);

            Assert.Equal(Path.Combine(_tempDir, "report_redacted-draft.txt"), output);
        }

        [Fact]
        public void GetOutputPath_CustomFolder_UsesThatFolder()
        {
            string custom = Path.Combine(_tempDir, "out");
            var settings = new SettingsEntity { OutputToOriginalLocation = false, CustomOutputFolder = custom };
            string input = Path.Combine(_tempDir, "in", "memo.docx");

            string output = RedactionService.GetOutputPath(input, settings);

            Assert.Equal(Path.Combine(custom, "memo_redacted-draft.docx"), output);
        }

        [Fact]
        public void GetOutputPath_UsesConfiguredSuffix()
        {
            var settings = new SettingsEntity { OutputToOriginalLocation = true, RedactedSuffix = "_clean" };
            string input = Path.Combine(_tempDir, "report.txt");

            string output = RedactionService.GetOutputPath(input, settings);

            Assert.Equal(Path.Combine(_tempDir, "report_clean.txt"), output);
        }

        [Theory]
        [InlineData(null, "_redacted-draft")]
        [InlineData("", "_redacted-draft")]
        [InlineData("   ", "_redacted-draft")]
        [InlineData("_clean", "_clean")]
        [InlineData("  _spaced  ", "_spaced")]
        [InlineData("_bad:/\\name", "_badname")] // invalid file-name chars stripped
        public void NormalizeSuffix_CleansOrFallsBackToDefault(string? input, string expected)
        {
            Assert.Equal(expected, RedactionService.NormalizeSuffix(input));
        }

        [Fact]
        public void ResolveOutputPath_NoVersions_ReturnsDefault()
        {
            string fallback = Path.Combine(_tempDir, "default.txt");
            string result = RedactionService.ResolveOutputPath(Array.Empty<RedactionVersionEntity>(), fallback);
            Assert.Equal(fallback, result);
        }

        [Fact]
        public void ResolveOutputPath_ReturnsLatestVersionWhoseOutputExists()
        {
            string out1 = CreateFile("v1.txt");
            string out2 = CreateFile("v2.txt");
            var versions = new[]
            {
                new RedactionVersionEntity { Version = 1, OutputPath = out1 },
                new RedactionVersionEntity { Version = 2, OutputPath = out2 }, // latest, exists
            };

            string result = RedactionService.ResolveOutputPath(versions, Path.Combine(_tempDir, "default.txt"));

            Assert.Equal(out2, result);
        }

        [Fact]
        public void ResolveOutputPath_SkipsLatest_WhenItsOutputIsMissing()
        {
            string out1 = CreateFile("v1.txt");
            var versions = new[]
            {
                new RedactionVersionEntity { Version = 1, OutputPath = out1 },                       // exists
                new RedactionVersionEntity { Version = 2, OutputPath = Path.Combine(_tempDir, "gone.txt") }, // missing
            };

            string result = RedactionService.ResolveOutputPath(versions, Path.Combine(_tempDir, "default.txt"));

            Assert.Equal(out1, result); // latest *existing*
        }

        [Fact]
        public void ResolveOutputPath_ReturnsDefault_WhenNoStoredOutputExists()
        {
            string fallback = Path.Combine(_tempDir, "default.txt");
            var versions = new[]
            {
                new RedactionVersionEntity { Version = 1, OutputPath = string.Empty },
                new RedactionVersionEntity { Version = 2, OutputPath = Path.Combine(_tempDir, "gone.txt") },
            };

            string result = RedactionService.ResolveOutputPath(versions, fallback);

            Assert.Equal(fallback, result);
        }

        private string CreateFile(string name)
        {
            string path = Path.Combine(_tempDir, name);
            File.WriteAllText(path, "x");
            return path;
        }

        [Fact]
        public void InitialSaveDirectory_PrefersExistingLastSaveFolder()
        {
            var settings = new SettingsEntity { LastSaveFolder = _tempDir };
            string suggested = Path.Combine(@"C:\some\other", "x_redacted-draft.txt");

            string dir = RedactionService.InitialSaveDirectory(settings, suggested, @"C:\src\x.txt");

            Assert.Equal(_tempDir, dir);
        }

        [Fact]
        public void InitialSaveDirectory_FallsBackToSuggestedFolder_WhenNoLastSaveFolder()
        {
            var settings = new SettingsEntity { LastSaveFolder = string.Empty };
            string suggested = Path.Combine(_tempDir, "x_redacted-draft.txt");

            string dir = RedactionService.InitialSaveDirectory(settings, suggested, @"C:\src\x.txt");

            Assert.Equal(_tempDir, dir);
        }

        [Fact]
        public void InitialSaveDirectory_IgnoresLastSaveFolder_WhenItNoLongerExists()
        {
            var settings = new SettingsEntity { LastSaveFolder = Path.Combine(_tempDir, "gone") };
            string suggested = Path.Combine(_tempDir, "x_redacted-draft.txt");

            string dir = RedactionService.InitialSaveDirectory(settings, suggested, @"C:\src\x.txt");

            Assert.Equal(_tempDir, dir); // falls through to the suggested folder
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

        [Fact]
        public async Task RedactFileAsync_CapturesExplanationDetailOnSpans()
        {
            string input = Path.Combine(_tempDir, "in.txt");
            string output = Path.Combine(_tempDir, "out.txt");
            await File.WriteAllTextAsync(input, "Email john@example.com here.");

            var policy = new PhileasPolicy { Name = "x", Identifiers = new Identifiers { EmailAddress = new EmailAddress() } };
            List<RedactionSpanEntity> spans = await RedactionService.RedactFileAsync(input, output, policy, "ctx");

            RedactionSpanEntity span = Assert.Single(spans);
            // The engine's "why" detail is captured for the explanation export.
            Assert.Equal("EmailAddress", span.FilterType);
            Assert.True(span.Confidence > 0);
        }

        [Fact]
        public async Task RedactFileAsync_Docx_RedactsViaWordRedactor()
        {
            string input = Path.Combine(_tempDir, "in.docx");
            string output = Path.Combine(_tempDir, "out.docx");
            WordDocs.Create(input, "ssn 123-45-6789 here.");

            await RedactionService.RedactFileAsync(input, output, EditorStylePolicy(), "ctx");

            string text = string.Join("\n", WordDocs.BodyParagraphs(output));
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
