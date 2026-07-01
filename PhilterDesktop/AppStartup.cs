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
using Microsoft.Win32;
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using PhilterData;
using PhileasPolicy = Phileas.Policy.Policy;
using PhileasDictionary = Phileas.Policy.Filters.Dictionary;

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

    /// <summary>
    /// Manages "start at sign-in" for Philter Desktop by writing an <c>HKCU\…\Run</c> entry that
    /// relaunches the app minimized to the system tray.
    /// </summary>
    internal sealed class StartupManager
    {
        /// <summary>Command-line switch that tells the app to start hidden in the system tray.</summary>
        public const string MinimizedSwitch = "--minimized";

        private const string DefaultAppName = "PhilterDesktop";
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

        private readonly string _appName;
        private readonly string _command;
        private readonly string _keyPath;

        public StartupManager(string appName, string command, string keyPath = RunKeyPath)
        {
            _appName = appName;
            _command = command;
            _keyPath = keyPath;
        }

        /// <summary>Builds a manager for this application (relaunches the running exe, minimized).</summary>
        public static StartupManager CreateDefault()
        {
            string exe = Environment.ProcessPath ?? Application.ExecutablePath;
            return new StartupManager(DefaultAppName, $"\"{exe}\" {MinimizedSwitch}");
        }

        /// <summary>Returns true if the Run entry exists.</summary>
        public bool IsEnabled()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(_keyPath, writable: false);
            return key?.GetValue(_appName) is not null;
        }

        /// <summary>Adds (or refreshes) the Run entry so the app starts at sign-in.</summary>
        public void Enable()
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(_keyPath, writable: true);
            key.SetValue(_appName, _command, RegistryValueKind.String);
        }

        /// <summary>Removes the Run entry.</summary>
        public void Disable()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(_keyPath, writable: true);
            key?.DeleteValue(_appName, throwOnMissingValue: false);
        }
    }

    /// <summary>
    /// Durable <see cref="IContextService"/> backed by the application's (encrypted) LiteDB database.
    /// Phileas calls this to keep RANDOM_REPLACE replacements consistent — the same token maps to the
    /// same replacement within a context. Storing the mappings in the database (instead of the default
    /// in-memory store) makes them persist across documents and app restarts and keeps the redaction
    /// engine from accumulating those mappings in memory over a long-running session.
    /// </summary>
    internal sealed class LiteDbContextService : IContextService
    {
        private readonly ContextEntryRepository _entries;

        // Serializes the read-modify-write in Put so two threads (e.g. concurrent watched-folder
        // redactions) can't both insert a mapping for the same token. LiteDB itself is thread-safe.
        private readonly object _writeLock = new();

        public LiteDbContextService(ContextEntryRepository entries) => _entries = entries;

        /// <inheritdoc />
        public string? Get(string contextName, string token) => _entries.FindEntry(contextName, token)?.Replacement;

        /// <inheritdoc />
        public void Put(string contextName, string token, string replacement)
        {
            lock (_writeLock)
            {
                _entries.UpsertEntry(contextName, token, replacement);
            }
        }
    }

    /// <summary>
    /// A single, app-wide <see cref="FilterService"/>, shared so the on-device name-detection model
    /// (PhEye/GLiNER, ~90&#160;MB) loads <b>once</b> for the whole app instead of being reloaded for every
    /// redaction or preview (which made PDF previews with name detection take tens of seconds each time).
    ///
    /// The service is <b>not</b> stateless: it keeps RANDOM_REPLACE replacements consistent within a
    /// context. Call <see cref="UseContextService"/> at startup to back that with the durable
    /// (database) context service so the mappings persist across documents/restarts rather than
    /// accumulating in memory. Concurrent use is safe (the context service is thread-safe).
    /// </summary>
    internal static class SharedFilterService
    {
        private static IContextService? _contextService;
        private static FilterService? _instance;

        /// <summary>
        /// Sets the (durable) context service the shared <see cref="FilterService"/> uses for consistent
        /// replacements. Call once at startup, before the first redaction; rebuilds the instance so every
        /// later redaction uses it.
        /// </summary>
        public static void UseContextService(IContextService contextService)
        {
            _contextService = contextService;
            _instance = null;
        }

        public static FilterService Instance =>
            _instance ??= _contextService is null ? new FilterService() : new FilterService(_contextService);

        private static int _warmedUp;

        /// <summary>
        /// Loads the name model in the background (if bundled) so the first redaction that uses it
        /// doesn't pay the one-time model-load cost. Best-effort; runs at most once per app run.
        /// </summary>
        public static void WarmUp()
        {
            if (!PhEyeModel.IsAvailable || Interlocked.Exchange(ref _warmedUp, 1) != 0)
            {
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    var policy = new PhileasPolicy
                    {
                        Name = "warmup",
                        Identifiers = new Identifiers { PhEyes = new List<PhEye> { PhEyeModel.CreateDefaultFilter() } }
                    };
                    PhEyeModel.Prepare(policy);
                    Instance.Filter(policy, "warmup", 0, "John Smith called yesterday.");
                }
                catch
                {
                    // best effort — a warm-up failure must never affect the app
                }
            });
        }
    }

    /// <summary>
    /// Builds a throwaway, terms-only policy for the ad-hoc "Find &amp; Redact" action — redact exactly
    /// the given terms in one document, without creating or using a saved policy. Terms are matched via
    /// a dictionary filter (the same mechanism behind the Always Redact lists).
    /// </summary>
    internal static class FindAndRedact
    {
        public const string DictionaryName = "find-and-redact";

        public static PhileasPolicy BuildPolicy(IReadOnlyList<string> terms)
        {
            var policy = new PhileasPolicy { Name = "Find and Redact", Identifiers = new Identifiers() };
            if (terms.Count > 0)
            {
                policy.Identifiers.Dictionaries = new List<PhileasDictionary>
                {
                    new() { Name = DictionaryName, Terms = terms.ToList(), Enabled = true }
                };
            }
            return policy;
        }
    }
}
