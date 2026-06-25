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

using Microsoft.Win32;

namespace PhilterDesktop
{
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
}
