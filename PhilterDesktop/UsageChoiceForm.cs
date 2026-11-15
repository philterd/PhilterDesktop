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
    /// Shown as the main window opens (for anyone without a verified work license, once per run and again
    /// after a day for a run that stays open): personal or work use
    /// on top, the redaction-review notice below, and a choice of Personal use or Work use to continue.
    /// Work use then offers I'm Trying It Out or Buy a License (which opens the buy page; the main window
    /// then opens Manage License). Choosing also acknowledges the notice.
    /// </summary>
    internal sealed class UsageChoiceForm : Form
    {
        internal const string LicensingHeading = "Personal or work use?";
        internal const string LicensingText =
            "Philter Desktop is free for personal use and for trying it out. Using it for work (your job, your " +
            "clients, or an organization, even if you work alone) needs a license.";
        internal const string NoticeHeading = "Reviewing redactions";
        internal const string ConfirmText = "By continuing, you confirm you've read the notice about reviewing redactions.";
        internal const string WorkText =
            "Work use needs a license. Trying it out first is free, until you use Philter Desktop or its redacted " +
            "files in your work.";

        private readonly Action<string> _openUrl;
        private readonly Label _footerText;
        private readonly Button _personal;
        private readonly Button _work;
        private readonly Button _trying;
        private readonly Button _buy;

        public UsageChoiceForm() : this(Links.Open)
        {
        }

        /// <summary>With the URL opener tests can supply.</summary>
        internal UsageChoiceForm(Action<string> openUrl)
        {
            _openUrl = openUrl;

            Text = "Philter Desktop";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            const int width = 560;
            var root = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                MinimumSize = new Size(width, 0),
                MaximumSize = new Size(width, 0),
                Padding = new Padding(20, 18, 20, 16)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var headingFont = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            Disposed += (_, _) => headingFont.Dispose();
            Label Heading(string text, int top) => new()
            {
                Text = text,
                AutoSize = true,
                Font = headingFont,
                Margin = new Padding(0, top, 0, 6)
            };
            Label Body(string text) => new()
            {
                Text = text,
                AutoSize = true,
                MaximumSize = new Size(width - 40, 0),
                Margin = new Padding(0)
            };

            root.Controls.Add(Heading(LicensingHeading, 0));
            root.Controls.Add(Body(LicensingText));
            LinkLabel faq = Links.CreateLink("Do I need a license?", Links.SupportUrl("first-run"));
            faq.Margin = new Padding(0, 6, 0, 0);
            root.Controls.Add(faq);

            root.Controls.Add(Heading(NoticeHeading, 20));
            root.Controls.Add(Body(RedactionNoticeForm.NoticeText));
            var learnMore = new LinkLabel { Text = "Learn more about redaction accuracy", AutoSize = true, Margin = new Padding(0, 6, 0, 0) };
            learnMore.LinkClicked += (_, _) => _openUrl(RedactionNoticeForm.LearnMoreUrl);
            root.Controls.Add(learnMore);

            _footerText = Body(ConfirmText);
            _footerText.Name = "_footerText";
            _footerText.Margin = new Padding(0, 20, 0, 12);
            root.Controls.Add(_footerText);

            _personal = ChoiceButton("&Personal Use");
            _work = ChoiceButton("&Work Use");
            _trying = ChoiceButton("I'm &Trying It Out");
            _buy = ChoiceButton("&Buy a License");
            _trying.Visible = false;
            _buy.Visible = false;
            var buttons = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0)
            };
            // Right to left: the second-added button sits to the left of the first.
            buttons.Controls.AddRange(new Control[] { _work, _personal, _buy, _trying });
            root.Controls.Add(buttons);

            Controls.Add(root);

            _personal.Click += (_, _) => Choose(UsageKind.Personal);
            _work.Click += (_, _) => ShowWorkChoices();
            _trying.Click += (_, _) => Choose(UsageKind.WorkTrying);
            _buy.Click += (_, _) =>
            {
                _openUrl(Links.BuyUrl("first-run"));
                Choose(UsageKind.WorkBuying);
            };

            ModernTheme.Apply(this);
            faq.LinkColor = learnMore.LinkColor = ModernTheme.Accent;
            faq.ActiveLinkColor = learnMore.ActiveLinkColor = ModernTheme.Accent;
            _footerText.ForeColor = ModernTheme.SubtleText;
        }

        /// <summary>The choice made; null until one is (or if the window was closed without one).</summary>
        public UsageKind? Choice { get; private set; }

        /// <summary>Whether the Work use follow-up (trying it out, or buying) is showing.</summary>
        internal bool ShowingWorkChoices { get; private set; }

        internal string FooterText => _footerText.Text;

        /// <summary>Work use: explain that it needs a license and offer trying it out or buying.</summary>
        internal void ShowWorkChoices()
        {
            ShowingWorkChoices = true;
            _footerText.Text = WorkText;
            _personal.Visible = false;
            _work.Visible = false;
            _trying.Visible = true;
            _buy.Visible = true;
            _trying.Focus();
        }

        internal void Choose(UsageKind kind)
        {
            Choice = kind;
            DialogResult = DialogResult.OK;
        }

        private static Button ChoiceButton(string text)
        {
            var button = new Button { Text = text, AutoSize = true, Margin = new Padding(8, 0, 0, 0) };
            button.MinimumSize = new Size(ModernTheme.StandardButtonSize.Width + 20, ModernTheme.StandardButtonSize.Height);
            return button;
        }
    }
}
