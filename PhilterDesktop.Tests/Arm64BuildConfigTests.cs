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

using System.Xml.Linq;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Windows-on-ARM needs a native win-arm64 build: under x64 emulation the native ONNX Runtime
    /// fails to initialize, so on-device name detection crashes. These guard the build wiring that
    /// produces the arm64 installer, so a regression that drops arm64 (back to x64-only) is caught in
    /// CI rather than only when someone tries to install on an ARM machine. They assert on the repo's
    /// build files (csproj, build-setup.ps1, PhilterDesktop.iss), not runtime behavior.
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
        public void BuildScript_IsParameterizedByRid_NotHardCodedToX64()
        {
            string script = File.ReadAllText(RepoPath("Installer", "build-setup.ps1"));

            // A -Runtime parameter constrained to the two supported RIDs, defaulting to BOTH.
            Assert.Contains("$Runtime", script);
            Assert.Contains("ValidateSet('win-x64', 'win-arm64')", script);
            Assert.Contains("[string[]]$Runtime = @('win-x64', 'win-arm64')", script);

            // The publish loops over the RIDs, not a literal win-x64.
            Assert.Contains("foreach ($rid in $Runtime)", script);
            Assert.Contains("'-r', $rid", script);
            Assert.DoesNotContain("'-r', 'win-x64'", script);
            Assert.DoesNotContain("\\win-x64\\publish", script);

            // The installer filename check must carry the arch tag (two per-arch installers).
            Assert.Contains("PhilterDesktop-Setup-$Version-$archLabel.exe", script);
        }

        [Fact]
        public void InnoSetupScript_IsArchParameterized()
        {
            string iss = File.ReadAllText(RepoPath("Installer", "PhilterDesktop.iss"));

            // Arch identifier and filename tag are overridable defines (build-setup passes them per RID).
            Assert.Contains("#ifndef Arch", iss);
            Assert.Contains("#ifndef ArchLabel", iss);

            // The architecture directives and output name are driven by those defines, not literals.
            Assert.Contains("ArchitecturesAllowed={#Arch}", iss);
            Assert.Contains("ArchitecturesInstallIn64BitMode={#Arch}", iss);
            Assert.Contains("OutputBaseFilename=PhilterDesktop-Setup-{#AppVersion}-{#ArchLabel}", iss);
        }

        [Fact]
        public void Ci_BuildsInstallersViaBuildScript()
        {
            // build-setup.ps1 builds both arches by default, so CI just invokes it (no per-arch pin).
            // The both-arches guarantee is asserted on the script's default in the test above.
            string ci = File.ReadAllText(RepoPath(".github", "workflows", "ci.yml"));

            Assert.Contains("build-setup.ps1", ci);
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
