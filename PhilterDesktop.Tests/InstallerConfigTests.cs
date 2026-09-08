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

using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Guards how the installer presents itself to Windows and to the user. These assert on
    /// Installer\PhilterDesktop.iss, not runtime behavior. Architecture wiring lives in
    /// <see cref="Arm64BuildConfigTests"/>.
    /// </summary>
    public sealed class InstallerConfigTests
    {
        [Fact]
        public void AddRemovePrograms_ShowsTheAppNameWithoutTheVersion()
        {
            // Inno defaults the Add/Remove Programs name to AppVerName ("Philter Desktop version
            // 1.1.0"). The version has its own column there, so the name carries only the app name.
            Assert.Contains("UninstallDisplayName={#AppName}", InnoScript());
        }

        [Fact]
        public void AddRemovePrograms_StillReportsTheVersion()
        {
            // UninstallDisplayName only renames the entry; AppVersion is what fills the Version
            // column, so dropping it would hide the version rather than tidy the name.
            Assert.Contains("AppVersion={#AppVersion}", InnoScript());
        }

        [Fact]
        public void AppId_IsPinned_SoUpgradesReplaceThePriorInstall()
        {
            // A changed AppId makes Windows treat a new release as a separate product, leaving the
            // old one installed alongside it.
            Assert.Contains("AppId={{B7E5A3D2-9C41-4E8A-A1F6-2D0C7B9E4F31}", InnoScript());
        }

        [Fact]
        public void Install_DefaultsToPerUser_WithoutRequiringAdmin()
        {
            Assert.Contains("PrivilegesRequired=lowest", InnoScript());
        }

        [Fact]
        public void Install_AcceptsAllUsersAndCurrentUserOnTheCommandLine()
        {
            // "commandline" is what makes the /CURRENTUSER and /ALLUSERS switches work. The user docs
            // (docs/docs/getting-started.md, "Installing without the wizard") tell people to pass
            // /CURRENTUSER for an unattended install, so dropping it would break a documented flow.
            Assert.Contains("PrivilegesRequiredOverridesAllowed=dialog commandline", InnoScript());
        }

        [Fact]
        public void OptionalTasks_AreOffByDefault_SoASilentInstallAddsNeither()
        {
            // A silent install takes each task's default. Both are "unchecked", which is what the docs
            // promise: no desktop icon and no start-at-sign-in unless /TASKS asks for them.
            string iss = InnoScript();

            foreach (string task in new[] { "desktopicon", "autostart" })
            {
                string entry = Assert.Single(
                    iss.Split('\n').Select(l => l.Trim()),
                    l => l.StartsWith("Name: \"" + task + "\"", StringComparison.Ordinal));
                Assert.Contains("Flags: unchecked", entry);
            }
        }

        [Fact]
        public void SilentUninstall_NeverAsksAboutSavedData()
        {
            // A scripted uninstall must not stop on the "also remove your data?" dialog: it would hang
            // unattended, and deletes policies, contexts, settings and redaction history if answered
            // Yes. UninstallSilent covers a bare /VERYSILENT (plain MsgBox ignores /SUPPRESSMSGBOXES
            // and displays anyway), so the prompt is reachable only with a person there to answer it.
            Assert.Contains("not UninstallSilent", InnoScript());
        }

        [Fact]
        public void SavedDataPrompt_DefaultsToKeepingTheData()
        {
            string iss = InnoScript();

            // SuppressibleMsgBox with an explicit IDNO default, not MsgBox: the suppressed answer must
            // be "keep". MB_DEFBUTTON2 makes No the default button for the interactive case too.
            Assert.Contains("SuppressibleMsgBox(", iss);
            Assert.Contains("MB_YESNO or MB_DEFBUTTON2, IDNO)", iss);
        }

        [Fact]
        public void SavedData_IsDeletedOnlyOnAnExplicitYes()
        {
            // The one DelTree of the data directory must sit behind the = IDYES test, so no other path
            // can reach it.
            string iss = InnoScript();
            string[] lines = iss.Split('\n').Select(l => l.Trim()).ToArray();

            int delete = Array.FindIndex(lines, l => l.StartsWith("DelTree(DataDir", StringComparison.Ordinal));
            Assert.True(delete >= 0, "The data-directory DelTree call is gone; this test needs updating.");
            Assert.Equal(1, lines.Count(l => l.StartsWith("DelTree(DataDir", StringComparison.Ordinal)));

            string guard = string.Join(" ", lines[Math.Max(0, delete - 8)..delete]);
            Assert.Contains("= IDYES then", guard);
        }

        private static string InnoScript()
        {
            for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            {
                string candidate = Path.Combine(dir.FullName, "Installer", "PhilterDesktop.iss");
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }
            }
            throw new FileNotFoundException("Could not locate Installer\\PhilterDesktop.iss from " + AppContext.BaseDirectory);
        }
    }
}
