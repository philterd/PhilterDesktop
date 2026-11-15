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

using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Windows.Forms;
using LiteDB;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// When automatic update checks run (first window open, and a background timer for a tray-resident app
    /// such as one watching folders) and how a found update is announced (dialog with the window open, a
    /// tray notification without it).
    /// </summary>
    public sealed class UpdateCheckScheduleTests
    {
        private static readonly DateTime Now = new(2026, 11, 15, 12, 0, 0, DateTimeKind.Utc);
        private const System.Reflection.BindingFlags Flags =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

        // --- When to check -------------------------------------------------------------

        [Fact]
        public void WindowOpened_ChecksWhenNothingHasSucceededThisRun() =>
            Assert.True(UpdateChecker.ShouldCheck(UpdateChecker.Trigger.WindowOpened, null, Now));

        // A background check that already succeeded this run (e.g. while in the tray) isn't repeated just
        // because the window opened; any update it found is shown then instead.
        [Theory]
        [InlineData(0.1)]
        [InlineData(5)]
        [InlineData(30)]
        public void WindowOpened_SkipsAfterASuccessThisRun(double hoursAgo) =>
            Assert.False(UpdateChecker.ShouldCheck(UpdateChecker.Trigger.WindowOpened, Now.AddHours(-hoursAgo), Now));

        [Fact]
        public void Background_ChecksWhenNothingHasSucceeded() =>
            Assert.True(UpdateChecker.ShouldCheck(UpdateChecker.Trigger.Background, null, Now));

        [Theory]
        [InlineData(1, false)]
        [InlineData(23.9, false)]
        [InlineData(24, true)]
        [InlineData(72, true)]
        public void Background_ContactsPhilterdAtMostDaily(double hoursAgo, bool expected) =>
            Assert.Equal(expected, UpdateChecker.ShouldCheck(UpdateChecker.Trigger.Background, Now.AddHours(-hoursAgo), Now));

        // A clock moved backwards must not suppress checks for days.
        [Fact]
        public void Background_ChecksWhenTheLastSuccessIsInTheFuture() =>
            Assert.True(UpdateChecker.ShouldCheck(UpdateChecker.Trigger.Background, Now.AddDays(2), Now));

        [Fact]
        public void Timings_GiveTheNetworkTimeAndRetryFailures()
        {
            // The first background check waits for a sign-in start's network; failures retry hourly; a success
            // holds for a day.
            Assert.Equal(TimeSpan.FromMinutes(5), UpdateChecker.BackgroundFirstDelay);
            Assert.Equal(TimeSpan.FromHours(1), UpdateChecker.BackgroundRetryInterval);
            Assert.Equal(TimeSpan.FromHours(24), UpdateChecker.BackgroundCheckInterval);
            Assert.True(UpdateChecker.BackgroundRetryInterval < UpdateChecker.BackgroundCheckInterval);
            // Timer intervals are ints of milliseconds.
            Assert.True(UpdateChecker.BackgroundRetryInterval.TotalMilliseconds < int.MaxValue);
        }

        // --- Recording a successful check ------------------------------------------------

        [Fact]
        public async Task Reaching_PhilterdIsReported_EvenWithNothingNew()
        {
            int reached = 0;
            await UpdateChecker.RunAutoCheckAsync(new FakeUpdatePreferences(),
                _ => Task.FromResult<UpdateChecker.UpdateManifest?>(new UpdateChecker.UpdateManifest { Version = "1.0.0" }),
                new Version(1, 4, 0), onReached: () => reached++);
            Assert.Equal(1, reached);
        }

        // Only a real answer counts: failures stay "not reached" so the hourly retry tries again.
        [Fact]
        public async Task Failure_IsNotReportedAsReached()
        {
            int reached = 0;
            await UpdateChecker.RunAutoCheckAsync(new FakeUpdatePreferences(),
                _ => throw new HttpRequestException("offline"),
                new Version(1, 4, 0), onReached: () => reached++);
            await UpdateChecker.RunAutoCheckAsync(new FakeUpdatePreferences(),
                _ => Task.FromResult<UpdateChecker.UpdateManifest?>(new UpdateChecker.UpdateManifest { Version = "" }),
                new Version(1, 4, 0), onReached: () => reached++);
            Assert.Equal(0, reached);
        }

        [Fact]
        public async Task TurnedOff_IsNotReportedAsReached()
        {
            int reached = 0;
            await UpdateChecker.RunAutoCheckAsync(new FakeUpdatePreferences { AutoCheckEnabled = false },
                _ => Task.FromResult<UpdateChecker.UpdateManifest?>(new UpdateChecker.UpdateManifest { Version = "1.5.0" }),
                new Version(1, 4, 0), onReached: () => reached++);
            Assert.Equal(0, reached);
        }

        [Fact]
        public async Task ThrowingReachedCallback_DoesNotHideTheUpdate()
        {
            UpdateChecker.UpdateManifest? m = await UpdateChecker.RunAutoCheckAsync(new FakeUpdatePreferences(),
                _ => Task.FromResult<UpdateChecker.UpdateManifest?>(new UpdateChecker.UpdateManifest { Version = "1.5.0" }),
                new Version(1, 4, 0), onReached: () => throw new InvalidOperationException());
            Assert.Equal("1.5.0", m?.Version);
        }

        // --- Tray notification ----------------------------------------------------------

        [Theory]
        [InlineData("1.5.0", null, true)]
        [InlineData("1.5.0", "1.4.1", true)]
        [InlineData("1.5.0", "1.5.0", false)]   // already shown this run
        [InlineData(" 1.5.0 ", "1.5.0", false)]
        [InlineData(null, null, false)]
        [InlineData("", null, false)]
        public void TrayNotification_AtMostOncePerRunPerVersion(string? version, string? shown, bool expected) =>
            Assert.Equal(expected, UpdateChecker.ShouldShowTrayNotification(version, shown));

        [Fact]
        public void TrayNotification_NamesTheVersion()
        {
            (string title, string text) = UpdateChecker.TrayNotification(" 1.5.0 ");
            Assert.Equal("Philter Desktop 1.5.0 is available", title);
            Assert.Contains("Click", text);
            Assert.DoesNotContain('—', title + text);
            // Windows truncates balloon titles past 63 characters and text past 255.
            Assert.True(title.Length <= 63 && text.Length <= 255);
        }

        // Without the window, the update is announced in the tray, and the version is NOT marked as seen:
        // the dialog still appears when the window opens, and a missed notification returns next run.
        [Fact]
        public void Hidden_AnnouncesInTheTray_WithoutMarkingTheVersionSeen() => WithHiddenMainForm((form, prefs) =>
        {
            var manifest = new UpdateChecker.UpdateManifest { Version = "1.5.0" };
            Invoke(form, "AnnounceUpdate", manifest);

            Assert.Null(prefs.LastNotifiedVersion);
            Assert.Equal("1.5.0", Get<string>(form, "_balloonedUpdateVersion"));
            Assert.True(Get<bool>(form, "_updateBalloonShowing"));
            NotifyIcon tray = Get<NotifyIcon>(form, "_trayIcon");
            Assert.Equal("Philter Desktop 1.5.0 is available", tray.BalloonTipTitle);
        });

        [Fact]
        public void Hidden_DoesNotRepeatTheTrayNotificationInTheSameRun() => WithHiddenMainForm((form, _) =>
        {
            var manifest = new UpdateChecker.UpdateManifest { Version = "1.5.0" };
            Invoke(form, "AnnounceUpdate", manifest);
            NotifyIcon tray = Get<NotifyIcon>(form, "_trayIcon");
            tray.BalloonTipTitle = "something else since";
            Set(form, "_updateBalloonShowing", false);

            Invoke(form, "AnnounceUpdate", manifest); // e.g. the next day's background check
            Assert.Equal("something else since", tray.BalloonTipTitle);
            Assert.False(Get<bool>(form, "_updateBalloonShowing"));
        });

        // Clicking the update notification opens the window (where the pending dialog then appears), rather
        // than the folder of the last redacted file.
        [Fact]
        public void ClickingTheUpdateNotification_OpensTheWindow() => WithHiddenMainForm((form, _) =>
        {
            Invoke(form, "AnnounceUpdate", new UpdateChecker.UpdateManifest { Version = "1.5.0" });
            Assert.False(form.Visible);

            Invoke(form, "OnTrayBalloonClicked");

            Assert.True(form.Visible);
            Assert.False(Get<bool>(form, "_updateBalloonShowing"));
        });

        [Fact]
        public void ClickingARedactionNotification_DoesNotOpenTheWindow() => WithHiddenMainForm((form, _) =>
        {
            Invoke(form, "OnTrayBalloonClicked"); // no update balloon: falls through to the folder handler
            Assert.False(form.Visible);
        });

        // Outside the real GUI (as in tests) no background timer is created, so nothing touches the network.
        [Fact]
        public void NoBackgroundTimer_OutsideTheRealGui() => WithHiddenMainForm((form, _) =>
            Assert.Null(Get<System.Windows.Forms.Timer?>(form, "_updateCheckTimer")));

        [Fact]
        public void UpdateCheckWording_DescribesBothTriggers()
        {
            Assert.Contains("when you open Philter Desktop", SettingsForm.AutoUpdateHintText);
            Assert.Contains("once a day", SettingsForm.AutoUpdateHintText);
            Assert.Contains("only the version number", SettingsForm.AutoUpdateHintText);
            Assert.Contains("Help, then Check for Updates", SettingsForm.AutoUpdateHintText);
            Assert.True(SettingsForm.AutoUpdateHintText.Length <= 200, "keep the hint short");
            Assert.DoesNotContain('—', SettingsForm.AutoUpdateHintText);
        }

        // --- Helpers ----------------------------------------------------------------------

        private static void WithHiddenMainForm(Action<MainForm, FakeUpdatePreferences> test)
        {
            IUpdatePreferences original = UpdatePreferences.Current;
            var prefs = new FakeUpdatePreferences();
            UpdatePreferences.Current = prefs;
            try
            {
                Sta(() =>
                {
                    string path = Path.Combine(Path.GetTempPath(), "sched-" + Guid.NewGuid().ToString("N") + ".db");
                    try
                    {
                        using var db = new LiteDatabase(path);
                        using var form = new MainForm(db, startMinimized: false);
                        _ = form.Handle;
                        // A hidden tray icon makes ShowBalloonTip a no-op, so tests don't pop real notifications.
                        Get<NotifyIcon>(form, "_trayIcon").Visible = false;
                        try
                        {
                            test(form, prefs);
                        }
                        finally
                        {
                            Get<NotifyIcon>(form, "_trayIcon").Visible = false;
                        }
                    }
                    finally { try { File.Delete(path); } catch { /* best effort */ } }
                });
            }
            finally
            {
                UpdatePreferences.Current = original;
            }
        }

        private static T Get<T>(object o, string name) => (T)typeof(MainForm).GetField(name, Flags)!.GetValue(o)!;

        private static void Set(object o, string name, object value) => typeof(MainForm).GetField(name, Flags)!.SetValue(o, value);

        private static void Invoke(object o, string method, params object[] args) =>
            typeof(MainForm).GetMethod(method, Flags)!.Invoke(o, args);

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
