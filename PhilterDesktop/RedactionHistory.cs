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

using PhilterData;

namespace PhilterDesktop
{
    /// <summary>
    /// Data operations for the saved redaction history, kept out of the form so they can be tested.
    /// </summary>
    internal static class RedactionHistory
    {
        /// <summary>
        /// Clears all saved redaction history — every version and its spans (including the detected
        /// text) — and removes the completed documents from the queue. In-progress and pending items
        /// are left alone, and redacted output files on disk are not touched.
        /// </summary>
        public static void ClearAll(
            RedactionSpanRepository spans,
            RedactionVersionRepository versions,
            RedactionQueueRepository queue)
        {
            spans.DeleteAll();
            versions.DeleteAll();
            queue.DeleteWhere(x => x.Status == "Completed");
        }
    }
}
