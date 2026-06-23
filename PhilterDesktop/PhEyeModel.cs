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

using Phileas.Policy.Filters;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop
{
    /// <summary>
    /// Locates the on-device PhEye GLiNER model bundled with the app and prepares policies
    /// to use it. The bundled <c>ph-eye-pii-en-xsmall</c> model detects person names — the
    /// one PII type that genuinely needs a model because names have no fixed format and depend
    /// on context. Pattern-based filters (email, SSN, phone, …) handle the rest. Inference runs
    /// entirely in-process with no network call.
    /// </summary>
    internal static class PhEyeModel
    {
        /// <summary>Folder name of the bundled model (under <c>Models\</c> in the app directory).</summary>
        public const string ModelName = "ph-eye-pii-en-xsmall";

        /// <summary>The GLiNER detection prompt for the bundled model: it is trained for names.</summary>
        public static readonly IReadOnlyList<string> DefaultLabels = new[] { "name" };

        /// <summary>The model's recommended default confidence threshold.</summary>
        public const double DefaultThreshold = 0.5;

        /// <summary>
        /// Resolves the bundled model directory. The <c>PHEYE_MODEL_DIR</c> environment
        /// variable, when set, overrides the bundled location (used by tests/CI).
        /// </summary>
        public static string ModelDirectory
        {
            get
            {
                string? overridden = Environment.GetEnvironmentVariable("PHEYE_MODEL_DIR");
                return !string.IsNullOrWhiteSpace(overridden)
                    ? overridden
                    : Path.Combine(AppContext.BaseDirectory, "Models", ModelName);
            }
        }

        /// <summary>True when a usable GLiNER model directory is present on disk.</summary>
        public static bool IsAvailable => HasModelFiles(ModelDirectory);

        /// <summary>
        /// Returns true if <paramref name="dir"/> contains the files a <c>GlinerModel</c>
        /// needs: <c>gliner_config.json</c>, <c>spm.model</c>, and an ONNX graph (directly
        /// or under an <c>onnx\</c> subdirectory).
        /// </summary>
        public static bool HasModelFiles(string? dir)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                return false;
            }

            bool hasOnnx =
                File.Exists(Path.Combine(dir, "model.onnx")) ||
                File.Exists(Path.Combine(dir, "onnx", "model.onnx")) ||
                File.Exists(Path.Combine(dir, "model_quantized.onnx")) ||
                File.Exists(Path.Combine(dir, "onnx", "model_quantized.onnx"));

            return hasOnnx
                && File.Exists(Path.Combine(dir, "spm.model"))
                && File.Exists(Path.Combine(dir, "gliner_config.json"));
        }

        /// <summary>
        /// Builds a default PhEye filter entry (on-device name detection). The model path is
        /// intentionally left empty so policies stay portable; <see cref="Prepare"/> injects
        /// the runtime-resolved path at redaction time.
        /// </summary>
        public static PhEye CreateDefaultFilter() => new()
        {
            Enabled = true,
            PhEyeConfiguration = new PhEyeConfiguration
            {
                Labels = new List<string>(DefaultLabels),
                Threshold = DefaultThreshold
            }
        };

        /// <summary>
        /// Prepares <paramref name="policy"/> for redaction: injects the bundled model path into
        /// every PhEye entry that lacks one and fills in default labels/threshold. If no model is
        /// available, the PhEye filters are removed so redaction still runs (with the remaining
        /// filters) instead of failing on a missing model directory.
        /// </summary>
        /// <returns>True if on-device PhEye name detection will run for this policy.</returns>
        public static bool Prepare(PhileasPolicy policy)
        {
            List<PhEye>? phEyes = policy.Identifiers?.PhEyes;
            if (phEyes is null || phEyes.Count == 0)
            {
                return false;
            }

            if (!IsAvailable)
            {
                policy.Identifiers!.PhEyes = null;
                return false;
            }

            string dir = ModelDirectory;
            foreach (PhEye phEye in phEyes)
            {
                phEye.PhEyeConfiguration ??= new PhEyeConfiguration();
                PhEyeConfiguration config = phEye.PhEyeConfiguration;

                if (string.IsNullOrWhiteSpace(config.ModelPath))
                {
                    config.ModelPath = dir;
                }
                if (config.Labels is null || config.Labels.Count == 0)
                {
                    config.Labels = new List<string>(DefaultLabels);
                }
                if (config.Threshold <= 0)
                {
                    config.Threshold = DefaultThreshold;
                }
            }

            return true;
        }
    }
}
