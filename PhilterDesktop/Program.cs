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
            CommandLineOptions options = CommandLineOptions.Parse(args);

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

            // Use the modern Windows 11 UI font everywhere, including dialogs that
            // are not individually themed.
            Application.SetDefaultFont(ModernTheme.UiFont);

            // Launched at sign-in (or with --minimized): start hidden in the system tray.
            bool startMinimized = args.Any(a =>
                a.Equals(StartupManager.MinimizedSwitch, StringComparison.OrdinalIgnoreCase) ||
                a.Equals("-m", StringComparison.OrdinalIgnoreCase));

            // First-run license / EULA acknowledgement. Skipped during a silent tray auto-start;
            // if the user disagrees, the application exits without starting.
            if (!startMinimized && WelcomeForm.ShouldShow())
            {
                using var welcome = new WelcomeForm();
                if (welcome.ShowDialog() != DialogResult.OK)
                {
                    return 0;
                }
                if (welcome.DoNotShowAgain)
                {
                    WelcomeForm.RememberAccepted();
                }
            }

            // Hold a session-scoped mutex for the GUI's lifetime so the Explorer context-menu flow can
            // tell the app is running (and let it process the queue instead of launching a new instance).
            using Mutex guiLifetime = AppInstance.CreateGuiLifetime();

            Application.Run(new MainForm(startMinimized));
            return 0;
        }
    }
}