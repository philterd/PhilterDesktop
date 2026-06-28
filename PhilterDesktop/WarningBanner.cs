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
    /// A full-width amber warning strip docked at the top of a form — used to surface a safety-relevant
    /// condition (such as the on-device name model being unavailable) prominently and persistently,
    /// rather than letting it pass silently.
    /// </summary>
    internal static class WarningBanner
    {
        private static readonly Color BackColor = Color.FromArgb(255, 244, 206);  // soft amber
        private static readonly Color ForeColor = Color.FromArgb(124, 77, 0);     // dark amber text

        /// <summary>Creates a top-docked warning strip (not yet added to any form).</summary>
        public static Panel Create(string text)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                BackColor = BackColor,
                Padding = new Padding(10, 8, 10, 8),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            var label = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Text = "⚠  " + text, // ⚠ + message
                ForeColor = ForeColor,
                Font = new Font(ModernTheme.UiFont, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Height = 38
            };

            panel.Controls.Add(label);
            return panel;
        }

        /// <summary>
        /// Adds a top-docked warning strip to <paramref name="form"/>. Added last so it docks at the very
        /// top, above the form's other content.
        /// </summary>
        public static void AddTo(Form form, string text)
        {
            Panel panel = Create(text);
            form.Controls.Add(panel);
            panel.BringToFront();
        }
    }
}
