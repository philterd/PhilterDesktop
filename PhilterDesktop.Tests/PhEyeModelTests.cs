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

using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    public sealed class PhEyeModelTests
    {
        private const string SkipReason =
            "PhEye model not bundled (run scripts/download-pheye-model.ps1 or set PHEYE_MODEL_DIR).";

        [Fact]
        public void CreateDefaultFilter_UsesNameLabel_AndDefaultThreshold_WithNoModelPath()
        {
            PhEye filter = PhEyeModel.CreateDefaultFilter();

            Assert.True(filter.Enabled);
            Assert.NotNull(filter.PhEyeConfiguration);
            Assert.Equal(new[] { "name" }, filter.PhEyeConfiguration.Labels);
            Assert.Equal(0.5, filter.PhEyeConfiguration.Threshold);
            // Portable: the runtime model path is injected at redaction, not stored in the policy.
            Assert.True(string.IsNullOrEmpty(filter.PhEyeConfiguration.ModelPath));
        }

        [Fact]
        public void Prepare_NoPhEyeFilters_ReturnsFalse_AndDoesNothing()
        {
            var policy = new PhileasPolicy { Name = "p", Identifiers = new Identifiers() };

            Assert.False(PhEyeModel.Prepare(policy));
            Assert.Null(policy.Identifiers.PhEyes);
        }

        [Fact]
        public void HasModelFiles_MissingDirectory_IsFalse()
        {
            Assert.False(PhEyeModel.HasModelFiles(null));
            Assert.False(PhEyeModel.HasModelFiles(Path.Combine(Path.GetTempPath(), "no-such-pheye-" + Guid.NewGuid().ToString("N"))));
        }

        [Fact]
        public void Prepare_WhenModelUnavailable_RemovesPhEyeFiltersInsteadOfFailing()
        {
            string emptyDir = Path.Combine(Path.GetTempPath(), "pheye-empty-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(emptyDir);
            string? previous = Environment.GetEnvironmentVariable("PHEYE_MODEL_DIR");
            try
            {
                Environment.SetEnvironmentVariable("PHEYE_MODEL_DIR", emptyDir); // no model files here
                Assert.False(PhEyeModel.IsAvailable);

                var policy = new PhileasPolicy
                {
                    Name = "p",
                    Identifiers = new Identifiers { PhEyes = new List<PhEye> { PhEyeModel.CreateDefaultFilter() } }
                };

                Assert.False(PhEyeModel.Prepare(policy));
                Assert.Null(policy.Identifiers.PhEyes); // stripped so redaction still runs
            }
            finally
            {
                Environment.SetEnvironmentVariable("PHEYE_MODEL_DIR", previous);
                try { Directory.Delete(emptyDir, recursive: true); } catch { /* best effort */ }
            }
        }

        [SkippableFact]
        public void Prepare_InjectsBundledModelPath()
        {
            Skip.IfNot(PhEyeModel.IsAvailable, SkipReason);

            var policy = new PhileasPolicy
            {
                Name = "p",
                Identifiers = new Identifiers { PhEyes = new List<PhEye> { PhEyeModel.CreateDefaultFilter() } }
            };

            Assert.True(PhEyeModel.Prepare(policy));
            PhEye filter = Assert.Single(policy.Identifiers.PhEyes!);
            Assert.Equal(PhEyeModel.ModelDirectory, filter.PhEyeConfiguration!.ModelPath);
        }

        [SkippableFact]
        public async Task RedactionService_RedactsNames_ViaBundledModel()
        {
            Skip.IfNot(PhEyeModel.IsAvailable, SkipReason);

            string dir = Path.Combine(Path.GetTempPath(), "pheye-svc-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            string input = Path.Combine(dir, "in.txt");
            string output = Path.Combine(dir, "out.txt");
            try
            {
                await File.WriteAllTextAsync(input, "Please forward the invoice to Toni Levine today.");

                var policy = new PhileasPolicy
                {
                    Name = "pheye",
                    Identifiers = new Identifiers { PhEyes = new List<PhEye> { PhEyeModel.CreateDefaultFilter() } }
                };

                // RedactionService.Prepare injects the bundled model path internally.
                await RedactionService.RedactFileAsync(input, output, policy, "ctx");

                string redacted = await File.ReadAllTextAsync(output);
                Assert.DoesNotContain("Toni Levine", redacted);
                Assert.Contains("REDACTED", redacted);
            }
            finally
            {
                try { Directory.Delete(dir, recursive: true); } catch { /* best effort */ }
            }
        }

        [SkippableFact]
        public void OnDeviceModel_RedactsPersonNames()
        {
            Skip.IfNot(PhEyeModel.IsAvailable, SkipReason);

            var policy = new PhileasPolicy
            {
                Name = "pheye",
                Identifiers = new Identifiers { PhEyes = new List<PhEye> { PhEyeModel.CreateDefaultFilter() } }
            };
            PhEyeModel.Prepare(policy);

            const string input = "Please forward the invoice to Toni Levine and copy Maria Gonzalez.";
            var result = new FilterService().Filter(policy, "ctx", 0, input);

            Assert.DoesNotContain("Toni Levine", result.FilteredText);
            Assert.DoesNotContain("Maria Gonzalez", result.FilteredText);
            Assert.True(result.Spans.Count >= 2, $"expected >=2 name spans, got {result.Spans.Count}");
            Assert.All(result.Spans, s => Assert.Equal("name", s.Classification));
        }
    }
}
