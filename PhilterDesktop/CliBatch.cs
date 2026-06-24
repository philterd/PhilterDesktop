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

using System.Diagnostics;
using System.IO.Pipes;

namespace PhilterDesktop
{
    /// <summary>
    /// Coalesces a multi-file Explorer context-menu selection into a single redaction run.
    ///
    /// Windows launches a classic shell verb as <i>one process per selected file</i>. To avoid N
    /// concurrent processes, the first process to start becomes the "primary" (it owns a named pipe);
    /// every other ("secondary") process connects to that pipe, hands over its file path(s), and exits
    /// immediately. The primary collects all the paths until the burst of launches goes quiet, then
    /// redacts the whole set in one batch.
    /// </summary>
    internal static class CliBatch
    {
        private static readonly TimeSpan DefaultQuietWindow = TimeSpan.FromMilliseconds(1200);
        private static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromMilliseconds(400);

        /// <summary>
        /// Coordinates the batch. Returns the full set of files this process should redact when it is
        /// the primary, or <c>null</c> when it forwarded its files to an already-running primary and
        /// should simply exit.
        /// </summary>
        public static List<string>? Coordinate(IReadOnlyList<string> files) =>
            Coordinate(files, DefaultPipeName(), DefaultQuietWindow, DefaultConnectTimeout);

        // Overload with injectable pipe name / timings for testing.
        internal static List<string>? Coordinate(IReadOnlyList<string> files, string pipeName, TimeSpan quietWindow, TimeSpan connectTimeout)
        {
            string[] ourFiles = files.Select(FullPath).ToArray();

            // 1. Hand off to an existing primary if one is already accepting.
            if (TryForward(ourFiles, pipeName, connectTimeout))
            {
                return null;
            }

            // 2. Try to become the primary. If a sibling wins the race to create the pipe, forward instead.
            NamedPipeServerStream? server = TryCreateServer(pipeName);
            for (int attempt = 0; server is null && attempt < 10; attempt++)
            {
                if (TryForward(ourFiles, pipeName, connectTimeout))
                {
                    return null;
                }
                Thread.Sleep(30);
                server = TryCreateServer(pipeName);
            }

            var batch = new List<string>(ourFiles);
            if (server is null)
            {
                // Couldn't coordinate at all — just redact our own files.
                return batch;
            }

            // 3. Primary: gather siblings' files until the launch burst goes quiet.
            Collect(server, pipeName, batch, quietWindow);
            return Dedupe(batch);
        }

        private static void Collect(NamedPipeServerStream server, string pipeName, List<string> batch, TimeSpan quietWindow)
        {
            NamedPipeServerStream current = server;
            while (true)
            {
                Task wait = current.WaitForConnectionAsync();
                if (!wait.Wait(quietWindow))
                {
                    current.Dispose(); // cancels the pending wait; no more siblings arriving
                    return;
                }

                try
                {
                    using var reader = new StreamReader(current); // disposes 'current' too
                    string? line;
                    while ((line = reader.ReadLine()) is not null)
                    {
                        if (line.Length > 0)
                        {
                            batch.Add(line);
                        }
                    }
                }
                catch
                {
                    // Ignore a malformed/aborted client and keep listening.
                }

                NamedPipeServerStream? next = TryCreateServer(pipeName);
                if (next is null)
                {
                    return;
                }
                current = next;
            }
        }

        private static bool TryForward(IReadOnlyList<string> files, string pipeName, TimeSpan connectTimeout)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
                client.Connect((int)connectTimeout.TotalMilliseconds);
                using var writer = new StreamWriter(client) { AutoFlush = true };
                foreach (string file in files)
                {
                    writer.WriteLine(file);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static NamedPipeServerStream? TryCreateServer(string pipeName)
        {
            try
            {
                // Exactly one instance => exactly one primary. A second creation attempt throws
                // IOException ("All pipe instances are busy"/in use), which elects everyone else as a
                // secondary that forwards to the primary.
                return new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous);
            }
            catch (IOException)
            {
                return null; // the pipe name is already in use (a sibling is the primary)
            }
        }

        // Named pipes are machine-wide; scope to the current session so different logged-in users
        // (e.g. on a terminal server) don't share a batch.
        private static string DefaultPipeName() =>
            $"PhilterDesktop.ContextMenu.{Process.GetCurrentProcess().SessionId}";

        private static string FullPath(string path)
        {
            try { return Path.GetFullPath(path); }
            catch { return path; }
        }

        private static List<string> Dedupe(IEnumerable<string> files) =>
            files.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
