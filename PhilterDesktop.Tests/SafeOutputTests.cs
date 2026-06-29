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
    /// <summary>
    /// <see cref="SafeOutput"/> backs the "build the redacted file in memory, write once" guarantee
    /// (philterd-website issue #483): a managed failure during the final write must never leave a
    /// partial output file behind.
    /// </summary>
    public sealed class SafeOutputTests : IDisposable
    {
        private readonly string _dir;

        public SafeOutputTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-safeoutput-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        [Fact]
        public void Write_WritesAllBytes()
        {
            string path = Path.Combine(_dir, "out.bin");
            byte[] bytes = { 1, 2, 3, 4, 5 };

            SafeOutput.Write(path, bytes);

            Assert.Equal(bytes, File.ReadAllBytes(path));
        }

        [Fact]
        public void Write_WhenFinalWriteFails_LeavesNoPartialFile()
        {
            // Target a path whose parent directory does not exist: WriteAllBytes throws after we've
            // committed to writing, exercising the delete-on-failure cleanup. No file must remain.
            string path = Path.Combine(_dir, "missing-subdir", "out.bin");

            Assert.ThrowsAny<Exception>(() => SafeOutput.Write(path, new byte[] { 9, 9, 9 }));
            Assert.False(File.Exists(path));
        }

        [Fact]
        public void Write_BuildCallback_ThatThrows_WritesNothing()
        {
            string path = Path.Combine(_dir, "callback.bin");

            Assert.Throws<InvalidOperationException>(() =>
                SafeOutput.Write(path, _ => throw new InvalidOperationException("build failed")));
            Assert.False(File.Exists(path), "the destination must not be touched when the build step fails");
        }

        [Fact]
        public void ReadToEditableStream_ReturnsExpandableCopyPositionedAtStart()
        {
            string path = Path.Combine(_dir, "in.bin");
            byte[] bytes = { 10, 20, 30 };
            File.WriteAllBytes(path, bytes);

            using MemoryStream stream = SafeOutput.ReadToEditableStream(path);

            Assert.Equal(0, stream.Position);
            Assert.True(stream.CanWrite);
            Assert.Equal(bytes, stream.ToArray());
            // Expandable: writing past the original length must not throw.
            stream.Position = stream.Length;
            stream.WriteByte(40);
            Assert.Equal(4, stream.Length);
        }
    }
}
