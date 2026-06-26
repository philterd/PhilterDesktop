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
    /// <summary>A named starting-point policy a user can create a new policy from.</summary>
    internal sealed record PolicyTemplate(string Id, string Name, string Description, Func<string> BuildJson);

    /// <summary>
    /// Built-in starting-point policies. They're constructed via the Phileas object model (so every
    /// key matches the engine and validates against the PhiSQL schema) and use on-device PhEye for
    /// names — unlike the raw upstream policy library, whose name key (<c>person</c>) is silently
    /// dropped by phileas-dotnet. They are templates, not compliance guarantees (see <see cref="Disclaimer"/>).
    /// </summary>
    internal static class PolicyTemplates
    {
        /// <summary>Point-of-use disclaimer shown wherever a template is chosen.</summary>
        public const string Disclaimer =
            "Templates are starting points, not compliance guarantees. Any name that references a law or " +
            "standard (such as \"HIPAA Safe Harbor\") is for reference only and is not a certification of " +
            "compliance. Review and adapt the policy, and always check the redacted output, before relying on it.";

        public static IReadOnlyList<PolicyTemplate> All { get; } = new[]
        {
            new PolicyTemplate(
                "common-pii",
                "Common PII (recommended)",
                "High-confidence identifiers — Social Security numbers, email, phone, credit cards, bank/IBAN, " +
                "passport, driver's license, IP/MAC, VIN, ZIP — plus on-device name detection. A safe, " +
                "general-purpose starting point with few false positives.",
                DefaultPolicy.Json),

            new PolicyTemplate(
                "hipaa-safe-harbor",
                "HIPAA Safe Harbor (template)",
                "Broad coverage aimed at the HIPAA Safe Harbor identifiers: names, dates, phone, email, SSN, " +
                "IP and URLs, ZIP codes, ages, geographic terms (city/county), and hospital names. " +
                "Deliberately aggressive — expect some over-redaction to review.",
                BuildHipaaSafeHarbor),

            new PolicyTemplate(
                "legal-court-filing",
                "Legal court filing (template)",
                "Targets personal data commonly redacted in court filings: names, Social Security numbers, " +
                "dates, and financial account numbers (credit card, bank routing, IBAN). A starting point " +
                "for e-discovery and filing redaction.",
                BuildLegalCourtFiling),

            new PolicyTemplate(
                "financial-records",
                "Financial records (template)",
                "Targets financial and account data: names, Social Security numbers, credit cards, bank " +
                "routing numbers, IBAN codes, and crypto addresses. A starting point for statements, " +
                "applications, and other financial documents.",
                BuildFinancialRecords),
        };

        private static PhEye Names() => PhEyeModel.CreateDefaultFilter();

        private static string BuildHipaaSafeHarbor()
        {
            var policy = new PhileasPolicy
            {
                Name = "HIPAA Safe Harbor",
                Identifiers = new Identifiers
                {
                    Ssn = new Ssn(),
                    EmailAddress = new EmailAddress(),
                    PhoneNumber = new PhoneNumber(),
                    PhoneNumberExtension = new PhoneNumberExtension(),
                    Date = new Date(),
                    Age = new Age(),
                    IpAddress = new IpAddress(),
                    Url = new Url(),
                    ZipCode = new ZipCode(),
                    City = new City(),
                    County = new County(),
                    Hospital = new Hospital(),
                    PhEyes = new List<PhEye> { Names() }
                }
            };
            return PolicySerializer.SerializeToJson(policy);
        }

        private static string BuildLegalCourtFiling()
        {
            var policy = new PhileasPolicy
            {
                Name = "Legal court filing",
                Identifiers = new Identifiers
                {
                    Ssn = new Ssn(),
                    Date = new Date(),
                    CreditCard = new CreditCard(),
                    BankRoutingNumber = new BankRoutingNumber(),
                    IbanCode = new IbanCode(),
                    PhEyes = new List<PhEye> { Names() }
                }
            };
            return PolicySerializer.SerializeToJson(policy);
        }

        private static string BuildFinancialRecords()
        {
            var policy = new PhileasPolicy
            {
                Name = "Financial records",
                Identifiers = new Identifiers
                {
                    Ssn = new Ssn(),
                    CreditCard = new CreditCard(),
                    BankRoutingNumber = new BankRoutingNumber(),
                    IbanCode = new IbanCode(),
                    BitcoinAddress = new BitcoinAddress(),
                    PhEyes = new List<PhEye> { Names() }
                }
            };
            return PolicySerializer.SerializeToJson(policy);
        }
    }
}
