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
        public void UnlockWithDpapi_ConcurrentFirstRun_IsStableAcrossManyRuns()
        {
            // The race is timing-dependent (it only tripped on a slow CI runner where a barging creator
            // published a torn key). Repeat it over many fresh key files to guard against regressions:
            // every run must still agree on exactly one key with exactly one creator, and none may throw.
            for (int run = 0; run < 40; run++)
            {
                string dir = Path.Combine(_dir, "run-" + run);
                Directory.CreateDirectory(dir);
                string dbPath = Path.Combine(dir, "data.db");

                List<DatabaseKeyStore> stores = Enumerable.Range(0, 8)
                    .Select(_ => DatabaseKeyStore.ForDatabase(dbPath))
                    .ToList();

                Parallel.ForEach(stores, s => s.UnlockWithDpapi());

                Assert.Single(stores.Select(s => s.DatabasePassword).Distinct());
                Assert.Single(stores.Where(s => s.CreatedNewKey));
            }
        }

        [Fact]
        public void UnlockWithDpapi_WhileTheKeyFileIsBeingRewritten_NeverMisreadsItAsLegacy()
        {
            // The concurrency bug behind the flaky first-run test: loading read the file TWICE, once
            // for the JSON model and once for a legacy raw blob. A writer's atomic Move/Replace makes
            // an open fail for an instant, so the model read returned "not JSON", the file still
            // existed, and the JSON was DPAPI-decrypted as a legacy blob: "The data is invalid".
            // Rewriting the key file under concurrent readers reproduces that window directly.
            var owner = DatabaseKeyStore.ForDatabase(_dbPath);
            owner.UnlockWithDpapi();
            string expected = owner.DatabasePassword;

            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            Exception? failure = null;

            // Republishes data.key over and over (temp file, then Replace) for readers to race.
            Task writer = Task.Run(() =>
            {
                while (!stop.IsCancellationRequested)
                {
                    owner.DisablePassphrase(); // re-wraps the same key and rewrites the file
                }
            });

            try
            {
                for (int i = 0; i < 300 && !stop.IsCancellationRequested; i++)
                {
                    Parallel.For(0, 4, _ =>
                    {
                        try
                        {
                            var reader = DatabaseKeyStore.ForDatabase(_dbPath);
                            reader.UnlockWithDpapi();
                            Assert.Equal(expected, reader.DatabasePassword);
                            Assert.False(reader.CreatedNewKey);
                        }
                        catch (Exception e)
                        {
                            Interlocked.CompareExchange(ref failure, e, null);
                            stop.Cancel();
                        }
                    });
                }
            }
            finally
            {
                stop.Cancel();
                writer.Wait(TimeSpan.FromSeconds(5));
            }

            Assert.Null(failure);
        }

        [Fact]
        public void UnlockWithDpapi_CorruptJsonKeyFile_SaysItIsCorrupt_NotThatTheDataIsInvalid()
        {
            // A file that starts with '{' but will not parse is corrupt, not legacy. Falling through to
            // the legacy path would DPAPI-decrypt JSON and report the misleading "The data is invalid".
            File.WriteAllText(Path.Combine(_dir, "data.key"), "{ \"version\": 1, \"mode\": ");

            var store = DatabaseKeyStore.ForDatabase(_dbPath);

            InvalidDataException e = Assert.Throws<InvalidDataException>(() => store.UnlockWithDpapi());
            Assert.Contains("corrupt", e.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void UnlockWithDpapi_LegacyRawBlobKeyFile_StillLoads()
        {
            // The legacy path must keep working: the fix narrows when it is taken, not whether it exists.
            var original = DatabaseKeyStore.ForDatabase(_dbPath);
            original.UnlockWithDpapi();
            string expected = original.DatabasePassword;

            // Rewrite data.key in the pre-JSON format: the whole file is a DPAPI blob of the raw key.
            byte[] raw = Convert.FromBase64String(expected);
            File.WriteAllBytes(
                Path.Combine(_dir, "data.key"),
                System.Security.Cryptography.ProtectedData.Protect(
                    raw, null, System.Security.Cryptography.DataProtectionScope.CurrentUser));

            var reloaded = DatabaseKeyStore.ForDatabase(_dbPath);
            reloaded.UnlockWithDpapi();

            Assert.Equal(expected, reloaded.DatabasePassword);
            Assert.False(reloaded.CreatedNewKey);
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
