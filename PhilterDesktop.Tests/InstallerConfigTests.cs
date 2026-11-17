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

using System.Text.RegularExpressions;
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
        public void SetupFile_HasTheAppVersionAsItsFileVersion()
        {
            // Without it, the setup file's "File version" in Explorer is 0.0.0.0.
            Assert.Contains("VersionInfoVersion={#AppVersion}", InnoScript());
        }

        [Fact]
        public void DefaultAppVersion_IsNumeric_AsVersionInfoVersionRequires()
        {
            Match m = Regex.Match(InnoScript(), "#define AppVersion \"([^\"]*)\"");
            Assert.True(m.Success);
            Assert.Matches(@"^\d+\.\d+\.\d+(\.\d+)?$", m.Groups[1].Value);
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
        public void DesktopIcon_IsTickedInTheWizard()
        {
            Assert.DoesNotContain("Flags: unchecked", TaskEntry("desktopicon"));
        }

        [Fact]
        public void StartAtSignIn_IsNotOfferedByTheInstaller()
        {
            // It's turned on from the app's Settings instead; the installer neither offers nor writes it.
            string[] lines = InnoScript().Split('\n').Select(l => l.Trim()).ToArray();
            Assert.DoesNotContain(lines, l => l.StartsWith("Name: \"autostart\"", StringComparison.Ordinal));
            Assert.DoesNotContain(lines, l => l.StartsWith("Root: HKCU; Subkey: \"Software\\Microsoft\\Windows\\CurrentVersion\\Run\"", StringComparison.Ordinal));
        }

        [Fact]
        public void Uninstall_StillRemovesTheSignInEntry_HoweverItWasSet()
        {
            // The app's Settings toggle (or an older installer) may have written it.
            Assert.Matches(@"RegDeleteValue\(HKEY_CURRENT_USER,\s+'Software\\Microsoft\\Windows\\CurrentVersion\\Run', 'PhilterDesktop'\);", InnoScript());
        }

        [Fact]
        public void ASilentInstall_AddsTheDesktopIconOnlyWhenAsked()
        {
            // The task is ticked, so the shortcut itself checks: the docs promise an unattended install adds
            // no desktop icon unless /TASKS or /MERGETASKS names it.
            string iss = InnoScript();
            string shortcut = Assert.Single(
                iss.Split('\n').Select(l => l.Trim()),
                l => l.StartsWith("Name: \"{autodesktop}", StringComparison.Ordinal));

            Assert.Contains("Tasks: desktopicon; Check: DesktopIconWanted", shortcut);
            Assert.Contains("Result := not WizardSilent or TaskRequestedOnCommandLine('desktopicon');", iss);
            Assert.Contains("if Pos('/TASKS=', Uppercase(Param)) = 1 then", iss);
            Assert.Contains("else if Pos('/MERGETASKS=', Uppercase(Param)) = 1 then", iss);
            Assert.Contains("if (Item = Lowercase(Name)) or (Item = '*' + Lowercase(Name)) then", iss);
        }

        private static string TaskEntry(string task) => Assert.Single(
            InnoScript().Split('\n').Select(l => l.Trim()),
            l => l.StartsWith("Name: \"" + task + "\"", StringComparison.Ordinal));

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

        // The app's first-run screen is the one place users accept the EULA (per Windows user, per EULA
        // version); a setup-wizard license page would be a second, unrecorded acceptance.
        [Fact]
        public void Installer_HasNoLicensePage() =>
            Assert.DoesNotMatch(@"(?m)^\s*LicenseFile\s*=", InnoScript());

        // The app still reads the EULA from the file next to the exe, so the installer must ship it.
        [Fact]
        public void Installer_StillShipsTheEulaFile() =>
            Assert.Contains(@"Source: ""{#SharedDir}\philterd-eula.txt""; DestDir: ""{app}""", InnoScript());

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
