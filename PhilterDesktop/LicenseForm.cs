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

using System.Reflection;
using Microsoft.Win32;

namespace PhilterDesktop
{
    /// <summary>
    /// The license dialog: shows the <b>Apache License 2.0</b> (the open-source source license) and the
    /// <b>Philterd Commercial License Agreement</b> (the EULA governing the official build) each in its own
    /// read-only text box, notes that redaction uses statistical methods and must always be reviewed by a
    /// human, and — at first run — gates startup behind an "I Agree" / "I Disagree" choice. Once the user
    /// agrees, acceptance is recorded so the dialog is not shown again. Also reachable from Help → View
    /// License (see <paramref name="viewOnly"/>). Both texts are embedded so they display offline.
    /// </summary>
    public partial class LicenseForm : Form
    {
        private const string ApacheResource = "PhilterDesktop.ApacheLicense.txt";

        /// <summary>
        /// The EULA text file shipped next to the executable. The installer build refreshes it from
        /// https://philterd.ai/philterd-eula.txt at packaging time; a checked-in snapshot is copied to the
        /// build output so it is present in development too.
        /// </summary>
        private const string EulaFileName = "philterd-eula.txt";

        /// <summary>
        /// Where acceptance is persisted. Defaults to the Windows registry; tests can swap in an
        /// in-memory store so they don't touch the real per-user registry.
        /// </summary>
        internal static IEulaAcceptanceStore AcceptanceStore { get; set; } = new RegistryEulaAcceptanceStore();

        public LicenseForm() : this(viewOnly: false)
        {
        }

        /// <param name="viewOnly">
        /// When true, the dialog is shown for reference (from Help → View License) rather than as the
        /// first-run gate: the Agree/Disagree choice is replaced with a single Close button, since the
        /// user has already accepted.
        /// </param>
        public LicenseForm(bool viewOnly)
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_agree);

            _licenseBody.Text = ReadResource(ApacheResource);
            _licenseBody.Select(0, 0); // keep the view scrolled to the top
            _eulaBody.Text = ReadEula();
            _eulaBody.Select(0, 0);

            if (viewOnly)
            {
                _disagree.Visible = false;
                _agree.Text = "&Close";
                _agree.DialogResult = DialogResult.OK;
                CancelButton = _agree; // Esc / the window close button just closes
            }
        }

        /// <summary>Whether the license dialog should be shown on this launch (i.e. not yet accepted).</summary>
        public static bool ShouldShow() => !AcceptanceStore.HasAccepted();

        /// <summary>
        /// Persists that the user accepted the agreement. Called unconditionally once the user agrees, so
        /// acceptance is remembered and the dialog is never re-shown after that (no opt-out to leave off).
        /// </summary>
        public static void RememberAccepted() => AcceptanceStore.RememberAccepted();

        // Reads an embedded license text (the Apache license). Returns a short fallback rather than throwing
        // if a resource is somehow missing, so the acceptance gate always renders.
        private static string ReadResource(string logicalName)
        {
            try
            {
                using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(logicalName);
                if (stream is null)
                {
                    return "(license text unavailable)";
                }
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch
            {
                return "(license text unavailable)";
            }
        }

        // Reads the EULA from the loose file next to the executable (refreshed by the installer build).
        // Falls back to a short pointer rather than throwing if the file is missing, so the gate still renders.
        private static string ReadEula()
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, EulaFileName);
                string text = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
                return string.IsNullOrWhiteSpace(text)
                    ? "The Philterd Commercial License Agreement is available at https://philterd.ai/philterd-eula.txt."
                    : text;
            }
            catch
            {
                return "The Philterd Commercial License Agreement is available at https://philterd.ai/philterd-eula.txt.";
            }
        }
    }

    /// <summary>Stores whether the user has accepted the first-run agreement.</summary>
    internal interface IEulaAcceptanceStore
    {
        bool HasAccepted();
        void RememberAccepted();
    }

    /// <summary>Default acceptance store: a per-user value under HKCU\Software\PhilterDesktop.</summary>
    internal sealed class RegistryEulaAcceptanceStore : IEulaAcceptanceStore
    {
        private const string PrefsKeyPath = @"Software\PhilterDesktop";
        private const string AcceptedValueName = "EulaAccepted";

        public bool HasAccepted()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PrefsKeyPath, writable: false);
                return key?.GetValue(AcceptedValueName) is int value && value == 1;
            }
            catch
            {
                return false;
            }
        }

        public void RememberAccepted()
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(PrefsKeyPath, writable: true);
            key.SetValue(AcceptedValueName, 1, RegistryValueKind.DWord);
        }
    }
}
