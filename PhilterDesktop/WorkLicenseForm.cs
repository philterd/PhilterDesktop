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
    /// Help, then Manage License: enter a work-use license by emailing a code to the purchase address, or by
    /// importing a license file on a computer that's never online. Shows the current license and can remove
    /// it. Built in code (no designer); the steps live in <see cref="LicenseActivation"/>.
    /// </summary>
    internal sealed class WorkLicenseForm : Form
    {
        internal const string IntroText =
            "Philter Desktop is free for personal use and for trying it out. Using it for work (your job, your " +
            "clients, or an organization, even if you work alone) needs a license. If you've bought one, enter " +
            "the email address you used to buy it and we'll email you a code.";

        internal const string NotLicensedStatus = "Not licensed for work use on this computer.";

        private readonly LicenseActivation _activation;
        private readonly WorkLicense _license;
        private readonly Func<DateTime> _utcNow;

        private readonly Label _status;
        private readonly TextBox _email;
        private readonly Button _sendCode;
        private readonly TextBox _code;
        private readonly Button _verify;
        private readonly Label _message;
        private readonly LinkLabel _remove;

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

            var layout = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 3,
                MinimumSize = new Size(500, 0),
                MaximumSize = new Size(500, 0),
                Padding = new Padding(16, 14, 16, 14)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            void AddWide(Control control)
            {
                layout.Controls.Add(control);
                layout.SetColumnSpan(control, 3);
            }

            AddWide(Wrapping(IntroText, new Padding(0, 0, 0, 12)));

            _status = Wrapping(string.Empty, new Padding(0, 0, 0, 14));
            var statusFont = new Font(ModernTheme.UiFont, FontStyle.Bold);
            _status.Font = statusFont;
            Disposed += (_, _) => statusFont.Dispose();
            _status.Name = "_status";
            AddWide(_status);

            _email = new TextBox { Name = "_email", Dock = DockStyle.Fill, Margin = new Padding(8, 3, 8, 3) };
            _sendCode = Button("&Send Code");
            _sendCode.Name = "_sendCode";
            AddRow("Purchase &email:", _email, _sendCode);

            _code = new TextBox { Name = "_code", Dock = DockStyle.Fill, Margin = new Padding(8, 3, 8, 3), Enabled = false };
            _verify = Button("&Verify");
            _verify.Name = "_verify";
            _verify.Enabled = false;
            AddRow("&Code:", _code, _verify);

            _message = Wrapping(string.Empty, new Padding(0, 10, 0, 0));
            _message.Name = "_message";
            AddWide(_message);

            var links = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Margin = new Padding(0, 14, 0, 0) };
            LinkLabel buy = Links.CreateLink("Buy a license", Links.BuyUrl("work-license"));
            var import = new LinkLabel { Text = "Import a license file...", AutoSize = true, Name = "_import" };
            import.LinkClicked += (_, _) => ImportFile();
            var account = Links.CreateLink("Your account", Links.AccountUrl);
            _remove = new LinkLabel { Text = "Remove license", AutoSize = true, Name = "_remove" };
            _remove.LinkClicked += (_, _) => RemoveLicense();
            foreach (LinkLabel link in new[] { buy, import, account, _remove })
            {
                link.Margin = new Padding(0, 0, 18, 0);
                links.Controls.Add(link);
            }
            AddWide(links);

            var close = new Button { Text = "Close", DialogResult = DialogResult.OK, Anchor = AnchorStyles.Right, Margin = new Padding(0, 16, 0, 0) };
            close.Size = ModernTheme.StandardButtonSize;
            layout.Controls.Add(close);
            layout.SetColumnSpan(close, 3);
            CancelButton = close;

            Controls.Add(layout);

            _sendCode.Click += async (_, _) => await SendCodeAsync();
            _verify.Click += async (_, _) => await VerifyAsync();
            _email.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _sendCode.PerformClick(); } };
            _code.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _verify.PerformClick(); } };

            ModernTheme.Apply(this);
            foreach (LinkLabel link in new[] { import, _remove })
            {
                link.LinkColor = ModernTheme.Accent;
                link.ActiveLinkColor = ModernTheme.Accent;
            }
            _message.ForeColor = ModernTheme.SubtleText;
            ShowLicense();

            void AddRow(string label, TextBox box, Button button)
            {
                layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 3, 0, 3) });
                layout.Controls.Add(box);
                layout.Controls.Add(button);
            }
        }

        /// <summary>The steps behind the dialog (exposed for tests).</summary>
        internal LicenseActivation Activation => _activation;

        internal string StatusText => _status.Text;

        /// <summary>Whether the Remove link is offered (a license is stored, current or not).</summary>
        internal bool RemoveOffered { get; private set; }
        internal string MessageText => _message.Text;

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
            _status.Text = current is null ? NotLicensedStatus : LicenseActivation.Describe(current);
            RemoveOffered = stored is not null;
            _remove.Visible = RemoveOffered;
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
            }
        }

        private static Button Button(string text)
        {
            var button = new Button { Text = text, AutoSize = true, Anchor = AnchorStyles.Right };
            button.MinimumSize = new Size(ModernTheme.StandardButtonSize.Width, 0);
            return button;
        }

        private static Label Wrapping(string text, Padding margin) => new()
        {
            Text = text,
            AutoSize = true,
            MaximumSize = new Size(468, 0),
            Margin = margin
        };
    }
}
