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

using System.Reflection;

namespace PhilterDesktop
{
    /// <summary>
    /// The license dialog. At the gate it shows the <b>Philterd Commercial License Agreement</b> (the EULA
    /// governing the official build) behind an "I Agree" / "I Disagree" choice, with a link to the
    /// <b>Apache License 2.0</b> that covers the source code. Acceptance is recorded with the EULA's version,
    /// so the gate returns only when a release ships a newer EULA. From the About box's View licenses link (see
    /// <paramref name="viewOnly"/>) it shows both texts. Both display offline.
    /// </summary>
    public partial class LicenseForm : Form
    {
        private const string ApacheResource = "PhilterDesktop.ApacheLicense.txt";

        /// <summary>The Apache License 2.0 for Philter Desktop's source code, in the GitHub repository.</summary>
        internal const string ApacheLicenseUrl = "https://github.com/philterd/PhilterDesktop/blob/main/LICENSE";

        internal const string ApacheLinkText = "Philter Desktop's source code is open source under the Apache License 2.0.";
        private const string ApacheLinkWords = "Apache License 2.0";

        internal const string GateTitle = "License Agreement";
        internal const string GateSubtitle = "Please read the license agreement for Philter Desktop. To continue, click I Agree.";
        internal const string ViewTitle = "Licenses";
        internal const string ViewSubtitle =
            "The open-source license for Philter Desktop's source code, and the license agreement for the official build.";

        public LicenseForm() : this(viewOnly: false)
        {
        }

        /// <param name="viewOnly">
        /// When true, the dialog is shown for reference (from the About box) rather than as the
        /// first-run gate: the Agree/Disagree choice is replaced with a single Close button, since the
        /// user has already accepted.
        /// </param>
        public LicenseForm(bool viewOnly)
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_agree);
            StyleChrome();

            // At the gate only the EULA is shown, since it's the only agreement accepted; the Apache License
            // (a grant of permissions, nothing to accept) is a link. The view from About shows both texts offline.
            _apacheLink.Text = ApacheLinkText;
            _apacheLink.LinkArea = new LinkArea(ApacheLinkText.IndexOf(ApacheLinkWords, StringComparison.Ordinal), ApacheLinkWords.Length);
            ShowsApacheText = viewOnly;
            _title.Text = viewOnly ? ViewTitle : GateTitle;
            _subtitle.Text = viewOnly ? ViewSubtitle : GateSubtitle;
            if (viewOnly)
            {
                _licenseBody.Text = ForTextBox(ReadResource(ApacheResource));
                _licenseBody.Select(0, 0); // keep the view scrolled to the top
                _apacheLink.Visible = false;
            }
            else
            {
                // The header names the agreement, so the reading pane needs no heading of its own.
                _licenseHeading.Visible = false;
                _licenseFrame.Visible = false;
                _eulaHeading.Visible = false;
                _body.RowStyles[1] = new RowStyle(SizeType.Absolute, 0F); // the EULA takes the freed height
            }
            _eulaBody.Text = ForTextBox(ReadEula());
            _eulaBody.Select(0, 0);

            if (viewOnly)
            {
                _disagree.Visible = false;
                _agree.Text = "&Close";
                _agree.DialogResult = DialogResult.OK;
                CancelButton = _agree; // Esc / the window close button just closes
            }
            else
            {
                // Agreeing takes a deliberate click (or Alt+A), so a stray Enter can't accept the terms.
                // Focus starts in the agreement, not on a button Enter or Space would press.
                AcceptButton = null;
                ActiveControl = _eulaBody;
            }
        }

        /// <summary>Keeps the dialog on screen at high display scaling.</summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UiState.FitToScreen(this);
        }

        /// <summary>Whether the Apache License text is shown (the view from About) rather than linked (the gate).</summary>
        internal bool ShowsApacheText { get; }

        private void ApacheLink_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = ApacheLicenseUrl, UseShellExecute = true });
                _apacheLink.LinkVisited = true;
            }
            catch
            {
                // best effort: opening a browser shouldn't break the license screen
            }
        }

        // ModernTheme.Apply paints every panel the window colour and gives text boxes a border, so the
        // header band, hairlines, and borderless reading panes are coloured after it.
        private void StyleChrome()
        {
            _header.BackColor = ModernTheme.Surface;
            _subtitle.ForeColor = ModernTheme.SubtleText;
            _headerDivider.BackColor = ModernTheme.Border;
            _footerDivider.BackColor = ModernTheme.Border;
            _apacheLink.ForeColor = ModernTheme.SubtleText;
            _apacheLink.LinkColor = ModernTheme.Accent;
            _apacheLink.ActiveLinkColor = ModernTheme.Accent;
            foreach ((Panel frame, Panel inner, TextBox text) in new[]
                     { (_licenseFrame, _licenseInner, _licenseBody), (_eulaFrame, _eulaInner, _eulaBody) })
            {
                frame.BackColor = ModernTheme.Border;
                inner.BackColor = ModernTheme.Surface;
                text.BorderStyle = BorderStyle.None;
                text.BackColor = ModernTheme.Surface;
            }

            if (Icon is not null)
            {
                using var sized = new Icon(Icon, 48, 48);
                Bitmap image = sized.ToBitmap();
                _headerIcon.Image = image;
                Disposed += (_, _) => image.Dispose();
            }
        }

        /// <summary>Whether the license dialog should be shown on this launch.</summary>
        public static bool ShouldShow() => ShouldShow(BundledEula.Version());

        /// <summary>
        /// Shown when never accepted, or when <paramref name="bundledVersion"/> is newer than the version
        /// last accepted. An acceptance from before versioning counts as older than any version. If the
        /// bundled EULA declares no version, a prior acceptance stands.
        /// </summary>
        internal static bool ShouldShow(Version? bundledVersion)
        {
            if (!Acknowledgements.Store.HasAccepted(Acknowledgements.LicenseKey))
            {
                return true;
            }
            return bundledVersion is not null && EulaVersion.IsNewer(bundledVersion, AcceptedEulaVersion());
        }

        /// <summary>The EULA version the user last accepted, or null (never, or before versioning).</summary>
        internal static Version? AcceptedEulaVersion() =>
            EulaVersion.TryParse(Acknowledgements.Store.GetValue(Acknowledgements.LicenseVersionKey));

        /// <summary>
        /// Shows the license screen if acceptance is owed. On Agree, records the acceptance and returns true;
        /// returns false if the user disagrees. Returns true when nothing is owed.
        /// </summary>
        public static bool RunGateIfNeeded()
        {
            if (!ShouldShow())
            {
                return true;
            }
            using var form = new LicenseForm();
            if (form.ShowDialog() != DialogResult.OK)
            {
                return false;
            }
            RememberAccepted();
            return true;
        }

        /// <summary>Persists that the user accepted the bundled EULA (and its version, when it declares one).</summary>
        public static void RememberAccepted() => RememberAccepted(BundledEula.Version());

        internal static void RememberAccepted(Version? bundledVersion)
        {
            Acknowledgements.Store.RememberAccepted(Acknowledgements.LicenseKey);
            if (bundledVersion is not null)
            {
                Acknowledgements.Store.SetValue(Acknowledgements.LicenseVersionKey, bundledVersion.ToString());
            }
        }

        // A Windows TextBox only breaks lines on CRLF; bare LFs (as in a file checked out with Unix line
        // endings) are dropped, running the words on either side together.
        internal static string ForTextBox(string text) => text.ReplaceLineEndings("\r\n");

        // Reads an embedded license text (the Apache license). Returns a short fallback rather than throwing
        // if a resource is somehow missing, so the acceptance gate always renders.
        private static string ReadResource(string logicalName)
        {
            try
            {
                using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(logicalName);
                if (stream is null)
                {
                    return "(license text unavailable)";
                }
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
            catch
            {
                return "(license text unavailable)";
            }
        }

        // Reads the bundled EULA. Falls back to a short pointer rather than throwing if the file is missing,
        // so the gate still renders.
        private static string ReadEula() =>
            BundledEula.ReadText() ??
            "The Philterd Commercial License Agreement is available at https://philterd.ai/philterd-eula.txt.";
    }
}
