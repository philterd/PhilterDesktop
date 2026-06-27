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

namespace PhilterData
{
    /// <summary>
    /// Entity representing application settings stored in LiteDB.
    /// </summary>
    public class SettingsEntity
    {
        /// <summary>
        /// Gets or sets the unique identifier for the settings record.
        /// </summary>
        [BsonId]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether redacted files should be output to their original location.
        /// If false, files will be output to a custom folder specified by <see cref="CustomOutputFolder"/>.
        /// </summary>
        public bool OutputToOriginalLocation { get; set; } = true;

        /// <summary>
        /// Gets or sets the custom output folder path for redacted files.
        /// Only used when <see cref="OutputToOriginalLocation"/> is false.
        /// </summary>
        public string CustomOutputFolder { get; set; } = string.Empty;

        /// <summary>
        /// Suffix appended to the redacted output file name (before the extension). Defaults to
        /// <c>_redacted-draft</c> — deliberately not just "_redacted", so the name doesn't imply the
        /// output is verified safe (redaction is statistical and must always be reviewed by a person).
        /// </summary>
        public string RedactedSuffix { get; set; } = "_redacted-draft";

        /// <summary>
        /// Gets or sets a value indicating whether logging is enabled.
        /// </summary>
        public bool LoggingEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the one-time "still running in the tray" hint
        /// has been shown (so closing the window to the tray is only explained once).
        /// </summary>
        public bool TrayHintShown { get; set; } = false;

        /// <summary>Whether a tray balloon is shown when a document finishes redacting (default on).</summary>
        public bool NotificationsEnabled { get; set; } = true;

        /// <summary>
        /// How many watched-folder files may be redacted at once (default 1). Higher values speed up
        /// bursts of small files; large files always run alone regardless. Clamped to 1–4 in the UI.
        /// </summary>
        public int WatchedFolderMaxConcurrency { get; set; } = 1;

        /// <summary>Global "always redact" terms (one per line), applied on top of every policy.</summary>
        public string GlobalAlwaysRedact { get; set; } = string.Empty;

        /// <summary>Global "always ignore" terms (one per line), applied on top of every policy.</summary>
        public string GlobalAlwaysIgnore { get; set; } = string.Empty;

        /// <summary>The policy chosen most recently, pre-selected next time a redaction is started.</summary>
        public string LastPolicy { get; set; } = string.Empty;

        /// <summary>The context chosen most recently, pre-selected next time a redaction is started.</summary>
        public string LastContext { get; set; } = string.Empty;

        /// <summary>The folder a preview redaction was last saved to (used to seed the Save dialog).</summary>
        public string LastSaveFolder { get; set; } = string.Empty;

        // --- Remembered main-window layout (restored on next launch) -------------------------------

        /// <summary>Last normal (non-maximized) window position/size. Width 0 means "never saved".</summary>
        public int WindowX { get; set; }
        public int WindowY { get; set; }
        public int WindowWidth { get; set; }
        public int WindowHeight { get; set; }

        /// <summary>Whether the window was maximized when last closed.</summary>
        public bool WindowMaximized { get; set; }

        /// <summary>Queue sort column index and direction.</summary>
        public int SortColumn { get; set; }
        public bool SortAscending { get; set; } = true;

        /// <summary>Comma-separated queue column widths, e.g. "350,180,120,120". Empty means default.</summary>
        public string ColumnWidths { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time when the settings were last modified.
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }
}