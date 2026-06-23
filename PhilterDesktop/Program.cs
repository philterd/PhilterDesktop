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
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            // Register the Xceed Words for .NET license (read from an untracked config
            // file or the XCEED_LICENSE_KEY environment variable). Without it, Word
            // redaction falls back to Xceed's trial behavior.
            string? xceedKey = LicenseConfig.GetXceedLicenseKey();
            if (!string.IsNullOrEmpty(xceedKey))
            {
                Xceed.Words.NET.Licenser.LicenseKey = xceedKey;
            }

            // Use the modern Windows 11 UI font everywhere, including dialogs that
            // are not individually themed.
            Application.SetDefaultFont(ModernTheme.UiFont);

            Application.Run(new MainForm());
        }
    }
}