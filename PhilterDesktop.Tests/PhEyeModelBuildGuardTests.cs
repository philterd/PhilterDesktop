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

using System.Diagnostics;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Person names are detected only by the bundled on-device model, so a Release build must include it
    /// or names ship unredacted (#557). These exercise the csproj build guard (EnsurePhEyeModelPresent):
    /// it fails a build whose model is missing/incomplete and passes when the model is complete, while a
    /// dev build (guard not enforced) is unaffected. Runs the target via `dotnet msbuild` with an
    /// overridden model directory, so it never triggers the real ~90 MB download.
    /// </summary>
    public sealed class PhEyeModelBuildGuardTests : IDisposable
    {
        private readonly string _dir;

        public PhEyeModelBuildGuardTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-model-guard-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        [Fact]
        public void Guard_Fails_WhenModelAbsent()
        {
            string emptyModelDir = NewModelDir(); // no files
            (int exit, string output) = RunGuard(emptyModelDir, enforce: true);

            Assert.NotEqual(0, exit);
            Assert.Contains("model is missing", output);
        }

        [Fact]
        public void Guard_Fails_WhenModelIncomplete()
        {
            string modelDir = NewModelDir("model.onnx"); // onnx present, companions missing
            (int exit, string output) = RunGuard(modelDir, enforce: true);

            Assert.NotEqual(0, exit);
            Assert.Contains("incomplete", output);
        }

        [Fact]
        public void Guard_Passes_WhenModelComplete()
        {
            string modelDir = NewModelDir("model.onnx", "gliner_config.json", "spm.model");
            (int exit, string output) = RunGuard(modelDir, enforce: true);

            Assert.True(exit == 0, $"expected success, got exit {exit}. Output:\n{output}");
        }

        [Fact]
        public void Guard_NotEnforced_AllowsAbsentModel()
        {
            // A local/dev build (Debug, or -p:SkipPhEyeModelDownload=true) does not enforce the guard.
            string emptyModelDir = NewModelDir();
            (int exit, string output) = RunGuard(emptyModelDir, enforce: false);

            Assert.True(exit == 0, $"dev build should not trip the guard, got exit {exit}. Output:\n{output}");
        }

        private string NewModelDir(params string[] files)
        {
            string dir = Path.Combine(_dir, "m-" + Guid.NewGuid().ToString("N"), "ph-eye-pii-en-xsmall");
            Directory.CreateDirectory(dir);
            foreach (string f in files)
            {
                File.WriteAllText(Path.Combine(dir, f), "test");
            }
            return dir;
        }

        private static (int exit, string output) RunGuard(string modelDir, bool enforce)
        {
            string csproj = FindCsproj();
            var psi = new ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("msbuild");
            psi.ArgumentList.Add(csproj);
            psi.ArgumentList.Add("-t:EnsurePhEyeModelPresent");
            psi.ArgumentList.Add("-nologo");
            psi.ArgumentList.Add("-v:m");
            psi.ArgumentList.Add("-p:SkipPhEyeModelDownload=true"); // never hit the network
            psi.ArgumentList.Add("-p:PhEyeModelDir=" + modelDir);
            if (enforce)
            {
                psi.ArgumentList.Add("-p:EnforcePhEyeModelPresent=true");
            }

            using var p = Process.Start(psi)!;
            Task<string> outTask = p.StandardOutput.ReadToEndAsync();
            Task<string> errTask = p.StandardError.ReadToEndAsync();
            if (!p.WaitForExit(180_000))
            {
                try { p.Kill(entireProcessTree: true); } catch { /* best effort */ }
                throw new TimeoutException("dotnet msbuild did not finish in time.");
            }
            return (p.ExitCode, outTask.Result + errTask.Result);
        }

        private static string FindCsproj()
        {
            for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            {
                string candidate = Path.Combine(dir.FullName, "PhilterDesktop", "PhilterDesktop.csproj");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            throw new FileNotFoundException("Could not locate PhilterDesktop.csproj from " + AppContext.BaseDirectory);
        }
    }
}
