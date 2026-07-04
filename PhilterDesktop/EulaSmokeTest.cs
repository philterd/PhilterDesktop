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

using System.Runtime.InteropServices;

namespace PhilterDesktop
{
    /// <summary>
    /// A release-time check (<c>PhilterDesktop.exe --smoketest</c>): confirms the EULA bundled next to the
    /// executable (<c>philterd-eula.txt</c>, refreshed by the installer build) still matches the live copy at
    /// https://philterd.ai/philterd-eula.txt. Requires network access. Prints PASS/FAIL and exits 0 (match)
    /// or 1 (mismatch, missing file, or fetch failure) — so a build shipping a stale agreement is caught.
    /// </summary>
    internal static class EulaSmokeTest
    {
        private const string EulaUrl = "https://philterd.ai/philterd-eula.txt";
        private const string EulaFileName = "philterd-eula.txt";

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int processId);
        private const int AttachParentProcess = -1;

        public static int Run()
        {
            AttachConsole(AttachParentProcess); // a WinExe has no console; attach to the launching one if present
            Console.WriteLine("Philter Desktop EULA smoke test");

            try
            {
                string localPath = Path.Combine(AppContext.BaseDirectory, EulaFileName);
                if (!File.Exists(localPath))
                {
                    Console.Error.WriteLine($"FAIL: bundled {EulaFileName} not found next to the executable ({localPath}).");
                    return 1;
                }
                string local = File.ReadAllText(localPath);

                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                string remote = http.GetStringAsync(EulaUrl).GetAwaiter().GetResult();

                if (Normalize(local) == Normalize(remote))
                {
                    Console.WriteLine($"PASS: the bundled EULA matches {EulaUrl}.");
                    return 0;
                }

                Console.Error.WriteLine($"FAIL: the bundled EULA differs from {EulaUrl} — the shipped copy is stale.");
                Console.Error.WriteLine($"  bundled: {Normalize(local).Length} chars; live: {Normalize(remote).Length} chars.");
                Console.Error.WriteLine("  Rebuild the installer so it re-downloads the current EULA.");
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: " + ex.Message);
                return 1;
            }
        }

        // Compare on content, ignoring line-ending style, a leading byte-order mark, and surrounding
        // whitespace — differences that come from how the file is downloaded/saved vs. fetched here.
        private static string Normalize(string s)
        {
            s = s.Replace("\r\n", "\n").Replace("\r", "\n");
            return s.TrimStart((char)0xFEFF).Trim(); // drop a leading BOM, then surrounding whitespace
        }
    }
}
