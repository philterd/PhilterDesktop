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

using System.Diagnostics;

namespace PhilterDesktop
{
    /// <summary>
    /// Builds links to Philterd's other offerings — <b>Philter</b> (server/API, for pipeline/at-scale
    /// redaction) and <b>policy consulting</b> — tagged with the in-app surface that produced the click
    /// (<c>utm_medium</c>), so the team can see which touchpoints actually feed the funnel.
    /// </summary>
    internal static class Upsell
    {
        private const string PhilterBase = "https://www.philterd.ai/philter";
        private const string ConsultingBase = "https://www.philterd.ai/consulting";

        /// <summary>Philter (server/API) landing page, tagged with the calling surface.</summary>
        public static string PhilterUrl(string medium) => Tag(PhilterBase, medium);

        /// <summary>Policy-consulting landing page, tagged with the calling surface.</summary>
        public static string ConsultingUrl(string medium) => Tag(ConsultingBase, medium);

        private static string Tag(string url, string medium) =>
            $"{url}?utm_source=desktop&utm_medium={Uri.EscapeDataString(medium)}";

        /// <summary>Opens a URL in the default browser (best-effort).</summary>
        public static void Open(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { /* best effort — never disrupt the app over a marketing link */ }
        }

        /// <summary>A consistent one-line link that opens the given (already-tagged) URL when clicked.</summary>
        public static LinkLabel CreateLink(string text, string url)
        {
            var link = new LinkLabel
            {
                Text = text,
                AutoSize = true,
                LinkColor = ModernTheme.Accent,
                ActiveLinkColor = ModernTheme.Accent
            };
            link.LinkClicked += (_, _) => Open(url);
            return link;
        }
    }
}
