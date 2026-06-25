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
    public class UpdateCheckerTests
    {
        [Theory]
        [InlineData("1.0.1", "1.0.0", true)]   // patch newer
        [InlineData("1.1.0", "1.0.0", true)]   // minor newer
        [InlineData("2.0.0", "1.9.9", true)]   // major newer
        [InlineData("1.0.0", "1.0.0", false)]  // same
        [InlineData("0.9.0", "1.0.0", false)]  // older
        [InlineData("1.0.0", "1.0.0.0", false)] // 3-part manifest vs 4-part assembly = same release
        public void IsNewer_ComparesOnMajorMinorBuild(string published, string current, bool expected)
        {
            Assert.Equal(expected, UpdateChecker.IsNewer(published, Version.Parse(current)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-version")]
        public void IsNewer_ReturnsNull_ForUnparseablePublishedVersion(string? published)
        {
            Assert.Null(UpdateChecker.IsNewer(published, new Version(1, 0, 0)));
        }

        [Fact]
        public void CurrentVersion_IsThreePartAndNonNegative()
        {
            Version v = UpdateChecker.CurrentVersion();
            Assert.True(v.Major >= 0 && v.Minor >= 0 && v.Build >= 0);
            Assert.Equal(-1, v.Revision); // normalized to Major.Minor.Build (no revision)
        }

        [Fact]
        public void ComputeSha256_MatchesKnownVector()
        {
            // SHA-256("abc")
            const string expected = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
            string path = Path.Combine(Path.GetTempPath(), "uc-" + Guid.NewGuid().ToString("N") + ".bin");
            try
            {
                File.WriteAllText(path, "abc");
                Assert.Equal(expected, UpdateChecker.ComputeSha256(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void HashMatches_IsCaseInsensitive_AndDetectsMismatch()
        {
            const string sha = "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad";
            string path = Path.Combine(Path.GetTempPath(), "uc-" + Guid.NewGuid().ToString("N") + ".bin");
            try
            {
                File.WriteAllText(path, "abc");
                Assert.True(UpdateChecker.HashMatches(path, sha.ToUpperInvariant()));
                Assert.False(UpdateChecker.HashMatches(path, "deadbeef"));
                Assert.False(UpdateChecker.HashMatches(path, null));
                Assert.False(UpdateChecker.HashMatches(path, ""));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void GetDesktopDownloadPath_UsesDesktopAndUrlFileName()
        {
            string path = UpdateChecker.GetDesktopDownloadPath("https://philterd.ai/philter_desktop_setup.exe");
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            Assert.Equal(desktop, Path.GetDirectoryName(path));
            Assert.Equal(".exe", Path.GetExtension(path));
            Assert.StartsWith("philter_desktop_setup", Path.GetFileName(path));
        }
    }
}
