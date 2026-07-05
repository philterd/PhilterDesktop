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

namespace PhilterDesktop
{
    /// <summary>
    /// Second first-run gate, shown after <see cref="LicenseForm"/>: it states that redaction uses
    /// statistical/machine-learning methods that can miss or over-flag information and must always be
    /// reviewed by a qualified person, and requires the user to agree. Declining (like the license form)
    /// exits the application before the main window opens.
    /// </summary>
    public partial class RedactionNoticeForm : Form
    {
        private const string LearnMoreUrl = "https://philterd.github.io/PhilterDesktop/mistakes/";

        private const string NoticeText =
            "Philter Desktop identifies personal and sensitive information using statistical and " +
            "machine-learning methods. These methods are not perfect: they can miss sensitive information " +
            "(false negatives) or flag text that is not sensitive (false positives).\r\n\r\n" +
            "Redacted documents must always be carefully reviewed by a qualified person before they are " +
            "shared or relied upon. You are responsible for verifying that every document has been redacted " +
            "appropriately for your needs.";

        public RedactionNoticeForm() : this(viewOnly: false)
        {
        }

        /// <param name="viewOnly">
        /// When true, the notice is shown for reference (e.g. from the main-window status bar) rather than
        /// as the first-run acknowledgement: the OK button is labelled Close and Esc dismisses it.
        /// </param>
        public RedactionNoticeForm(bool viewOnly)
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_ok);
            _body.Text = NoticeText;

            if (viewOnly)
            {
                _ok.Text = "&Close";
                CancelButton = _ok; // Esc / the window close button just closes
            }
        }

        /// <summary>Opens the redaction-accuracy documentation page in the user's browser.</summary>
        private void OnLearnMoreClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(LearnMoreUrl) { UseShellExecute = true });
            }
            catch
            {
                // Ignore failures to launch the browser; the notice text is shown above regardless.
            }
        }

        /// <summary>Whether the redaction-review notice should be shown on this launch (i.e. not yet accepted).</summary>
        public static bool ShouldShow() => !Acknowledgements.Store.HasAccepted(Acknowledgements.RedactionNoticeKey);

        /// <summary>Persists that the user acknowledged the redaction-review notice, so it isn't shown again.</summary>
        public static void RememberAccepted() => Acknowledgements.Store.RememberAccepted(Acknowledgements.RedactionNoticeKey);
    }
}
