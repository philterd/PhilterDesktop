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
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

namespace PhilterDesktop
{
    /// <summary>Thrown by the DPAPI open path when the database is passphrase-protected.</summary>
    internal sealed class PassphraseRequiredException : Exception
    {
        public PassphraseRequiredException() : base("The database is protected with a passphrase.") { }
    }

    /// <summary>
    /// Manages the database encryption key in <c>data.key</c> using <b>envelope encryption</b>: a
    /// random 32-byte key actually encrypts the LiteDB file, and that key is itself wrapped either by
    /// Windows DPAPI (default) or by a key derived from a user passphrase (PBKDF2 + AES-GCM).
    ///
    /// Because only the <i>wrapping</i> changes, switching passphrase protection on or off just
    /// rewrites <c>data.key</c> — the database is never re-encrypted (no migration/rebuild). The
    /// passphrase itself is never stored; only the salt, KDF parameters, and the wrapped key are.
    /// </summary>
    public sealed class DatabaseKeyStore
    {
        private const string KeyFileName = "data.key";
        private const int KeySize = 32;   // AES-256 database key
        private const int SaltSize = 16;
        private const int NonceSize = 12; // AES-GCM standard nonce
        private const int TagSize = 16;   // AES-GCM tag
        private const int Pbkdf2Iterations = 600_000;
        private const int FormatVersion = 1;
        private const string ModeDpapi = "dpapi";
        private const string ModePassphrase = "passphrase";
        // Retry budget for contended access to data.key: ~1s, far longer than the moment a concurrent
        // reader or a publishing rename holds the file.
        private const int RetryAttempts = 50;
        private const int RetryDelayMs = 20;

        private readonly string _keyPath;
        private byte[]? _key;

        /// <summary>True when the key file is wrapped with a passphrase (so unlocking needs it).</summary>
        public bool IsPassphraseProtected { get; private set; }

        /// <summary>True when <see cref="UnlockWithDpapi"/> generated a brand-new key (first run).</summary>
        public bool CreatedNewKey { get; private set; }

        private DatabaseKeyStore(string keyPath) => _keyPath = keyPath;

        /// <summary>Creates a store for the key file beside <paramref name="dbPath"/> and reads its mode.</summary>
        public static DatabaseKeyStore ForDatabase(string dbPath)
        {
            string dir = Path.GetDirectoryName(dbPath) ?? ".";
            Directory.CreateDirectory(dir);
            var store = new DatabaseKeyStore(Path.Combine(dir, KeyFileName));
            // This only probes the mode, so a corrupt key file must not stop construction: unlocking
            // is what reports it, with a message that says the file is corrupt.
            try
            {
                store.IsPassphraseProtected = store.ReadModel()?.Mode == ModePassphrase;
            }
            catch (InvalidDataException)
            {
                store.IsPassphraseProtected = false;
            }
            return store;
        }

        /// <summary>The LiteDB password (base64 of the raw key). Throws if the store isn't unlocked.</summary>
        public string DatabasePassword =>
            _key is not null ? Convert.ToBase64String(_key) : throw new InvalidOperationException("Key store is locked.");

        /// <summary>
        /// Unlocks via DPAPI (or creates a new key on first run / migrates a legacy raw key file).
        /// Throws <see cref="PassphraseRequiredException"/> if the key file is passphrase-protected.
        /// </summary>
        public void UnlockWithDpapi()
        {
            if (TryLoadDpapiOrThrow())
            {
                return;
            }

            // First run: no key file yet. Serialize creation across processes so two simultaneous
            // first-run launches (e.g. the GUI, a CLI redaction, and the Explorer right-click flow all
            // starting before data.key exists) can't each generate a different key and clobber the file,
            // which would leave data.db encrypted with one key while data.key holds another (unreadable DB).
            using KeyInitLock initLock = KeyInitLock.Acquire(_keyPath);

            // Another process/thread may have created the key while we waited for the lock — re-check.
            if (TryLoadDpapiOrThrow())
            {
                return;
            }

            // Only the lock holder may create the key. If we could not acquire it (the mutex was
            // unavailable, or the wait timed out because a slow holder was still creating the key),
            // barging in would write a second key through the shared temp file and race the holder's
            // write — the source of torn reads. Instead, wait for the holder's key to appear.
            if (!initLock.Held)
            {
                for (int attempt = 0; attempt < 100; attempt++)
                {
                    Thread.Sleep(100);
                    if (TryLoadDpapiOrThrow())
                    {
                        return;
                    }
                }
                // ~10s later and still nothing: the presumed holder never produced a key (e.g. it
                // crashed before writing without leaving an abandoned mutex). Create one as a last resort.
            }

            _key = RandomNumberGenerator.GetBytes(KeySize);
            CreatedNewKey = true;
            WriteDpapiModel();
            IsPassphraseProtected = false;
        }

        // Loads the existing key via DPAPI — the JSON model, or a legacy raw DPAPI blob. Returns false
        // when no key file exists yet (first run). Throws when the file is passphrase-protected.
        private bool TryLoadDpapiOrThrow()
        {
            // ONE read decides everything. Reading twice (once for the model, once for the legacy
            // blob) let a concurrent first-run writer slip between them: the model read failed
            // transiently, the file then existed, and the JSON got DPAPI-decrypted as a legacy blob,
            // failing with "The data is invalid".
            if (!TryReadKeyFile(out byte[] bytes))
            {
                return false; // no key file yet: first run
            }

            KeyFileModel? model = ParseModel(bytes);
            if (model is not null)
            {
                if (model.Mode == ModePassphrase)
                {
                    throw new PassphraseRequiredException();
                }
                _key = ProtectedData.Unprotect(Convert.FromBase64String(model.DpapiKey!), null, DataProtectionScope.CurrentUser);
            }
            else
            {
                // Legacy format: the whole file is a DPAPI blob of the raw key.
                _key = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            }

            CreatedNewKey = false;
            IsPassphraseProtected = false;
            return true;
        }

        /// <summary>Unlocks with a passphrase. Returns false if it's wrong (or not passphrase-protected).</summary>
        public bool TryUnlockWithPassphrase(string passphrase)
        {
            byte[]? key = DecryptWithPassphrase(passphrase);
            if (key is null)
            {
                return false;
            }
            _key = key;
            CreatedNewKey = false;
            IsPassphraseProtected = true;
            return true;
        }

        /// <summary>Checks a passphrase without changing the unlocked state.</summary>
        public bool VerifyPassphrase(string passphrase) => DecryptWithPassphrase(passphrase) is not null;

        /// <summary>Switches to passphrase protection (re-wraps the existing key; no DB rebuild).</summary>
        public void EnablePassphrase(string passphrase)
        {
            RequireUnlocked();
            WritePassphraseModel(passphrase);
            IsPassphraseProtected = true;
        }

        /// <summary>Switches back to DPAPI protection (re-wraps the existing key; no DB rebuild).</summary>
        public void DisablePassphrase()
        {
            RequireUnlocked();
            WriteDpapiModel();
            IsPassphraseProtected = false;
        }

        /// <summary>Re-wraps the key with a new passphrase (caller should verify the old one first).</summary>
        public void ChangePassphrase(string newPassphrase)
        {
            RequireUnlocked();
            WritePassphraseModel(newPassphrase);
            IsPassphraseProtected = true;
        }

        private byte[]? DecryptWithPassphrase(string passphrase)
        {
            KeyFileModel? model = ReadModel();
            if (model is null || model.Mode != ModePassphrase)
            {
                return null;
            }
            byte[]? passwordBytes = null;
            byte[]? wrappingKey = null;
            try
            {
                byte[] salt = Convert.FromBase64String(model.Salt!);
                byte[] wrapped = Convert.FromBase64String(model.WrappedKey!);
                passwordBytes = Encoding.UTF8.GetBytes(passphrase);
                wrappingKey = Rfc2898DeriveBytes.Pbkdf2(
                    passwordBytes, salt, model.Iterations, HashAlgorithmName.SHA256, KeySize);

                byte[] nonce = wrapped[..NonceSize];
                byte[] tag = wrapped[NonceSize..(NonceSize + TagSize)];
                byte[] cipher = wrapped[(NonceSize + TagSize)..];
                byte[] key = new byte[cipher.Length];
                using var gcm = new AesGcm(wrappingKey, TagSize);
                gcm.Decrypt(nonce, cipher, tag, key); // throws CryptographicException on wrong passphrase
                return key;
            }
            catch (Exception ex) when (ex is CryptographicException or FormatException or ArgumentException)
            {
                return null;
            }
            finally
            {
                // Wipe the passphrase-derived key material (and the passphrase bytes) from the heap; the
                // returned database key is kept, but these intermediates must not linger.
                if (wrappingKey is not null) CryptographicOperations.ZeroMemory(wrappingKey);
                if (passwordBytes is not null) CryptographicOperations.ZeroMemory(passwordBytes);
            }
        }

        private void RequireUnlocked()
        {
            if (_key is null)
            {
                throw new InvalidOperationException("Key store is locked.");
            }
        }

        private void WriteDpapiModel()
        {
            byte[] prot = ProtectedData.Protect(_key!, null, DataProtectionScope.CurrentUser);
            WriteModel(new KeyFileModel { Version = FormatVersion, Mode = ModeDpapi, DpapiKey = Convert.ToBase64String(prot) });
        }

        private void WritePassphraseModel(string passphrase)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] passwordBytes = Encoding.UTF8.GetBytes(passphrase);
            byte[] wrappingKey = Rfc2898DeriveBytes.Pbkdf2(
                passwordBytes, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256, KeySize);
            try
            {
                byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
                byte[] cipher = new byte[_key!.Length];
                byte[] tag = new byte[TagSize];
                using (var gcm = new AesGcm(wrappingKey, TagSize))
                {
                    gcm.Encrypt(nonce, _key, cipher, tag);
                }

                byte[] wrapped = new byte[NonceSize + TagSize + cipher.Length];
                Buffer.BlockCopy(nonce, 0, wrapped, 0, NonceSize);
                Buffer.BlockCopy(tag, 0, wrapped, NonceSize, TagSize);
                Buffer.BlockCopy(cipher, 0, wrapped, NonceSize + TagSize, cipher.Length);

                WriteModel(new KeyFileModel
                {
                    Version = FormatVersion,
                    Mode = ModePassphrase,
                    Salt = Convert.ToBase64String(salt),
                    Iterations = Pbkdf2Iterations,
                    WrappedKey = Convert.ToBase64String(wrapped)
                });
            }
            finally
            {
                // Wipe the passphrase-derived key material (and the passphrase bytes) from the heap.
                CryptographicOperations.ZeroMemory(wrappingKey);
                CryptographicOperations.ZeroMemory(passwordBytes);
            }
        }

        private KeyFileModel? ReadModel() =>
            TryReadKeyFile(out byte[] bytes) ? ParseModel(bytes) : null;

        // Reads the key file, retrying briefly on transient I/O. A concurrent writer's Move/Replace
        // makes an open fail for a moment, and callers distinguish "no key file" (first run) from
        // "this file is a legacy raw blob" by whether this succeeds - so returning false for a
        // momentary sharing violation would misclassify a perfectly good JSON key file.
        // Returns false only when the file genuinely is not there.
        private bool TryReadKeyFile(out byte[] bytes)
        {
            for (int attempt = 0; ; attempt++)
            {
                try
                {
                    bytes = File.ReadAllBytes(_keyPath);
                    return true;
                }
                catch (FileNotFoundException)
                {
                    bytes = [];
                    return false;
                }
                catch (DirectoryNotFoundException)
                {
                    bytes = [];
                    return false;
                }
                catch (Exception e) when ((e is IOException or UnauthorizedAccessException) && attempt < RetryAttempts)
                {
                    Thread.Sleep(RetryDelayMs);
                }
            }
        }

        // Parses the key file's bytes. Null means the content is a legacy raw DPAPI blob rather than
        // JSON. Pure: it never conflates "could not read" with "not JSON".
        private static KeyFileModel? ParseModel(byte[] bytes)
        {
            int i = 0;
            while (i < bytes.Length && bytes[i] is 0x20 or 0x09 or 0x0A or 0x0D or 0xEF or 0xBB or 0xBF)
            {
                i++; // skip whitespace / UTF-8 BOM
            }
            if (i >= bytes.Length || bytes[i] != (byte)'{')
            {
                return null; // legacy raw DPAPI blob, not JSON
            }
            try
            {
                return JsonSerializer.Deserialize<KeyFileModel>(bytes);
            }
            catch (JsonException)
            {
                // Starts with '{' but will not parse: the file is corrupt, not legacy. Say so rather
                // than letting the caller try to DPAPI-decrypt JSON and report "The data is invalid".
                throw new InvalidDataException(
                    "The database key file is corrupt: it looks like JSON but could not be parsed.");
            }
        }

        private void WriteModel(KeyFileModel model)
        {
            string json = JsonSerializer.Serialize(model);
            // A per-write temp name (not a shared "data.key.tmp") so two writers can never scribble over
            // one temp file and publish a torn key. The move/replace onto data.key is atomic, so a
            // concurrent reader only ever sees the old file or a complete new one.
            string tmp = _keyPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(tmp, json);
                // Publish with an atomic replacing rename. NOT File.Replace: that deletes the target
                // before renaming, so data.key briefly does not exist, and a reader catching that
                // window decides it is a first run and generates a SECOND key - leaving the database
                // encrypted with one key and data.key holding another. File.Move(overwrite: true) is
                // MoveFileEx(MOVEFILE_REPLACE_EXISTING): readers only ever see the old file or the new
                // one. It still needs delete access, which a concurrent reader denies for an instant,
                // so retry rather than failing the caller's passphrase change.
                for (int attempt = 0; ; attempt++)
                {
                    try
                    {
                        File.Move(tmp, _keyPath, overwrite: true);
                        break;
                    }
                    catch (Exception e) when ((e is IOException or UnauthorizedAccessException) && attempt < RetryAttempts)
                    {
                        Thread.Sleep(RetryDelayMs);
                    }
                }
            }
            finally
            {
                // File.Move/Replace consumes tmp on success; clean it up if we threw before that.
                if (File.Exists(tmp))
                {
                    try { File.Delete(tmp); } catch { /* best effort */ }
                }
            }
            RestrictToCurrentUser(_keyPath);
        }

        // Locks the key file down to the current user only: removes inherited permissions and grants
        // full control to just this account. Defense-in-depth on top of the DPAPI/passphrase wrapping —
        // even the wrapped key, salt, and KDF parameters shouldn't be readable or tamperable by other
        // accounts on a shared machine. Best-effort: the wrapping is the real protection, so an ACL
        // failure must never block writing the key.
        private static void RestrictToCurrentUser(string path)
        {
            try
            {
                SecurityIdentifier? user = WindowsIdentity.GetCurrent().User;
                if (user is null)
                {
                    return;
                }
                var security = new FileSecurity();
                security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
                security.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl, AccessControlType.Allow));
                new FileInfo(path).SetAccessControl(security);
            }
            catch
            {
                // best effort — never let an ACL change prevent the key from being written
            }
        }

        // A cross-process lock that serializes first-run key creation for one key file. Named from the
        // key path so only processes using the same file (same user) contend; Global-scoped so it also
        // serializes across that user's sessions. Entirely best-effort — if the OS won't give us the
        // mutex, we proceed unlocked and rely on the double-checked read in UnlockWithDpapi.
        private sealed class KeyInitLock : IDisposable
        {
            private readonly Mutex? _mutex;
            private readonly bool _held;

            private KeyInitLock(Mutex? mutex, bool held)
            {
                _mutex = mutex;
                _held = held;
            }

            /// <summary>True when this instance actually owns the mutex (so it may create the key).</summary>
            public bool Held => _held;

            public static KeyInitLock Acquire(string keyPath)
            {
                string name = NameFor(keyPath);
                Mutex? mutex = TryCreate(@"Global\" + name) ?? TryCreate(@"Local\" + name);
                if (mutex is null)
                {
                    return new KeyInitLock(null, false);
                }

                bool held;
                try
                {
                    // Key creation itself is fast, but it can queue behind several other first-run
                    // processes each doing a DPAPI-protect + file write + ACL tighten. Wait generously so
                    // a slow, contended runner doesn't give up and let a second creator barge in.
                    held = mutex.WaitOne(TimeSpan.FromSeconds(60));
                }
                catch (AbandonedMutexException)
                {
                    held = true; // a previous owner crashed mid-creation; we now hold it
                }
                return new KeyInitLock(mutex, held);
            }

            public void Dispose()
            {
                try
                {
                    if (_held)
                    {
                        _mutex?.ReleaseMutex();
                    }
                }
                catch
                {
                    // never let lock release surface as an error
                }
                _mutex?.Dispose();
            }

            private static Mutex? TryCreate(string name)
            {
                try
                {
                    return new Mutex(initiallyOwned: false, name);
                }
                catch
                {
                    return null;
                }
            }

            private static string NameFor(string keyPath)
            {
                byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(keyPath.ToLowerInvariant()));
                return "PhilterDesktop.KeyInit." + Convert.ToHexString(hash, 0, 8);
            }
        }

        private sealed class KeyFileModel
        {
            public int Version { get; set; }
            public string Mode { get; set; } = ModeDpapi;
            public string? DpapiKey { get; set; }
            public string? Salt { get; set; }
            public int Iterations { get; set; }
            public string? WrappedKey { get; set; }
        }
    }
}
