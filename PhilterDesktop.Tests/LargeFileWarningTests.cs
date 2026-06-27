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
    public sealed class LargeFileWarningTests
    {
        [Theory]
        [InlineData(0, false)]
        [InlineData(50L * 1024 * 1024, false)]       // exactly at the threshold is fine
        [InlineData(50L * 1024 * 1024 + 1, true)]    // just over
        [InlineData(500L * 1024 * 1024, true)]
        public void IsLarge_UsesThreshold(long bytes, bool expected)
        {
            Assert.Equal(expected, LargeFileWarning.IsLarge(bytes));
        }

        [Fact]
        public void LargeFiles_ReturnsOnlyOversizedExistingFiles()
        {
            string dir = Path.Combine(Path.GetTempPath(), "philter-large-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string small = Path.Combine(dir, "small.txt");
                File.WriteAllText(small, "tiny");
                string missing = Path.Combine(dir, "missing.txt"); // never created

                List<string> large = LargeFileWarning.LargeFiles(new[] { small, missing });

                Assert.Empty(large); // small is under the cap; missing can't be sized
            }
            finally
            {
                try { Directory.Delete(dir, recursive: true); } catch { /* best effort */ }
            }
        }

        [Fact]
        public void BuildMessage_ListsFileNamesAndThreshold()
        {
            string message = LargeFileWarning.BuildMessage(new[] { @"C:\data\huge-export.csv" });

            Assert.Contains("huge-export.csv", message);
            Assert.DoesNotContain(@"C:\data", message); // file name only, not the full path
            Assert.Contains(LargeFileWarning.ThresholdText, message);
        }
    }
}
