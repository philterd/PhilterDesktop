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
    /// Manages the Windows Explorer right-click ("Redact with Philter Desktop") menu entry for the
    /// supported file types (PDF, Word, text, RTF, and email). The entries are written per-user under
    /// <c>HKCU\Software\Classes\SystemFileAssociations\&lt;ext&gt;\shell\PhilterDesktop</c> (no admin
    /// needed) and invoke the app's command-line redactor on the selected file. The unpackaged
    /// installer also declares these keys with the "delete on uninstall" flag so they're cleaned up.
    /// </summary>
    internal sealed class ContextMenuManager
    {
        private const string VerbKeyName = "PhilterDesktop";
        private const string DefaultCaption = "Redact with Philter Desktop";
        private const string DefaultBaseSubKey = @"Software\Classes\SystemFileAssociations";

        /// <summary>File types the context-menu entry is registered for.</summary>
        public static readonly string[] Extensions = { ".pdf", ".docx", ".txt", ".rtf", ".eml", ".msg" };

        private readonly string _exePath;
        private readonly string _caption;
        private readonly string _command;
        private readonly string _baseSubKey;

        public ContextMenuManager(string exePath, string caption = DefaultCaption, string baseSubKey = DefaultBaseSubKey)
        {
            _exePath = exePath;
            _caption = caption;
            _baseSubKey = baseSubKey;
            // --shell enables multi-file coalescing (one instance redacts a whole selection).
            _command = $"\"{exePath}\" --shell \"%1\"";
        }

        /// <summary>Builds a manager that invokes the currently running executable.</summary>
        public static ContextMenuManager CreateDefault()
        {
            string exe = Environment.ProcessPath ?? Application.ExecutablePath;
            return new ContextMenuManager(exe);
        }

        private string KeyPath(string extension) =>
            $@"{_baseSubKey}\{extension}\shell\{VerbKeyName}";

        /// <summary>True when the entry is registered for every handled extension.</summary>
        public bool IsEnabled()
        {
            foreach (string extension in Extensions)
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(KeyPath(extension), writable: false);
                if (key is null)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Adds (or refreshes) the context-menu entry for all handled extensions.</summary>
        public void Enable()
        {
            foreach (string extension in Extensions)
            {
                using RegistryKey verb = Registry.CurrentUser.CreateSubKey(KeyPath(extension), writable: true);
                verb.SetValue(null, _caption);          // the menu text
                verb.SetValue("Icon", $"{_exePath},0"); // the app's embedded icon (index 0)
                using RegistryKey command = verb.CreateSubKey("command", writable: true);
                command.SetValue(null, _command);
            }
        }

        /// <summary>Removes the context-menu entry for all handled extensions (no-op if absent).</summary>
        public void Disable()
        {
            foreach (string extension in Extensions)
            {
                Registry.CurrentUser.DeleteSubKeyTree(KeyPath(extension), throwOnMissingSubKey: false);
            }
        }
    }
}
