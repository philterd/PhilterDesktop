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

namespace PhilterDesktop
{
    /// <summary>
    /// Shown when a newer version is published. Displays the installed/latest versions and links to the
    /// download page; it downloads nothing itself.
    /// </summary>
    public partial class UpdateAvailableForm : Form
    {
        // The public download page serves everyone, free and licensed users alike. The manifest's
        // downloadUrl is intentionally not used, so a tampered manifest can't redirect the download.
        internal const string DownloadUrl = "https://philterd.ai/philter-desktop/download/";

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public UpdateAvailableForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            _link.Text = DownloadUrl;
        }

        public UpdateAvailableForm(string currentVersion, string latestVersion, string? releaseDate)
            : this()
        {
            _message.Text = BuildMessage(currentVersion, latestVersion, releaseDate);
        }

        internal static string BuildMessage(string currentVersion, string latestVersion, string? releaseDate)
        {
            string released = string.IsNullOrWhiteSpace(releaseDate) ? string.Empty : $"  (released {releaseDate})";
            return "A new version of Philter Desktop is available." + Environment.NewLine + Environment.NewLine +
                   $"Installed version: {currentVersion}" + Environment.NewLine +
                   $"Latest version: {latestVersion}{released}";
        }

        private void Link_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = DownloadUrl, UseShellExecute = true });
                _link.LinkVisited = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not open the link:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                    "Update Available", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
