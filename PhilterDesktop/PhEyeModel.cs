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
        /// User-facing warning shown when the on-device name model is missing: name detection silently
        /// does nothing, so a policy expecting to catch person names will leave them in. Surfaced loudly
        /// (main window, redaction previews, command line) rather than failing quietly.
        /// </summary>
        public const string UnavailableWarning =
            "On-device name detection is unavailable, so person names will NOT be redacted by policies " +
            "that look for names. Reinstall Philter Desktop to restore name detection.";

        /// <summary>
        /// True when <paramref name="policy"/> asks for on-device name detection but the model is not
        /// available — i.e. names the policy expects to catch would silently be left in. Only the bundled
        /// name entry needs the app's model; user-added local models carry their own path and are ignored
        /// here. Check this <b>before</b> <see cref="Prepare"/>, which strips the bundled entry when the
        /// model is missing.
        /// </summary>
        public static bool RequestedButUnavailable(PhileasPolicy policy) =>
            !IsAvailable
            && policy.Identifiers?.PhEyes is { } phEyes
            && phEyes.Any(IsBundledNameEntry);

        /// <summary>
        /// True for the bundled on-device name entry: it has no explicit model path, so <see cref="Prepare"/>
        /// injects the app's bundled model directory at redaction time (kept out of the stored policy so it
        /// stays portable).
        /// </summary>
        public static bool IsBundledNameEntry(PhEye phEye) =>
            string.IsNullOrWhiteSpace(phEye.PhEyeConfiguration?.ModelPath);

        /// <summary>
        /// True for a user-added local model entry: it carries its own on-disk model path (and its own
        /// entity types), and runs independently of the bundled name model.
        /// </summary>
        public static bool IsUserLocalModel(PhEye phEye) => !IsBundledNameEntry(phEye);

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
        /// Prepares <paramref name="policy"/> for redaction. The bundled name entry (no model path) has the
        /// app's model directory injected and default labels/threshold filled in; if the bundled model is
        /// unavailable that entry is dropped so redaction still runs with the remaining filters. User-added
        /// local models (their own model path) are kept as-is and run independently of the bundled model.
        /// </summary>
        /// <returns>True if the bundled on-device name detection will run for this policy.</returns>
        public static bool Prepare(PhileasPolicy policy)
        {
            List<PhEye>? phEyes = policy.Identifiers?.PhEyes;
            if (phEyes is null || phEyes.Count == 0)
            {
                return false;
            }

            bool bundledAvailable = IsAvailable;
            string bundledDir = ModelDirectory;
            bool bundledWillRun = false;

            var kept = new List<PhEye>(phEyes.Count);
            foreach (PhEye phEye in phEyes)
            {
                phEye.PhEyeConfiguration ??= new PhEyeConfiguration();
                PhEyeConfiguration config = phEye.PhEyeConfiguration;

                if (string.IsNullOrWhiteSpace(config.ModelPath))
                {
                    // Bundled on-device name model: inject the app's model path. Without it, drop the entry
                    // so redaction still runs (instead of failing on a missing model directory).
                    if (!bundledAvailable)
                    {
                        continue;
                    }
                    config.ModelPath = bundledDir;
                    if (config.Labels is null || config.Labels.Count == 0)
                    {
                        config.Labels = new List<string>(DefaultLabels);
                    }
                    if (config.Threshold <= 0)
                    {
                        config.Threshold = DefaultThreshold;
                    }
                    bundledWillRun = true;
                }
                else if (config.Threshold <= 0)
                {
                    // User-added local model: keep its own path and labels; only supply a threshold default.
                    config.Threshold = DefaultThreshold;
                }

                kept.Add(phEye);
            }

            policy.Identifiers!.PhEyes = kept.Count > 0 ? kept : null;
            return bundledWillRun;
        }
    }
}
