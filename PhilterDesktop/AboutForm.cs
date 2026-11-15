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
        public AboutForm() : this(null)
        {
        }

        /// <param name="license">The current work-use license, shown in place of the licensing note; null if none.</param>
        internal AboutForm(LicenseInfo? license)
        {
            InitializeComponent();
            lblLicense.Text = LicenseText(license);
            ArrangeLayout();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(btnOK);
            LoadVersionInfo();
        }

        internal const string TrademarkText = "'Philter' is a trademark of Philterd, LLC.";

        internal static string LicenseText(LicenseInfo? license) =>
            "Open source under the Apache License 2.0. " +
            (license is null
                ? "Free for personal use and evaluation; work use needs a per-user subscription from Philterd. "
                : LicenseActivation.Describe(license) + " ") +
            TrademarkText;

        // The designer places everything at fixed positions, which overlap and clip once the theme font and
        // display scaling grow the text. Stack the text column top to bottom in auto-sizing panels instead,
        // and size the dialog to fit. Runs before theming so the scaling applies to the new spacing.
        private readonly LinkLabel linkLicenses = new()
        {
            Name = "linkLicenses",
            Text = "Third-party licenses",
            AutoSize = true,
            TabStop = true,
            TabIndex = 9
        };

        private void ArrangeLayout()
        {
            linkLicenses.LinkClicked += (_, _) => Open(LicensesTarget(AppContext.BaseDirectory));
            var wrap = new Size(lblDescription.Width + LogicalToDeviceUnits(15), 0);
            foreach (Label label in new[] { lblDescription, lblLicense, lblReview })
            {
                label.AutoSize = true;
                label.MaximumSize = wrap;
            }

            FlowLayoutPanel Row(params Control[] controls)
            {
                var row = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = Padding.Empty };
                foreach (Control c in controls)
                {
                    c.Margin = new Padding(0, 0, LogicalToDeviceUnits(16), 0);
                }
                row.Controls.AddRange(controls);
                return row;
            }

            int gap = LogicalToDeviceUnits(10), links = LogicalToDeviceUnits(6), group = LogicalToDeviceUnits(14);
            (Control Control, int Top)[] inOrder =
            {
                (lblTitle, 0),
                (lblVersion, LogicalToDeviceUnits(2)),
                (lblDescription, gap),
                (lblLicense, gap),
                (Row(linkLicense, linkLabelWebsite), gap),
                (Row(linkNotices, linkLicenses), links),
                (lblReview, group),
                (lblCopyright, group),
                (btnOK, gap)
            };
            var stack = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = Padding.Empty
            };
            foreach ((Control c, int top) in inOrder)
            {
                c.Margin = new Padding(0, top, 0, 0);
                stack.Controls.Add(c);
            }
            btnOK.AutoSize = true;
            btnOK.MinimumSize = btnOK.Size;
            btnOK.Anchor = AnchorStyles.Right; // right-aligned under the widest line

            var root = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(pictureBox1.Left, lblTitle.Top, pictureBox1.Left, pictureBox1.Left),
                Location = Point.Empty
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pictureBox1.Margin = new Padding(0, 0, lblTitle.Left - pictureBox1.Right, 0);
            root.Controls.Add(pictureBox1, 0, 0);
            root.Controls.Add(stack, 1, 0);

            Controls.Clear();
            Controls.Add(root);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }

        private void LoadVersionInfo()
        {
            // Prefer the informational version so a pre-release suffix (e.g. "1.1.0-preview") is shown; the
            // numeric AssemblyVersion can't carry it. Strip any build metadata the SDK appends after '+'.
            Assembly asm = Assembly.GetExecutingAssembly();
            string? informational = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            string display = string.IsNullOrEmpty(informational)
                ? (asm.GetName().Version is { } v ? $"{v.Major}.{v.Minor}.{v.Build}" : "?")
                : informational.Split('+')[0];
            lblVersion.Text = $"Version {display}";
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void linkLabelWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Open("https://philterd.ai");
        }

        // Both license texts (the Apache License and the Philterd agreement), offline.
        private void linkLicense_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using var licenses = new LicenseForm(viewOnly: true);
            licenses.ShowDialog(this);
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

        internal const string NoticesFileName = "NOTICE.txt";
        internal const string NoticesUrl = "https://github.com/philterd/PhilterDesktop/blob/main/NOTICE";
        internal const string LicensesFileName = "THIRD-PARTY-LICENSES.txt";
        internal const string LicensesUrl = "https://github.com/philterd/PhilterDesktop/blob/main/THIRD-PARTY-LICENSES.txt";

        /// <summary>The installed third-party notices file, or the copy on GitHub if it's missing.</summary>
        internal static string NoticesTarget(string baseDirectory) => LocalOrUrl(baseDirectory, NoticesFileName, NoticesUrl);

        /// <summary>The installed third-party license texts, or the copy on GitHub if it's missing.</summary>
        internal static string LicensesTarget(string baseDirectory) => LocalOrUrl(baseDirectory, LicensesFileName, LicensesUrl);

        private static string LocalOrUrl(string baseDirectory, string fileName, string url)
        {
            string path = Path.Combine(baseDirectory, fileName);
            return File.Exists(path) ? path : url;
        }

        private void linkNotices_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Open(NoticesTarget(AppContext.BaseDirectory));
        }
    }
}