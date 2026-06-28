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

using System.Text;
using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    public sealed class FileHashTests
    {
        [Fact]
        public void Sha256_MatchesKnownVector()
        {
            string path = Path.Combine(Path.GetTempPath(), "philter-hash-" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllBytes(path, Encoding.ASCII.GetBytes("abc"));
            try
            {
                // Well-known SHA-256("abc").
                Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", FileHash.Sha256(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void Sha256OrUnavailable_MissingFile_ReturnsPlaceholder()
        {
            string missing = Path.Combine(Path.GetTempPath(), "philter-missing-" + Guid.NewGuid().ToString("N") + ".txt");
            Assert.Equal("(file not available)", FileHash.Sha256OrUnavailable(missing));
            Assert.Equal("(file not available)", FileHash.Sha256OrUnavailable(""));
        }

        [Fact]
        public void Sha256OrUnavailable_PresentFile_ReturnsHash()
        {
            string path = Path.Combine(Path.GetTempPath(), "philter-hash-" + Guid.NewGuid().ToString("N") + ".txt");
            File.WriteAllBytes(path, Encoding.ASCII.GetBytes("abc"));
            try
            {
                Assert.Equal(FileHash.Sha256(path), FileHash.Sha256OrUnavailable(path));
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
