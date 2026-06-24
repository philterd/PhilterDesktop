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
    /// Edits a policy's "always redact" terms — text that is always redacted even when no filter
    /// would otherwise detect it. One term per line; matching is case-insensitive (Phileas
    /// dictionaries do not carry a case-sensitivity flag).
    /// </summary>
    public partial class AlwaysRedactTermsForm : Form
    {
        /// <summary>The terms to always redact (valid after the dialog returns <see cref="DialogResult.OK"/>).</summary>
        public IReadOnlyList<string> Terms { get; private set; } = Array.Empty<string>();

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public AlwaysRedactTermsForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
        }

        public AlwaysRedactTermsForm(IEnumerable<string> terms)
            : this()
        {
            termsTextBox.Text = string.Join(Environment.NewLine, terms ?? Array.Empty<string>());
        }

        private void OkButton_Click(object? sender, EventArgs e)
        {
            Terms = ParseTerms(termsTextBox.Text);
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
