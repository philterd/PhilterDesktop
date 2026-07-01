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
}
