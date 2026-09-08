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
    /// Guards how build-setup.ps1 picks signtool.exe and the Azure Trusted Signing dlib. signtool
    /// loads the dlib into its own process, so the two must share an architecture. When they don't,
    /// signtool ignores /dlib without saying so, falls back to the local certificate store, and fails
    /// with "Multiple certificates were found" - which looks like an Azure authentication problem and
    /// sends you chasing the wrong thing. The Windows SDK puts a 32-bit signtool on PATH, so
    /// preferring PATH silently produced exactly that on an x64 machine.
    /// </summary>
    public sealed class SigningToolConfigTests
    {
        [Fact]
        public void Signtool_PrefersTheSdkCopyOverWhateverIsOnPath()
        {
            string script = BuildScript();
            int sdkLookup = script.IndexOf(@"Windows Kits\10\bin\*\$arch\signtool.exe", StringComparison.Ordinal);
            int pathLookup = script.IndexOf("Get-Command signtool.exe", StringComparison.Ordinal);

            Assert.True(sdkLookup >= 0, "The SDK signtool lookup is gone from build-setup.ps1.");
            Assert.True(pathLookup >= 0, "The PATH signtool lookup is gone from build-setup.ps1.");
            Assert.True(
                sdkLookup < pathLookup,
                "Resolve-Signtool must try the host-arch SDK copy BEFORE PATH: the SDK puts a 32-bit " +
                "signtool on PATH, which cannot load the 64-bit Trusted Signing dlib.");
        }

        [Fact]
        public void Dlib_IsChosenToMatchTheResolvedSigntool_NotJustTheHost()
        {
            // The dlib is loaded into signtool, so an explicit -SigntoolPath of a different
            // architecture must still get a matching dlib. Selecting on $hostArch would not.
            string script = BuildScript();

            Assert.Contains(@"foreach ($arch in @($script:ToolArch, 'x64') | Select-Object -Unique)", script);
            Assert.Contains("$script:ToolArch = Get-PeMachine $script:Signtool", script);
        }

        [Fact]
        public void Signtool_IsResolvedBeforeTheDlib()
        {
            // Ordering is what makes the match possible: the dlib choice depends on the signtool.
            string script = BuildScript();
            int signtool = script.IndexOf("$script:Signtool = Resolve-Signtool", StringComparison.Ordinal);
            int dlib = script.IndexOf("$SigningDlib = Resolve-SigningDlib", StringComparison.Ordinal);

            Assert.True(signtool >= 0 && dlib >= 0, "The signing setup block no longer resolves both tools.");
            Assert.True(signtool < dlib, "signtool must be resolved before the dlib, which is chosen to match it.");
        }

        [Fact]
        public void ArchitectureMismatch_FailsWithItsOwnMessage_BeforeSigningIsAttempted()
        {
            // Hand-passed -SigntoolPath / -SigningDlib can still disagree. Catch that up front rather
            // than letting signtool report it as a certificate-selection problem.
            string script = BuildScript();

            Assert.Contains("$dlibArch = Get-PeMachine $SigningDlib", script);
            Assert.Contains("Architecture mismatch: signtool is", script);
        }

        [Fact]
        public void PeMachineReader_CoversTheThreeArchitecturesInPlay()
        {
            string script = BuildScript();

            Assert.Contains("0x8664", script);  // x64
            Assert.Contains("0xAA64", script);  // arm64
            Assert.Contains("0x014C", script);  // x86, the one that caused the failure
        }

        [Fact]
        public void SigningFailure_DoesNotSuggestSigningWithALocalCertificate()
        {
            // signtool's own error recommends /a or /sha1. Following it would sign with an unrelated
            // self-signed certificate from the local store, which is worse than failing.
            string script = BuildScript();

            Assert.Contains("do NOT retry with /a or", script);
        }

        private static string BuildScript()
        {
            for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            {
                string candidate = Path.Combine(dir.FullName, "Installer", "build-setup.ps1");
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }
            }
            throw new FileNotFoundException("Could not locate Installer\\build-setup.ps1 from " + AppContext.BaseDirectory);
        }
    }
}
