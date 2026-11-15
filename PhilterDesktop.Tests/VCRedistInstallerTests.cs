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

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// ONNX Runtime needs the Microsoft Visual C++ Runtime, which a clean Windows install may lack. The
    /// installer bundles Microsoft's redistributables and runs the matching one only when needed. These
    /// guard the pins (Installer\vcredist.json), the build-time verification (build-setup.ps1), and the
    /// install logic (Installer\vcredist.iss).
    /// </summary>
    public sealed class VCRedistInstallerTests
    {
        // ---- Pins ----

        [Theory]
        [InlineData("x64")]
        [InlineData("arm64")]
        public void Pins_HaveAFullSha256(string arch) =>
            Assert.Matches("^[0-9A-F]{64}$", Pin(arch).GetProperty("sha256").GetString());

        [Theory]
        [InlineData("x64")]
        [InlineData("arm64")]
        public void Pins_UseMicrosoftsVersionedHttpsUrl(string arch)
        {
            // The aka.ms link always points at the latest build, so its hash would drift; the versioned
            // download URL it redirects to doesn't change, and embeds the file's hash.
            JsonElement pin = Pin(arch);
            Uri url = new(pin.GetProperty("url").GetString()!);

            Assert.Equal(Uri.UriSchemeHttps, url.Scheme);
            Assert.Equal("download.visualstudio.microsoft.com", url.Host);
            Assert.EndsWith("/VC_redist." + arch + ".exe", url.AbsolutePath, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("/" + pin.GetProperty("sha256").GetString() + "/", url.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Pins_AreDistinctPerArchitecture()
        {
            Assert.NotEqual(Pin("x64").GetProperty("sha256").GetString(), Pin("arm64").GetProperty("sha256").GetString());
            Assert.NotEqual(Pin("x64").GetProperty("url").GetString(), Pin("arm64").GetProperty("url").GetString());
        }

        [Fact]
        public void PinnedVersion_IsAFourPartVersion() =>
            Assert.Matches(@"^14\.\d+\.\d+\.\d+$", PinnedVersion().ToString());

        [Theory]
        [InlineData("x64")]
        [InlineData("arm64")]
        public void PinnedVersion_IsNewEnoughForTheShippedOnnxRuntime(string arch)
        {
            // A runtime must be at least as new as the toolset that built the binary. An ONNX Runtime
            // upgrade built with a newer toolset fails here until the pins are updated (the build fails too).
            string dir = Path.Combine(AppContext.BaseDirectory, "runtimes", "win-" + arch, "native");
            string[] dlls = Directory.Exists(dir) ? Directory.GetFiles(dir, "onnxruntime*.dll") : [];
            Assert.NotEmpty(dlls);

            Version pinned = new(PinnedVersion().Major, PinnedVersion().Minor);
            foreach (string dll in dlls)
            {
                byte[] bytes = File.ReadAllBytes(dll);
                Assert.True(ImportsVCRuntime(bytes), Path.GetFileName(dll) + " no longer imports the VC++ Runtime; revisit bundling it.");
                Version linker = LinkerVersion(bytes);
                Assert.True(pinned >= linker, $"{Path.GetFileName(dll)} ({arch}) was linked with {linker}, newer than the pinned runtime {pinned}.");
            }
        }

        [Fact]
        public void Redistributables_AreNeverCommitted()
        {
            // build-setup.ps1 caches them under Installer\tools, which is gitignored.
            Assert.Contains(@"tools\vcredist\", BuildScript());
            Assert.Contains("Installer/tools/", File.ReadAllText(RepoPath(".gitignore")));
            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(RepoPath("Installer", "vcredist.json"))!, "vc_redist*.exe"));
        }

        // ---- Build script ----

        [Fact]
        public void BuildScript_RefusesADownloadThatDoesntMatchItsPin()
        {
            string script = BuildScript();

            Assert.Contains("vcredist.json", script);
            Assert.Contains("Get-FileHash $download -Algorithm SHA256", script);
            Assert.Matches(@"if \(\$hash -ne \$pin\.sha256\) \{\s+throw", script);
        }

        [Fact]
        public void BuildScript_RequiresAValidMicrosoftSignature()
        {
            string script = BuildScript();

            Assert.Contains("Get-AuthenticodeSignature $download", script);
            Assert.Contains("$sig.Status -ne 'Valid'", script);
            Assert.Contains("O=Microsoft Corporation", script);
        }

        [Fact]
        public void BuildScript_ReusesTheCacheOnlyWhenItsHashMatches() =>
            Assert.Contains("(Test-Path $path) -and (Get-FileHash $path -Algorithm SHA256).Hash -eq $pin.sha256", BuildScript());

        [Fact]
        public void BuildScript_FailsWhenThePinnedRuntimeIsTooOldForThePublishedDlls()
        {
            string script = BuildScript();

            Assert.Contains("Get-VCRuntimeRequirement $publishDir", script);
            Assert.Matches(@"if \(\$bundled -lt \$vcRuntimeRequired\) \{\s+throw", script);
        }

        [Fact]
        public void BuildScript_PassesTheRedistAndMinimumToTheInstaller()
        {
            string script = BuildScript();

            Assert.Contains("/DVCRedistDir=", script);
            Assert.Contains("/DVCRedistMinMajor=", script);
            Assert.Contains("/DVCRedistMinMinor=", script);
        }

        [Fact]
        public void BuildScript_DetectsTheSameRuntimeImportsAsTheTests()
        {
            string script = BuildScript();

            foreach (string dll in VCRuntimeImports)
            {
                Assert.Contains("'" + dll + "'", script);
            }
        }

        // ---- Installer ----

        [Fact]
        public void Installer_IncludesTheRuntimeLogic_AfterGetCurrentArch()
        {
            string iss = InnoScript();
            int include = iss.IndexOf("#include \"vcredist.iss\"", StringComparison.Ordinal);

            Assert.True(include >= 0);
            Assert.True(iss.IndexOf("function GetCurrentArch", StringComparison.Ordinal) < include);
        }

        [Fact]
        public void RuntimeLogic_AddsNothingUnlessTheBuildBundlesTheRedist()
        {
            // A direct ISCC run (no /DVCRedistDir) must still compile, so everything sits inside the guard.
            string[] lines = Lines(Include());
            int open = Array.IndexOf(lines, "#ifdef VCRedistDir");

            Assert.True(open >= 0);
            Assert.Equal("#endif", lines.Last(l => l.Length > 0));
            Assert.All(lines[..open], l => Assert.True(l.Length == 0 || l.StartsWith("//", StringComparison.Ordinal), l));
        }

        [Fact]
        public void RuntimeLogic_HasNoSemicolonComments_SinceItIsIncludedInsideCode() =>
            Assert.DoesNotContain(Lines(Include()), l => l.StartsWith(';'));

        [Theory]
        [InlineData("x64")]
        [InlineData("arm64")]
        public void Redistributables_AreBundledButNeverCopiedToTheAppFolder(string arch) =>
            Assert.Contains(@"Source: ""{#VCRedistDir}\vc_redist." + arch + @".exe""; Flags: dontcopy", Include());

        [Fact]
        public void RuntimeCheck_PicksTheArchitectureWithIsArm64First()
        {
            string include = Include();

            Assert.Matches(@"if IsArm64 then\s+Key := 'SOFTWARE\\Microsoft\\VisualStudio\\14\.0\\VC\\Runtimes\\ARM64'\s+else\s+Key := 'SOFTWARE\\Microsoft\\VisualStudio\\14\.0\\VC\\Runtimes\\x64'", include);
            Assert.Contains("FileName := 'vc_redist.' + GetCurrentArch('') + '.exe';", include);
        }

        [Fact]
        public void RuntimeCheck_LooksInBothRegistryViews() =>
            Assert.Contains("VCRuntimeRecorded(HKLM64) or VCRuntimeRecorded(HKLM32)", Include());

        [Fact]
        public void RuntimeCheck_RequiresInstalledAndTheMinimumVersion()
        {
            string include = Include();

            Assert.Contains("'Installed', Installed) and (Installed = 1)", include);
            Assert.Contains("(Major > VCRedistMinMajor) or ((Major = VCRedistMinMajor) and (Minor >= VCRedistMinMinor))", include);
        }

        [Fact]
        public void RuntimeInstall_IsSkippedWhenTheRuntimeIsCurrent()
        {
            string body = Body("procedure InstallVCRuntimeIfNeeded");
            int check = body.IndexOf("if VCRuntimeInstalled then", StringComparison.Ordinal);

            Assert.True(check >= 0);
            Assert.True(check < body.IndexOf("ExtractTemporaryFile", StringComparison.Ordinal));
            Assert.Matches(@"if VCRuntimeInstalled then\s+begin\s+Log\([^)]*\);\s+exit;", body);
        }

        [Fact]
        public void RuntimeInstall_RunsQuietlyWithoutRestarting()
        {
            string body = Body("procedure InstallVCRuntimeIfNeeded");

            Assert.Contains("'/install /quiet /norestart'", body);
            Assert.Contains("SW_HIDE, ewWaitUntilTerminated", body);
        }

        [Fact]
        public void RuntimeInstall_ElevatesOnlyForAPerUserInstall()
        {
            string body = Body("procedure InstallVCRuntimeIfNeeded");

            Assert.Matches(@"if IsAdmin then\s+Started := Exec\(Path, Params", body);
            Assert.Matches(@"else\s+Started := RunElevated\(Path, Params, Code\);", body);
        }

        [Fact]
        public void ElevatedRun_GetsTheExitCodeThroughCmd()
        {
            // ShellExec reports only whether it launched, never the exit code, so 3010 and failures
            // would be missed. cmd records the code in a file, read after the process exits.
            string body = Body("function RunElevated");

            Assert.Contains("ShellExec('runas', ExpandConstant('{cmd}'),", body);
            Assert.Contains("'/v:on /c \"\"' + Path + '\" ' + Params + ' & >\"' + CodeFile + '\" echo !errorlevel!\"'", body);
            Assert.Contains("SW_HIDE, ewWaitUntilTerminated, Code);", body);
            Assert.Contains("DeleteFile(CodeFile);", body);
            Assert.Contains("Code := StrToIntDef(Trim(String(Text)), -1)", body);
        }

        [Fact]
        public void ElevatedRun_ReadsTheCodeOnlyAfterALaunch()
        {
            // When the launch fails, Code is the launch error (1223 for a declined prompt), so keep it.
            Assert.Matches(@"if Result then\s+begin\s+if LoadStringFromFile\(CodeFile, Text\) then", Body("function RunElevated"));
        }

        [Fact]
        public void UnknownExitCode_FallsBackToTheRegistry()
        {
            string body = Body("procedure InstallVCRuntimeIfNeeded");

            Assert.Matches(@"if Started and \(Code = -1\) then\s+begin\s+if VCRuntimeInstalled then\s+Code := VCRedistOk;", body);
        }

        [Fact]
        public void SilentPerUserInstall_NeverShowsAUacPrompt()
        {
            // /VERYSILENT /CURRENTUSER is documented as prompt-free, so it skips instead of elevating.
            string body = Body("procedure InstallVCRuntimeIfNeeded");
            int guard = body.IndexOf("if not IsAdmin and WizardSilent then", StringComparison.Ordinal);

            Assert.True(guard >= 0);
            Assert.True(guard < body.IndexOf("RunElevated(", StringComparison.Ordinal));
            Assert.Matches(@"if not IsAdmin and WizardSilent then\s+begin\s+WarnVCRuntimeMissing\([^;]*\);\s+exit;", body);
        }

        [Fact]
        public void RestartRequired_IsReportedToSetup()
        {
            string include = Include();

            Assert.Contains("VCRedistRestartRequired = 3010;", include);
            Assert.Matches(@"else if Code = VCRedistRestartRequired then\s+begin\s+Log\([^)]*\);\s+VCRedistNeedsRestart := True;", include);
            Assert.Matches(@"function NeedRestart: Boolean;\s+begin\s+Result := VCRedistNeedsRestart;", include);
        }

        [Fact]
        public void SuccessCodes_AreZeroAndNewerAlreadyInstalled()
        {
            string include = Include();

            Assert.Contains("VCRedistOk = 0;", include);
            Assert.Contains("VCRedistNewerInstalled = 1638;", include);
            Assert.Contains("(Code = VCRedistOk) or (Code = VCRedistNewerInstalled)", include);
        }

        [Fact]
        public void Failures_WarnButNeverAbortTheInstall()
        {
            string body = Body("procedure InstallVCRuntimeIfNeeded");

            Assert.Contains("ErrorCancelled = 1223;", Include());
            Assert.Contains("WarnVCRuntimeMissing('administrator approval was declined')", body);
            Assert.Contains("WarnVCRuntimeMissing('its installer returned error ' + IntToStr(Code))", body);
            Assert.DoesNotContain("Abort", Include());
            Assert.DoesNotContain("RaiseException", Include());
        }

        [Fact]
        public void Warning_IsNeverShownToAnUnattendedInstall()
        {
            // Plain MsgBox ignores /SUPPRESSMSGBOXES; a bare /VERYSILENT would still stop on it.
            string body = Body("procedure WarnVCRuntimeMissing");

            Assert.Matches(@"Log\([^;]*\);\s+// [^\n]*\n\s+if not WizardSilent then\s+SuppressibleMsgBox\(", body);
            Assert.DoesNotContain("MsgBox(", body.Replace("SuppressibleMsgBox(", ""));
        }

        [Fact]
        public void RuntimeInstall_RunsBeforeSetupAsksAboutRestarting()
        {
            // Setup calls NeedRestart during the install step, before ssPostInstall; a 3010 recorded
            // after that would never produce the restart prompt.
            string include = Include();

            Assert.Matches(@"if CurStep = ssInstall then\s+InstallVCRuntimeIfNeeded;", include);
            Assert.DoesNotContain("ssPostInstall then", include);
        }

        [Fact]
        public void Warning_SaysNameRedactionFails_NotThatTheAppIsInstalled()
        {
            // It's shown before the app's files are copied, and without the runtime those redactions error.
            string body = Body("procedure WarnVCRuntimeMissing");

            Assert.Contains("Setup will finish installing {#AppName}", body);
            Assert.Matches(@"detects names will ' \+\s+'fail until the runtime is installed", body);
            Assert.DoesNotContain("is installed, but", body);
        }

        [Fact]
        public void InstallerText_HasNoEmDashes() =>
            Assert.DoesNotContain("—", Include());

        // ---- Helpers ----

        private static readonly string[] VCRuntimeImports =
            ["vcruntime140.dll", "vcruntime140_1.dll", "msvcp140.dll", "msvcp140_1.dll", "msvcp140_2.dll"];

        private static bool ImportsVCRuntime(byte[] pe)
        {
            string text = Encoding.Latin1.GetString(pe).ToLowerInvariant();
            return VCRuntimeImports.Any(name => text.Contains(name + "\0", StringComparison.Ordinal));
        }

        private static Version LinkerVersion(byte[] pe)
        {
            int optionalHeader = BitConverter.ToInt32(pe, 0x3C) + 24;
            return new Version(pe[optionalHeader + 2], pe[optionalHeader + 3]);
        }

        private static JsonElement Pins() =>
            JsonDocument.Parse(File.ReadAllText(RepoPath("Installer", "vcredist.json"))).RootElement;

        private static JsonElement Pin(string arch) => Pins().GetProperty(arch);

        private static Version PinnedVersion() => Version.Parse(Pins().GetProperty("version").GetString()!);

        /// <summary>A Pascal routine's text, from its header to the next routine.</summary>
        private static string Body(string header)
        {
            string include = Include();
            int start = include.IndexOf(header, StringComparison.Ordinal);
            Assert.True(start >= 0, header + " is gone from vcredist.iss.");
            Match next = new Regex(@"\n(procedure|function) ").Match(include, start + header.Length);
            return next.Success ? include[start..next.Index] : include[start..];
        }

        private static string[] Lines(string text) => text.Split('\n').Select(l => l.TrimEnd('\r').Trim()).ToArray();

        private static string Include() => File.ReadAllText(RepoPath("Installer", "vcredist.iss")).ReplaceLineEndings("\n");

        private static string InnoScript() => File.ReadAllText(RepoPath("Installer", "PhilterDesktop.iss"));

        private static string BuildScript() => File.ReadAllText(RepoPath("Installer", "build-setup.ps1"));

        private static string RepoPath(params string[] parts)
        {
            for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            {
                string candidate = Path.Combine([dir.FullName, .. parts]);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            throw new FileNotFoundException("Could not locate " + Path.Combine(parts) + " from " + AppContext.BaseDirectory);
        }
    }
}
