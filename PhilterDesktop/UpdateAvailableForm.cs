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
    /// Shown when a newer version is published. Displays the installed/latest versions and a clickable
    /// link to the download URL from the update manifest; it does not download anything itself.
    /// </summary>
    public partial class UpdateAvailableForm : Form
    {
        // Subscribers download new versions from their account page. The manifest's downloadUrl is
        // intentionally not used — the official build is provided to subscribers, not via a public URL.
        private const string AccountUrl = "https://account.philterd.ai";

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public UpdateAvailableForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            _link.Text = AccountUrl;
        }

        public UpdateAvailableForm(string currentVersion, string latestVersion, string? releaseDate)
            : this()
        {
            string released = string.IsNullOrWhiteSpace(releaseDate) ? string.Empty : $"  (released {releaseDate})";
            _message.Text =
                "A new version of Philter Desktop is available." + Environment.NewLine + Environment.NewLine +
                $"Installed version: {currentVersion}" + Environment.NewLine +
                $"Latest version: {latestVersion}{released}";
        }

        private void Link_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = AccountUrl, UseShellExecute = true });
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
