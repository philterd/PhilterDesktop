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

using System.Globalization;

namespace PhilterDesktop
{
    /// <summary>How the person said they'd use Philter Desktop, on the first-run screen.</summary>
    public enum UsageKind
    {
        Personal,

        /// <summary>Work use, trying it out first (evaluation is free).</summary>
        WorkTrying,

        /// <summary>Work use, and chose to buy a license.</summary>
        WorkBuying
    }

    /// <summary>
    /// The first-run screen's choice (personal or work use), which also acknowledges the redaction notice.
    /// Kept with the other first-run acknowledgements in the per-user registry store, since the gate runs
    /// before the database is unlocked.
    /// </summary>
    internal static class UsageChoice
    {
        internal const string ChoiceKey = "UsageChoice";
        internal const string ChoiceTimeKey = "UsageChoiceUtc";

        private static readonly IReadOnlyDictionary<UsageKind, string> Tokens = new Dictionary<UsageKind, string>
        {
            [UsageKind.Personal] = "personal",
            [UsageKind.WorkTrying] = "work-trying",
            [UsageKind.WorkBuying] = "work-buying"
        };

        /// <summary>The recorded choice, or null if none (or an unrecognized value).</summary>
        public static UsageKind? Recorded()
        {
            string? token = Acknowledgements.Store.GetValue(ChoiceKey);
            foreach ((UsageKind kind, string value) in Tokens)
            {
                if (string.Equals(token, value, StringComparison.Ordinal))
                {
                    return kind;
                }
            }
            return null;
        }

        /// <summary>When the choice was recorded, or null.</summary>
        public static DateTime? RecordedAtUtc() =>
            DateTime.TryParse(Acknowledgements.Store.GetValue(ChoiceTimeKey), CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime t) ? t : null;

        /// <summary>Never shown to someone with a verified, current work license.</summary>
        public static bool ShouldShow(bool licensed) => !licensed;

        /// <summary>
        /// How long a choice lasts while the app keeps running: a run that stays open (for example in the
        /// tray, watching folders) is asked again at the next window open after this long.
        /// </summary>
        internal static readonly TimeSpan RepeatInterval = TimeSpan.FromDays(1);

        /// <summary>
        /// Whether to offer the screen as the window opens: without a license, once per run (the first open
        /// after starting, even from the tray) and again at the first open a day or more after the last
        /// choice. <paramref name="lastChosenThisRunUtc"/> is when a choice was last made in this run, or null;
        /// closing without choosing doesn't count, so the screen returns at the next open.
        /// </summary>
        internal static bool ShouldOfferOnOpen(bool licensed, DateTime? lastChosenThisRunUtc, DateTime nowUtc) =>
            ShouldShow(licensed) &&
            (lastChosenThisRunUtc is not DateTime last || last > nowUtc || nowUtc - last >= RepeatInterval);

        /// <summary>Records the choice and, with it, the acknowledgement of the redaction notice.</summary>
        public static void Record(UsageKind kind, DateTime nowUtc)
        {
            Acknowledgements.Store.SetValue(ChoiceKey, Tokens[kind]);
            Acknowledgements.Store.SetValue(ChoiceTimeKey, nowUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
            RedactionNoticeForm.RememberAccepted();
        }

        /// <summary>
        /// Shows the screen if it's owed. <c>Continue</c> is true when a choice was made or nothing was owed,
        /// and false if the person closed it without choosing; <c>Choice</c> is what they picked, if anything.
        /// </summary>
        public static (bool Continue, UsageKind? Choice) ShowIfNeeded(IWin32Window? owner, bool licensed)
        {
            if (!ShouldShow(licensed))
            {
                return (true, null);
            }
            using var form = new UsageChoiceForm();
            if (form.ShowDialog(owner) != DialogResult.OK || form.Choice is not UsageKind choice)
            {
                return (false, null);
            }
            Record(choice, DateTime.UtcNow);
            return (true, choice);
        }

        /// <summary>After Buy a License, Manage License opens so the purchase can be entered.</summary>
        public static bool OpensManageLicense(UsageKind? choice) => choice == UsageKind.WorkBuying;

        /// <summary>What the main window does once the screen closes.</summary>
        internal enum Next
        {
            Continue,
            ContinueToManageLicense,

            /// <summary>Closed without a choice when the app was opened directly: quit.</summary>
            Exit,

            /// <summary>Closed without a choice when the window was opened from the tray: back to the tray.</summary>
            BackToTray
        }

        /// <param name="openedFromTray">
        /// True when the window was opened from the tray, including the first open of a run that started
        /// there at sign-in, so watched folders keep running.
        /// </param>
        internal static Next AfterScreen(bool chose, UsageKind? choice, bool openedFromTray) =>
            chose ? (OpensManageLicense(choice) ? Next.ContinueToManageLicense : Next.Continue)
                  : (openedFromTray ? Next.BackToTray : Next.Exit);
    }
}
