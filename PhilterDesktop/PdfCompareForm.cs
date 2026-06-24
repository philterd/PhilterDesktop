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
    /// Side-by-side visual comparison of two PDFs (original vs. redacted). Hosts the reusable
    /// <see cref="PdfSideBySideView"/>. Because redacted PDFs are image-based, this is a visual
    /// comparison rather than a text/pixel diff.
    /// </summary>
    public partial class PdfCompareForm : Form
    {
        private readonly byte[] _before;
        private readonly byte[] _after;
        private readonly string _beforeName;
        private readonly string _afterName;

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public PdfCompareForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            _before = Array.Empty<byte>();
            _after = Array.Empty<byte>();
            _beforeName = string.Empty;
            _afterName = string.Empty;
        }

        public PdfCompareForm(byte[] beforePdf, byte[] afterPdf, string beforeName, string afterName)
            : this()
        {
            _before = beforePdf;
            _after = afterPdf;
            _beforeName = beforeName;
            _afterName = afterName;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _view.SetDocuments(_before, _after, $"Before — {_beforeName}", $"After — {_afterName}");
        }
    }
}
