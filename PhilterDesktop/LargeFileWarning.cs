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
    /// Warns before queueing very large documents. Redaction loads a document into memory (the whole
    /// workbook for a spreadsheet, the whole file for text/CSV), so a very large input can be slow and
    /// memory-heavy. The work runs on a background thread, so this never blocks the UI — it's a
    /// proceed/cancel heads-up offered at the interactive add points (drag-drop, Add Files, Redact
    /// Spreadsheet); the watched-folder and command-line paths are non-interactive and just proceed.
    /// </summary>
    internal static class LargeFileWarning
    {
        /// <summary>Files larger than this trigger the heads-up.</summary>
        public const long WarnAboveBytes = 50L * 1024 * 1024; // 50 MB

        /// <summary>Human-readable threshold (e.g. "50 MB") for messages.</summary>
        public static string ThresholdText => $"{WarnAboveBytes / (1024 * 1024)} MB";

        public static bool IsLarge(long bytes) => bytes > WarnAboveBytes;

        public static bool IsLarge(string path)
        {
            try
            {
                var info = new FileInfo(path);
                return info.Exists && IsLarge(info.Length);
            }
            catch
            {
                return false; // can't stat it — don't warn
            }
        }

        /// <summary>The subset of <paramref name="paths"/> that exceed the threshold.</summary>
        public static List<string> LargeFiles(IEnumerable<string> paths) => paths.Where(IsLarge).ToList();

        /// <summary>The proceed/cancel message for a set of large files (file names only).</summary>
        public static string BuildMessage(IReadOnlyList<string> largeFiles)
        {
            string list = string.Join(Environment.NewLine, largeFiles.Select(p => "  • " + Path.GetFileName(p)));
            string lead = largeFiles.Count == 1 ? "This file is" : "These files are";
            return $"{lead} larger than {ThresholdText}:" + Environment.NewLine + Environment.NewLine +
                   list + Environment.NewLine + Environment.NewLine +
                   "Redacting very large files can take a while and use a lot of memory. " +
                   "Add to the queue anyway?";
        }

        /// <summary>
        /// If any of <paramref name="paths"/> are large, asks the user whether to proceed; returns
        /// true to continue (nothing large, or the user confirmed) and false to cancel.
        /// </summary>
        public static bool ConfirmIfLarge(IWin32Window owner, IReadOnlyList<string> paths)
        {
            List<string> large = LargeFiles(paths);
            if (large.Count == 0)
            {
                return true;
            }
            return MessageBox.Show(owner, BuildMessage(large), "Large File",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        }
    }
}
