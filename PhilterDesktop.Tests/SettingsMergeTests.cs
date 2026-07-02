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
    /// The Settings dialog reads the whole settings singleton on open and saves it back, so a naive
    /// full Upsert reverts any field a concurrent writer changed while it was open. These pin the
    /// merge that keeps the dialog to the fields it owns.
    /// </summary>
    public sealed class SettingsMergeTests : IDisposable
    {
        private readonly string _path;
        private readonly LiteDatabase _db;

        public SettingsMergeTests()
        {
            _path = Path.Combine(Path.GetTempPath(), "philter-settings-merge-" + Guid.NewGuid().ToString("N") + ".db");
            _db = new LiteDatabase(_path);
        }

        public void Dispose()
        {
            _db.Dispose();
            try { File.Delete(_path); } catch { /* best effort */ }
        }

        [Fact]
        public void CopyExternallyManagedFields_AdoptsSourceExternal_KeepsOwnDialogFields()
        {
            // Dialog snapshot: dialog-owned fields are the user's new choices; external fields are stale.
            var dialog = new SettingsEntity
            {
                LoggingEnabled = true,
                RedactedSuffix = "_dialog",
                MaxInputFileSizeMb = 42,
                VerificationUseBroadPolicy = false,
                OcrMaxPages = 7,
                // stale externals captured when the dialog opened
                GlobalAlwaysRedact = "stale",
                LastPolicy = "stale-policy",
                WindowWidth = 100,
                SortColumn = 0,
                ColumnWidths = "stale",
                TrayHintShown = false
            };

            // The current DB record: external fields were updated by other writers since the dialog opened.
            var fresh = new SettingsEntity
            {
                LoggingEnabled = false,            // a dialog-owned field — must NOT be adopted
                RedactedSuffix = "_fresh",
                GlobalAlwaysRedact = "acme\nwidget",
                GlobalAlwaysIgnore = "sample",
                LastPolicy = "hipaa",
                LastContext = "ctx-9",
                LastSaveFolder = @"C:\out",
                WindowX = 5, WindowY = 6, WindowWidth = 1280, WindowHeight = 720, WindowMaximized = true,
                SortColumn = 3, SortAscending = false, ColumnWidths = "350,180,120",
                TrayHintShown = true
            };

            dialog.CopyExternallyManagedFieldsFrom(fresh);

            // Dialog-owned fields are untouched (the user's edits win).
            Assert.True(dialog.LoggingEnabled);
            Assert.Equal("_dialog", dialog.RedactedSuffix);
            Assert.Equal(42, dialog.MaxInputFileSizeMb);
            Assert.False(dialog.VerificationUseBroadPolicy);
            Assert.Equal(7, dialog.OcrMaxPages);

            // Externally-managed fields are adopted from the fresh record (concurrent changes preserved).
            Assert.Equal("acme\nwidget", dialog.GlobalAlwaysRedact);
            Assert.Equal("sample", dialog.GlobalAlwaysIgnore);
            Assert.Equal("hipaa", dialog.LastPolicy);
            Assert.Equal("ctx-9", dialog.LastContext);
            Assert.Equal(@"C:\out", dialog.LastSaveFolder);
            Assert.Equal(5, dialog.WindowX);
            Assert.Equal(6, dialog.WindowY);
            Assert.Equal(1280, dialog.WindowWidth);
            Assert.Equal(720, dialog.WindowHeight);
            Assert.True(dialog.WindowMaximized);
            Assert.Equal(3, dialog.SortColumn);
            Assert.False(dialog.SortAscending);
            Assert.Equal("350,180,120", dialog.ColumnWidths);
            Assert.True(dialog.TrayHintShown);
        }

        [Fact]
        public void DialogSaveWithMerge_PreservesConcurrentWrite_AndPersistsDialogEdit()
        {
            var repo = new SettingsRepository(_db);

            // Initial state.
            SettingsEntity initial = repo.GetSettings();
            initial.LoggingEnabled = false;
            initial.GlobalAlwaysRedact = "acme";
            initial.LastPolicy = "default";
            repo.SaveSettings(initial);

            // The Settings dialog opens and snapshots the whole record.
            SettingsEntity dialogSnapshot = repo.GetSettings();

            // Meanwhile a concurrent writer (e.g. the Global Lists dialog + a background redaction) updates
            // externally-managed fields via its own read-modify-write.
            SettingsEntity concurrent = repo.GetSettings();
            concurrent.GlobalAlwaysRedact = "acme\nwidget"; // a just-added always-redact term
            concurrent.LastPolicy = "hipaa";
            repo.SaveSettings(concurrent);

            // The user clicks Save in the dialog: a dialog-owned field changed on the stale snapshot, then
            // the externally-managed fields are re-read and carried forward before persisting.
            dialogSnapshot.LoggingEnabled = true;
            dialogSnapshot.CopyExternallyManagedFieldsFrom(repo.GetSettings());
            repo.SaveSettings(dialogSnapshot);

            SettingsEntity final = repo.GetSettings();
            Assert.True(final.LoggingEnabled);                       // the dialog's edit persisted
            Assert.Equal("acme\nwidget", final.GlobalAlwaysRedact);  // the concurrent term was NOT wiped
            Assert.Equal("hipaa", final.LastPolicy);                 // the concurrent last-used was NOT reverted
        }

        [Fact]
        public void WithoutMerge_ConcurrentWriteIsLost_DemonstratesTheBug()
        {
            // Control: the old full-Upsert-of-stale-snapshot behavior reverts the concurrent change.
            var repo = new SettingsRepository(_db);
            SettingsEntity initial = repo.GetSettings();
            initial.GlobalAlwaysRedact = "acme";
            repo.SaveSettings(initial);

            SettingsEntity dialogSnapshot = repo.GetSettings();      // opens with "acme"

            SettingsEntity concurrent = repo.GetSettings();
            concurrent.GlobalAlwaysRedact = "acme\nwidget";          // term added while dialog open
            repo.SaveSettings(concurrent);

            repo.SaveSettings(dialogSnapshot);                       // no merge -> reverts to "acme"

            Assert.Equal("acme", repo.GetSettings().GlobalAlwaysRedact); // the term is lost (the defect)
        }
    }
}
