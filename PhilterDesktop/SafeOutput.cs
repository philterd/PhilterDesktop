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

namespace PhilterDesktop
{
    /// <summary>
    /// Writes a redacted output file from an in-memory buffer, deleting any partial output on failure
    /// (no temp files). Callers build the whole document first, so the destination is never created
    /// until correct, complete bytes exist — never the original or a half-written file.
    /// </summary>
    internal static class SafeOutput
    {
        /// <summary>Writes <paramref name="bytes"/>, removing a partial file on failure.</summary>
        public static void Write(string path, byte[] bytes)
        {
            try
            {
                File.WriteAllBytes(path, bytes);
            }
            catch
            {
                TryDelete(path);
                throw;
            }
        }

        /// <summary>Async overload of <see cref="Write(string, byte[])"/>.</summary>
        public static async Task WriteAsync(string path, byte[] bytes, CancellationToken cancellationToken = default)
        {
            try
            {
                await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                TryDelete(path);
                throw;
            }
        }

        /// <summary>Writes <paramref name="text"/> (UTF-8, no BOM), removing a partial file on failure.</summary>
        public static async Task WriteTextAsync(string path, string text, CancellationToken cancellationToken = default)
        {
            try
            {
                await File.WriteAllTextAsync(path, text, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                TryDelete(path);
                throw;
            }
        }

        /// <summary>Builds content in memory via <paramref name="build"/>, then writes it once.</summary>
        public static void Write(string path, Action<MemoryStream> build)
        {
            using var buffer = new MemoryStream();
            build(buffer);
            Write(path, buffer.ToArray());
        }

        /// <summary>Reads a file into an expandable, writable stream (positioned at 0) for editing an Open XML package.</summary>
        public static MemoryStream ReadToEditableStream(string inputPath)
        {
            var stream = new MemoryStream();
            using (FileStream input = File.OpenRead(inputPath))
            {
                input.CopyTo(stream);
            }
            stream.Position = 0;
            return stream;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // best effort
            }
        }
    }
}
