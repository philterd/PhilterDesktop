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

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// Edits a policy's "always ignore" terms — text that is never redacted even when a filter
    /// detects it. One term per line; matching is whole-value (a detected entity equal to a term is
    /// kept), case-insensitive unless <see cref="CaseSensitive"/> is set.
    /// </summary>
    public partial class IgnoredTermsForm : Form
    {
        /// <summary>The terms to ignore (valid after the dialog returns <see cref="DialogResult.OK"/>).</summary>
        public IReadOnlyList<string> Terms { get; private set; } = Array.Empty<string>();

        /// <summary>Whether matching is case-sensitive.</summary>
        public bool CaseSensitive { get; private set; }

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public IgnoredTermsForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
        }

        public IgnoredTermsForm(IEnumerable<string> terms, bool caseSensitive)
            : this()
        {
            termsTextBox.Text = string.Join(Environment.NewLine, terms ?? Array.Empty<string>());
            caseSensitiveCheckBox.Checked = caseSensitive;
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            Terms = ParseTerms(termsTextBox.Text);
            CaseSensitive = caseSensitiveCheckBox.Checked;
        }

        // One term per line; trims whitespace, drops blanks, and removes case-insensitive duplicates
        // while preserving order.
        private static List<string> ParseTerms(string text)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string line in text.Split('\n'))
            {
                string term = line.Trim();
                if (term.Length > 0 && seen.Add(term))
                {
                    result.Add(term);
                }
            }
            return result;
        }
    }
}
