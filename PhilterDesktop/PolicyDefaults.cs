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
using PhilterData;
using PhilterDesktop.PolicyEditing;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop
{
    /// <summary>
    /// The starter "default" policy created on first run. It enables a conservative set of
    /// high-confidence, structured identifiers (low false-positive risk), dates (valid calendar dates
    /// only, to catch dates of birth), plus on-device PhEye name detection — so a brand-new user who
    /// clicks Redact actually gets meaningful redactions instead of an unchanged copy (an empty "{}"
    /// policy redacts nothing). Users can tailor it in the Policy Editor afterward.
    /// </summary>
    internal static class DefaultPolicy
    {
        /// <summary>Name of the starter policy.</summary>
        public const string Name = "default";

        /// <summary>
        /// Builds the default policy's JSON via the Phileas object model (so every identifier key and
        /// the PhEye entry match exactly what the engine expects). A filter is "enabled" simply by
        /// being present. PhEye's model path is injected at redaction time and, if the bundled model
        /// is absent (e.g. a dev build), PhEye is skipped gracefully.
        /// </summary>
        public static string Json()
        {
            var policy = new PhileasPolicy
            {
                Name = Name,
                Identifiers = new Identifiers
                {
                    // Structured, high-confidence identifiers (minimal false positives).
                    Ssn = new Ssn(),
                    EmailAddress = new EmailAddress(),
                    PhoneNumber = new PhoneNumber(),
                    PhoneNumberExtension = new PhoneNumberExtension(),
                    CreditCard = new CreditCard(),
                    BankRoutingNumber = new BankRoutingNumber(),
                    IbanCode = new IbanCode(),
                    PassportNumber = new PassportNumber(),
                    DriversLicense = new DriversLicense(),
                    IpAddress = new IpAddress(),
                    MacAddress = new MacAddress(),
                    Vin = new Vin(),
                    BitcoinAddress = new BitcoinAddress(),
                    TrackingNumber = new TrackingNumber(),
                    ZipCode = new ZipCode(),

                    // Dates (e.g. dates of birth). OnlyValidDates keeps it to real calendar dates to
                    // limit false positives on incidental number/slash text.
                    Date = new Date { OnlyValidDates = true },

                    // On-device name detection (no network, model bundled with the app).
                    PhEyes = new List<PhEye> { PhEyeModel.CreateDefaultFilter() }
                }
            };

            return PolicySerializer.SerializeToJson(policy);
        }
    }

    /// <summary>Decides when a redaction-completed tray notification should be shown.</summary>
    internal static class NotificationPolicy
    {
        /// <summary>
        /// True when a notification should be shown — i.e. the user isn't already looking at the
        /// window. That's the case when the window is hidden (in the tray) or minimized.
        /// </summary>
        public static bool ShouldNotify(bool windowVisible, FormWindowState windowState) =>
            !(windowVisible && windowState != FormWindowState.Minimized);

        /// <summary>
        /// As above, but also honoring the user's preference: when notifications are turned off in
        /// Settings, none are shown regardless of window state.
        /// </summary>
        public static bool ShouldNotify(bool enabled, bool windowVisible, FormWindowState windowState) =>
            enabled && ShouldNotify(windowVisible, windowState);
    }

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

    /// <summary>
    /// Helpers that keep watched folders from being orphaned when a policy is deleted: a folder that
    /// references a now-missing policy silently fails every file ("unknown policy"). The policy editor
    /// uses these to warn and reassign affected folders to the default policy.
    /// </summary>
    internal static class WatchedFolderPolicyGuard
    {
        public const string DefaultPolicyName = "default";

        /// <summary>The watched folders that reference <paramref name="policyName"/> (case-insensitive).</summary>
        public static List<WatchedFolderEntity> FoldersUsing(WatchedFolderRepository repository, string policyName) =>
            repository.GetAll()
                .Where(f => string.Equals(f.Policy, policyName, StringComparison.OrdinalIgnoreCase))
                .ToList();

        /// <summary>Switches each folder to the default policy and persists it.</summary>
        public static void ReassignToDefault(WatchedFolderRepository repository, IEnumerable<WatchedFolderEntity> folders)
        {
            foreach (WatchedFolderEntity folder in folders)
            {
                folder.Policy = DefaultPolicyName;
                repository.Update(folder);
            }
        }
    }
}
