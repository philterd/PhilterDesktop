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
using LiteDB;
using PhilterData;

namespace PhilterDesktop
{
    /// <summary>
    /// Handles the Explorer right-click ("Redact with Philter Desktop") invocation: coalesces a
    /// multi-file selection into one instance, shows <see cref="ContextMenuRedactForm"/> to pick the
    /// policy/context, queues the chosen files, and makes sure the main app is running to process them.
    /// </summary>
    internal static class ShellContextMenu
    {
        public static int Run(CommandLineOptions options)
        {
            // A multi-file selection launches one process per file; coalesce into a single instance.
            List<string>? coalesced = CliBatch.Coordinate(options.Files);
            if (coalesced is null)
            {
                return 0; // secondary process — it forwarded its files to the primary
            }

            List<string> files = coalesced
                .Where(RedactionService.IsSupported)
                .ToList();
            if (files.Count == 0)
            {
                return 0; // nothing redactable was selected
            }

            ApplicationConfiguration.Initialize();
            Application.SetDefaultFont(ModernTheme.UiFont);

            LiteDatabase database;
            try
            {
                database = EncryptedDatabase.Open(DatabasePath());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not open the Philter Desktop database: {ex.Message}",
                    "Redact with Philter Desktop", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 2;
            }

            using (database)
            {
                var policies = new PolicyRepository(database);
                var contexts = new ContextRepository(database);
                var queue = new RedactionQueueRepository(database);
                EnsureDefaults(policies, contexts);

                using var form = new ContextMenuRedactForm(files, policies, contexts, queue);
                if (form.ShowDialog() == DialogResult.OK && form.EnqueuedCount > 0)
                {
                    EnsureMainAppRunning();
                }
            }

            return 0;
        }

        // Starts the main window if it isn't already running, so the queued files get processed.
        // If it is running, its queue timer picks them up from the (shared) database.
        private static void EnsureMainAppRunning()
        {
            if (AppInstance.IsGuiRunning())
            {
                return;
            }
            try
            {
                string exe = Environment.ProcessPath ?? Application.ExecutablePath;
                Process.Start(new ProcessStartInfo { FileName = exe, UseShellExecute = false });
            }
            catch
            {
                // The files are queued regardless; they'll be processed next time the app runs.
            }
        }

        private static void EnsureDefaults(PolicyRepository policies, ContextRepository contexts)
        {
            if (policies.FindByName("default") is null)
            {
                policies.Insert(new PolicyEntity { Name = "default", Json = "{}" });
            }
            if (contexts.FindByName("default") is null)
            {
                contexts.Insert(new ContextEntity { Name = "default" });
            }
        }

        private static string DatabasePath()
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(root, "PhilterDesktop", "data.db");
        }
    }
}
