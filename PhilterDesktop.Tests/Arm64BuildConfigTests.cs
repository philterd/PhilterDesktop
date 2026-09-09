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
using System.Xml.Linq;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Windows-on-ARM needs a native win-arm64 build: under x64 emulation the native ONNX Runtime
    /// fails to initialize, so on-device name detection crashes. Both architectures ship in a single
    /// installer that picks the native build at install time, so nobody has to choose by hardware.
    /// These guard that wiring, so a regression that drops arm64, ships the wrong arch's files, or
    /// splits the download back into two is caught in CI rather than only on an ARM machine. They
    /// assert on the repo's build files (csproj, build-setup.ps1, PhilterDesktop.iss), not runtime
    /// behavior.
    /// </summary>
    public sealed class Arm64BuildConfigTests
    {
        [Fact]
        public void Csproj_DeclaresBothX64AndArm64Rids()
        {
            XDocument csproj = XDocument.Load(RepoPath("PhilterDesktop", "PhilterDesktop.csproj"));
            string? rids = csproj.Descendants("RuntimeIdentifiers").FirstOrDefault()?.Value;

            Assert.False(string.IsNullOrWhiteSpace(rids), "<RuntimeIdentifiers> is missing from PhilterDesktop.csproj.");
            string[] declared = rids!.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Assert.Contains("win-x64", declared);
            Assert.Contains("win-arm64", declared);
        }

        [Fact]
        public void BuildScript_PublishesBothRids_NotHardCodedToX64()
        {
            string script = BuildScript();

            // A -Runtime parameter constrained to the two supported RIDs, defaulting to BOTH.
            Assert.Contains("$Runtime", script);
            Assert.Contains("ValidateSet('win-x64', 'win-arm64')", script);
            Assert.Contains("[string[]]$Runtime = @('win-x64', 'win-arm64')", script);

            // The publish loops over the RIDs, not a literal win-x64.
            Assert.Contains("foreach ($rid in $Runtime)", script);
            Assert.Contains("'-r', $rid", script);
            Assert.DoesNotContain("'-r', 'win-x64'", script);
            Assert.DoesNotContain("\\win-x64\\publish", script);
        }

        [Fact]
        public void BuildScript_HandsBothPublishDirsToIscc()
        {
            string script = BuildScript();

            // The .iss needs both trees to build the combined installer.
            Assert.Contains("/DPublishDirX64=", script);
            Assert.Contains("/DPublishDirArm64=", script);

            // The old single-arch defines must be gone, or ISCC would silently use the .iss fallbacks.
            Assert.DoesNotContain("/DPublishDir=", script);
            Assert.DoesNotContain("/DArch=", script);
            Assert.DoesNotContain("/DArchLabel=", script);
        }

        [Fact]
        public void BuildScript_ProducesOneInstaller_WithNoArchTagInTheName()
        {
            string script = BuildScript();

            Assert.Contains("PhilterDesktop-Setup-$Version.exe", script);
            Assert.DoesNotContain("PhilterDesktop-Setup-$Version-$archLabel.exe", script);

            // The Output cleanup must stay scoped to the version being built: a wildcard over every
            // version would delete previously released installers archived there.
            Assert.DoesNotContain("PhilterDesktop-Setup-*.exe", script);
            Assert.Contains("PhilterDesktop-Setup-$Version-arm64.exe", script);
        }

        [Fact]
        public void BuildScript_SkipsTheInstallerWhenOnlyOneRidWasPublished()
        {
            string script = BuildScript();

            // A single-RID run is a dev shortcut. Packaging must be gated on having BOTH trees;
            // compiling from one would ship an installer that is empty on the other architecture.
            Assert.Contains("$publishDirs.ContainsKey('x64') -and $publishDirs.ContainsKey('arm64')", script);
        }

        [Fact]
        public void InnoSetupScript_AllowsBothArchitectures()
        {
            string iss = InnoScript();

            Assert.Contains("ArchitecturesAllowed=x64compatible or arm64", iss);
            Assert.Contains("ArchitecturesInstallIn64BitMode=x64compatible or arm64", iss);

            // One download for everyone: no arch tag in the output filename.
            Assert.Contains("OutputBaseFilename=PhilterDesktop-Setup-{#AppVersion}", iss);
            Assert.DoesNotContain("{#ArchLabel}", iss);
        }

        [Fact]
        public void InnoSetupScript_InstallsExactlyOneArchitecturesFiles()
        {
            string[] archEntries = FilesEntries()
                .Where(line => line.Contains("PublishDirX64") || line.Contains("PublishDirArm64"))
                .ToArray();

            Assert.Equal(2, archEntries.Length);

            string arm64Entry = Assert.Single(archEntries, line => line.Contains("{#PublishDirArm64}"));
            string x64Entry = Assert.Single(archEntries, line => line.Contains("{#PublishDirX64}"));

            // The guards must be mutually exclusive and complete, so exactly one set is installed.
            Assert.Matches(@"Check:\s*IsArm64", arm64Entry);
            Assert.Matches(@"Check:\s*not IsArm64", x64Entry);
        }

        [Fact]
        public void InnoSetupScript_DoesNotGuardTheX64FilesWithIsX64Compatible()
        {
            // IsX64Compatible is TRUE on Windows-on-ARM (it can emulate x64), so using it as the x64
            // guard would install BOTH sets on ARM: the x64 natives would overwrite the arm64 ones
            // and on-device name detection would fail exactly where the arm64 build was meant to help.
            // Only the entries matter; the identifier is named in a comment explaining this.
            Assert.DoesNotContain(FilesEntries(), entry => entry.Contains("IsX64Compatible"));
        }

        [Fact]
        public void InnoSetupScript_ShipsTheArchNeutralModelOnlyOnce()
        {
            string[] entries = FilesEntries();

            // The PhEye model is ~89 MB and byte-identical across arches: exactly one entry sources it,
            // and that entry is unconditional (no Check), so it lands on every machine.
            string modelEntry = Assert.Single(entries, line => line.Contains(@"\Models\"));
            Assert.DoesNotContain("Check:", modelEntry);

            // The per-arch entries must exclude it, or it would be stored twice.
            foreach (string entry in entries.Where(e => e.Contains("PublishDirX64") || e.Contains("PublishDirArm64")))
            {
                Assert.Contains(@"Models\*", entry);
                Assert.Contains("Excludes:", entry);
            }
        }

        [Theory]
        // Publish flattens natives to the app root, so same-named files are overwritten on a switch.
        // Only these arch-NAMED files would survive, one pattern per arch token they carry.
        [InlineData(@"{app}\*.arm64.dll", "not IsArm64")]
        [InlineData(@"{app}\*_arm64_*.dll", "not IsArm64")]
        [InlineData(@"{app}\*.amd64.dll", "IsArm64")]
        [InlineData(@"{app}\*_amd64_*.dll", "IsArm64")]
        public void InnoSetupScript_DeletesTheOtherArchitecturesNamedFiles(string pattern, string check)
        {
            string entry = Assert.Single(
                SectionEntries("InstallDelete"),
                line => line.Contains("Name: \"" + pattern + "\"", StringComparison.Ordinal));

            Assert.Matches(@"Check:\s*" + Regex.Escape(check) + @"\s*$", entry);
            Assert.Contains("Type: files", entry);
        }

        [Fact]
        public void InnoSetupScript_DeletesCannotMatchTheFilesBeingInstalled()
        {
            // The guard and the arch token must agree: deleting "*_amd64_*.dll" under "not IsArm64"
            // would wipe files the x64 install just needs.
            foreach (string entry in SectionEntries("InstallDelete"))
            {
                if (entry.Contains("amd64"))
                {
                    Assert.Matches(@"Check:\s*IsArm64\s*$", entry);
                }
                else if (entry.Contains("arm64"))
                {
                    Assert.Matches(@"Check:\s*not IsArm64\s*$", entry);
                }
            }
        }

        [Fact]
        public void InnoSetupScript_RecordsWhichArchitectureWasInstalled()
        {
            // Support and the release checklist use this to confirm a machine got the native build.
            string iss = InnoScript();

            Assert.Contains("InstalledArch", iss);
            Assert.Contains("function GetCurrentArch", iss);
            Assert.Contains("{code:GetCurrentArch}", iss);
        }

        [Fact]
        public void Ci_BuildsTheInstallerViaBuildScript()
        {
            // build-setup.ps1 publishes both arches and builds the combined installer by default, so
            // CI just invokes it (no per-arch pin). The both-arches default is asserted above.
            string ci = File.ReadAllText(RepoPath(".github", "workflows", "ci.yml"));

            Assert.Contains("build-setup.ps1", ci);
        }

        private static string BuildScript() => File.ReadAllText(RepoPath("Installer", "build-setup.ps1"));

        private static string InnoScript() => File.ReadAllText(RepoPath("Installer", "PhilterDesktop.iss"));

        private static string[] FilesEntries() => SectionEntries("Files");

        /// <summary>Returns a section's entry lines, skipping comments and blank lines.</summary>
        private static string[] SectionEntries(string section)
        {
            string[] lines = InnoScript().Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
            int start = Array.FindIndex(lines, l => l.Trim().Equals("[" + section + "]", StringComparison.OrdinalIgnoreCase));
            Assert.True(start >= 0, "PhilterDesktop.iss has no [" + section + "] section.");

            int end = Array.FindIndex(lines, start + 1, l => Regex.IsMatch(l.Trim(), @"^\[\w+\]$"));
            if (end < 0)
            {
                end = lines.Length;
            }

            return lines[(start + 1)..end]
                .Select(l => l.Trim())
                .Where(l => l.Length > 0 && !l.StartsWith(';'))
                .ToArray();
        }

        private static string RepoPath(params string[] parts)
        {
            for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            {
                string candidate = Path.Combine(new[] { dir.FullName }.Concat(parts).ToArray());
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            throw new FileNotFoundException("Could not locate " + Path.Combine(parts) + " from " + AppContext.BaseDirectory);
        }
    }
}
