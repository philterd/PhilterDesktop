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
using LiteDB.Engine;
using PhilterData;

namespace PhilterDesktop
{
    /// <summary>
    /// Data operations for the saved redaction history, kept out of the form so they can be tested.
    /// </summary>
    internal static class RedactionHistory
    {
        /// <summary>
        /// Persists a redaction version and its spans atomically, in a single transaction, so a failure
        /// can never leave a span-less version (which the Modify-Redaction UI would show but be unable to
        /// re-apply). On any error both writes are rolled back and the exception is rethrown. Callers must
        /// set each span's <c>VersionId</c> to <paramref name="version"/>.Id before calling.
        /// </summary>
        public static void SaveVersionWithSpans(
            LiteDatabase database,
            RedactionVersionRepository versions,
            RedactionSpanRepository spans,
            RedactionVersionEntity version,
            IReadOnlyList<RedactionSpanEntity> versionSpans)
        {
            bool ownsTransaction = database.BeginTrans();
            try
            {
                versions.Insert(version);
                if (versionSpans.Count > 0)
                {
                    spans.InsertBulk(versionSpans);
                }
                if (ownsTransaction)
                {
                    database.Commit();
                }
            }
            catch
            {
                if (ownsTransaction)
                {
                    database.Rollback();
                }
                throw;
            }
        }

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

        /// <summary>
        /// Compacts the database after a clear so the pages freed by the deletes are physically reclaimed.
        /// LiteDB only marks deleted pages free (it doesn't zero them or shrink the file), so the original
        /// detected text can otherwise linger in free space until those pages are reused. Pass the current
        /// encryption <paramref name="password"/> so the rebuilt file stays encrypted with the same key —
        /// omitting it would rewrite the database as plaintext. Safe under LiteDB Shared mode: Rebuild runs
        /// inside the cross-process mutex, so concurrent callers block until it completes rather than
        /// seeing a half-written file.
        /// </summary>
        public static void Compact(LiteDatabase database, string password)
        {
            ArgumentException.ThrowIfNullOrEmpty(password);
            database.Rebuild(new RebuildOptions { Password = password });
        }
    }
}
