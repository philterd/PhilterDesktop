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
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The queue's Remove and Retry context-menu actions must act on <b>every</b> selected row, not
    /// just the first. These exercise the extracted bulk logic against a
    /// real LiteDB.
    /// </summary>
    public sealed class QueueBulkActionsTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly LiteDatabase _db;
        private readonly RedactionQueueRepository _repo;

        public QueueBulkActionsTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), "qba-" + Guid.NewGuid().ToString("N") + ".db");
            _db = new LiteDatabase(_dbPath);
            _repo = new RedactionQueueRepository(_db);
        }

        public void Dispose()
        {
            _db.Dispose();
            try { File.Delete(_dbPath); } catch { /* best effort */ }
        }

        private ObjectId Add(string name, string status)
        {
            var entity = new RedactionQueueEntity { Name = name, Status = status, Policy = "p", Context = "c" };
            _repo.Insert(entity);
            return entity.Id;
        }

        // --- RemoveMany ---------------------------------------------------------------------------

        [Fact]
        public void RemoveMany_RemovesAllSelected_AndTheirHistory()
        {
            ObjectId a = Add(@"C:\a.txt", "Completed");
            ObjectId b = Add(@"C:\b.txt", "Pending");
            Add(@"C:\c.txt", "Completed"); // not selected — must remain

            var historyDeleted = new List<ObjectId>();
            List<string> skipped = QueueBulkActions.RemoveMany(_repo, new[] { a, b }, historyDeleted.Add);

            Assert.Empty(skipped);
            Assert.Single(_repo.GetAll());                       // only c remains
            Assert.Null(_repo.GetById(a));
            Assert.Null(_repo.GetById(b));
            Assert.Equal(new[] { a, b }, historyDeleted);        // history cleaned for each removed item
        }

        [Fact]
        public void RemoveMany_SkipsProcessingItems_AndReportsThem()
        {
            ObjectId a = Add(@"C:\a.txt", "Completed");
            ObjectId busy = Add(@"C:\busy.txt", "Processing");

            var historyDeleted = new List<ObjectId>();
            List<string> skipped = QueueBulkActions.RemoveMany(_repo, new[] { a, busy }, historyDeleted.Add);

            Assert.Equal(new[] { "busy.txt" }, skipped);         // processing item reported, not removed
            Assert.Null(_repo.GetById(a));                       // the other was removed
            Assert.NotNull(_repo.GetById(busy));                 // the processing one survives
            Assert.Equal(new[] { a }, historyDeleted);           // history deleted only for the removed item
        }

        [Fact]
        public void RemoveMany_AllProcessing_RemovesNothing()
        {
            ObjectId p1 = Add(@"C:\p1.txt", "Processing");
            ObjectId p2 = Add(@"C:\p2.txt", "Processing");

            List<string> skipped = QueueBulkActions.RemoveMany(_repo, new[] { p1, p2 }, _ => { });

            Assert.Equal(2, skipped.Count);
            Assert.Equal(2, _repo.GetAll().Count());
        }

        [Fact]
        public void RemoveMany_IgnoresUnknownIds()
        {
            ObjectId a = Add(@"C:\a.txt", "Completed");
            ObjectId ghost = ObjectId.NewObjectId(); // never inserted

            List<string> skipped = QueueBulkActions.RemoveMany(_repo, new[] { a, ghost }, _ => { });

            Assert.Empty(skipped);
            Assert.Empty(_repo.GetAll());
        }

        [Fact]
        public void RemoveMany_EmptySelection_IsNoOp()
        {
            Add(@"C:\a.txt", "Completed");

            List<string> skipped = QueueBulkActions.RemoveMany(_repo, Array.Empty<ObjectId>(), _ => Assert.Fail("should not delete history"));

            Assert.Empty(skipped);
            Assert.Single(_repo.GetAll());
        }

        // --- RemoveConfirmationMessage -----------------------------------------------------

        [Fact]
        public void RemoveConfirmationMessage_SingleItem_NamesTheFile()
        {
            string message = QueueBulkActions.RemoveConfirmationMessage(1, "invoice.pdf");

            Assert.Contains("invoice.pdf", message);
            Assert.Contains("are not deleted", message);
            Assert.Contains("history for the removed item.", message); // singular
        }

        [Fact]
        public void RemoveConfirmationMessage_MultipleItems_UsesCountAndPlural()
        {
            string message = QueueBulkActions.RemoveConfirmationMessage(3, null);

            Assert.Contains("Remove 3 items", message);
            Assert.Contains("removed items", message); // plural
            Assert.Contains("are not deleted", message);
        }

        // --- RetryManyFailed ----------------------------------------------------------------------

        [Fact]
        public void RetryManyFailed_RequeuesEverySelectedFailedItem()
        {
            ObjectId f1 = Add(@"C:\f1.txt", "Failed");
            ObjectId f2 = Add(@"C:\f2.txt", "Failed");

            int requeued = QueueBulkActions.RetryManyFailed(_repo, new[] { f1, f2 });

            Assert.Equal(2, requeued);
            Assert.Equal("Pending", _repo.GetById(f1)!.Status);
            Assert.Equal("Pending", _repo.GetById(f2)!.Status);
        }

        [Fact]
        public void RetryManyFailed_SkipsNonFailedItems()
        {
            ObjectId failed = Add(@"C:\f.txt", "Failed");
            ObjectId completed = Add(@"C:\c.txt", "Completed");
            ObjectId pending = Add(@"C:\p.txt", "Pending");

            int requeued = QueueBulkActions.RetryManyFailed(_repo, new[] { failed, completed, pending });

            Assert.Equal(1, requeued);                            // only the failed one
            Assert.Equal("Pending", _repo.GetById(failed)!.Status);
            Assert.Equal("Completed", _repo.GetById(completed)!.Status); // untouched
        }

        [Fact]
        public void RetryManyFailed_ClearsErrorMessage()
        {
            var entity = new RedactionQueueEntity { Name = @"C:\f.txt", Status = "Failed", ErrorMessage = "disk full" };
            _repo.Insert(entity);

            QueueBulkActions.RetryManyFailed(_repo, new[] { entity.Id });

            RedactionQueueEntity reloaded = _repo.GetById(entity.Id)!;
            Assert.Equal("Pending", reloaded.Status);
            Assert.True(string.IsNullOrEmpty(reloaded.ErrorMessage)); // cleared (LiteDB round-trips "" as null)
        }

        [Fact]
        public void RetryManyFailed_NoFailedSelected_ReturnsZero()
        {
            ObjectId completed = Add(@"C:\c.txt", "Completed");

            Assert.Equal(0, QueueBulkActions.RetryManyFailed(_repo, new[] { completed }));
        }

        [Fact]
        public void RetryManyFailed_IgnoresUnknownIds()
        {
            Assert.Equal(0, QueueBulkActions.RetryManyFailed(_repo, new[] { ObjectId.NewObjectId() }));
        }

        [Fact]
        public void RetryManyFailed_EmptySelection_ReturnsZero()
        {
            Assert.Equal(0, QueueBulkActions.RetryManyFailed(_repo, Array.Empty<ObjectId>()));
        }
    }
}
