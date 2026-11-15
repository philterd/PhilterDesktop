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
using System.Text.Json;
using System.Windows.Forms;
using LiteDB;
using PhilterData;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>In-memory update preferences, so tests never touch the registry.</summary>
    internal sealed class FakeUpdatePreferences : IUpdatePreferences
    {
        public bool AutoCheckEnabled { get; set; } = true;
        public bool DisabledByPolicy { get; set; }
        public string? LastNotifiedVersion { get; set; }
        public int Writes { get; private set; }

        bool IUpdatePreferences.AutoCheckEnabled
        {
            get => AutoCheckEnabled;
            set { AutoCheckEnabled = value; Writes++; }
        }
    }

    /// <summary>The automatic update check: when it runs, what it announces, and how failures stay silent.</summary>
    public sealed class AutoUpdateCheckTests
    {
        private static readonly Version Current = new(1, 4, 0);

        private static Func<CancellationToken, Task<UpdateChecker.UpdateManifest?>> Returns(
            string? version, Action? onCall = null) => _ =>
        {
            onCall?.Invoke();
            return Task.FromResult<UpdateChecker.UpdateManifest?>(
                new UpdateChecker.UpdateManifest { Version = version });
        };

        private static Task<UpdateChecker.UpdateManifest?> Run(
            FakeUpdatePreferences prefs, Func<CancellationToken, Task<UpdateChecker.UpdateManifest?>> fetch) =>
            UpdateChecker.RunAutoCheckAsync(prefs, fetch, Current);

        // --- What gets announced ---------------------------------------------

        [Theory]
        [InlineData("1.5.0", null, true)]       // newer, never announced
        [InlineData("1.5.0", "1.4.1", true)]    // newer than the last one announced
        [InlineData("1.5.0", "1.5.0", false)]   // already announced
        [InlineData(" 1.5.0 ", "1.5.0", false)] // whitespace doesn't make it new
        [InlineData("1.4.0", null, false)]      // same as installed
        [InlineData("1.3.1", null, false)]      // older than installed
        [InlineData("garbage", null, false)]    // unparseable
        [InlineData(null, null, false)]
        public void ShouldNotify(string? published, string? lastNotified, bool expected) =>
            Assert.Equal(expected, UpdateChecker.ShouldNotify(published, Current, lastNotified));

        // --- The check itself -------------------------------------------------

        [Fact]
        public async Task NewerVersion_IsReturned()
        {
            var prefs = new FakeUpdatePreferences();
            UpdateChecker.UpdateManifest? m = await Run(prefs, Returns("1.5.0"));
            Assert.Equal("1.5.0", m?.Version);
        }

        // Every launch checks; there is no once-a-day limit.
        [Fact]
        public async Task EachRun_ContactsTheServer()
        {
            int calls = 0;
            var prefs = new FakeUpdatePreferences();
            await Run(prefs, Returns("1.4.0", onCall: () => calls++));
            await Run(prefs, Returns("1.4.0", onCall: () => calls++));
            Assert.Equal(2, calls);
        }

        [Fact]
        public async Task TurnedOff_DoesNotContactTheServer()
        {
            int calls = 0;
            var prefs = new FakeUpdatePreferences { AutoCheckEnabled = false };
            Assert.Null(await Run(prefs, Returns("1.5.0", onCall: () => calls++)));
            Assert.Equal(0, calls);
        }

        [Fact]
        public async Task DisabledByPolicy_DoesNotContactTheServer_EvenIfTheUserWantsChecks()
        {
            int calls = 0;
            var prefs = new FakeUpdatePreferences { AutoCheckEnabled = true, DisabledByPolicy = true };
            Assert.Null(await Run(prefs, Returns("1.5.0", onCall: () => calls++)));
            Assert.Equal(0, calls);
        }

        // Offline or blocked: nothing is shown, and the next launch tries again.
        [Fact]
        public async Task NetworkFailure_IsSilent()
        {
            var prefs = new FakeUpdatePreferences();
            Assert.Null(await Run(prefs, _ => throw new HttpRequestException("offline")));
            Assert.Null(prefs.LastNotifiedVersion);
        }

        [Fact]
        public async Task FaultedTask_IsSilent()
        {
            var prefs = new FakeUpdatePreferences();
            Assert.Null(await Run(prefs, _ => Task.FromException<UpdateChecker.UpdateManifest?>(new JsonException("bad json"))));
        }

        [Fact]
        public async Task EmptyManifest_IsIgnored()
        {
            var prefs = new FakeUpdatePreferences();
            Assert.Null(await Run(prefs, _ => Task.FromResult<UpdateChecker.UpdateManifest?>(null)));
            Assert.Null(await Run(prefs, Returns("  ")));
        }

        [Fact]
        public async Task SameVersion_IsNotAnnounced()
        {
            var prefs = new FakeUpdatePreferences();
            Assert.Null(await Run(prefs, Returns("1.4.0")));
        }

        [Fact]
        public async Task AlreadyAnnouncedVersion_IsNotAnnouncedAgain()
        {
            var prefs = new FakeUpdatePreferences { LastNotifiedVersion = "1.5.0" };
            Assert.Null(await Run(prefs, Returns("1.5.0")));
            Assert.NotNull(await Run(new FakeUpdatePreferences { LastNotifiedVersion = "1.5.0" }, Returns("1.6.0")));
        }

        [Fact]
        public async Task TheCheckDoesNotRecordTheAnnouncement()
        {
            // The caller records LastNotifiedVersion only when it actually shows the dialog, so an update
            // found while the window is hidden in the tray is still announced later.
            var prefs = new FakeUpdatePreferences();
            Assert.NotNull(await Run(prefs, Returns("1.5.0")));
            Assert.Null(prefs.LastNotifiedVersion);
        }

        // --- What the check sends ----------------------------------------------

        // The version is the only thing the request carries; the docs promise exactly this.
        [Theory]
        [InlineData("1.4.0", "PhilterDesktop/1.4.0")]
        [InlineData("1.4", "PhilterDesktop/1.4.0")]        // two-part versions don't throw
        [InlineData("1.4.0.7", "PhilterDesktop/1.4.0")]    // revision dropped
        [InlineData("10.20.30", "PhilterDesktop/10.20.30")]
        public void UserAgent_CarriesOnlyTheAppVersion(string version, string expected) =>
            Assert.Equal(expected, UpdateChecker.UserAgent(Version.Parse(version)));

        // HttpClient rejects malformed product tokens, which would make every check fail silently.
        [Fact]
        public void UserAgent_IsAValidProductToken()
        {
            using var http = new HttpClient();
            Assert.True(http.DefaultRequestHeaders.UserAgent.TryParseAdd(UpdateChecker.UserAgent(UpdateChecker.CurrentVersion())));
            var product = Assert.Single(http.DefaultRequestHeaders.UserAgent).Product!;
            Assert.Equal("PhilterDesktop", product.Name);
            Assert.Equal(UpdateChecker.CurrentVersion().ToString(3), product.Version);
        }

        // --- The manifest -----------------------------------------------------

        // The published manifest is just the version and release date; anything else in it is ignored.
        [Fact]
        public void Manifest_Parses_AndIgnoresUnknownFields()
        {
            const string json = "{ \"version\": \"1.3.1\", \"releaseDate\": \"2026-11-12\", \"eulaVersion\": \"2.0\" }";
            var m = System.Text.Json.JsonSerializer.Deserialize<UpdateChecker.UpdateManifest>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.Equal("1.3.1", m!.Version);
            Assert.Equal("2026-11-12", m.ReleaseDate);
        }

        // --- The "update available" dialog -------------------------------------

        [Fact]
        public void UpdateMessage_ShowsBothVersions_AndNothingAboutLicenseTerms()
        {
            string message = UpdateAvailableForm.BuildMessage("1.4.0", "1.5.0", "2026-12-01");
            Assert.Contains("Installed version: 1.4.0", message);
            Assert.Contains("Latest version: 1.5.0  (released 2026-12-01)", message);
            Assert.DoesNotContain("license", message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void UpdateMessage_OmitsAMissingReleaseDate() =>
            Assert.DoesNotContain("released", UpdateAvailableForm.BuildMessage("1.4.0", "1.5.0", null));

        // Free users have no account page; the public download page works for everyone.
        [Fact]
        public void UpdateDialog_LinksToThePublicDownloadPage() => Sta(() =>
        {
            Assert.Equal("https://philterd.ai/philter-desktop/download/", UpdateAvailableForm.DownloadUrl);
            using var f = new UpdateAvailableForm("1.4.0", "1.5.0", "2026-12-01");
            _ = f.Handle;
            var link = (LinkLabel)typeof(UpdateAvailableForm).GetField("_link",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(f)!;
            Assert.Equal(UpdateAvailableForm.DownloadUrl, link.Text);
        });

        // --- The license dialog doesn't offer the choice (it lives in Settings) --------

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void LicenseDialog_HasNoUpdateCheckChoice(bool viewOnly) => Sta(() =>
        {
            using var f = new LicenseForm(viewOnly);
            Assert.Empty(AllControls(f).OfType<CheckBox>());
        });

        // Agreeing records only the acceptance; it never changes the update preference.
        [Fact]
        public void LicenseGate_LeavesTheUpdatePreferenceAlone() => WithPrefs(new FakeUpdatePreferences(), prefs =>
        {
            Assert.DoesNotContain(typeof(LicenseForm).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static),
                m => m.Name.Contains("AutoUpdate", StringComparison.Ordinal));
            Assert.Equal(0, prefs.Writes);
        });

        private static IEnumerable<Control> AllControls(Control root) =>
            root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(AllControls(c)));

        // --- Settings ------------------------------------------------------------

        [Fact]
        public void Settings_ShowsAndSavesTheChoice() => WithPrefs(new FakeUpdatePreferences(), prefs => WithSettingsForm(form =>
        {
            CheckBox chk = Field<CheckBox>(form, "chkAutoUpdateCheck");
            Invoke(form, "LoadUpdateCheckSetting");
            Assert.True(chk.Checked);
            Assert.True(chk.Enabled);
            Assert.Equal(SettingsForm.AutoUpdateHintText, Field<Label>(form, "lblAutoUpdateHint").Text);

            chk.Checked = false;
            Invoke(form, "SaveUpdateCheckSetting");
            Assert.False(prefs.AutoCheckEnabled);
            Assert.Equal(1, prefs.Writes);
        }));

        // Saving other settings must not write (and so pin) the update preference.
        [Fact]
        public void Settings_DoesNotWriteAnUnchangedChoice() => WithPrefs(new FakeUpdatePreferences(), prefs => WithSettingsForm(form =>
        {
            Invoke(form, "LoadUpdateCheckSetting");
            Invoke(form, "SaveUpdateCheckSetting");
            Assert.Equal(0, prefs.Writes);
        }));

        [Fact]
        public void Settings_LocksTheChoice_WhenPolicyDisablesChecks() => WithPrefs(new FakeUpdatePreferences { AutoCheckEnabled = true, DisabledByPolicy = true }, prefs => WithSettingsForm(form =>
        {
            CheckBox chk = Field<CheckBox>(form, "chkAutoUpdateCheck");
            Invoke(form, "LoadUpdateCheckSetting");
            Assert.False(chk.Checked);
            Assert.False(chk.Enabled);
            Assert.Equal(SettingsForm.AutoUpdatePolicyHintText, Field<Label>(form, "lblAutoUpdateHint").Text);

            chk.Checked = true; // even if forced, nothing is written under policy
            Invoke(form, "SaveUpdateCheckSetting");
            Assert.Equal(0, prefs.Writes);
        }));

        [Fact]
        public void Settings_HintText_HasNoEmDashes()
        {
            Assert.DoesNotContain('—', SettingsForm.AutoUpdateHintText);
            Assert.DoesNotContain('—', SettingsForm.AutoUpdatePolicyHintText);
        }

        // --- Helpers ---------------------------------------------------------------

        private static void WithPrefs(FakeUpdatePreferences prefs, Action<FakeUpdatePreferences> test)
        {
            IUpdatePreferences original = UpdatePreferences.Current;
            try
            {
                UpdatePreferences.Current = prefs;
                test(prefs);
            }
            finally
            {
                UpdatePreferences.Current = original;
            }
        }

        private static void WithSettingsForm(Action<SettingsForm> test) => Sta(() =>
        {
            string path = Path.Combine(Path.GetTempPath(), "upd-" + Guid.NewGuid().ToString("N") + ".db");
            try
            {
                using var db = new LiteDatabase(path);
                using var form = new SettingsForm(new SettingsRepository(db));
                test(form);
            }
            finally { try { File.Delete(path); } catch { /* best effort */ } }
        });

        private const System.Reflection.BindingFlags Flags =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

        private static T Field<T>(object o, string name) => (T)o.GetType().GetField(name, Flags)!.GetValue(o)!;

        private static void Invoke(object o, string method) => o.GetType().GetMethod(method, Flags)!.Invoke(o, null);

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
