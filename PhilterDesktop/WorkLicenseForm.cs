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

using System.Globalization;

namespace PhilterDesktop
{
    /// <summary>
    /// Help, then Manage License: enter a work-use license by emailing a code to the purchase address, or by
    /// importing a license file on a computer that's never online. A status card says whether this computer
    /// is licensed; when it is, entering a different license is tucked behind a link. Built in code (no
    /// designer); the steps live in <see cref="LicenseActivation"/>.
    /// </summary>
    internal sealed class WorkLicenseForm : Form
    {
        internal const string IntroText =
            "Free for personal use and for trying it out. Using it for work (your job, your clients, or an " +
            "organization, even if you work alone) needs a license.";

        internal const string EntryHint = "Enter the email address you bought it with, and we'll email you a code.";

        internal const string NotLicensedStatus = "Not licensed for work use on this computer.";

        private static readonly Color OkColor = Color.FromArgb(0x1B, 0x5E, 0x20);
        private static readonly Color WarnColor = Color.FromArgb(0x8A, 0x4B, 0x00);
        private const int Width_ = 540;       // the window's content width
        private const int ActionWidth = 120;  // Send Code and Verify share one size
        private const int ActionHeight = 30;

        private readonly LicenseActivation _activation;
        private readonly WorkLicense _license;
        private readonly Func<DateTime> _utcNow;

        // Status card.
        private readonly PictureBox _statusIcon = new() { SizeMode = PictureBoxSizeMode.CenterImage, Margin = new Padding(0, 1, 10, 0) };
        // The card's text column: the content width less the card's frame, padding and icon.
        private const int CardTextWidth = Width_ - 40 - 2 - 24 - 24 - 10;
        private readonly Label _statusHeadline = new() { AutoSize = true, MaximumSize = new Size(CardTextWidth, 0), Margin = new Padding(0, 0, 0, 2) };
        private readonly Label _statusDetail = new() { AutoSize = true, MaximumSize = new Size(CardTextWidth, 0), Margin = new Padding(0) };
        private readonly FlowLayoutPanel _statusActions = new() { AutoSize = true, WrapContents = false, Margin = new Padding(0, 8, 0, 0) };
        private readonly LinkLabel _remove;
        private string _status = NotLicensedStatus;

        // Entering a license.
        private readonly TableLayoutPanel _entry;
        private readonly LinkLabel _useDifferent;
        private bool _entryOpened;
        private bool _entryShown; // tracked here: a control reports hidden until its window is on screen
        private LicenseInfo? _shownLicense;
        private readonly TextBox _email;
        private readonly Button _sendCode;
        private readonly TextBox _code;
        private readonly Button _verify;
        private readonly Label _message;

        public WorkLicenseForm(WorkLicense license, LicenseClient client, Func<DateTime>? utcNow = null)
        {
            _license = license;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            _activation = new LicenseActivation(license, client, _utcNow);

            Text = "Manage License";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // Header band.
            var title = new Label { Text = "Work License", AutoSize = true, Margin = new Padding(0, 0, 0, 4) };
            var subtitle = Wrapping(IntroText, new Padding(0));
            var header = Stack(new Padding(20, 16, 20, 16), title, subtitle);

            // Status card: a hairline frame around a surface-coloured panel.
            _remove = Link("Remove license", RemoveLicense);
            _remove.Name = "_remove";
            _statusActions.Controls.Add(Links.CreateLink("Your account", Links.AccountUrl));
            _statusActions.Controls.Add(_remove);
            foreach (Control action in _statusActions.Controls)
            {
                action.Margin = new Padding(0, 0, 16, 0);
            }
            var statusText = Stack(new Padding(0), _statusHeadline, _statusDetail, _statusActions);
            var card = new TableLayoutPanel
            {
                ColumnCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 12, 12, 12),
                Margin = new Padding(0)
            };
            card.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            card.Controls.Add(_statusIcon, 0, 0);
            card.Controls.Add(statusText, 1, 0);
            var cardFrame = new Panel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill, Padding = new Padding(1), Margin = new Padding(0, 0, 0, 16), Name = "_statusCard" };
            cardFrame.Controls.Add(card);

            // Entering a license: two numbered steps with matching buttons.
            _email = new TextBox { Name = "_email", Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0, 4, 10, 4) };
            _sendCode = ActionButton("&Send Code", "_sendCode");
            _code = new TextBox { Name = "_code", Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(0, 4, 10, 4), Enabled = false };
            _verify = ActionButton("&Verify", "_verify");
            _verify.Enabled = false;
            var steps = new TableLayoutPanel
            {
                ColumnCount = 4,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 8, 0, 0)
            };
            steps.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            steps.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            steps.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            steps.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            AddStep(steps, "1", "Purchase &email", _email, _sendCode);
            AddStep(steps, "2", "&Code", _code, _verify);

            var entryHeading = new Label { Text = "Enter a license", AutoSize = true, Margin = new Padding(0, 0, 0, 4) };
            _entry = Stack(new Padding(0), entryHeading, Wrapping(EntryHint, new Padding(0)), steps);
            _entry.Name = "_entry";
            _useDifferent = Link("Use a different license", OpenEntry);
            _useDifferent.Name = "_useDifferent";
            _useDifferent.Margin = new Padding(0, 0, 0, 0);

            _message = Wrapping(string.Empty, new Padding(0, 10, 0, 0));
            _message.Name = "_message";
            _message.Visible = false;

            var body = Stack(new Padding(20, 16, 20, 18), cardFrame, _entry, _useDifferent, _message);

            // Footer: secondary options on the left, Close on the right.
            var buy = Links.CreateLink("Buy a license", Links.BuyUrl("work-license"));
            var import = Link("Import a license file...", ImportFile);
            import.Name = "_import";
            var footerLinks = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Anchor = AnchorStyles.Left, Margin = new Padding(0) };
            foreach (LinkLabel link in new[] { buy, import })
            {
                link.Margin = new Padding(0, 0, 18, 0);
                footerLinks.Controls.Add(link);
            }
            var close = new Button { Text = "Close", DialogResult = DialogResult.OK, Anchor = AnchorStyles.Right, Margin = new Padding(0), Size = ModernTheme.StandardButtonSize };
            CancelButton = close;
            var footer = new TableLayoutPanel
            {
                ColumnCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 12, 20, 12),
                Margin = new Padding(0)
            };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footer.Controls.Add(footerLinks, 0, 0);
            footer.Controls.Add(close, 1, 0);

            var headerDivider = Divider();
            var footerDivider = Divider();
            var root = Stack(new Padding(0), header, headerDivider, body, footerDivider, footer);
            root.MinimumSize = new Size(Width_, 0);
            root.MaximumSize = new Size(Width_, 0);
            Controls.Add(root);

            _sendCode.Click += async (_, _) => await SendCodeAsync();
            _verify.Click += async (_, _) => await VerifyAsync();
            _email.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _sendCode.PerformClick(); } };
            _code.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _verify.PerformClick(); } };

            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_sendCode);

            // Theming paints every panel the window colour; colour the header, card and dividers after it.
            header.BackColor = ModernTheme.Surface;
            title.Font = new Font(ModernTheme.UiFont.FontFamily, 13f, FontStyle.Bold);
            subtitle.ForeColor = ModernTheme.SubtleText;
            headerDivider.BackColor = ModernTheme.Border;
            footerDivider.BackColor = ModernTheme.Border;
            cardFrame.BackColor = ModernTheme.Border;
            card.BackColor = ModernTheme.Surface;
            statusText.BackColor = ModernTheme.Surface;
            _statusActions.BackColor = ModernTheme.Surface;
            _statusHeadline.Font = new Font(ModernTheme.UiFont, FontStyle.Bold);
            _statusDetail.ForeColor = ModernTheme.SubtleText;
            entryHeading.Font = new Font(ModernTheme.UiFont, FontStyle.Bold);
            _message.ForeColor = ModernTheme.SubtleText;
            Disposed += (_, _) => _statusIcon.Image?.Dispose();

            ShowLicense();
        }

        /// <summary>The steps behind the dialog (exposed for tests).</summary>
        internal LicenseActivation Activation => _activation;

        /// <summary>The license this computer has, in a sentence (or that it has none).</summary>
        internal string StatusText => _status;

        /// <summary>Whether the Remove link is offered (a license is stored, current or not).</summary>
        internal bool RemoveOffered { get; private set; }
        internal string MessageText => _message.Text;

        /// <summary>Whether the email and code steps are showing (always, unless a current license tucks them away).</summary>
        internal bool EntryShown => _entryShown;

        /// <summary>Whether the "Use a different license" link is offered.</summary>
        internal bool UseDifferentOffered => !_entryShown;

        /// <summary>Sends the code request for the email in the box.</summary>
        internal async Task SendCodeAsync()
        {
            SetBusy(true);
            ActivationStep step;
            try
            {
                step = await _activation.SendCodeAsync(_email.Text);
            }
            catch (Exception ex)
            {
                step = Failed(ex);
            }
            if (IsDisposed)
            {
                return; // closed while waiting
            }
            _message.Text = step.Message;
            SetBusy(false);
            if (step.Success)
            {
                _code.Focus();
            }
        }

        /// <summary>Exchanges the typed code for a license.</summary>
        internal async Task VerifyAsync()
        {
            SetBusy(true);
            ActivationStep step;
            try
            {
                step = await _activation.VerifyAsync(_code.Text);
            }
            catch (Exception ex)
            {
                step = Failed(ex);
            }
            if (IsDisposed)
            {
                return; // closed while waiting
            }
            _message.Text = step.Message;
            if (!_activation.CodeRequested)
            {
                _code.Clear();
            }
            SetBusy(false);
            ShowLicense();
        }

        private void ImportFile()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Import a License File",
                Filter = "License files (*.txt)|*.txt|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
            ImportText(ReadLicenseFile(dialog.FileName));
        }

        /// <summary>Imports a license file's text (the file dialog's second half, for tests).</summary>
        internal void ImportText(string? text)
        {
            ActivationStep step;
            try
            {
                step = text is null
                    ? new ActivationStep(false, LicenseActivation.InvalidFileMessage)
                    : _activation.Import(text);
            }
            catch (Exception ex)
            {
                step = Failed(ex);
            }
            _message.Text = step.Message;
            ShowLicense();
        }

        internal static string? ReadLicenseFile(string path)
        {
            try
            {
                var file = new FileInfo(path);
                return file.Length > LicenseActivation.MaxLicenseFileBytes ? null : File.ReadAllText(path);
            }
            catch
            {
                return null;
            }
        }

        private void RemoveLicense()
        {
            if (MessageBox.Show(this, "Remove the work license from this computer? You can enter it again at any time.",
                    "Manage License", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
            {
                return;
            }
            RemoveNow();
        }

        /// <summary>Removes the license without asking (the confirmation's second half, for tests).</summary>
        internal void RemoveNow()
        {
            try
            {
                _message.Text = _activation.Remove().Message;
            }
            catch (Exception ex)
            {
                _message.Text = Failed(ex).Message;
            }
            ShowLicense();
        }

        private void ShowLicense()
        {
            LicenseInfo? current = null;
            LicenseInfo? stored = null;
            try
            {
                current = _license.Current(_utcNow());
                stored = _license.Stored();
            }
            catch
            {
                // An unreadable store reads as no license.
            }
            _status = current is null ? NotLicensedStatus : LicenseActivation.Describe(current);
            RemoveOffered = stored is not null;
            _remove.Visible = RemoveOffered;
            _statusActions.Visible = RemoveOffered;

            (string glyph, Color color, string headline, string detail) = Card(current, stored, _utcNow());
            _statusIcon.Image?.Dispose();
            _statusIcon.Image = ModernTheme.CreateGlyphImage(glyph, LogicalToDeviceUnits(24), color);
            _statusIcon.Size = new Size(LogicalToDeviceUnits(24), LogicalToDeviceUnits(24));
            _statusHeadline.Text = headline;
            _statusHeadline.ForeColor = color == ModernTheme.SubtleText ? ModernTheme.Text : color;
            _statusDetail.Text = detail;

            // With a current license, entering another is tucked behind a link until asked for, and tucked
            // away again once a different license takes its place.
            if (current is null || current != _shownLicense)
            {
                _entryOpened = false;
            }
            _shownLicense = current;
            _entryShown = current is null || _entryOpened;
            _entry.Visible = _entryShown;
            _useDifferent.Visible = !_entryShown;
            _message.Visible = _message.Text.Length > 0;
        }

        /// <summary>The status card's icon, colour, headline and detail line.</summary>
        internal static (string Glyph, Color Color, string Headline, string Detail) Card(LicenseInfo? current, LicenseInfo? stored, DateTime nowUtc)
        {
            const string Check = "", Warning = "", Info = "";
            LicenseInfo? license = current ?? stored;
            if (license is null)
            {
                return (Info, ModernTheme.SubtleText, NotLicensedStatus, "Personal use and trying it out are free.");
            }
            string seats = license.Seats > 1 ? $" · {license.Seats} seats" : string.Empty;
            string expires = Date(license.Expires);
            if (current is not null && nowUtc.Date <= license.Expires.Date)
            {
                return (Check, OkColor, "Licensed for work use", $"{license.Email}{seats} · through {expires}");
            }
            if (current is not null)
            {
                string renewBy = Date(license.Expires.AddDays(LicenseInfo.GraceDays));
                return (Warning, WarnColor, "License expired",
                    $"{license.Email}{seats} · expired {expires}. Renew by {renewBy} to keep using it for work.");
            }
            return (Warning, WarnColor, "License expired", $"{license.Email}{seats} · expired {expires}. Work use needs a current license.");
        }

        private static string Date(DateTime date) => date.ToString("MMMM d, yyyy", CultureInfo.CurrentCulture);

        private void OpenEntry()
        {
            _entryOpened = true;
            ShowLicense();
            _email.Focus();
        }

        // A storage error is reported in the dialog rather than reaching the app's crash handler. (Not
        // logged here: the dialog doesn't know whether the user turned logging on.)
        private static ActivationStep Failed(Exception _) =>
            new(false, LicenseActivation.UnexpectedErrorMessage);

        private void SetBusy(bool busy)
        {
            UseWaitCursor = busy;
            _sendCode.Enabled = !busy;
            _email.Enabled = !busy;
            if (busy)
            {
                _verify.Enabled = false;
            }
            else
            {
                _verify.Enabled = _activation.CodeRequested;
                _code.Enabled = _activation.CodeRequested;
                _message.Visible = _message.Text.Length > 0;
            }
        }

        // One step row: its number, label, box, and button.
        private static void AddStep(TableLayoutPanel steps, string number, string label, TextBox box, Button button)
        {
            int row = steps.RowCount++;
            steps.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            steps.Controls.Add(new Label { Text = number, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 10, 0), ForeColor = ModernTheme.Accent, Font = new Font(ModernTheme.UiFont, FontStyle.Bold) }, 0, row);
            steps.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 12, 0) }, 1, row);
            steps.Controls.Add(box, 2, row);
            steps.Controls.Add(button, 3, row);
        }

        // Send Code and Verify are the same size, whatever their text.
        private static Button ActionButton(string text, string name) => new()
        {
            Text = text,
            Name = name,
            Size = new Size(ActionWidth, ActionHeight),
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, 4, 0, 4),
            UseVisualStyleBackColor = true
        };

        private static LinkLabel Link(string text, Action onClick)
        {
            var link = new LinkLabel { Text = text, AutoSize = true, LinkColor = ModernTheme.Accent, ActiveLinkColor = ModernTheme.Accent };
            link.LinkClicked += (_, _) => onClick();
            return link;
        }

        private static Panel Divider() => new() { Height = 1, Dock = DockStyle.Fill, Margin = new Padding(0) };

        // A vertical stack of controls, each filling the width.
        private static TableLayoutPanel Stack(Padding padding, params Control[] controls)
        {
            var stack = new TableLayoutPanel
            {
                ColumnCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Padding = padding,
                Margin = new Padding(0)
            };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < controls.Length; i++)
            {
                stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                stack.Controls.Add(controls[i], 0, i);
            }
            return stack;
        }

        private static Label Wrapping(string text, Padding margin) => new()
        {
            Text = text,
            AutoSize = true,
            MaximumSize = new Size(Width_ - 40, 0),
            Margin = margin
        };
    }
}
