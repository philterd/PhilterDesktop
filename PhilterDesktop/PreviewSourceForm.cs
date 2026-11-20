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
    /// Picks the document for "Redact with Preview", by Browse or by dropping a file. Browse goes
    /// straight on to the preview; a dropped file waits for Continue.
    /// </summary>
    internal sealed partial class PreviewSourceForm : Form
    {
        private static readonly string[] PreviewableExtensions = { ".txt", ".docx", ".pdf", ".rtf", ".eml", ".msg" };

        private const string PickerFilter =
            "Supported (*.txt;*.docx;*.pdf;*.rtf;*.eml;*.msg)|*.txt;*.docx;*.pdf;*.rtf;*.eml;*.msg|Text (*.txt)|*.txt|Rich Text (*.rtf)|*.rtf|Word (*.docx)|*.docx|PDF (*.pdf)|*.pdf|Email (*.eml;*.msg)|*.eml;*.msg";

        internal const string UnsupportedMessage =
            "Preview supports .txt, .docx, .pdf, .rtf, .eml, and .msg files. Choose one of those.";

        internal const string MissingMessage = "That file no longer exists. Choose another document.";

        /// <summary>The Browse dialog; tests replace it. Returns the chosen path, or null if cancelled.</summary>
        internal Func<IWin32Window, string?> PickFile = owner =>
        {
            using var dlg = new OpenFileDialog { Title = "Select a file to redact", Filter = PickerFilter, CheckFileExists = true };
            return dlg.ShowDialog(owner) == DialogResult.OK ? dlg.FileName : null;
        };

        /// <summary>Shows a warning; tests replace it to check the message without a dialog.</summary>
        internal Action<string> ShowWarning;

        /// <summary>The chosen document, set when the form closes with OK.</summary>
        public string? SelectedPath { get; private set; }

        public PreviewSourceForm()
        {
            InitializeComponent();
            _source.KeepEndVisible();
            ShowWarning = text => MessageBox.Show(this, text, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);

            _dropZone.Accepts = IsPreviewable;
            _dropZone.FileDropped += OnFileDropped;

            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_continue);
            // Enter opens Browse straight away; Continue takes over once a file is dropped.
            ActiveControl = _browse;
        }

        internal static bool IsPreviewable(string path) =>
            PreviewableExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

        private void OnBrowse(object? sender, EventArgs e)
        {
            string? path = PickFile(this);
            if (path is null)
            {
                return;
            }
            if (!IsPreviewable(path))
            {
                ShowWarning(UnsupportedMessage);
                return;
            }
            Finish(path);
        }

        private void OnFileDropped(string path)
        {
            _source.Text = path;
            _continue.Enabled = true;
            ActiveControl = _continue;
        }

        private void OnContinue(object? sender, EventArgs e)
        {
            string path = _source.Text;
            if (!File.Exists(path))
            {
                ShowWarning(MissingMessage);
                _source.Text = string.Empty;
                _continue.Enabled = false;
                ActiveControl = _browse;
                return;
            }
            Finish(path);
        }

        private void Finish(string path)
        {
            _source.Text = path;
            SelectedPath = path;
            DialogResult = DialogResult.OK;
        }
    }
}
