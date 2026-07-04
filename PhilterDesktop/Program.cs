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

namespace PhilterDesktop
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static int Main(string[] args)
        {
            // Cap every regex match process-wide before any redaction runs, so a runaway custom-identifier
            // pattern from a policy can't hang the app (GUI, watched folders, or the command line).
            RegexSafety.InstallDefaultMatchTimeout();

            CommandLineOptions options = CommandLineOptions.Parse(args);

            // Post-install smoke test: redact a small built-in corpus and verify it, then exit. Self-contained
            // (needs no user data or database) — see RELEASE_TESTING.md.
            if (args.Any(a => a.Equals("--selftest", StringComparison.OrdinalIgnoreCase) || a.Equals("/selftest", StringComparison.OrdinalIgnoreCase)))
            {
                return SelfTest.Run();
            }

            // Release check: confirm the bundled EULA still matches the live copy on philterd.ai (network
            // required), then exit — catches a build that shipped a stale agreement.
            if (args.Any(a => a.Equals("--smoketest", StringComparison.OrdinalIgnoreCase) || a.Equals("/smoketest", StringComparison.OrdinalIgnoreCase)))
            {
                return EulaSmokeTest.Run();
            }

            // Explorer right-click menu: show the "Redact with Philter Desktop" dialog (pick policy +
            // context, then queue the files). Coalesces a multi-file selection into one instance.
            if (options.ShellInvoked && options.Files.Count > 0)
            {
                return ShellContextMenu.Run(options);
            }

            // Headless redaction mode: when launched with file arguments (e.g.
            // `PhilterDesktop.exe /p mypolicy /c mycontext file1.pdf`), redact and exit without a UI.
            if (options.IsCommandLine)
            {
                return CommandLineRedactor.Run(options);
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Catch unhandled exceptions: log them and show a friendly dialog instead of the raw .NET
            // crash window. UI-thread errors route through ThreadException; background-thread/terminal
            // ones through the AppDomain handler.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => HandleUnhandledException(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, e) => HandleUnhandledException(e.ExceptionObject as Exception);

            // Use the modern Windows 11 UI font everywhere, including dialogs that
            // are not individually themed.
            Application.SetDefaultFont(ModernTheme.UiFont);

            // Single-instance: only one GUI per session may run, since the database is shared multi-process
            // and two GUIs would both run the queue timer and double-process the same items. A second
            // launch brings the running window to the front and exits. Checked before the EULA/passphrase
            // prompts so a second launch doesn't re-prompt. Held for the GUI's lifetime (also lets the
            // Explorer context-menu flow detect a running GUI).
            using Mutex guiLifetime = AppInstance.CreateGuiLifetime(out bool isFirstInstance);
            if (!isFirstInstance)
            {
                AppInstance.SignalExistingInstance();
                return 0;
            }

            // Launched at sign-in (or with --minimized): start hidden in the system tray.
            bool startMinimized = args.Any(a =>
                a.Equals(StartupManager.MinimizedSwitch, StringComparison.OrdinalIgnoreCase) ||
                a.Equals("-m", StringComparison.OrdinalIgnoreCase));

            // First-run license / EULA acknowledgement. Skipped during a silent tray auto-start;
            // if the user disagrees, the application exits without starting.
            if (!startMinimized && LicenseForm.ShouldShow())
            {
                using var licenseForm = new LicenseForm();
                if (licenseForm.ShowDialog() != DialogResult.OK)
                {
                    return 0;
                }
                // Once the user agrees, persist acceptance so the dialog is never shown again. (There is
                // no opt-out checkbox to leave unchecked, which previously caused it to re-prompt forever.)
                LicenseForm.RememberAccepted();
            }

            // Unlock the database key. If it's passphrase-protected, prompt now (cancel = exit);
            // otherwise it's unlocked via DPAPI. The unlocked key store is handed to MainForm's open.
            string dbPath = EncryptedDatabase.DefaultPath();
            DatabaseKeyStore keyStore = DatabaseKeyStore.ForDatabase(dbPath);
            if (keyStore.IsPassphraseProtected)
            {
                if (!PassphraseForm.Unlock(keyStore, owner: null))
                {
                    return 0; // user cancelled the unlock prompt
                }
            }
            else
            {
                keyStore.UnlockWithDpapi();
            }
            EncryptedDatabase.Prepare(keyStore);

            Application.Run(new MainForm(startMinimized));
            return 0;
        }

        // Logs an unhandled exception and shows a friendly message rather than a raw crash dialog.
        private static void HandleUnhandledException(Exception? ex)
        {
            try
            {
                Logger.LogError("Unhandled exception", ex ?? new Exception("Unknown error"));
            }
            catch
            {
                // Never let logging failures mask the original error.
            }

            try
            {
                MessageBox.Show(
                    "Philter Desktop ran into an unexpected problem." + Environment.NewLine + Environment.NewLine +
                    "The details were written to the log file (Settings → Open Log File). If this keeps " +
                    "happening, please contact support@philterd.ai." + Environment.NewLine + Environment.NewLine +
                    (ex?.Message ?? "Unknown error"),
                    "Philter Desktop",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
                // If we can't even show the dialog (e.g. during shutdown), there's nothing more to do.
            }
        }
    }
}