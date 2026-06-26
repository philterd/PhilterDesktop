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
    /// Edits the global "always redact" and "always ignore" term lists (one term per line) that apply
    /// on top of every policy. The caller reads <see cref="AlwaysRedactText"/>/<see cref="AlwaysIgnoreText"/>
    /// after the dialog returns OK and saves them to settings.
    /// </summary>
    public partial class GlobalListsForm : Form
    {
        public string AlwaysRedactText => txtRedact.Text;
        public string AlwaysIgnoreText => txtIgnore.Text;

        /// <summary>Parameterless constructor (required by the Windows Forms designer).</summary>
        public GlobalListsForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(btnOk);
        }

        public GlobalListsForm(string alwaysRedact, string alwaysIgnore) : this()
        {
            txtRedact.Text = alwaysRedact;
            txtIgnore.Text = alwaysIgnore;
        }
    }
}
