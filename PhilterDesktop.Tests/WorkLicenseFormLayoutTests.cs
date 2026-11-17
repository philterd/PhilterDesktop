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

using System.Reflection;
using System.Runtime.ExceptionServices;
using LiteDB;
using PhilterData;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The Manage License window's layout: a status card for this computer's license, the email and code
    /// steps (tucked behind a link while a license is current), matching step buttons, and a footer.
    /// </summary>
    public sealed class WorkLicenseFormLayoutTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly DateTime Now = new(2026, 11, 17, 12, 0, 0, DateTimeKind.Utc);

        // ---- The status card ----

        [Fact]
        public void Card_NotLicensed_IsANeutralNote()
        {
            var (glyph, color, headline, detail) = WorkLicenseForm.Card(null, null, Now);

            Assert.Equal("", glyph);
            Assert.Equal(ModernTheme.SubtleText, color);
            Assert.Equal(WorkLicenseForm.NotLicensedStatus, headline);
            Assert.Equal("Personal use and trying it out are free.", detail);
        }

        [Fact]
        public void Card_Licensed_IsAGreenCheckWithTheDetails()
        {
            LicenseInfo license = License(seats: 3, expires: "2027-11-15");
            var (glyph, color, headline, detail) = WorkLicenseForm.Card(license, license, Now);

            Assert.Equal("", glyph);
            Assert.Equal(Color.FromArgb(0x1B, 0x5E, 0x20), color);
            Assert.Equal("Licensed for work use", headline);
            Assert.Equal("jane@example.com · 3 seats · through November 15, 2027", detail);
        }

        [Fact]
        public void Card_OneSeat_LeavesSeatsOut()
        {
            LicenseInfo license = License(seats: 1, expires: "2027-11-15");
            Assert.Equal("jane@example.com · through November 15, 2027", WorkLicenseForm.Card(license, license, Now).Detail);
        }

        [Fact]
        public void Card_ExpiryDayItself_IsStillLicensed()
        {
            LicenseInfo license = License(seats: 1, expires: "2026-11-17");
            Assert.Equal("Licensed for work use", WorkLicenseForm.Card(license, license, Now).Headline);
        }

        [Fact]
        public void Card_InTheGracePeriod_SaysWhenToRenewBy()
        {
            LicenseInfo license = License(seats: 1, expires: "2026-11-10");
            var (glyph, color, headline, detail) = WorkLicenseForm.Card(license, license, Now);

            Assert.Equal("", glyph);
            Assert.Equal(Color.FromArgb(0x8A, 0x4B, 0x00), color);
            Assert.Equal("License expired", headline);
            Assert.Equal("jane@example.com · expired November 10, 2026. Renew by December 10, 2026 to keep using it for work.", detail);
        }

        [Fact]
        public void Card_PastTheGracePeriod_SaysWorkUseNeedsACurrentLicense()
        {
            LicenseInfo license = License(seats: 1, expires: "2026-09-01");
            var (_, _, headline, detail) = WorkLicenseForm.Card(null, license, Now);

            Assert.Equal("License expired", headline);
            Assert.EndsWith("Work use needs a current license.", detail);
        }

        [Fact]
        public void Card_TextHasNoEmDashes()
        {
            LicenseInfo license = License(seats: 3, expires: "2026-11-10");
            foreach ((LicenseInfo? current, LicenseInfo? stored) in new[] { ((LicenseInfo?)null, (LicenseInfo?)null), (license, license), (null, license) })
            {
                var card = WorkLicenseForm.Card(current, stored, Now);
                Assert.DoesNotContain('—', card.Headline + card.Detail);
            }
        }

        // ---- Entering a license ----

        [Fact]
        public void NotLicensed_ShowsTheSteps_WithoutTheUseADifferentLicenseLink() => WithForm(licensed: false, form =>
        {
            // The window isn't on screen yet here, which is exactly when this used to go wrong.
            Assert.True(form.EntryShown);
            Assert.False(form.UseDifferentOffered);
            Assert.False(form.RemoveOffered);
        });

        [Fact]
        public void Licensed_TucksTheStepsBehindALink() => WithForm(licensed: true, form =>
        {
            Assert.False(form.EntryShown);
            Assert.True(form.UseDifferentOffered);
            Assert.True(form.RemoveOffered);
        });

        [Fact]
        public void UseADifferentLicense_OpensTheSteps() => WithForm(licensed: true, form =>
        {
            Invoke(form, "OpenEntry");

            Assert.True(form.EntryShown);
            Assert.False(form.UseDifferentOffered);
        });

        [Fact]
        public void EnteringADifferentLicense_TucksTheStepsAwayAgain() => WithForm(licensed: true, form =>
        {
            Invoke(form, "OpenEntry");
            form.ImportText(TestLicenses.Make(email: "other@example.com"));

            Assert.StartsWith("Licensed for work use to other@example.com", form.StatusText);
            Assert.False(form.EntryShown);
        });

        [Fact]
        public void AFailedEntry_KeepsTheStepsOpen() => WithForm(licensed: true, form =>
        {
            Invoke(form, "OpenEntry");
            form.ImportText("not a license");

            Assert.True(form.EntryShown);
            Assert.Equal(LicenseActivation.InvalidFileMessage, form.MessageText);
        });

        [Fact]
        public void RemovingTheLicense_ShowsTheStepsAgain() => WithForm(licensed: true, form =>
        {
            form.RemoveNow();

            Assert.True(form.EntryShown);
            Assert.False(form.UseDifferentOffered);
            Assert.Equal(WorkLicenseForm.NotLicensedStatus, form.StatusText);
        });

        // ---- Layout ----

        [Fact]
        public void SendCodeAndVerify_AreTheSameSize() => WithForm(licensed: false, form =>
        {
            Assert.Equal(Field<Button>(form, "_sendCode").Size, Field<Button>(form, "_verify").Size);
        });

        [Fact]
        public void TheMessageLine_OnlyShowsWhenThereIsAMessage() => WithForm(licensed: false, form =>
        {
            Label message = Field<Label>(form, "_message");
            Assert.False(Visible(message));

            form.ImportText("not a license");

            Assert.True(Visible(message));
        });

        [Fact]
        public void TheStatusCard_HasAnIcon() => WithForm(licensed: true, form =>
        {
            Assert.NotNull(Field<PictureBox>(form, "_statusIcon").Image);
        });

        [Fact]
        public void TheFooter_OffersBuyingAndImporting_AndClose() => WithForm(licensed: false, form =>
        {
            var links = All<LinkLabel>(form).Select(l => l.Text).ToList();
            Assert.Contains("Buy a license", links);
            Assert.Contains("Import a license file...", links);
            Assert.Contains(All<Button>(form), b => b.Text == "Close" && ReferenceEquals(b, form.CancelButton));
        });

        // ---- Helpers ----

        private static LicenseInfo License(int seats, string expires) => LicenseToken.Verify(
            TestLicenses.Make(seats: seats, expires: expires), TestLicenses.Keys)!;

        private static void Invoke(WorkLicenseForm form, string method) =>
            typeof(WorkLicenseForm).GetMethod(method, Flags)!.Invoke(form, null);

        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;

        // Whether the control itself is set to show (its window may not be on screen).
        private static bool Visible(Control control) =>
            (bool)typeof(Control).GetMethod("GetState", Flags)!.Invoke(control, new object[] { 2 })!;

        private static IEnumerable<T> All<T>(Control parent) where T : Control
        {
            foreach (Control child in parent.Controls)
            {
                if (child is T match)
                {
                    yield return match;
                }
                foreach (T nested in All<T>(child))
                {
                    yield return nested;
                }
            }
        }

        private static void WithForm(bool licensed, Action<WorkLicenseForm> test)
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                string path = Path.Combine(Path.GetTempPath(), "work-license-" + Guid.NewGuid().ToString("N") + ".db");
                try
                {
                    using var db = new LiteDatabase(path);
                    var license = new WorkLicense(new LicenseRepository(db), TestLicenses.Keys);
                    if (licensed)
                    {
                        Assert.NotNull(license.Accept(TestLicenses.Make(seats: 3), Now));
                    }
                    using var form = new WorkLicenseForm(license, new LicenseClient(), () => Now);
                    test(form);
                }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
                finally { try { File.Delete(path); } catch { } }
            })
            { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            captured?.Throw();
        }
    }
}
