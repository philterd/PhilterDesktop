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

using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    public class TermFileImporterTests
    {
        [Fact]
        public void Parse_OneTermPerLine_TrimsBlanksAndDeduplicates()
        {
            var terms = TermFileImporter.Parse("Acme\r\n  Beta  \n\nacme\nGamma");
            Assert.Equal(new[] { "Acme", "Beta", "Gamma" }, terms);
        }

        [Fact]
        public void Parse_StripsSurroundingQuotes_KeepingInternalCommas()
        {
            var terms = TermFileImporter.Parse("\"Smith, John\"\n\"Doe\"\nPlain");
            Assert.Equal(new[] { "Smith, John", "Doe", "Plain" }, terms);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   \n  \n")]
        public void Parse_EmptyOrBlank_ReturnsEmpty(string? text)
        {
            Assert.Empty(TermFileImporter.Parse(text));
        }

        [Fact]
        public void AppendNew_AddsOnlyNewTerms_CaseInsensitive()
        {
            using var box = new TextBox { Multiline = true, Text = "Acme" + Environment.NewLine + "Beta" };

            int added = TermFileImporter.AppendNew(box, new[] { "beta", "Gamma", "Delta" });

            Assert.Equal(2, added); // "beta" is a dup of "Beta"; Gamma + Delta are new
            var lines = box.Text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(new[] { "Acme", "Beta", "Gamma", "Delta" }, lines);
        }

        [Fact]
        public void AppendNew_IntoEmptyBox_NoLeadingBlankLine()
        {
            using var box = new TextBox { Multiline = true };
            TermFileImporter.AppendNew(box, new[] { "One", "Two" });
            Assert.Equal(new[] { "One", "Two" }, box.Text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries));
        }

        [Fact]
        public void AppendNew_NothingNew_ReturnsZero()
        {
            using var box = new TextBox { Multiline = true, Text = "Acme" + Environment.NewLine + "Beta" };
            Assert.Equal(0, TermFileImporter.AppendNew(box, new[] { "acme", "BETA" }));
        }

        [Fact]
        public void ParseFile_ReadsTermsFromDisk()
        {
            string path = Path.Combine(Path.GetTempPath(), "philter-terms-" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllText(path, "One\nTwo\nOne\n");
            try
            {
                Assert.Equal(new[] { "One", "Two" }, TermFileImporter.ParseFile(path));
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
