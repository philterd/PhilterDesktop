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

using DocumentFormat.OpenXml.Packaging;
using Phileas.Policy;
using Phileas.Policy.Filters;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Proves a redacted <c>.xlsx</c> doesn't leak through document metadata: with "Remove document
    /// metadata" on, the author/last-modified-by/title/etc. are stripped (and the PII is gone from the
    /// cells); with it off, the metadata is left intact (so the scrub is genuinely gated by the setting).
    /// </summary>
    public sealed class XlsxMetadataTests : IDisposable
    {
        private readonly string _dir;

        public XlsxMetadataTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-xlsxmeta-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private static PhileasPolicy SsnPolicy() => new()
        {
            Name = "ssn",
            Identifiers = new Identifiers { Ssn = new Ssn() }
        };

        private string MakeWorkbookWithMetadataAndPii()
        {
            string path = Path.Combine(_dir, "in.xlsx");
            SpreadsheetTestHelper.CreateXlsx(path, new[]
            {
                new string?[] { "Name", "SSN" },
                new string?[] { "Bob", "123-45-6789" }
            });
            SetCoreProperties(path);
            return path;
        }

#pragma warning disable OOXML0001 // PackageProperties is the supported way to edit core document properties
        private static void SetCoreProperties(string path)
        {
            using SpreadsheetDocument doc = SpreadsheetDocument.Open(path, isEditable: true);
            doc.PackageProperties.Creator = "Jane Author";
            doc.PackageProperties.LastModifiedBy = "Bob Editor";
            doc.PackageProperties.Title = "Confidential client list";
            doc.PackageProperties.Subject = "SSNs";
            doc.PackageProperties.Keywords = "secret";
        }

        private static (string? Creator, string? LastModifiedBy, string? Title, string? Subject, string? Keywords) ReadCoreProperties(string path)
        {
            using SpreadsheetDocument doc = SpreadsheetDocument.Open(path, isEditable: false);
            var p = doc.PackageProperties;
            return (p.Creator, p.LastModifiedBy, p.Title, p.Subject, p.Keywords);
        }
#pragma warning restore OOXML0001

        [Fact]
        public async Task RedactFileAsync_Xlsx_ScrubsMetadata_AndPii_WhenEnabled()
        {
            string input = MakeWorkbookWithMetadataAndPii();
            string output = Path.Combine(_dir, "out.xlsx");

            await RedactionService.RedactFileAsync(input, output, SsnPolicy(), "ctx",
                wordScrub: WordScrubOptions.Metadata);

            var props = ReadCoreProperties(output);
            Assert.True(string.IsNullOrEmpty(props.Creator), "Creator should be cleared");
            Assert.True(string.IsNullOrEmpty(props.LastModifiedBy), "LastModifiedBy should be cleared");
            Assert.True(string.IsNullOrEmpty(props.Title), "Title should be cleared");
            Assert.True(string.IsNullOrEmpty(props.Subject), "Subject should be cleared");
            Assert.True(string.IsNullOrEmpty(props.Keywords), "Keywords should be cleared");

            string text = SpreadsheetTestHelper.AllText(output);
            Assert.DoesNotContain("123-45-6789", text);
            Assert.DoesNotContain("Jane Author", text);
        }

        [Fact]
        public async Task RedactFileAsync_Xlsx_LeavesMetadata_WhenDisabled()
        {
            string input = MakeWorkbookWithMetadataAndPii();
            string output = Path.Combine(_dir, "out-nometa.xlsx");

            // No metadata flag -> the scrub must not run (proves it is gated by the setting).
            await RedactionService.RedactFileAsync(input, output, SsnPolicy(), "ctx",
                wordScrub: WordScrubOptions.None);

            var props = ReadCoreProperties(output);
            Assert.Equal("Jane Author", props.Creator);
            Assert.Equal("Bob Editor", props.LastModifiedBy);

            // The cell PII is still redacted regardless of the metadata setting.
            Assert.DoesNotContain("123-45-6789", SpreadsheetTestHelper.AllText(output));
        }
    }
}
