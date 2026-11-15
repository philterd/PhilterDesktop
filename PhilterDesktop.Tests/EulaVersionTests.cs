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

using Xunit;

namespace PhilterDesktop.Tests
{
    public sealed class EulaVersionTests
    {
        private const string Title = "PHILTERD COMMERCIAL LICENSE AGREEMENT";

        [Theory]
        [InlineData(Title + "\r\n\r\nVersion 2.0\r\n\r\nCopyright", "2.0.0.0")] // CRLF, as shipped
        [InlineData(Title + "\n\nVersion 2.0\n\nCopyright", "2.0.0.0")]       // LF
        [InlineData(Title + "\n\n   Version 2.1.3   \n", "2.1.3.0")]           // padded, three parts
        [InlineData(Title + "\n\nversion 3.0\n", "3.0.0.0")]                   // case-insensitive
        [InlineData("Version 1.0.0.7", "1.0.0.7")]                             // first line, four parts
        public void Parse_ReadsTheHeaderVersionLine(string text, string expected) =>
            Assert.Equal(Version.Parse(expected), EulaVersion.Parse(text));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(Title + "\n\nCopyright (c) 2026")]                   // no version line
        [InlineData(Title + "\n\nVersion 2\n")]                            // a bare number isn't a version line
        [InlineData(Title + "\n\nVersion: 2.0\n")]                         // the format is "Version X.Y"
        [InlineData(Title + "\n\nThis Version 2.0 applies\n")]             // must be the whole line
        [InlineData(Title + "\n\nVersion two\n")]
        public void Parse_ReturnsNull_WithoutAVersionLine(string? text) =>
            Assert.Null(EulaVersion.Parse(text));

        // A "Version ..." line deep in the body (e.g. quoting another agreement) is not the EULA's version.
        [Fact]
        public void Parse_IgnoresVersionLinesBelowTheHeader()
        {
            string body = string.Concat(Enumerable.Repeat("Clause text.\n", 30));
            Assert.Null(EulaVersion.Parse(Title + "\n" + body + "Version 9.0\n"));
        }

        [Fact]
        public void Parse_TakesTheFirstVersionLine() =>
            Assert.Equal(new Version(2, 0, 0, 0), EulaVersion.Parse("Version 2.0\nVersion 3.0\n"));

        [Theory]
        [InlineData("2.0", "2.0.0.0")]
        [InlineData(" 2.1 ", "2.1.0.0")]
        [InlineData("2.0.0", "2.0.0.0")]
        public void TryParse_NormalizesVersions(string value, string expected) =>
            Assert.Equal(Version.Parse(expected), EulaVersion.TryParse(value));

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("2")]
        [InlineData("v2.0")]
        [InlineData("2.0-beta")]
        public void TryParse_ReturnsNull_ForNonVersions(string? value) =>
            Assert.Null(EulaVersion.TryParse(value));

        [Theory]
        [InlineData("2.1", "2.0", true)]
        [InlineData("3.0", "2.9", true)]
        [InlineData("2.0", "2.0", false)]
        [InlineData("2.0", "2.0.0", false)]   // same version, different spelling
        [InlineData("2.0.0", "2.0", false)]
        [InlineData("1.0", "2.0", false)]
        public void IsNewer_ComparesVersions(string candidate, string baseline, bool expected) =>
            Assert.Equal(expected, EulaVersion.IsNewer(Version.Parse(candidate), Version.Parse(baseline)));

        [Fact]
        public void IsNewer_AnyVersionBeatsNoBaseline() =>
            Assert.True(EulaVersion.IsNewer(new Version(1, 0), null));

        [Fact]
        public void IsNewer_NoCandidateIsNeverNewer()
        {
            Assert.False(EulaVersion.IsNewer(null, new Version(1, 0)));
            Assert.False(EulaVersion.IsNewer(null, null));
        }
    }
}
