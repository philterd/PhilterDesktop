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
using System.Text.RegularExpressions;
using LiteDB;
using PhilterData;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The re-scan of redacted output for anything still detectable is called the "leftover check".
    /// "Verified" promised more than the check does, so user-facing text must not call it that.
    /// </summary>
    public sealed class LeftoverCheckWordingTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        // ---- Queue window ----

        [Fact]
        public void Menu_IsCheckForLeftoverPii() => WithMainForm(form =>
        {
            ToolStripMenuItem item = Field<ToolStripMenuItem>(form, "verifyRedactionToolStripMenuItem");

            Assert.Equal("Check for Lefto&ver PII", item.Text);
            Assert.Equal(new[] { "Using the &same policy", "Using a &broad policy" },
                item.DropDownItems.Cast<ToolStripItem>().Select(i => i.Text));
        });

        [Fact]
        public void QueueMenu_AccessKeysAreUnique() => WithMainForm(form =>
        {
            // Changing the item's text could have given it a key another item already uses.
            var keys = Field<ContextMenuStrip>(form, "contextMenuStrip1").Items.OfType<ToolStripMenuItem>()
                .Select(i => AccessKey(i.Text))
                .Where(k => k is not null)
                .ToList();

            Assert.Equal(keys.Count, keys.Distinct().Count());
            Assert.Contains('V', keys); // kept from "&Verify Redaction" for people used to it
        });

        [Fact]
        public void QueueColumn_IsLeftovers() => WithMainForm(form =>
        {
            Assert.Equal("Leftovers", Field<ColumnHeader>(form, "columnHeader5").Text);
        });

        [Theory]
        [InlineData("Clean", 0, "None found")]
        [InlineData("ResidualsFound", 3, "3 found")]
        [InlineData("Error", 0, "Check failed")]
        [InlineData("NamesNotChecked", 0, "Names not checked")]
        [InlineData("NotRun", 0, "Not checked")]
        public void QueueColumn_ShowsPlainResults(string status, int count, string shown)
        {
            var entity = new RedactionQueueEntity { VerificationStatus = status, VerificationFindingCount = count };
            string display = (string)typeof(MainForm).GetMethod("VerificationDisplay", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, new object[] { entity })!;

            Assert.Equal(shown, display);
        }

        // ---- Settings ----

        [Fact]
        public void Settings_DescribeTheLeftoverCheck() => WithSettings(form =>
        {
            Assert.Equal("After each redaction, re-scan the output for leftover PII", Field<CheckBox>(form, "chkVerifyAfterRedaction").Text);
            string same = Field<RadioButton>(form, "rdoVerifySamePolicy").Text;
            string broad = Field<RadioButton>(form, "rdoVerifyBroadPolicy").Text;
            string hint = Field<Label>(form, "lblVerifyHint").Text;

            Assert.StartsWith("Use the same policy", same);
            Assert.StartsWith("Use a broad policy", broad);
            Assert.Contains("leftover PII", hint);
            foreach (string text in new[] { same, broad, hint })
            {
                Assert.DoesNotContain("verif", text, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain('—', text);
            }
        });

        // ---- Result dialog ----

        [Fact]
        public void ResultDialog_IsTitledLeftoverCheck() => Sta(() =>
        {
            using var form = new VerificationResultForm("a.txt", new VerificationOutcome { Status = VerificationStatus.Clean });

            Assert.Equal("Leftover Check", form.Text);
            Assert.Equal("No leftover PII was detected in a.txt.", Field<Label>(form, "_summary").Text);
        });

        [Fact]
        public void ResultDialog_Error_NamesTheLeftoverCheck() => Sta(() =>
        {
            using var form = new VerificationResultForm("a.txt", new VerificationOutcome { Status = VerificationStatus.Error, Error = "boom" });

            Assert.Equal("The check could not be completed", Field<Label>(form, "_headline").Text);
            Assert.Equal("a.txt wasn't checked. boom", Field<Label>(form, "_summary").Text);
        });

        // ---- Warnings in notifications, the CLI and watched folders ----

        [Fact]
        public void Warnings_NameTheLeftoverCheck()
        {
            string found = RedactionVerifier.WarningFor(new VerificationOutcome
            {
                Status = VerificationStatus.ResidualsFound,
                Residuals = new[] { new RedactionSpanEntity(), new RedactionSpanEntity() }
            })!;
            string failed = RedactionVerifier.WarningFor(new VerificationOutcome { Status = VerificationStatus.Error })!;

            Assert.StartsWith("The leftover check found 2 possible items", found);
            Assert.StartsWith("The leftover check could not be completed", failed);
            Assert.DoesNotContain('—', found + failed);
        }

        // ---- Nothing user-facing says "verify" for this feature ----

        // Other features and wording that legitimately use the word.
        private static readonly string[] Allowed =
        {
            "LicenseActivation.cs",   // the work license is "Verified"
            "WorkLicenseForm.cs",     // its "Verify" button
            "CommandLineRedactor.cs", // --selftest help: "verify it"
            "RedactionNoticeForm.cs", // "You are responsible for verifying..."
        };

        [Fact]
        public void NoUserFacingTextSaysVerify()
        {
            string app = Path.GetDirectoryName(RepoFile("PhilterDesktop", "PhilterDesktop.csproj"))!;
            var offenders = new List<string>();
            foreach (string file in Directory.EnumerateFiles(app, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".cs") || f.EndsWith(".resx"))
                .Where(f => !Regex.IsMatch(f, @"[\\/](bin|obj)[\\/]"))
                .Where(f => !Allowed.Contains(Path.GetFileName(f))))
            {
                string text = File.ReadAllText(file);
                IEnumerable<string> strings = file.EndsWith(".resx")
                    ? Regex.Matches(text, "<value>([^<]*)</value>").Select(m => m.Groups[1].Value)
                    : Regex.Matches(text, "\"(?:[^\"\\\\\\n]|\\\\.)*\"")
                        .Select(m => Regex.Replace(m.Value.Trim('"'), @"\{[^{}]*\}", "")); // drop {code} holes
                // Identifier-like strings (control names, resource keys, a link's tracking tag) aren't shown.
                offenders.AddRange(strings
                    .Where(s => s.Contains("verif", StringComparison.OrdinalIgnoreCase) && !Regex.IsMatch(s, @"^[A-Za-z_.]+$"))
                    .Select(s => Path.GetFileName(file) + ": " + s));
            }

            Assert.Empty(offenders);
        }

        // ---- Helpers ----

        private static char? AccessKey(string? text)
        {
            Match m = Regex.Match(text ?? "", "&([^&])");
            return m.Success ? char.ToUpperInvariant(m.Groups[1].Value[0]) : null;
        }

        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;

        private static void WithMainForm(Action<MainForm> test) => WithDb(db =>
        {
            using var form = new MainForm(db, startMinimized: false);
            test(form);
        });

        private static void WithSettings(Action<SettingsForm> test) => WithDb(db =>
        {
            var policies = new PolicyRepository(db);
            policies.Insert(new PolicyEntity { Name = "default", Json = DefaultPolicy.Json() });
            var contexts = new ContextRepository(db);
            contexts.Insert(new ContextEntity { Name = "default" });
            using var form = new SettingsForm(new SettingsRepository(db), policies, contexts, new WatchedFolderRepository(db));
            test(form);
        });

        private static void WithDb(Action<LiteDatabase> test) => Sta(() =>
        {
            string path = Path.Combine(Path.GetTempPath(), "leftover-" + Guid.NewGuid().ToString("N") + ".db");
            try
            {
                using var db = new LiteDatabase(path);
                test(db);
            }
            finally { try { File.Delete(path); } catch { } }
        });

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

        private static string RepoFile(params string[] parts)
        {
            for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            {
                string candidate = Path.Combine([dir.FullName, .. parts]);
                if (File.Exists(candidate) && File.Exists(Path.Combine(dir.FullName, "PhilterDesktop.slnx")))
                {
                    return candidate;
                }
            }
            throw new FileNotFoundException("Could not locate " + Path.Combine(parts));
        }
    }
}
