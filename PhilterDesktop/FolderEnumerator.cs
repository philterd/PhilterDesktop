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
    /// Selects the redactable files in a folder for the one-shot "Redact Folder" command. This is the
    /// UI-independent, side-effect-free core (no queue writes, no dialogs) so the selection rules —
    /// supported file types, optional recursion, skipping prior redaction outputs, and tolerating
    /// inaccessible subfolders — are unit-testable.
    /// </summary>
    internal static class FolderEnumerator
    {
        /// <summary>
        /// Returns the supported, redactable files under <paramref name="root"/>, sorted by full path.
        /// Recurses into subfolders when <paramref name="recursive"/> is true. Files that already look
        /// like a redaction output (their name ends with <paramref name="redactedSuffix"/> before the
        /// extension) are skipped, so re-running on a folder doesn't redact its own drafts. Subfolders
        /// that can't be read (permissions/IO) are skipped rather than aborting the whole scan. A
        /// missing or empty root yields an empty list rather than throwing.
        /// </summary>
        public static List<string> EnumerateRedactable(string root, bool recursive, string? redactedSuffix = null)
        {
            var results = new List<string>();
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                return results;
            }

            string suffix = RedactionService.NormalizeSuffix(redactedSuffix);

            foreach (string file in SafeEnumerateFiles(root, recursive))
            {
                if (RedactionService.IsSupported(file) && !LooksLikeRedactionOutput(file, suffix))
                {
                    results.Add(file);
                }
            }

            results.Sort(StringComparer.OrdinalIgnoreCase);
            return results;
        }

        /// <summary>True if the file name (without extension) ends with the redacted-output suffix.</summary>
        public static bool LooksLikeRedactionOutput(string path, string suffix) =>
            !string.IsNullOrEmpty(suffix) &&
            Path.GetFileNameWithoutExtension(path).EndsWith(suffix, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Counts the selected files grouped by lower-cased extension (e.g. <c>(".pdf", 3)</c>),
        /// ordered by extension, for a human-readable pre-run summary.
        /// </summary>
        public static IReadOnlyList<(string Extension, int Count)> SummarizeByType(IEnumerable<string> files) =>
            files.GroupBy(f => Path.GetExtension(f).ToLowerInvariant())
                 .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                 .Select(g => (g.Key, g.Count()))
                 .ToList();

        // Manual recursion so a single unreadable subfolder (permissions) doesn't abort the scan:
        // Directory.EnumerateFiles(root, "*", AllDirectories) throws on the first inaccessible folder
        // and loses everything found so far. Each level's listing is materialized inside the try so a
        // mid-iteration access error is caught here, not propagated to the caller.
        private static IEnumerable<string> SafeEnumerateFiles(string root, bool recursive)
        {
            List<string> files;
            try
            {
                files = Directory.EnumerateFiles(root).ToList();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                yield break;
            }

            foreach (string file in files)
            {
                yield return file;
            }

            if (!recursive)
            {
                yield break;
            }

            List<string> subdirectories;
            try
            {
                subdirectories = Directory.EnumerateDirectories(root).ToList();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                yield break;
            }

            foreach (string directory in subdirectories)
            {
                if (IsReparsePoint(directory))
                {
                    continue; // don't follow junctions/symlinks out of the selected folder
                }
                foreach (string file in SafeEnumerateFiles(directory, recursive: true))
                {
                    yield return file;
                }
            }
        }

        // A directory junction or symbolic link — following it could pull in files outside the folder.
        private static bool IsReparsePoint(string path)
        {
            try
            {
                return File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
            }
            catch
            {
                return false;
            }
        }
    }
}
