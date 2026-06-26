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
    /// Lets the user pick a built-in starting-point policy. Shows each template's description and a
    /// conspicuous point-of-use disclaimer that templates are starting points, not compliance guarantees.
    /// </summary>
    public partial class TemplatePickerForm : Form
    {
        /// <summary>The chosen template, or null if cancelled.</summary>
        internal PolicyTemplate? SelectedTemplate => _list.SelectedItem as PolicyTemplate;

        public TemplatePickerForm()
        {
            InitializeComponent();

            _disclaimer.Text = PolicyTemplates.Disclaimer;
            _list.DisplayMember = nameof(PolicyTemplate.Name);
            _list.DataSource = PolicyTemplates.All.ToList();
            _list.SelectedIndexChanged += (_, _) =>
                _description.Text = SelectedTemplate?.Description ?? string.Empty;
            _description.Text = SelectedTemplate?.Description ?? string.Empty;

            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_ok);
        }

        private void TemplatePickerForm_Load(object sender, EventArgs e)
        {

        }
    }
}
