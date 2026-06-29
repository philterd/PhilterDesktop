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
            store.IsPassphraseProtected = store.ReadModel()?.Mode == ModePassphrase;
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
            KeyFileModel? model = ReadModel();

            if (model is null)
            {
                if (File.Exists(_keyPath))
                {
                    // Legacy format: the whole file is a DPAPI blob of the raw key.
                    _key = ProtectedData.Unprotect(File.ReadAllBytes(_keyPath), null, DataProtectionScope.CurrentUser);
                    CreatedNewKey = false;
                }
                else
                {
                    _key = RandomNumberGenerator.GetBytes(KeySize);
                    CreatedNewKey = true;
                    WriteDpapiModel();
                }
                IsPassphraseProtected = false;
                return;
            }

            if (model.Mode == ModePassphrase)
            {
                throw new PassphraseRequiredException();
            }

            _key = ProtectedData.Unprotect(Convert.FromBase64String(model.DpapiKey!), null, DataProtectionScope.CurrentUser);
            CreatedNewKey = false;
            IsPassphraseProtected = false;
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

        private KeyFileModel? ReadModel()
        {
            if (!File.Exists(_keyPath))
            {
                return null;
            }
            try
            {
                byte[] bytes = File.ReadAllBytes(_keyPath);
                int i = 0;
                while (i < bytes.Length && bytes[i] is 0x20 or 0x09 or 0x0A or 0x0D or 0xEF or 0xBB or 0xBF)
                {
                    i++; // skip whitespace / UTF-8 BOM
                }
                if (i >= bytes.Length || bytes[i] != (byte)'{')
                {
                    return null; // legacy raw DPAPI blob, not JSON
                }
                return JsonSerializer.Deserialize<KeyFileModel>(bytes);
            }
            catch
            {
                return null;
            }
        }

        private void WriteModel(KeyFileModel model)
        {
            string json = JsonSerializer.Serialize(model);
            string tmp = _keyPath + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(_keyPath))
            {
                File.Replace(tmp, _keyPath, null);
            }
            else
            {
                File.Move(tmp, _keyPath);
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
