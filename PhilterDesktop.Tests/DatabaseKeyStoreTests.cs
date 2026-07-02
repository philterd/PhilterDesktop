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

using System.Security.AccessControl;
using System.Security.Principal;
using Xunit;

namespace PhilterDesktop.Tests
{
    public sealed class DatabaseKeyStoreTests : IDisposable
    {
        private readonly string _dir;
        private readonly string _dbPath;
        private const string Pass = "correct horse battery staple";

        public DatabaseKeyStoreTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-keystore-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            _dbPath = Path.Combine(_dir, "data.db");
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        [Fact]
        public void DefaultMode_IsDpapi_AndKeyPersists()
        {
            var s1 = DatabaseKeyStore.ForDatabase(_dbPath);
            Assert.False(s1.IsPassphraseProtected);
            s1.UnlockWithDpapi();
            string key = s1.DatabasePassword;

            var s2 = DatabaseKeyStore.ForDatabase(_dbPath);
            s2.UnlockWithDpapi();
            Assert.Equal(key, s2.DatabasePassword); // same key reused
        }

        [Fact]
        public void UnlockWithDpapi_ConcurrentFirstRun_AllAgreeOnOneKey()
        {
            // Simulate several processes doing their very first unlock at the same time (no key file yet).
            // They must all end up with the SAME key — exactly one generates it, the rest load it —
            // instead of each generating a different key and clobbering data.key.
            List<DatabaseKeyStore> stores = Enumerable.Range(0, 8)
                .Select(_ => DatabaseKeyStore.ForDatabase(_dbPath))
                .ToList();

            Parallel.ForEach(stores, s => s.UnlockWithDpapi());

            List<string> distinctKeys = stores.Select(s => s.DatabasePassword).Distinct().ToList();
            Assert.Single(distinctKeys);                        // one agreed key across all of them
            Assert.Single(stores.Where(s => s.CreatedNewKey));  // exactly one created it; the rest reused it
        }

        [Fact]
        public void KeyFile_IsRestrictedToCurrentUserOnly()
        {
            var store = DatabaseKeyStore.ForDatabase(_dbPath);
            store.UnlockWithDpapi(); // creates and writes data.key

            string keyPath = Path.Combine(_dir, "data.key");
            Assert.True(File.Exists(keyPath));

            FileSecurity security = new FileInfo(keyPath).GetAccessControl();
            Assert.True(security.AreAccessRulesProtected); // inherited permissions removed

            SecurityIdentifier me = WindowsIdentity.GetCurrent().User!;
            List<SecurityIdentifier> allowed = security
                .GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier))
                .Cast<FileSystemAccessRule>()
                .Where(r => r.AccessControlType == AccessControlType.Allow)
                .Select(r => (SecurityIdentifier)r.IdentityReference)
                .Distinct()
                .ToList();

            Assert.Contains(me, allowed);                       // the current user has access
            Assert.All(allowed, sid => Assert.Equal(me, sid));  // and no one else does
        }

        [Fact]
        public void EnablePassphrase_KeepsSameKey_AndRequiresPassphrase()
        {
            var s1 = DatabaseKeyStore.ForDatabase(_dbPath);
            s1.UnlockWithDpapi();
            string key = s1.DatabasePassword;
            s1.EnablePassphrase(Pass);

            // A fresh store sees passphrase mode and can't unlock via DPAPI.
            var s2 = DatabaseKeyStore.ForDatabase(_dbPath);
            Assert.True(s2.IsPassphraseProtected);
            Assert.Throws<PassphraseRequiredException>(() => s2.UnlockWithDpapi());

            // Wrong passphrase fails; correct one yields the SAME key (so no DB rebuild is needed).
            Assert.False(s2.TryUnlockWithPassphrase("wrong"));
            Assert.True(s2.TryUnlockWithPassphrase(Pass));
            Assert.Equal(key, s2.DatabasePassword);
        }

        [Fact]
        public void DisablePassphrase_RevertsToDpapi_WithSameKey()
        {
            var s1 = DatabaseKeyStore.ForDatabase(_dbPath);
            s1.UnlockWithDpapi();
            string key = s1.DatabasePassword;
            s1.EnablePassphrase(Pass);

            var s2 = DatabaseKeyStore.ForDatabase(_dbPath);
            Assert.True(s2.TryUnlockWithPassphrase(Pass));
            s2.DisablePassphrase();

            var s3 = DatabaseKeyStore.ForDatabase(_dbPath);
            Assert.False(s3.IsPassphraseProtected);
            s3.UnlockWithDpapi();
            Assert.Equal(key, s3.DatabasePassword);
        }

        [Fact]
        public void ChangePassphrase_OldFails_NewWorks_SameKey()
        {
            var s1 = DatabaseKeyStore.ForDatabase(_dbPath);
            s1.UnlockWithDpapi();
            string key = s1.DatabasePassword;
            s1.EnablePassphrase("first-pass-123");

            var s2 = DatabaseKeyStore.ForDatabase(_dbPath);
            Assert.True(s2.TryUnlockWithPassphrase("first-pass-123"));
            Assert.True(s2.VerifyPassphrase("first-pass-123"));
            s2.ChangePassphrase("second-pass-456");

            var s3 = DatabaseKeyStore.ForDatabase(_dbPath);
            Assert.False(s3.TryUnlockWithPassphrase("first-pass-123"));
            Assert.True(s3.TryUnlockWithPassphrase("second-pass-456"));
            Assert.Equal(key, s3.DatabasePassword);
        }

        [Fact]
        public void VerifyPassphrase_DoesNotUnlockState()
        {
            var s1 = DatabaseKeyStore.ForDatabase(_dbPath);
            s1.UnlockWithDpapi();
            s1.EnablePassphrase(Pass);

            var s2 = DatabaseKeyStore.ForDatabase(_dbPath);
            Assert.True(s2.VerifyPassphrase(Pass));
            Assert.False(s2.VerifyPassphrase("nope"));
            // Verify didn't unlock, so DatabasePassword is unavailable.
            Assert.Throws<InvalidOperationException>(() => s2.DatabasePassword);
        }
    }
}
