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

using System.Text;
using LiteDB;
using Xunit;

namespace PhilterDesktop.Tests
{
    public sealed class EncryptedDatabaseTests : IDisposable
    {
        private readonly string _dir;

        public EncryptedDatabaseTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-encdb-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private string DbPath => Path.Combine(_dir, "data.db");
        private string KeyPath => Path.Combine(_dir, "data.key");

        public sealed class Note
        {
            public int Id { get; set; }
            public string Secret { get; set; } = string.Empty;
        }

        [Fact]
        public void Open_PersistsData_AndReusesKey()
        {
            using (LiteDatabase db = EncryptedDatabase.Open(DbPath))
            {
                db.GetCollection<Note>("n").Insert(new Note { Id = 1, Secret = "top-secret" });
            }

            Assert.True(File.Exists(KeyPath));

            using (LiteDatabase db = EncryptedDatabase.Open(DbPath))
            {
                Assert.Equal("top-secret", db.GetCollection<Note>("n").FindById(1).Secret);
            }
        }

        [Fact]
        public void DatabaseFile_DoesNotContainPlaintextPii()
        {
            using (LiteDatabase db = EncryptedDatabase.Open(DbPath))
            {
                db.GetCollection<Note>("n").Insert(new Note { Id = 1, Secret = "UNIQUE_PII_TOKEN_42" });
            }

            Assert.False(ContainsText(File.ReadAllBytes(DbPath), "UNIQUE_PII_TOKEN_42"));
        }

        [Fact]
        public void Open_MigratesExistingUnencryptedDatabase()
        {
            // A pre-encryption database: plain LiteDB, no key file.
            using (var plain = new LiteDatabase(DbPath))
            {
                plain.GetCollection<Note>("n").Insert(new Note { Id = 1, Secret = "MIGRATE_ME_99" });
            }
            Assert.False(File.Exists(KeyPath));
            Assert.True(ContainsText(File.ReadAllBytes(DbPath), "MIGRATE_ME_99")); // plaintext before

            using (LiteDatabase db = EncryptedDatabase.Open(DbPath))
            {
                Assert.Equal("MIGRATE_ME_99", db.GetCollection<Note>("n").FindById(1).Secret); // data preserved
            }

            Assert.True(File.Exists(KeyPath));
            Assert.False(ContainsText(File.ReadAllBytes(DbPath), "MIGRATE_ME_99")); // encrypted after
        }

        [Fact]
        public void Open_AllowsConcurrentConnections()
        {
            // Shared mode lets a second process (e.g. the CLI launched from the Explorer context menu)
            // open the database while the main app already has it open. With exclusive (Direct) mode
            // the second Open would throw.
            using LiteDatabase first = EncryptedDatabase.Open(DbPath);
            first.GetCollection<Note>("n").Insert(new Note { Id = 1, Secret = "shared-ok" });

            using LiteDatabase second = EncryptedDatabase.Open(DbPath);
            Assert.Equal("shared-ok", second.GetCollection<Note>("n").FindById(1).Secret);
        }

        [Fact]
        public void Open_WithPassphrase_PreservesDataWithoutRebuild()
        {
            using (LiteDatabase db = EncryptedDatabase.Open(DbPath))
            {
                db.GetCollection<Note>("n").Insert(new Note { Id = 1, Secret = "keep-me" });
            }

            // Turn on passphrase protection over the existing key.
            DatabaseKeyStore store = DatabaseKeyStore.ForDatabase(DbPath);
            store.UnlockWithDpapi();
            store.EnablePassphrase("open sesame please");

            // The plain DPAPI open now refuses.
            Assert.Throws<PassphraseRequiredException>(() => EncryptedDatabase.Open(DbPath));

            // Unlock with the passphrase, hand off the key, and the original data is still readable
            // (the database was never re-encrypted).
            DatabaseKeyStore unlocked = DatabaseKeyStore.ForDatabase(DbPath);
            Assert.True(unlocked.TryUnlockWithPassphrase("open sesame please"));
            EncryptedDatabase.Prepare(unlocked);
            using (LiteDatabase db = EncryptedDatabase.Open(DbPath))
            {
                Assert.Equal("keep-me", db.GetCollection<Note>("n").FindById(1).Secret);
            }
        }

        [Fact]
        public void KeyFile_DoesNotContainRawKey()
        {
            using (EncryptedDatabase.Open(DbPath)) { }

            byte[] keyFile = File.ReadAllBytes(KeyPath);
            // The DPAPI blob is larger than the 32-byte key and shouldn't be a bare key.
            Assert.True(keyFile.Length > 32);
        }

        private static bool ContainsText(byte[] data, string text) =>
            IndexOf(data, Encoding.UTF8.GetBytes(text)) >= 0 ||
            IndexOf(data, Encoding.Unicode.GetBytes(text)) >= 0;

        private static int IndexOf(byte[] haystack, byte[] needle)
        {
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                int j = 0;
                while (j < needle.Length && haystack[i + j] == needle[j])
                {
                    j++;
                }
                if (j == needle.Length)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
