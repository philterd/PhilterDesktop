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

using System.Net;
using System.Net.Http;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Windows.Forms;
using LiteDB;
using PhilterData;
using Xunit;
using Stub = PhilterDesktop.Tests.TestLicenses.StubHandler;

namespace PhilterDesktop.Tests
{
    /// <summary>Entering a license (email, code, file) and the Manage License dialog, About box, and menu.</summary>
    public sealed class LicenseActivationTests : IDisposable
    {
        private static readonly DateTime Now = new(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc);

        private readonly string _path = Path.Combine(Path.GetTempPath(), "activation-" + Guid.NewGuid().ToString("N") + ".db");
        private readonly LiteDatabase _db;
        private readonly WorkLicense _license;

        public LicenseActivationTests()
        {
            _db = new LiteDatabase(_path);
            _license = new WorkLicense(new LicenseRepository(_db), TestLicenses.Keys);
        }

        public void Dispose()
        {
            _db.Dispose();
            try { File.Delete(_path); } catch { /* best effort */ }
        }

        // A stub that answers request-code with a request id, and token with the given reply.
        private static Stub Server(HttpStatusCode tokenStatus, string tokenJson) => new((request, _, _) =>
        {
            string path = request.RequestUri!.AbsolutePath;
            (HttpStatusCode status, string json) = path.EndsWith("/request-code", StringComparison.Ordinal)
                ? (HttpStatusCode.Accepted, "{\"status\":\"sent_if_eligible\",\"request\":\"req-1\"}")
                : (tokenStatus, tokenJson);
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") });
        });

        private LicenseActivation Activation(HttpMessageHandler handler) => new(_license, TestLicenses.Client(handler), () => Now);

        private static string Licensed(string token) => $"{{\"licensed\":true,\"token\":\"{token}\"}}";

        // --- Email --------------------------------------------------------------------------------

        [Theory]
        [InlineData("jane@example.com", true)]
        [InlineData("  jane@smithlaw.co.uk ", true)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("jane", false)]
        [InlineData("@example.com", false)]
        [InlineData("jane@", false)]
        [InlineData("jane@example", false)]
        [InlineData("jane@example.", false)]
        [InlineData("jane@@example.com", false)]
        [InlineData("jane doe@example.com", false)]
        [InlineData(null, false)]
        public void LooksLikeEmail(string? email, bool expected) => Assert.Equal(expected, LicenseActivation.LooksLikeEmail(email));

        [Fact]
        public async Task SendCode_ABadEmail_DoesNotCallTheServer()
        {
            Stub stub = Server(HttpStatusCode.OK, "{}");
            ActivationStep step = await Activation(stub).SendCodeAsync("not-an-email");
            Assert.False(step.Success);
            Assert.Equal(LicenseActivation.InvalidEmailMessage, step.Message);
            Assert.Empty(stub.Requests);
        }

        [Fact]
        public async Task SendCode_RemembersTheRequest()
        {
            LicenseActivation activation = Activation(Server(HttpStatusCode.OK, "{}"));
            ActivationStep step = await activation.SendCodeAsync(" Jane@Example.com ");
            Assert.True(step.Success);
            Assert.Equal(LicenseActivation.CodeSentMessage, step.Message);
            Assert.Equal("jane@example.com", activation.Email);
            Assert.Equal("req-1", activation.RequestId);
        }

        [Fact]
        public async Task SendCode_Offline_SaysSo()
        {
            LicenseActivation activation = Activation(Stub.Throws(new HttpRequestException("offline")));
            ActivationStep step = await activation.SendCodeAsync("jane@example.com");
            Assert.False(step.Success);
            Assert.Equal(LicenseActivation.UnreachableMessage, step.Message);
            Assert.False(activation.CodeRequested);
        }

        // --- Code ----------------------------------------------------------------------------------

        [Fact]
        public async Task Verify_BeforeSending_AsksForACode()
        {
            Stub stub = Server(HttpStatusCode.OK, "{}");
            ActivationStep step = await Activation(stub).VerifyAsync("123456");
            Assert.Equal(LicenseActivation.NoCodeRequestedMessage, step.Message);
            Assert.Empty(stub.Requests);
        }

        [Fact]
        public async Task Verify_AnEmptyCode_DoesNotCallTheServer()
        {
            Stub stub = Server(HttpStatusCode.OK, "{}");
            LicenseActivation activation = Activation(stub);
            await activation.SendCodeAsync("jane@example.com");
            ActivationStep step = await activation.VerifyAsync("   ");
            Assert.Equal(LicenseActivation.EmptyCodeMessage, step.Message);
            Assert.Single(stub.Requests); // only the request-code call
        }

        [Fact]
        public async Task Verify_Licensed_StoresTheLicense()
        {
            LicenseActivation activation = Activation(Server(HttpStatusCode.OK, Licensed(TestLicenses.Valid)));
            await activation.SendCodeAsync("jane@example.com");
            ActivationStep step = await activation.VerifyAsync("123456");

            Assert.True(step.Success);
            Assert.Equal("jane@example.com", step.License?.Email);
            Assert.StartsWith("Verified. Licensed for work use to jane@example.com (3 seats)", step.Message);
            Assert.NotNull(_license.Current(Now));
            Assert.False(activation.CodeRequested); // the code is used up
        }

        [Fact]
        public async Task Verify_ATokenThatDoesntVerify_IsNotStored()
        {
            LicenseActivation activation = Activation(Server(HttpStatusCode.OK, Licensed(TestLicenses.BadSignature)));
            await activation.SendCodeAsync("jane@example.com");
            ActivationStep step = await activation.VerifyAsync("123456");
            Assert.False(step.Success);
            Assert.Equal(LicenseActivation.BadTokenMessage, step.Message);
            Assert.Null(_license.Stored());
        }

        [Fact]
        public async Task Verify_NotLicensed_NamesTheEmail()
        {
            LicenseActivation activation = Activation(Server(HttpStatusCode.OK, "{\"licensed\":false}"));
            await activation.SendCodeAsync("jane@example.com");
            ActivationStep step = await activation.VerifyAsync("123456");
            Assert.False(step.Success);
            Assert.Contains("jane@example.com", step.Message);
            Assert.False(activation.CodeRequested);
            Assert.Null(_license.Stored());
        }

        // A wrong code, a rate limit, or no answer leaves the request in place, so the person can retry.
        [Theory]
        [InlineData(HttpStatusCode.BadRequest, "{\"error\":\"invalid_code\"}", LicenseActivation.InvalidCodeMessage)]
        [InlineData(HttpStatusCode.TooManyRequests, "{\"error\":\"rate_limited\"}", LicenseActivation.RateLimitedMessage)]
        [InlineData(HttpStatusCode.ServiceUnavailable, "{\"error\":\"unavailable\"}", LicenseActivation.UnreachableMessage)]
        public async Task Verify_Failures_KeepTheRequestForARetry(HttpStatusCode status, string json, string message)
        {
            LicenseActivation activation = Activation(Server(status, json));
            await activation.SendCodeAsync("jane@example.com");
            ActivationStep step = await activation.VerifyAsync("123456");
            Assert.False(step.Success);
            Assert.Equal(message, step.Message);
            Assert.True(activation.CodeRequested);
        }

        // --- License file --------------------------------------------------------------------------

        [Fact]
        public void Import_AValidFile_StoresIt()
        {
            ActivationStep step = Activation(Server(HttpStatusCode.OK, "{}")).Import(TestLicenses.Valid + "\n");
            Assert.True(step.Success);
            Assert.Equal("jane@example.com", _license.Current(Now)?.Email);
        }

        [Theory]
        [InlineData(TestLicenses.TamperedPayload)]
        [InlineData(TestLicenses.BadSignature)]
        [InlineData("this is not a license")]
        [InlineData("")]
        public void Import_AnInvalidFile_IsRefused(string text)
        {
            ActivationStep step = Activation(Server(HttpStatusCode.OK, "{}")).Import(text);
            Assert.False(step.Success);
            Assert.Equal(LicenseActivation.InvalidFileMessage, step.Message);
            Assert.Null(_license.Stored());
        }

        [Fact]
        public void Import_AnExpiredFile_IsRefused()
        {
            ActivationStep step = Activation(Server(HttpStatusCode.OK, "{}")).Import(TestLicenses.Make(expires: "2026-01-01"));
            Assert.False(step.Success);
            Assert.Equal(LicenseActivation.ExpiredFileMessage, step.Message);
            Assert.Null(_license.Stored());
        }

        [Fact]
        public void Import_NeverTouchesTheNetwork()
        {
            Stub stub = Server(HttpStatusCode.OK, "{}");
            Activation(stub).Import(TestLicenses.Valid);
            Assert.Empty(stub.Requests);
        }

        [Fact]
        public void ReadLicenseFile_RefusesOversizedOrMissingFiles()
        {
            string dir = Directory.CreateTempSubdirectory("licfile-").FullName;
            try
            {
                string small = Path.Combine(dir, "ok.txt");
                File.WriteAllText(small, TestLicenses.Valid + "\n");
                Assert.Equal(TestLicenses.Valid + "\n", WorkLicenseForm.ReadLicenseFile(small));

                string big = Path.Combine(dir, "big.txt");
                File.WriteAllText(big, new string('x', LicenseActivation.MaxLicenseFileBytes + 1));
                Assert.Null(WorkLicenseForm.ReadLicenseFile(big));

                Assert.Null(WorkLicenseForm.ReadLicenseFile(Path.Combine(dir, "missing.txt")));
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        [Fact]
        public void Remove_ClearsTheLicense()
        {
            _license.Accept(TestLicenses.Valid, Now);
            ActivationStep step = Activation(Server(HttpStatusCode.OK, "{}")).Remove();
            Assert.Equal(LicenseActivation.RemovedMessage, step.Message);
            Assert.Null(_license.Stored());
        }

        // --- Wording ----------------------------------------------------------------------------------

        [Fact]
        public void Describe_ShowsSeatsOnlyWhenThereAreSeveral()
        {
            LicenseInfo three = LicenseToken.Verify(TestLicenses.Valid, TestLicenses.Keys)!;
            LicenseInfo one = LicenseToken.Verify(TestLicenses.Make(seats: 1), TestLicenses.Keys)!;
            Assert.Contains("(3 seats)", LicenseActivation.Describe(three));
            Assert.DoesNotContain("seat", LicenseActivation.Describe(one));
            Assert.StartsWith("Licensed for work use to jane@example.com", LicenseActivation.Describe(one));
        }

        [Fact]
        public void Messages_HaveNoEmDashes()
        {
            foreach (string text in new[]
                     {
                         LicenseActivation.InvalidEmailMessage, LicenseActivation.CodeSentMessage, LicenseActivation.NoCodeRequestedMessage,
                         LicenseActivation.EmptyCodeMessage, LicenseActivation.InvalidCodeMessage, LicenseActivation.RateLimitedMessage,
                         LicenseActivation.UnreachableMessage, LicenseActivation.BadTokenMessage, LicenseActivation.InvalidFileMessage,
                         LicenseActivation.ExpiredFileMessage, LicenseActivation.RemovedMessage, WorkLicenseForm.IntroText,
                         WorkLicenseForm.NotLicensedStatus, AboutForm.LicenseText(null)
                     })
            {
                Assert.DoesNotContain('—', text);
            }
        }

        // --- The dialog -----------------------------------------------------------------------------

        [Fact]
        public void Dialog_ShowsNotLicensed_WithNoRemoveLink() => Sta(() =>
        {
            using var form = new WorkLicenseForm(_license, TestLicenses.Client(Server(HttpStatusCode.OK, "{}")), () => Now);
            _ = form.Handle;
            Assert.Equal(WorkLicenseForm.NotLicensedStatus, form.StatusText);
            Assert.False(form.RemoveOffered);
            Assert.False(Field<Button>(form, "_verify").Enabled);
            Assert.False(Field<TextBox>(form, "_code").Enabled);
        });

        [Fact]
        public void Dialog_ShowsTheCurrentLicense() => Sta(() =>
        {
            _license.Accept(TestLicenses.Valid, Now);
            using var form = new WorkLicenseForm(_license, TestLicenses.Client(Server(HttpStatusCode.OK, "{}")), () => Now);
            Assert.StartsWith("Licensed for work use to jane@example.com", form.StatusText);
            Assert.True(form.RemoveOffered);
        });

        [Fact]
        public void Dialog_SendThenVerify_UpdatesTheStatus() => Sta(() =>
        {
            using var form = new WorkLicenseForm(_license, TestLicenses.Client(Server(HttpStatusCode.OK, Licensed(TestLicenses.Valid))), () => Now);
            _ = form.Handle;
            Field<TextBox>(form, "_email").Text = "jane@example.com";
            form.SendCodeAsync().GetAwaiter().GetResult();
            Assert.True(Field<Button>(form, "_verify").Enabled);
            Assert.True(Field<TextBox>(form, "_code").Enabled);
            Assert.Equal(LicenseActivation.CodeSentMessage, form.MessageText);

            Field<TextBox>(form, "_code").Text = "123 456";
            form.VerifyAsync().GetAwaiter().GetResult();
            Assert.StartsWith("Licensed for work use to jane@example.com", form.StatusText);
            Assert.False(Field<Button>(form, "_verify").Enabled); // the code is used up
        });

        [Fact]
        public void Dialog_ImportAndRemove() => Sta(() =>
        {
            using var form = new WorkLicenseForm(_license, TestLicenses.Client(Server(HttpStatusCode.OK, "{}")), () => Now);
            form.ImportText(TestLicenses.Valid);
            Assert.StartsWith("Licensed for work use", form.StatusText);
            form.ImportText(null); // an unreadable file
            Assert.Equal(LicenseActivation.InvalidFileMessage, form.MessageText);
            Assert.StartsWith("Licensed for work use", form.StatusText); // the license stays
            form.RemoveNow();
            Assert.Equal(WorkLicenseForm.NotLicensedStatus, form.StatusText);
            Assert.False(form.RemoveOffered);
        });

        // A storage failure is reported in the dialog instead of reaching the app's crash handler.
        [Fact]
        public void Dialog_StorageFailure_IsReportedNotThrown() => Sta(() =>
        {
            string path = Path.Combine(Path.GetTempPath(), "broken-" + Guid.NewGuid().ToString("N") + ".db");
            var db = new LiteDatabase(path);
            var license = new WorkLicense(new LicenseRepository(db), TestLicenses.Keys);
            try
            {
                using var form = new WorkLicenseForm(license, TestLicenses.Client(Server(HttpStatusCode.OK, Licensed(TestLicenses.Valid))), () => Now);
                _ = form.Handle;
                db.Dispose(); // every read and write now throws

                form.ImportText(TestLicenses.Valid);
                Assert.Equal(LicenseActivation.UnexpectedErrorMessage, form.MessageText);
                Assert.Equal(WorkLicenseForm.NotLicensedStatus, form.StatusText);

                Field<TextBox>(form, "_email").Text = "jane@example.com";
                form.SendCodeAsync().GetAwaiter().GetResult();
                Field<TextBox>(form, "_code").Text = "123456";
                form.VerifyAsync().GetAwaiter().GetResult();
                Assert.Equal(LicenseActivation.UnexpectedErrorMessage, form.MessageText);

                form.RemoveNow();
                Assert.Equal(LicenseActivation.UnexpectedErrorMessage, form.MessageText);
            }
            finally { try { File.Delete(path); } catch { /* best effort */ } }
        });

        // Closing the dialog while a request is in flight must not touch the disposed controls.
        [Fact]
        public void Dialog_ClosedWhileWaiting_DoesNotThrow() => Sta(() =>
        {
            WorkLicenseForm? form = null;
            var closesMidRequest = new Stub((request, _, _) =>
            {
                form!.Dispose(); // the user closes the window before the reply arrives
                bool code = request.RequestUri!.AbsolutePath.EndsWith("/request-code", StringComparison.Ordinal);
                string json = code ? "{\"status\":\"sent_if_eligible\",\"request\":\"r\"}" : Licensed(TestLicenses.Valid);
                return Task.FromResult(new HttpResponseMessage(code ? HttpStatusCode.Accepted : HttpStatusCode.OK)
                    { Content = new StringContent(json, Encoding.UTF8, "application/json") });
            });

            form = new WorkLicenseForm(_license, TestLicenses.Client(closesMidRequest), () => Now);
            _ = form.Handle;
            Field<TextBox>(form, "_email").Text = "jane@example.com";
            form.SendCodeAsync().GetAwaiter().GetResult();
            Assert.True(form.IsDisposed);

            form = new WorkLicenseForm(_license, TestLicenses.Client(closesMidRequest), () => Now);
            _ = form.Handle;
            form.Activation.SendCodeAsync("jane@example.com").GetAwaiter().GetResult(); // disposes this form too
            Field<TextBox>(form, "_code").Text = "123456";
            form.VerifyAsync().GetAwaiter().GetResult();
            Assert.True(form.IsDisposed);
            Assert.NotNull(_license.Current(Now)); // the license was still saved
        });

        // --- About box and menu --------------------------------------------------------------------

        [Fact]
        public void About_DescribesTheLicense_OrHowToGetOne()
        {
            Assert.Contains("work use needs", AboutForm.LicenseText(null));
            LicenseInfo info = LicenseToken.Verify(TestLicenses.Valid, TestLicenses.Keys)!;
            string licensed = AboutForm.LicenseText(info);
            Assert.Contains("Licensed for work use to jane@example.com", licensed);
            Assert.DoesNotContain("needs a per-user", licensed);
            Assert.EndsWith(AboutForm.TrademarkText, licensed);
        }

        [Fact]
        public void About_WithALicense_Constructs() => Sta(() =>
        {
            using var about = new AboutForm(LicenseToken.Verify(TestLicenses.Valid, TestLicenses.Keys));
            _ = about.Handle;
            Assert.Contains("jane@example.com", Field<Label>(about, "lblLicense").Text);
        });

        // Help holds the app's own items only: no View License (it moved to About) and no other Philterd products.
        [Fact]
        public void HelpMenu_HasOnlyTheAppsItems() => Sta(() =>
        {
            using var main = new MainForm(_db, startMinimized: false);
            var help = Field<ToolStripMenuItem>(main, "helpToolStripMenuItem");
            string[] texts = help.DropDownItems.OfType<ToolStripMenuItem>().Select(i => i.Text ?? string.Empty).ToArray();
            Assert.Equal(new[] { "Help", "Check for Updates...", "Manage License...", "About..." }, texts);
            Assert.DoesNotContain(help.DropDownItems.OfType<ToolStripMenuItem>(), i => i.HasDropDownItems);
        });

        // About opens the licenses itself and no longer advertises other Philterd products.
        [Fact]
        public void About_HasViewLicenses_AndNoProductLinks() => Sta(() =>
        {
            using var about = new AboutForm();
            _ = about.Handle;
            string[] texts = All(about).Select(c => c.Text ?? string.Empty).ToArray();
            Assert.Contains("View licenses", texts);
            Assert.Contains("Third-party notices", texts);
            Assert.Contains("Third-party licenses", texts);
            foreach (string gone in new[] { "Also from Philterd", "Philter Scope", "Philter Diffuse", "Policy consulting", "View the EULA", "View the Apache License", "Source code on GitHub" })
            {
                Assert.DoesNotContain(texts, t => t.Contains(gone, StringComparison.OrdinalIgnoreCase));
            }
            Assert.Null(typeof(AboutForm).GetField("linkPhilter", Flags));
            Assert.Null(typeof(AboutForm).GetField("linkEula", Flags));
            Assert.Null(typeof(AboutForm).GetField("linkGitHub", Flags));
        });

        private static IEnumerable<Control> All(Control root) =>
            root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(All(c)));

        [Fact]
        public void HelpMenu_HasWorkLicense() => Sta(() =>
        {
            using var main = new MainForm(_db, startMinimized: false);
            var item = Field<ToolStripMenuItem>(main, "_workLicenseMenuItem");
            Assert.Equal("Manage License...", item.Text);
            var help = Field<ToolStripMenuItem>(main, "helpToolStripMenuItem");
            Assert.Contains(item, help.DropDownItems.Cast<ToolStripItem>());
            Assert.Null(main.CurrentWorkLicense()); // nothing entered
        });

        private const System.Reflection.BindingFlags Flags =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;

        private static T Field<T>(object o, string name) => (T)o.GetType().GetField(name, Flags)!.GetValue(o)!;

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
