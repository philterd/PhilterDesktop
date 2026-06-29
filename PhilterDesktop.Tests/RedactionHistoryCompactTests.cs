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
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Verifies that compacting (rebuilding) the database after a clear physically reclaims the pages the
    /// deletes freed — so the original detected text can't linger in the file — while keeping the database
    /// encrypted and its live data intact.
    /// </summary>
    public sealed class RedactionHistoryCompactTests : IDisposable
    {
        private readonly string _dir;

        public RedactionHistoryCompactTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-compact-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        [Fact]
        public void Compact_KeepsDatabaseEncrypted_PreservesLiveData_AndShrinksFile()
        {
            string path = Path.Combine(_dir, "enc.db");
            const string password = "test-passphrase-key";
            var encrypted = new ConnectionString { Filename = path, Password = password };

            using (var db = new LiteDatabase(encrypted))
            {
                var col = db.GetCollection("docs");
                for (int i = 0; i < 300; i++)
                {
                    col.Insert(new BsonDocument { ["text"] = new string('x', 500) + i });
                }
            }
            long grown = new FileInfo(path).Length;

            using (var db = new LiteDatabase(encrypted))
            {
                var col = db.GetCollection("docs");
                col.DeleteAll();
                col.Insert(new BsonDocument { ["keep"] = "LIVE-VALUE" }); // one row survives the clear
                RedactionHistory.Compact(db, password);
            }
            long compacted = new FileInfo(path).Length;

            Assert.True(compacted < grown, $"expected the file to shrink: grown={grown}, compacted={compacted}");

            // Still encrypted: opening without the password fails.
            Assert.ThrowsAny<Exception>(() =>
            {
                using var noKey = new LiteDatabase(new ConnectionString { Filename = path });
                _ = noKey.GetCollection("docs").Count();
            });

            // With the password, the surviving row is intact.
            using (var ok = new LiteDatabase(encrypted))
            {
                ILiteCollection<BsonDocument> col = ok.GetCollection("docs");
                Assert.Equal(1, col.Count());
                Assert.Equal("LIVE-VALUE", col.FindAll().First()["keep"].AsString);
            }
        }

        [Fact]
        public void Compact_RequiresPassword()
        {
            using var db = new LiteDatabase(Path.Combine(_dir, "p.db"));
            Assert.Throws<ArgumentException>(() => RedactionHistory.Compact(db, ""));
        }
    }
}
