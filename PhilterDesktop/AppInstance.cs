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

namespace PhilterDesktop
{
    /// <summary>
    /// Tracks whether the main Philter Desktop window is running, via a session-scoped named mutex the
    /// GUI holds for its lifetime. The Explorer context-menu flow uses this to decide whether to launch
    /// the app (so the redaction queue gets processed) or let the already-running instance pick it up.
    /// </summary>
    internal static class AppInstance
    {
        private static string MutexName => $@"Local\PhilterDesktop.Gui.{Process.GetCurrentProcess().SessionId}";

        /// <summary>
        /// Creates the GUI-lifetime mutex; keep the returned handle alive for the life of the process
        /// (dispose on exit). Its mere existence is what <see cref="IsGuiRunning"/> detects.
        /// </summary>
        public static Mutex CreateGuiLifetime() => new(initiallyOwned: true, MutexName, out _);

        /// <summary>True if a main window is currently running in this session.</summary>
        public static bool IsGuiRunning()
        {
            try
            {
                using Mutex existing = Mutex.OpenExisting(MutexName);
                return true;
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return false;
            }
        }
    }
}
