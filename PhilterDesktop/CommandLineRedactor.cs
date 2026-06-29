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

using System.Runtime.InteropServices;
using LiteDB;
using Phileas.Policy;
using Phileas.Services;
using PhilterData;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop
{
    /// <summary>
    /// Headless (command-line) redaction. Resolves the named policy (and output-location settings)
    /// from the application database when available, then redacts each file to a "_redacted" copy.
    /// Used when the exe is launched with file arguments — see <see cref="CommandLineOptions"/>.
    /// </summary>
    internal static class CommandLineRedactor
    {
        private const string DefaultName = "default";

        /// <summary>Runs headless redaction. Returns a process exit code (0 ok, 1 some failed, 2 usage/policy error).</summary>
        public static int Run(CommandLineOptions options)
        {
            // A WinExe has no console of its own; attach to the launching console so output is visible
            // when run from cmd/PowerShell (silently no-ops when launched without one).
            AttachConsole(AttachParentProcess);

            if (options.ShowHelp)
            {
                PrintUsage();
                return 0;
            }

            if (options.Files.Count == 0)
            {
                Console.Error.WriteLine("No files specified.");
                PrintUsage();
                return 2;
            }

            IReadOnlyList<string> files = options.Files;
            string policyName = options.Policy ?? DefaultName;
            string contextName = options.Context ?? DefaultName;

            if (!TryResolvePolicy(policyName, out string policyJson, out SettingsEntity settings, out string? error))
            {
                Console.Error.WriteLine(error);
                return 2;
            }

            // Validate the policy once up front so a bad policy fails fast.
            try
            {
                _ = DeserializePolicy(policyJson);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load policy '{policyName}': {ex.Message}");
                return 2;
            }

            // Warn loudly if the policy expects on-device name detection but the model is missing: names
            // would silently be left in. (Checked on the resolved policy before redaction strips it.)
            if (PhEyeModel.RequestedButUnavailable(DeserializePolicy(policyJson)))
            {
                Console.Error.WriteLine("Warning: " + PhEyeModel.UnavailableWarning);
            }

            // Apply the configured regex match timeout (over the startup default) before redacting.
            RegexSafety.InstallMatchTimeout(settings.RegexMatchTimeoutSeconds);

            var filterService = new FilterService();
            int failures = 0;

            foreach (string file in files)
            {
                string path = Path.GetFullPath(file);

                if (!File.Exists(path))
                {
                    Console.Error.WriteLine($"Not found: {file}");
                    failures++;
                    continue;
                }
                if (!RedactionService.IsSupported(path))
                {
                    Console.Error.WriteLine($"Unsupported file type (expected .txt, .docx, .pdf, .rtf, .xlsx, .csv, .eml, or .msg): {file}");
                    failures++;
                    continue;
                }
                if (LargeFileWarning.ExceedsHardLimit(path, settings.MaxInputFileSizeMb))
                {
                    Console.Error.WriteLine($"Skipped (exceeds the {settings.MaxInputFileSizeMb} MB size limit): {file}");
                    failures++;
                    continue;
                }

                try
                {
                    // Deserialize per file: redaction mutates the policy (PhEye model path, PDF scale).
                    PhileasPolicy policy = DeserializePolicy(policyJson);
                    GlobalLists.Apply(policy, settings); // global always-redact/ignore on top of every policy
                    string outputPath = RedactionService.GetUniqueOutputPath(RedactionService.GetOutputPath(path, settings));
                    RedactionService.RedactFileAsync(path, outputPath, policy, contextName, settings, filterService)
                        .GetAwaiter().GetResult();
                    Console.WriteLine($"Redacted: {path} -> {outputPath}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Failed: {file}: {ex.Message}");
                    failures++;
                }
            }

            return failures == 0 ? 0 : 1;
        }

        // Resolves the policy JSON + output settings, preferring the application database. Falls back
        // to a built-in empty policy + original-location output when the DB is unavailable (e.g. the
        // GUI has it open) — but only for the default policy; a named policy then fails clearly.
        private static bool TryResolvePolicy(string policyName, out string policyJson, out SettingsEntity settings, out string? error)
        {
            policyJson = DefaultPolicy.Json();
            settings = new SettingsEntity { OutputToOriginalLocation = true };
            error = null;

            LiteDatabase? database = TryOpenDatabase(out string? dbError);
            if (database is null)
            {
                if (!IsDefault(policyName))
                {
                    error = $"Cannot resolve policy '{policyName}': the database could not be opened ({dbError}). " +
                            "Omit /p to use the default policy.";
                    return false;
                }
                Console.Error.WriteLine($"Warning: database unavailable ({dbError}); using the built-in default policy and writing output next to each source file.");
                return true;
            }

            using (database)
            {
                PolicyEntity? entity = new PolicyRepository(database).FindByName(policyName);
                if (entity is null && !IsDefault(policyName))
                {
                    error = $"Policy '{policyName}' not found.";
                    return false;
                }
                policyJson = string.IsNullOrWhiteSpace(entity?.Json) ? DefaultPolicy.Json() : entity!.Json;
                settings = new SettingsRepository(database).GetSettings();
                return true;
            }
        }

        private static LiteDatabase? TryOpenDatabase(out string? error)
        {
            error = null;
            try
            {
                string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string dbPath = Path.Combine(root, "PhilterDesktop", "data.db");
                return EncryptedDatabase.Open(dbPath);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        private static PhileasPolicy DeserializePolicy(string json) =>
            PolicySerializer.DeserializeFromJson(string.IsNullOrWhiteSpace(json) ? "{}" : json);

        private static bool IsDefault(string name) => string.Equals(name, DefaultName, StringComparison.OrdinalIgnoreCase);

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: PhilterDesktop.exe [/p <policy>] [/c <context>] <file> [<file> ...]");
            Console.WriteLine();
            Console.WriteLine("  /p, --policy   Redaction policy name to use (default: \"default\").");
            Console.WriteLine("  /c, --context  Redaction context name to use (default: \"default\").");
            Console.WriteLine("  /h, --help     Show this help.");
            Console.WriteLine();
            Console.WriteLine("Each file is redacted to a copy with the configured suffix (default \"_redacted-draft\"); the original is not changed.");
            Console.WriteLine("Supported file types: .txt, .docx, .pdf, .rtf, .xlsx, .csv, .eml, .msg (.msg is redacted to .eml).");
            Console.WriteLine();
            Console.WriteLine("Redacting in a data pipeline or at scale? Philter (server/API) is built for it:");
            Console.WriteLine("  " + Links.PhilterUrl("cli"));
        }

        private const int AttachParentProcess = -1;

        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);
    }
}
