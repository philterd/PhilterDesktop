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

using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The personal-or-work screen shown when the main window opens: who sees it, what each button does,
    /// and what it records. Uses an in-memory store so the real per-user registry is never touched.
    /// </summary>
    public sealed class UsageChoiceTests
    {
        private static readonly DateTime Now = new(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

        private sealed class InMemoryStore : IAcceptanceStore
        {
            private readonly HashSet<string> _accepted = new();
            public readonly Dictionary<string, string> Values = new();
            public bool HasAccepted(string key) => _accepted.Contains(key);
            public void RememberAccepted(string key) => _accepted.Add(key);
            public string? GetValue(string key) => Values.TryGetValue(key, out string? v) ? v : null;
            public void SetValue(string key, string value) => Values[key] = value;
        }

        private static void WithStore(Action<InMemoryStore> test)
        {
            IAcceptanceStore original = Acknowledgements.Store;
            var store = new InMemoryStore();
            Acknowledgements.Store = store;
            try { test(store); }
            finally { Acknowledgements.Store = original; }
        }

        // --- Who sees it ----------------------------------------------------------------------------

        [Fact]
        public void ShownToEveryoneWithoutALicense_EveryTime() => WithStore(store =>
        {
            Assert.True(UsageChoice.ShouldShow(licensed: false));
            UsageChoice.Record(UsageKind.Personal, Now);
            Assert.True(UsageChoice.ShouldShow(licensed: false)); // a recorded choice doesn't stop it
        });

        // A verified license means never, even if the redaction notice was never acknowledged.
        [Fact]
        public void NeverShownWithALicense() => WithStore(store =>
        {
            Assert.True(RedactionNoticeForm.ShouldShow());
            Assert.False(UsageChoice.ShouldShow(licensed: true));
        });

        [Fact]
        public void ShowIfNeeded_WithALicense_ContinuesWithoutAWindow() => WithStore(store =>
        {
            (bool proceed, UsageKind? choice) = UsageChoice.ShowIfNeeded(owner: null, licensed: true); // a dialog here would block the test
            Assert.True(proceed);
            Assert.Null(choice);
            Assert.Null(UsageChoice.Recorded());
        });

        // Only Buy a License leads on to Manage License, ready to enter the purchase.
        [Theory]
        [InlineData(UsageKind.WorkBuying, true)]
        [InlineData(UsageKind.WorkTrying, false)]
        [InlineData(UsageKind.Personal, false)]
        [InlineData(null, false)]
        public void OnlyBuying_OpensManageLicense(UsageKind? choice, bool expected) =>
            Assert.Equal(expected, UsageChoice.OpensManageLicense(choice));

        // --- When it's offered as the window opens --------------------------------------------------

        // First open of a run (at launch, or the first open of a sign-in start): offered.
        [Fact]
        public void Offered_AtTheFirstOpenOfARun() =>
            Assert.True(UsageChoice.ShouldOfferOnOpen(licensed: false, lastChosenThisRunUtc: null, Now));

        // Reopening from the tray soon after a choice: not offered; a day or more later: offered again.
        [Theory]
        [InlineData(0.01, false)]
        [InlineData(1, false)]
        [InlineData(23.9, false)]
        [InlineData(24, true)]
        [InlineData(72, true)]
        public void Offered_AgainOnlyADayAfterTheLastChoice(double hoursSinceChoice, bool expected) =>
            Assert.Equal(expected, UsageChoice.ShouldOfferOnOpen(false, Now.AddHours(-hoursSinceChoice), Now));

        // A clock moved backwards must not hide it indefinitely.
        [Fact]
        public void Offered_WhenTheLastChoiceIsInTheFuture() =>
            Assert.True(UsageChoice.ShouldOfferOnOpen(false, Now.AddHours(5), Now));

        [Theory]
        [InlineData(null)]
        [InlineData(1.0)]
        [InlineData(100.0)]
        public void NeverOffered_WithALicense(double? hoursSinceChoice) =>
            Assert.False(UsageChoice.ShouldOfferOnOpen(true, hoursSinceChoice is double h ? Now.AddHours(-h) : null, Now));

        [Fact]
        public void RepeatInterval_IsOneDay() => Assert.Equal(TimeSpan.FromDays(1), UsageChoice.RepeatInterval);

        // --- What happens after it closes ------------------------------------------------------------

        [Theory]
        [InlineData(true, UsageKind.Personal, false, UsageChoice.Next.Continue)]
        [InlineData(true, UsageKind.WorkTrying, true, UsageChoice.Next.Continue)]
        [InlineData(true, UsageKind.WorkBuying, false, UsageChoice.Next.ContinueToManageLicense)]
        [InlineData(true, UsageKind.WorkBuying, true, UsageChoice.Next.ContinueToManageLicense)]
        [InlineData(true, null, false, UsageChoice.Next.Continue)]          // licensed: nothing was shown
        [InlineData(false, null, false, UsageChoice.Next.Exit)]             // closed when opened directly
        [InlineData(false, null, true, UsageChoice.Next.BackToTray)]        // closed when opened from the tray
        internal void AfterScreen(bool chose, UsageKind? choice, bool openedFromTray, UsageChoice.Next expected) =>
            Assert.Equal(expected, UsageChoice.AfterScreen(chose, choice, openedFromTray));

        // A run that started at sign-in counts as opened from the tray even on the window's first appearance,
        // so closing the screen then keeps the app (and its watched folders) running.
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MainForm_RemembersWhetherItStartedInTheTray(bool startMinimized) => Sta(() =>
        {
            string path = Path.Combine(Path.GetTempPath(), "usage-" + Guid.NewGuid().ToString("N") + ".db");
            try
            {
                using var db = new LiteDB.LiteDatabase(path);
                using var main = new MainForm(db, startMinimized);
                bool launchedToTray = (bool)typeof(MainForm)
                    .GetField("_launchedToTray", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                    .GetValue(main)!;
                Assert.Equal(startMinimized, launchedToTray);
            }
            finally { try { File.Delete(path); } catch { /* best effort */ } }
        });

        // --- What it records -------------------------------------------------------------------------

        [Theory]
        [InlineData(UsageKind.Personal, "personal")]
        [InlineData(UsageKind.WorkTrying, "work-trying")]
        [InlineData(UsageKind.WorkBuying, "work-buying")]
        public void Record_StoresTheChoiceAndTime_AndAcknowledgesTheNotice(UsageKind kind, string token) => WithStore(store =>
        {
            UsageChoice.Record(kind, Now);
            Assert.Equal(token, store.Values[UsageChoice.ChoiceKey]);
            Assert.Equal(kind, UsageChoice.Recorded());
            Assert.Equal(Now, UsageChoice.RecordedAtUtc());
            Assert.False(RedactionNoticeForm.ShouldShow()); // choosing acknowledges the notice
        });

        [Fact]
        public void Record_TheLatestChoiceWins() => WithStore(store =>
        {
            UsageChoice.Record(UsageKind.WorkTrying, Now);
            UsageChoice.Record(UsageKind.WorkBuying, Now.AddDays(3));
            Assert.Equal(UsageKind.WorkBuying, UsageChoice.Recorded());
            Assert.Equal(Now.AddDays(3), UsageChoice.RecordedAtUtc());
        });

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Personal")]   // tokens are exact
        [InlineData("business")]
        public void Recorded_IgnoresUnknownValues(string? value) => WithStore(store =>
        {
            if (value is not null)
            {
                store.Values[UsageChoice.ChoiceKey] = value;
            }
            Assert.Null(UsageChoice.Recorded());
        });

        [Fact]
        public void Keys_AreDistinctFromTheOtherAcknowledgements()
        {
            string[] keys = { UsageChoice.ChoiceKey, UsageChoice.ChoiceTimeKey, Acknowledgements.LicenseKey,
                              Acknowledgements.LicenseVersionKey, Acknowledgements.RedactionNoticeKey };
            Assert.Equal(keys.Length, keys.Distinct().Count());
        }

        // --- The screen ------------------------------------------------------------------------------

        [Fact]
        public void Screen_ShowsLicensingAndTheNotice_WithPersonalAndWork() => Sta(() =>
        {
            using var form = new UsageChoiceForm(_ => { });
            _ = form.Handle;
            string[] texts = All(form).Select(c => c.Text ?? string.Empty).ToArray();
            Assert.Contains(UsageChoiceForm.LicensingText, texts);
            Assert.Contains(RedactionNoticeForm.NoticeText, texts);
            Assert.Contains(UsageChoiceForm.ConfirmText, texts);
            Assert.Equal(UsageChoiceForm.ConfirmText, form.FooterText);
            Assert.False(form.ShowingWorkChoices);
            Assert.Null(form.Choice);
            Assert.Null(form.AcceptButton); // a deliberate choice: Enter doesn't pick one
            Assert.Null(form.CancelButton);
        });

        [Fact]
        public void Personal_ChoosesAndCloses() => Sta(() =>
        {
            using var form = new UsageChoiceForm(_ => { });
            Click(form, "&Personal Use");
            Assert.Equal(UsageKind.Personal, form.Choice);
            Assert.Equal(DialogResult.OK, form.DialogResult);
        });

        [Fact]
        public void Work_ShowsTryingAndBuy_WithoutClosing() => Sta(() =>
        {
            using var form = new UsageChoiceForm(_ => { });
            Click(form, "&Work Use");
            Assert.True(form.ShowingWorkChoices);
            Assert.Equal(UsageChoiceForm.WorkText, form.FooterText);
            Assert.Null(form.Choice);
            Assert.NotEqual(DialogResult.OK, form.DialogResult);
        });

        [Fact]
        public void Trying_ChoosesWorkTrying_AndOpensNothing() => Sta(() =>
        {
            var opened = new List<string>();
            using var form = new UsageChoiceForm(opened.Add);
            Click(form, "&Work Use");
            Click(form, "I'm &Trying It Out");
            Assert.Equal(UsageKind.WorkTrying, form.Choice);
            Assert.Equal(DialogResult.OK, form.DialogResult);
            Assert.Empty(opened);
        });

        [Fact]
        public void Buy_OpensTheBuyPage_AndContinues() => Sta(() =>
        {
            var opened = new List<string>();
            using var form = new UsageChoiceForm(opened.Add);
            Click(form, "&Work Use");
            Click(form, "&Buy a License");
            Assert.Equal(UsageKind.WorkBuying, form.Choice);
            Assert.Equal(DialogResult.OK, form.DialogResult);
            string url = Assert.Single(opened);
            Assert.StartsWith("https://philterd.ai/philter-desktop/buy/", url);
            Assert.Contains("utm_medium=first-run", url);
        });

        [Fact]
        public void Text_HasNoEmDashes()
        {
            foreach (string text in new[]
                     {
                         UsageChoiceForm.LicensingHeading, UsageChoiceForm.LicensingText, UsageChoiceForm.NoticeHeading,
                         UsageChoiceForm.ConfirmText, UsageChoiceForm.WorkText, RedactionNoticeForm.NoticeText
                     })
            {
                Assert.DoesNotContain('—', text);
            }
        }

        // The notice's own heading (still used from the status-bar link) lost its em-dash too.
        [Fact]
        public void RedactionNoticeHeading_HasNoEmDash() => Sta(() =>
        {
            using var notice = new RedactionNoticeForm(viewOnly: true);
            Assert.DoesNotContain(All(notice), c => (c.Text ?? string.Empty).Contains('—'));
        });

        // --- Helpers ---------------------------------------------------------------------------------

        // PerformClick does nothing on a window that isn't shown, so raise Click the way the button would.
        private static void Click(Form form, string text)
        {
            Button button = All(form).OfType<Button>().Single(b => b.Text == text);
            typeof(Control).GetMethod("OnClick", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
                .Invoke(button, new object[] { EventArgs.Empty });
        }

        private static IEnumerable<Control> All(Control root) =>
            root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(All(c)));

        private static void Sta(Action action)
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
            })
            { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            captured?.Throw();
        }
    }
}
