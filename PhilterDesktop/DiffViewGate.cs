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
    /// Decides whether "View Diff" should be offered for a document. The text diff loads both files
    /// into memory and renders one row per line, so it's limited to comparable file types and to files
    /// under a size cap (above which a huge input — e.g. a large CSV export — could freeze or run out
    /// of memory).
    /// </summary>
    internal static class DiffViewGate
    {
        /// <summary>Maximum size (per file) View Diff will compare.</summary>
        public const long MaxFileBytes = 10L * 1024 * 1024; // 10 MB

        /// <summary>Human-readable size limit (e.g. "10 MB") for messages.</summary>
        public static string MaxFileSizeText => $"{MaxFileBytes / (1024 * 1024)} MB";

        /// <summary>
        /// File types View Diff can compare: text-based types use the line diff (.txt/.docx/.csv/.eml),
        /// and .pdf uses the side-by-side page comparison.
        /// </summary>
        public static bool IsDiffableType(string path)
        {
            string ext = Path.GetExtension(path);
            return ext.Equals(".txt", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".docx", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".csv", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".eml", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>True if a file of the given size is within the diff size cap.</summary>
        public static bool IsWithinSizeLimit(long fileBytes) => fileBytes <= MaxFileBytes;
    }
}
