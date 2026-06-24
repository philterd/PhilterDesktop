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

using System.Security.Cryptography;
using LiteDB;
using LiteDB.Engine;

namespace PhilterDesktop
{
    /// <summary>
    /// Opens the application's LiteDB database AES-encrypted at rest. Because the database now stores
    /// detected PII (the redaction spans), the file is encrypted with a random key that is itself
    /// protected by Windows DPAPI scoped to the current user — so the key file is only usable by that
    /// Windows user on that machine, with no password prompt. An existing unencrypted database (from
    /// before encryption was added) is migrated in place on first open.
    ///
    /// The connection uses LiteDB <b>Shared</b> mode (a cross-process mutex) so multiple processes can
    /// use the database at once — e.g. the command-line redactor invoked from the Explorer context menu
    /// while the main app (system tray) is already running.
    /// </summary>
    internal static class EncryptedDatabase
    {
        private const string KeyFileName = "data.key";

        /// <summary>Opens (creating/migrating as needed) the encrypted database at <paramref name="dbPath"/>.</summary>
        public static LiteDatabase Open(string dbPath)
        {
            string directory = Path.GetDirectoryName(dbPath) ?? ".";
            Directory.CreateDirectory(directory);

            string keyPath = Path.Combine(directory, KeyFileName);
            bool keyExisted = File.Exists(keyPath);
            string password = GetOrCreatePassword(keyPath);

            // No key yet but a database already exists => it predates encryption; encrypt it in place.
            if (!keyExisted && File.Exists(dbPath))
            {
                try
                {
                    using var plain = new LiteDatabase(new ConnectionString { Filename = dbPath });
                    plain.Rebuild(new RebuildOptions { Password = password });
                }
                catch
                {
                    // Already encrypted (e.g. key file was lost): let the open below surface the error.
                }
            }

            return new LiteDatabase(new ConnectionString
            {
                Filename = dbPath,
                Password = password,
                Connection = ConnectionType.Shared // allow concurrent access from the GUI + CLI
            });
        }

        /// <summary>
        /// Returns the base64 database key, generating and DPAPI-protecting a new random 256-bit key on
        /// first use. The on-disk key file holds only the DPAPI-protected blob, never the raw key.
        /// </summary>
        private static string GetOrCreatePassword(string keyPath)
        {
            if (File.Exists(keyPath))
            {
                byte[] prot = File.ReadAllBytes(keyPath);
                byte[] key = ProtectedData.Unprotect(prot, optionalEntropy: null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(key);
            }

            byte[] newKey = RandomNumberGenerator.GetBytes(32);
            byte[] newProt = ProtectedData.Protect(newKey, optionalEntropy: null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(keyPath, newProt);
            return Convert.ToBase64String(newKey);
        }
    }
}
