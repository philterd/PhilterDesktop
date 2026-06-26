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
    /// Helpers for warning the user before they exit while redaction work is still in flight.
    /// "Active" work is anything queued or running (Pending or Processing); closing the window with
    /// the X button merely hides to the tray and keeps processing, so only a real Exit needs this.
    /// </summary>
    internal static class ExitGuard
    {
        public static bool IsActiveStatus(string? status) =>
            string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "Processing", StringComparison.OrdinalIgnoreCase);

        public static int CountActive(IEnumerable<string?> statuses) =>
            statuses.Count(IsActiveStatus);

        /// <summary>The confirmation prompt shown when exiting with <paramref name="active"/> items in flight.</summary>
        public static string Message(int active)
        {
            string subject = active == 1 ? "document is" : "documents are";
            return $"{active} {subject} still being redacted or waiting in the queue. " +
                   "If you exit now, that work will stop and those documents won't be finished." +
                   Environment.NewLine + Environment.NewLine +
                   "Exit anyway?";
        }
    }
}
