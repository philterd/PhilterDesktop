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
    /// Modal dialog that downloads a file (the installer) to a given path, showing progress and
    /// allowing cancellation. After <see cref="Form.ShowDialog()"/> returns, check <see cref="Canceled"/>
    /// and <see cref="Error"/> to learn the outcome.
    /// </summary>
    public partial class DownloadProgressForm : Form
    {
        private readonly string _url = string.Empty;
        private readonly string _destinationPath = string.Empty;
        private readonly CancellationTokenSource _cts = new();

        /// <summary>The user cancelled the download.</summary>
        public bool Canceled { get; private set; }

        /// <summary>The exception that ended the download, if it failed (null on success/cancel).</summary>
        public Exception? Error { get; private set; }

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public DownloadProgressForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
        }

        public DownloadProgressForm(string url, string destinationPath) : this()
        {
            _url = url;
            _destinationPath = destinationPath;
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                var progress = new Progress<(long Downloaded, long? Total)>(ReportProgress);
                await UpdateChecker.DownloadAsync(_url, _destinationPath, progress, _cts.Token);
                DialogResult = DialogResult.OK;
            }
            catch (OperationCanceledException)
            {
                Canceled = true;
                DialogResult = DialogResult.Cancel;
            }
            catch (Exception ex)
            {
                Error = ex;
                DialogResult = DialogResult.Abort;
            }

            Close();
        }

        private void ReportProgress((long Downloaded, long? Total) p)
        {
            if (p.Total is long total && total > 0)
            {
                if (_progress.Style != ProgressBarStyle.Blocks)
                {
                    _progress.Style = ProgressBarStyle.Blocks;
                    _progress.Maximum = 100;
                }
                _progress.Value = (int)Math.Clamp(p.Downloaded * 100 / total, 0, 100);
                _label.Text = $"Downloading… {Megabytes(p.Downloaded):0.0} MB of {Megabytes(total):0.0} MB";
            }
            else
            {
                _label.Text = $"Downloading… {Megabytes(p.Downloaded):0.0} MB";
            }
        }

        private static double Megabytes(long bytes) => bytes / 1024d / 1024d;

        private void Cancel_Click(object? sender, EventArgs e)
        {
            _cancel.Enabled = false;
            _label.Text = "Cancelling…";
            _cts.Cancel();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // If the window is closed while a download is still running, cancel it.
            if (!_cts.IsCancellationRequested)
            {
                _cts.Cancel();
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cts.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
