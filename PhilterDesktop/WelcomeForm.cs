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

namespace PhilterDesktop
{
    /// <summary>
    /// First-run welcome dialog: states the Philterd Commercial License Agreement (linked at
    /// https://philterd.ai/philterd-eula/) under which the official build is provided, notes the
    /// Apache 2.0 source license, warns that
    /// redaction uses statistical methods and must always be reviewed by a human, and gates startup
    /// behind an "I Agree" / "I Disagree" choice. Once the user agrees, acceptance is recorded so the
    /// dialog is not shown again. Shown by <see cref="Program"/> before the main window.
    /// </summary>
    public partial class WelcomeForm : Form
    {
        /// <summary>The published Philterd Commercial License Agreement governing the official build.</summary>
        private const string EulaUrl = "https://philterd.ai/philterd-eula/";

        /// <summary>
        /// Where acceptance is persisted. Defaults to the Windows registry; tests can swap in an
        /// in-memory store so they don't touch the real per-user registry.
        /// </summary>
        internal static IEulaAcceptanceStore AcceptanceStore { get; set; } = new RegistryEulaAcceptanceStore();

        public WelcomeForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_agree);
            _body.Text = BodyText;
            _body.Select(0, 0); // keep the view scrolled to the top
        }

        /// <summary>Whether the welcome dialog should be shown on this launch (i.e. not yet accepted).</summary>
        public static bool ShouldShow() => !AcceptanceStore.HasAccepted();

        /// <summary>Opens the published Commercial License Agreement in the user's browser.</summary>
        private void OnEulaLinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(EulaUrl) { UseShellExecute = true });
            }
            catch
            {
                // Ignore failures to launch the browser; the URL is also shown in the body text above.
            }
        }

        /// <summary>
        /// Persists that the user accepted the agreement. Called unconditionally once the user agrees, so
        /// acceptance is remembered and the dialog is never re-shown after that (no opt-out to leave off).
        /// </summary>
        public static void RememberAccepted() => AcceptanceStore.RememberAccepted();

        private const string BodyText =
            "Philter Desktop\r\n" +
            "Copyright © 2026 Philterd, LLC\r\n" +
            "\r\n" +
            "LICENSE\r\n" +
            "Philter Desktop is open-source software licensed under the Apache License, Version 2.0. " +
            "You may obtain a copy of the license at https://www.apache.org/licenses/LICENSE-2.0. " +
            "Unless required by applicable law or agreed to in writing, the software is provided on an " +
            "\"AS IS\" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.\r\n" +
            "\r\n" +
            "END USER LICENSE AGREEMENT\r\n" +
            "The official Philter Desktop build is provided under the Philterd Commercial License " +
            "Agreement, available at " + EulaUrl + ". The underlying source code " +
            "remains open source under the Apache License 2.0. By clicking \"I Agree\" you accept the " +
            "Philterd Commercial License Agreement and this notice. If you do not agree, click " +
            "\"I Disagree\" and the application will close.\r\n" +
            "\r\n" +
            "IMPORTANT — ALWAYS REVIEW REDACTIONS\r\n" +
            "Philter Desktop identifies personal and sensitive information using statistical and " +
            "machine-learning methods. These methods are not perfect: they can miss sensitive " +
            "information (false negatives) or flag text that is not sensitive (false positives). " +
            "Redacted documents must always be carefully reviewed by a qualified person before they " +
            "are shared or relied upon. You are responsible for verifying that every document has been " +
            "redacted appropriately for your needs.";
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
