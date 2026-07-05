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
    /// <summary>Persists whether the user has accepted a named first-run acknowledgement.</summary>
    internal interface IAcceptanceStore
    {
        bool HasAccepted(string key);
        void RememberAccepted(string key);
    }

    /// <summary>Registry-backed store: one per-user DWORD value per acknowledgement under HKCU\Software\PhilterDesktop.</summary>
    internal sealed class RegistryAcceptanceStore : IAcceptanceStore
    {
        private const string PrefsKeyPath = @"Software\PhilterDesktop";

        public bool HasAccepted(string key)
        {
            try
            {
                using RegistryKey? regKey = Registry.CurrentUser.OpenSubKey(PrefsKeyPath, writable: false);
                return regKey?.GetValue(key) is int value && value == 1;
            }
            catch
            {
                return false;
            }
        }

        public void RememberAccepted(string key)
        {
            using RegistryKey regKey = Registry.CurrentUser.CreateSubKey(PrefsKeyPath, writable: true);
            regKey.SetValue(key, 1, RegistryValueKind.DWord);
        }
    }

    /// <summary>
    /// The first-run acknowledgements the user must agree to before the app starts. Each has its own
    /// persisted flag, so the two gates (license and redaction notice) are tracked independently. The store
    /// is swappable so tests don't touch the real per-user registry.
    /// </summary>
    internal static class Acknowledgements
    {
        internal static IAcceptanceStore Store { get; set; } = new RegistryAcceptanceStore();

        /// <summary>License / EULA acceptance flag (kept as "EulaAccepted" for continuity).</summary>
        internal const string LicenseKey = "EulaAccepted";

        /// <summary>Redaction-review notice acceptance flag.</summary>
        internal const string RedactionNoticeKey = "RedactionNoticeAccepted";
    }
}
