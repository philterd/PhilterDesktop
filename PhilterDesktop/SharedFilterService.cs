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
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop
{
    /// <summary>
    /// A single, app-wide <see cref="FilterService"/>. The service is stateless and thread-safe, so
    /// sharing one instance is safe — and it lets the on-device name-detection model (PhEye/GLiNER,
    /// ~90&#160;MB) load <b>once</b> for the whole app instead of being reloaded for every redaction or
    /// preview, which was making PDF previews with name detection take tens of seconds each time.
    /// </summary>
    internal static class SharedFilterService
    {
        public static FilterService Instance { get; } = new();

        private static int _warmedUp;

        /// <summary>
        /// Loads the name model in the background (if bundled) so the first redaction that uses it
        /// doesn't pay the one-time model-load cost. Best-effort; runs at most once per app run.
        /// </summary>
        public static void WarmUp()
        {
            if (!PhEyeModel.IsAvailable || Interlocked.Exchange(ref _warmedUp, 1) != 0)
            {
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    var policy = new PhileasPolicy
                    {
                        Name = "warmup",
                        Identifiers = new Identifiers { PhEyes = new List<PhEye> { PhEyeModel.CreateDefaultFilter() } }
                    };
                    PhEyeModel.Prepare(policy);
                    Instance.Filter(policy, "warmup", 0, "John Smith called yesterday.");
                }
                catch
                {
                    // best effort — a warm-up failure must never affect the app
                }
            });
        }
    }
}
