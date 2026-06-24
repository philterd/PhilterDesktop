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

namespace PhilterDesktop
{
    /// <summary>
    /// Opens the application's LiteDB database AES-encrypted at rest. The encryption key is managed by
    /// <see cref="DatabaseKeyStore"/> (wrapped by Windows DPAPI by default, or by a user passphrase if
    /// enabled). Uses LiteDB <b>Shared</b> mode so multiple processes (GUI + CLI/context menu) can use
    /// the database at once.
    /// </summary>
    internal static class EncryptedDatabase
    {
        /// <summary>
        /// A key store that was unlocked interactively (e.g. after a passphrase prompt). The next
        /// <see cref="Open(string)"/> consumes it instead of unlocking via DPAPI. Lets the GUI prompt
        /// once at startup while <c>MainForm</c> still opens the database itself.
        /// </summary>
        public static DatabaseKeyStore? PreparedKeyStore { get; private set; }

        /// <summary>The key store backing the currently open database (for managing the passphrase).</summary>
        public static DatabaseKeyStore? CurrentKeyStore { get; private set; }

        /// <summary>The default database path under the user's local app data.</summary>
        public static string DefaultPath()
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(root, "PhilterDesktop", "data.db");
        }

        /// <summary>True if the database's key file is protected by a passphrase.</summary>
        public static bool IsPassphraseProtected(string dbPath) =>
            DatabaseKeyStore.ForDatabase(dbPath).IsPassphraseProtected;

        /// <summary>Hands an already-unlocked key store to the next <see cref="Open(string)"/> call.</summary>
        public static void Prepare(DatabaseKeyStore unlockedStore) => PreparedKeyStore = unlockedStore;

        /// <summary>
        /// Opens the database. If a key store was <see cref="Prepare"/>d it is used; otherwise the key
        /// is unlocked via DPAPI (which throws <see cref="PassphraseRequiredException"/> if the database
        /// is passphrase-protected).
        /// </summary>
        public static LiteDatabase Open(string dbPath)
        {
            DatabaseKeyStore store;
            if (PreparedKeyStore is { } prepared)
            {
                PreparedKeyStore = null;
                store = prepared;
            }
            else
            {
                store = DatabaseKeyStore.ForDatabase(dbPath);
                store.UnlockWithDpapi();
            }

            CurrentKeyStore = store;
            return OpenWithKey(dbPath, store.DatabasePassword, migrateLegacyPlaintext: store.CreatedNewKey);
        }

        private static LiteDatabase OpenWithKey(string dbPath, string password, bool migrateLegacyPlaintext)
        {
            // A pre-encryption (plaintext) database exists but we just generated a key => encrypt it in place.
            if (migrateLegacyPlaintext && File.Exists(dbPath))
            {
                try
                {
                    using var plain = new LiteDatabase(new ConnectionString { Filename = dbPath });
                    plain.Rebuild(new RebuildOptions { Password = password });
                }
                catch
                {
                    // Already encrypted (e.g. key file was lost): let the open below surface any error.
                }
            }

            return new LiteDatabase(new ConnectionString
            {
                Filename = dbPath,
                Password = password,
                Connection = ConnectionType.Shared // allow concurrent access from the GUI + CLI
            });
        }
    }
}
