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
    /// <summary>How much attention a watched folder needs, from its activity-log entries.</summary>
    internal enum WatchedFolderSeverity
    {
        None,
        Warning, // redacted with a caveat (e.g. no name detection)
        Error    // a file was not redacted (unknown policy, oversize skip, redaction error)
    }

    /// <summary>
    /// Turns a watched folder's error/warning counts (from its always-written activity log) into an
    /// at-a-glance summary, so failures are visible on the watched-folder list even when notifications
    /// and application logging are both off (#531).
    /// </summary>
    internal static class WatchedFolderIssueSummary
    {
        public static WatchedFolderSeverity Severity(int errors, int warnings) =>
            errors > 0 ? WatchedFolderSeverity.Error
            : warnings > 0 ? WatchedFolderSeverity.Warning
            : WatchedFolderSeverity.None;

        /// <summary>Short cell text for the "Issues" column (e.g. "2 errors, 1 warning" or "None").</summary>
        public static string Describe(int errors, int warnings)
        {
            if (errors > 0 && warnings > 0)
            {
                return $"{Count(errors, "error")}, {Count(warnings, "warning")}";
            }
            if (errors > 0)
            {
                return Count(errors, "error");
            }
            if (warnings > 0)
            {
                return Count(warnings, "warning");
            }
            return "None";
        }

        /// <summary>The hover tooltip explaining the issues and pointing at the log, or null when there are none.</summary>
        public static string? Tooltip(int errors, int warnings)
        {
            if (errors > 0)
            {
                return $"{Count(errors, "error")} in this folder's log — some files were not redacted. " +
                       "Select the folder and click View Log for details.";
            }
            if (warnings > 0)
            {
                return $"{Count(warnings, "warning")} in this folder's log — review these files. " +
                       "Select the folder and click View Log for details.";
            }
            return null;
        }

        private static string Count(int n, string noun) => n == 1 ? $"1 {noun}" : $"{n} {noun}s";
    }
}
