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
    /// Enforces a single GUI per Windows session (so two instances can't both process the shared
    /// redaction queue) and lets a second launch bring the running window to the front. Backed by a
    /// session-scoped named mutex the GUI holds for its lifetime; the Explorer context-menu flow also
    /// uses the mutex's existence to detect a running GUI.
    /// </summary>
    internal static class AppInstance
    {
        private static string MutexName => $@"Local\PhilterDesktop.Gui.{Process.GetCurrentProcess().SessionId}";
        private static string ActivationEventName => $@"Local\PhilterDesktop.Activate.{Process.GetCurrentProcess().SessionId}";

        /// <summary>
        /// Creates the GUI-lifetime mutex. <paramref name="createdNew"/> is true only for the first GUI in
        /// this session; a caller getting false should not start a second GUI. Keep the handle alive for
        /// the life of the process (dispose on exit).
        /// </summary>
        public static Mutex CreateGuiLifetime(out bool createdNew) => CreateLifetime(MutexName, out createdNew);

        /// <summary>True if a main window is currently running in this session.</summary>
        public static bool IsGuiRunning() => Exists(MutexName);

        /// <summary>
        /// Creates the activation signal the running GUI waits on; a second launch sets it (via
        /// <see cref="SignalExistingInstance"/>) to ask the GUI to come to the front. Dispose on exit.
        /// </summary>
        public static EventWaitHandle CreateActivationSignal() => CreateActivationSignal(ActivationEventName);

        /// <summary>
        /// Best-effort: ask an already-running GUI to activate its window. Returns true if a listener was
        /// signaled; false if none was found (e.g. the other instance is still starting up).
        /// </summary>
        public static bool SignalExistingInstance() => SignalExisting(ActivationEventName);

        // --- Name-parameterized primitives (testable without touching the real session objects) ---------

        internal static Mutex CreateLifetime(string name, out bool createdNew) =>
            new(initiallyOwned: true, name, out createdNew);

        internal static EventWaitHandle CreateActivationSignal(string name) =>
            new(initialState: false, EventResetMode.AutoReset, name);

        internal static bool SignalExisting(string name)
        {
            try
            {
                using EventWaitHandle handle = EventWaitHandle.OpenExisting(name);
                handle.Set();
                return true;
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return false;
            }
        }

        private static bool Exists(string mutexName)
        {
            try
            {
                using Mutex existing = Mutex.OpenExisting(mutexName);
                return true;
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return false;
            }
        }
    }
}
