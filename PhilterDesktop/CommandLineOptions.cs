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
    /// <summary>
    /// Parsed command-line options for headless redaction, e.g.
    /// <c>PhilterDesktop.exe /p mypolicy /c mycontext file1.pdf file2.pdf</c>.
    /// Policy and context are optional (they default to "default"); one or more files trigger
    /// command-line (headless) mode instead of launching the GUI.
    /// </summary>
    internal sealed class CommandLineOptions
    {
        /// <summary>Policy name from <c>/p</c> (null = use the default policy).</summary>
        public string? Policy { get; private set; }

        /// <summary>Context name from <c>/c</c> (null = use the default context).</summary>
        public string? Context { get; private set; }

        /// <summary>Files to redact (every non-switch argument).</summary>
        public List<string> Files { get; } = new();

        /// <summary>True if a help switch was given.</summary>
        public bool ShowHelp { get; private set; }

        /// <summary>
        /// True when <c>--highlight</c>/<c>/highlight</c> was given: highlight replacements in Word
        /// (.docx) output (ignored for other file types), matching the queue/watched-folder option.
        /// </summary>
        public bool Highlight { get; private set; }

        /// <summary>
        /// True when launched from the Explorer right-click menu (the hidden <c>--shell</c> switch).
        /// In this mode, files from a multi-file selection (which Explorer launches as one process per
        /// file) are coalesced into a single instance for one batched redaction.
        /// </summary>
        public bool ShellInvoked { get; private set; }

        /// <summary>True when the app should run headless (redact files or print help) rather than show the GUI.</summary>
        public bool IsCommandLine => ShowHelp || Files.Count > 0;

        /// <summary>
        /// Parses arguments. Recognizes <c>/p|-p|--policy</c>, <c>/c|-c|--context</c>,
        /// <c>--highlight|/highlight</c>, and <c>/h|/?|-h|--help</c>; the GUI-only <c>--minimized|-m</c>
        /// switch is ignored here; every other token is treated as a file to redact.
        /// </summary>
        public static CommandLineOptions Parse(string[] args)
        {
            var options = new CommandLineOptions();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                switch (arg.ToLowerInvariant())
                {
                    case "/p":
                    case "-p":
                    case "--policy":
                        if (i + 1 < args.Length)
                        {
                            options.Policy = args[++i];
                        }
                        break;

                    case "/c":
                    case "-c":
                    case "--context":
                        if (i + 1 < args.Length)
                        {
                            options.Context = args[++i];
                        }
                        break;

                    case "/h":
                    case "/?":
                    case "-h":
                    case "--help":
                        options.ShowHelp = true;
                        break;

                    case "--highlight":
                    case "/highlight":
                        options.Highlight = true;
                        break;

                    case "--shell":
                    case "/shell":
                        options.ShellInvoked = true;
                        break;

                    case "--minimized":
                    case "-m":
                        // Handled by the GUI launch path; not a file.
                        break;

                    default:
                        options.Files.Add(arg);
                        break;
                }
            }

            return options;
        }
    }
}
