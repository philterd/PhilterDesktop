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
    /// <summary>How <see cref="PassphraseForm"/> is being used.</summary>
    public enum PassphraseFormMode
    {
        /// <summary>Enter the existing passphrase to open the database.</summary>
        Unlock,
        /// <summary>Choose a new passphrase (with confirmation).</summary>
        Set,
        /// <summary>Enter the current passphrase and choose a new one.</summary>
        Change
    }

    /// <summary>Prompts for a passphrase to unlock, set, or change database passphrase protection.</summary>
    public partial class PassphraseForm : Form
    {
        private const int MinLength = 8;
        private PassphraseFormMode _mode;

        public string CurrentPassphrase => _current.Text;
        public string NewPassphrase => _new.Text;

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public PassphraseForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_ok);
        }

        public PassphraseForm(PassphraseFormMode mode) : this()
        {
            _mode = mode;
            Configure();
        }

        private void Configure()
        {
            switch (_mode)
            {
                case PassphraseFormMode.Unlock:
                    Text = "Unlock Philter Desktop";
                    _prompt.Text = "Enter your passphrase to open Philter Desktop.";
                    ShowRow(_currentLabel, _current, false);
                    _newLabel.Text = "Passphrase:";
                    ShowRow(_confirmLabel, _confirm, false);
                    _ok.Text = "Unlock";
                    break;

                case PassphraseFormMode.Set:
                    Text = "Set Passphrase";
                    _prompt.Text = "Choose a passphrase. There is no way to recover the data if you forget it.";
                    ShowRow(_currentLabel, _current, false);
                    _newLabel.Text = "New passphrase:";
                    _confirmLabel.Text = "Confirm passphrase:";
                    _ok.Text = "Set";
                    break;

                case PassphraseFormMode.Change:
                    Text = "Change Passphrase";
                    _prompt.Text = "Enter your current passphrase and choose a new one.";
                    _currentLabel.Text = "Current passphrase:";
                    _newLabel.Text = "New passphrase:";
                    _confirmLabel.Text = "Confirm passphrase:";
                    _ok.Text = "Change";
                    break;
            }
        }

        private static void ShowRow(Control label, Control field, bool visible)
        {
            label.Visible = visible;
            field.Visible = visible;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ClientSize = _root.PreferredSize;
        }

        private void OnOk(object? sender, EventArgs e)
        {
            if (_mode == PassphraseFormMode.Unlock)
            {
                if (NewPassphrase.Length == 0)
                {
                    Warn("Enter your passphrase.");
                }
                return;
            }

            if (_mode == PassphraseFormMode.Change && CurrentPassphrase.Length == 0)
            {
                Warn("Enter your current passphrase.");
                return;
            }
            if (NewPassphrase.Length < MinLength)
            {
                Warn($"The passphrase must be at least {MinLength} characters.");
                return;
            }
            if (NewPassphrase != _confirm.Text)
            {
                Warn("The passphrases do not match.");
            }
        }

        private void Warn(string message)
        {
            MessageBox.Show(this, message, "Passphrase", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
        }

        /// <summary>
        /// Shows the unlock prompt in a loop until the passphrase opens <paramref name="store"/>
        /// (returns true) or the user cancels (returns false).
        /// </summary>
        public static bool Unlock(DatabaseKeyStore store, IWin32Window? owner)
        {
            while (true)
            {
                using var form = new PassphraseForm(PassphraseFormMode.Unlock);
                if (form.ShowDialog(owner) != DialogResult.OK)
                {
                    return false;
                }
                if (store.TryUnlockWithPassphrase(form.NewPassphrase))
                {
                    return true;
                }
                MessageBox.Show(owner, "Incorrect passphrase. Please try again.",
                    "Philter Desktop", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
