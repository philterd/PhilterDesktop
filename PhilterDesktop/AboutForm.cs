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

using System.Diagnostics;
using System.Reflection;

namespace PhilterDesktop
{
    /// <summary>
    /// About dialog displaying application information.
    /// </summary>
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(btnOK);
            LoadVersionInfo();
        }

        private void LoadVersionInfo()
        {
            // Get version information from assembly
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            lblVersion.Text = $"Version {version?.Major}.{version?.Minor}.{version?.Build}";
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void linkLabelWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Open("https://www.philterd.ai");
        }

        private void linkLicense_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Open("https://github.com/philterd/PhilterDesktop/blob/main/LICENSE");
        }

        private void linkGitHub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Open("https://github.com/philterd/PhilterDesktop");
        }

        private static void Open(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch
            {
                // best effort — opening a browser shouldn't crash the dialog
            }
        }

        private void icons8LinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Open("https://icons8.com");
        }

        private void linkPhilter_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Open(Upsell.PhilterUrl("about"));
        }

        private void linkConsulting_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Open(Upsell.ConsultingUrl("about"));
        }
    }
}