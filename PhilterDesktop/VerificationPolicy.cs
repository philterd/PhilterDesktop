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

using System.Reflection;
using Phileas.Policy;
using Phileas.Policy.Filters;
using PhilterDesktop.PolicyEditing;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop
{
    /// <summary>
    /// Builds the "broad" policy used by the optional wide verification scan: every built-in detector
    /// enabled (discovered by reflection over the engine's <c>Identifiers</c>, so it never drifts from
    /// the engine) plus on-device name detection. Used to find residual PII of <i>types the redaction
    /// policy didn't look for</i> — at the cost of flagging things the user may have chosen not to redact.
    /// </summary>
    internal static class VerificationPolicy
    {
        public static PhileasPolicy Broad()
        {
            var identifiers = new Identifiers();

            foreach (PropertyInfo property in FilterCatalog.Discover().Values)
            {
                if (property.CanWrite && property.PropertyType.GetConstructor(Type.EmptyTypes) is not null)
                {
                    property.SetValue(identifiers, Activator.CreateInstance(property.PropertyType));
                }
            }

            // On-device name detection (skipped gracefully if no model is bundled).
            identifiers.PhEyes = new List<PhEye> { PhEyeModel.CreateDefaultFilter() };

            return new PhileasPolicy { Name = "Broad verification", Identifiers = identifiers };
        }
    }
}
