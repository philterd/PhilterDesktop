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
    /// <summary>Per-user state for the automatic update check.</summary>
    internal interface IUpdatePreferences
    {
        /// <summary>The user's choice (default on).</summary>
        bool AutoCheckEnabled { get; set; }

        /// <summary>True when an administrator turned automatic checks off by policy.</summary>
        bool DisabledByPolicy { get; }

        /// <summary>The newest version the user was already told about, so it isn't announced twice.</summary>
        string? LastNotifiedVersion { get; set; }
    }

    /// <summary>
    /// Registry-backed preferences under HKCU\Software\PhilterDesktop. Administrators can turn automatic
    /// checks off with the DWORD <c>DisableAutoUpdateCheck = 1</c> under
    /// HKLM (or HKCU) \Software\Policies\PhilterDesktop.
    /// </summary>
    internal sealed class RegistryUpdatePreferences : IUpdatePreferences
    {
        private const string PrefsKeyPath = @"Software\PhilterDesktop";
        internal const string PolicyKeyPath = @"Software\Policies\PhilterDesktop";
        internal const string PolicyValueName = "DisableAutoUpdateCheck";

        private const string AutoCheckValue = "AutoUpdateCheck";
        private const string LastNotifiedValue = "LastNotifiedUpdateVersion";

        public bool AutoCheckEnabled
        {
            get => Read(Registry.CurrentUser, PrefsKeyPath, AutoCheckValue) is not int v || v != 0;
            set => Write(AutoCheckValue, value ? 1 : 0, RegistryValueKind.DWord);
        }

        public bool DisabledByPolicy =>
            Read(Registry.LocalMachine, PolicyKeyPath, PolicyValueName) is int machine && machine == 1 ||
            Read(Registry.CurrentUser, PolicyKeyPath, PolicyValueName) is int user && user == 1;

        public string? LastNotifiedVersion
        {
            get => Read(Registry.CurrentUser, PrefsKeyPath, LastNotifiedValue) as string;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    Write(LastNotifiedValue, value, RegistryValueKind.String);
                }
            }
        }

        private static object? Read(RegistryKey hive, string path, string name)
        {
            try
            {
                using RegistryKey? key = hive.OpenSubKey(path, writable: false);
                return key?.GetValue(name);
            }
            catch
            {
                return null;
            }
        }

        // Best effort, never throws: at worst the opt-out or the last announced version isn't remembered.
        private static void Write(string name, object value, RegistryValueKind kind)
        {
            try
            {
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(PrefsKeyPath, writable: true);
                key.SetValue(name, value, kind);
            }
            catch
            {
            }
        }
    }

    /// <summary>The active update preferences; swappable so tests don't touch the registry.</summary>
    internal static class UpdatePreferences
    {
        internal static IUpdatePreferences Current { get; set; } = new RegistryUpdatePreferences();

        /// <summary>Whether automatic checks should run: the user wants them and policy allows them.</summary>
        public static bool IsAutoCheckOn(IUpdatePreferences prefs) => prefs.AutoCheckEnabled && !prefs.DisabledByPolicy;
    }
}
