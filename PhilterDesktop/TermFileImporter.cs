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
    /// Imports a list of terms from a text or single-column CSV file (one term per line) into a
    /// term-list text box — used by the global Lists dialog and the policy Ignore / Always-Redact
    /// dialogs so users can load existing lists instead of typing them.
    /// </summary>
    internal static class TermFileImporter
    {
        /// <summary>Parses file text into terms: one per line, trimmed, surrounding quotes removed,
        /// blanks dropped, de-duplicated (case-insensitive, first occurrence wins).</summary>
        public static List<string> Parse(string? text)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return result;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in text.Replace("\r\n", "\n").Split('\n'))
            {
                string term = raw.Trim();
                if (term.Length >= 2 && term[0] == '"' && term[^1] == '"')
                {
                    term = term[1..^1].Trim(); // unwrap a quoted CSV field (keeps any internal commas)
                }
                if (term.Length == 0)
                {
                    continue;
                }
                if (seen.Add(term))
                {
                    result.Add(term);
                }
            }
            return result;
        }

        public static List<string> ParseFile(string path) => Parse(File.ReadAllText(path));

        /// <summary>Appends terms not already present (case-insensitive) to the text box; returns how many were added.</summary>
        public static int AppendNew(TextBox target, IReadOnlyList<string> terms)
        {
            var existing = new HashSet<string>(
                target.Text.Replace("\r\n", "\n").Split('\n').Select(t => t.Trim()).Where(t => t.Length > 0),
                StringComparer.OrdinalIgnoreCase);

            var toAdd = terms.Where(t => existing.Add(t)).ToList();
            if (toAdd.Count == 0)
            {
                return 0;
            }

            bool needsNewline = target.TextLength > 0 && !target.Text.EndsWith("\n");
            target.AppendText((needsNewline ? Environment.NewLine : "") + string.Join(Environment.NewLine, toAdd) + Environment.NewLine);
            return toAdd.Count;
        }

        /// <summary>Prompts for a file and appends its terms to the text box.</summary>
        public static void PromptAndAppend(IWin32Window owner, TextBox target)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Import Terms",
                Filter = "Text or CSV files (*.txt;*.csv)|*.txt;*.csv|All files (*.*)|*.*",
                CheckFileExists = true
            };
            if (dlg.ShowDialog(owner) != DialogResult.OK)
            {
                return;
            }

            List<string> terms;
            try
            {
                terms = ParseFile(dlg.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, $"Could not read the file: {ex.Message}",
                    "Import Terms", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (AppendNew(target, terms) == 0)
            {
                MessageBox.Show(owner, "No new terms were found in the file.",
                    "Import Terms", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
