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
    /// A single, app-wide <see cref="FilterService"/>, shared so the on-device name-detection model
    /// (PhEye/GLiNER, ~90&#160;MB) loads <b>once</b> for the whole app instead of being reloaded for every
    /// redaction or preview (which made PDF previews with name detection take tens of seconds each time).
    ///
    /// The service is <b>not</b> stateless: it keeps RANDOM_REPLACE replacements consistent within a
    /// context. Call <see cref="UseContextService"/> at startup to back that with the durable
    /// (database) context service so the mappings persist across documents/restarts rather than
    /// accumulating in memory. Concurrent use is safe (the context service is thread-safe).
    /// </summary>
    internal static class SharedFilterService
    {
        private static IContextService? _contextService;
        private static FilterService? _instance;

        /// <summary>
        /// Sets the (durable) context service the shared <see cref="FilterService"/> uses for consistent
        /// replacements. Call once at startup, before the first redaction; rebuilds the instance so every
        /// later redaction uses it.
        /// </summary>
        public static void UseContextService(IContextService contextService)
        {
            _contextService = contextService;
            _instance = null;
        }

        public static FilterService Instance =>
            _instance ??= _contextService is null ? new FilterService() : new FilterService(_contextService);

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
