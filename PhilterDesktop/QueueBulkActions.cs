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

using LiteDB;
using PhilterData;

namespace PhilterDesktop
{
    /// <summary>
    /// Bulk queue operations that act on <b>every</b> selected row, extracted from the form so the
    /// multi-select behavior is unit-testable (the context-menu actions previously used only
    /// <c>SelectedItems[0]</c> — issue #487).
    /// </summary>
    internal static class QueueBulkActions
    {
        /// <summary>
        /// Removes each queued item (and, via <paramref name="deleteHistory"/>, its history). An item
        /// that is currently <c>Processing</c> is left alone; the names of any such skipped items are
        /// returned so the caller can tell the user. Unknown ids are ignored.
        /// </summary>
        public static List<string> RemoveMany(
            RedactionQueueRepository repository, IEnumerable<ObjectId> ids, Action<ObjectId> deleteHistory)
        {
            var skipped = new List<string>();
            foreach (ObjectId id in ids)
            {
                RedactionQueueEntity? entity = repository.GetById(id);
                if (entity is null)
                {
                    continue;
                }
                if (string.Equals(entity.Status, "Processing", StringComparison.OrdinalIgnoreCase))
                {
                    skipped.Add(Path.GetFileName(entity.Name));
                    continue;
                }
                repository.Delete(id);
                deleteHistory(id);
            }
            return skipped;
        }

        /// <summary>
        /// Requeues every <c>Failed</c> item among <paramref name="ids"/> (resets it to <c>Pending</c>
        /// and clears its error). Returns how many were requeued; non-failed and unknown ids are ignored.
        /// </summary>
        public static int RetryManyFailed(RedactionQueueRepository repository, IEnumerable<ObjectId> ids)
        {
            int requeued = 0;
            foreach (ObjectId id in ids)
            {
                RedactionQueueEntity? entity = repository.GetById(id);
                if (entity is null || !string.Equals(entity.Status, "Failed", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                entity.Status = "Pending";
                entity.ErrorMessage = string.Empty;
                repository.Update(entity);
                requeued++;
            }
            return requeued;
        }
    }
}
