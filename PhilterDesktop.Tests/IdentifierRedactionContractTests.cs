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
using Phileas.Services;
using PhilterDesktop;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;
using PhDictionary = Phileas.Policy.Filters.Dictionary;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Reflective guard over the editor↔redactor contract: every Phileas identifier type must, when enabled
    /// in a policy, actually redact a matching sample end-to-end (serialize → deserialize → filter). The
    /// reflection test fails the moment a new/renamed filter appears in <see cref="Identifiers"/> without a
    /// registered case, so a filter can never be added yet silently no-op (the earlier policy-model-drift bug).
    /// </summary>
    public sealed class IdentifierRedactionContractTests
    {
        /// <summary>One enabled-filter case: how to turn it on, a sample that should trip it, and the text
        /// that must be gone after redaction.</summary>
        private sealed record Case(string Property, Action<Identifiers> Enable, string Sample, string Sensitive, bool RequiresModel = false);

        private static readonly IReadOnlyList<Case> Cases = new[]
        {
            // Pattern / checksum filters.
            new Case("Age", i => i.Age = new Age(), "The patient is 45 years old.", "45 years old"),
            new Case("BankRoutingNumber", i => i.BankRoutingNumber = new BankRoutingNumber(), "Routing: 021000021", "021000021"),
            new Case("BitcoinAddress", i => i.BitcoinAddress = new BitcoinAddress(), "Send to 1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa"),
            new Case("CreditCard", i => i.CreditCard = new CreditCard(), "Card: 4111111111111111 on file.", "4111111111111111"),
            new Case("Currency", i => i.Currency = new Currency(), "Total: $1,200.50 due.", "$1,200.50"),
            new Case("Date", i => i.Date = new Date(), "DOB: 01/15/1990.", "01/15/1990"),
            new Case("DriversLicense", i => i.DriversLicense = new DriversLicense(), "DL: A1234567", "A1234567"),
            new Case("EmailAddress", i => i.EmailAddress = new EmailAddress(), "Email a@b.com here.", "a@b.com"),
            new Case("IbanCode", i => i.IbanCode = new IbanCode(), "IBAN: GB29NWBK60161331926819", "GB29NWBK60161331926819"),
            new Case("IpAddress", i => i.IpAddress = new IpAddress(), "Server at 192.168.1.1 is down.", "192.168.1.1"),
            new Case("MacAddress", i => i.MacAddress = new MacAddress(), "MAC: 00:1A:2B:3C:4D:5E", "00:1A:2B:3C:4D:5E"),
            new Case("PassportNumber", i => i.PassportNumber = new PassportNumber(), "Passport: A12345678", "A12345678"),
            new Case("PhoneNumber", i => i.PhoneNumber = new PhoneNumber(), "Call 555-123-4567 today.", "555-123-4567"),
            new Case("PhoneNumberExtension", i => i.PhoneNumberExtension = new PhoneNumberExtension(), "Call ext. 1234", "ext. 1234"),
            new Case("Ssn", i => i.Ssn = new Ssn(), "SSN 123-45-6789 filed.", "123-45-6789"),
            new Case("StateAbbreviation", i => i.StateAbbreviation = new StateAbbreviation(), "Austin, TX 78701", "TX"),
            new Case("StreetAddress", i => i.StreetAddress = new StreetAddress(), "Lives at 123 Main Street.", "123 Main Street"),
            new Case("TrackingNumber", i => i.TrackingNumber = new TrackingNumber(), "UPS: 1Z12345E0205271688", "1Z12345E0205271688"),
            new Case("Url", i => i.Url = new Url(), "Visit https://www.example.com for details.", "https://www.example.com"),
            new Case("Vin", i => i.Vin = new Vin(), "VIN: 1HGBH41JXMN109186", "1HGBH41JXMN109186"),
            new Case("ZipCode", i => i.ZipCode = new ZipCode(), "ZIP: 90210", "90210"),

            // Dictionary / gazetteer filters (exact members of the bundled lists).
            new Case("City", i => i.City = new City(), "Lived in Washington.", "Washington"),
            new Case("County", i => i.County = new County(), "He lived in Fayette County.", "Fayette"),
            new Case("State", i => i.State = new State(), "Moved to California.", "California"),
            new Case("Hospital", i => i.Hospital = new Hospital(), "Admitted to UCLA Medical Center.", "UCLA Medical Center"),
            new Case("FirstName", i => i.FirstName = new FirstName(), "Contact John today.", "John"),
            new Case("Surname", i => i.Surname = new Surname(), "Mr. Jones called.", "Jones"),

            // On-device name model (ONNX) — only runs when the model is bundled.
            new Case("PhEyes", i => i.PhEyes = new List<PhEye> { PhEyeModel.CreateDefaultFilter() },
                "Contact George Washington today.", "George Washington", RequiresModel: true),

            // Configuration-driven filters (the user supplies the terms/pattern/markers).
            new Case("Dictionaries", i => i.Dictionaries = new List<PhDictionary> { new() { Name = "proj", Terms = new List<string> { "Wanderlust" } } },
                "Project Wanderlust launched.", "Wanderlust"),
            new Case("CustomDictionaries", i => i.CustomDictionaries = new List<CustomDictionary> { new() { Terms = new List<string> { "Zephyrous" } } },
                "Codename Zephyrous active.", "Zephyrous"),
            new Case("CustomIdentifiers", i => i.CustomIdentifiers = new List<Identifier> { new() { Pattern = @"CASE-\d+" } },
                "Ref CASE-4821 filed.", "CASE-4821"),
            new Case("Sections", i => i.Sections = new List<Section> { new() { StartPattern = "<secret>", EndPattern = "</secret>" } },
                "Note: <secret>hidden</secret> end.", "hidden"),
        };

        [Fact]
        public void EveryPhileasFilterType_HasAContractCase()
        {
            var expected = FilterPropertyNames();
            var covered = Cases.Select(c => c.Property).ToHashSet();

            var missing = expected.Except(covered).OrderBy(x => x).ToList();
            var unknown = covered.Except(expected).OrderBy(x => x).ToList();

            Assert.True(missing.Count == 0,
                "Phileas filter types with no contract case (add a sample so a new filter can't silently no-op): " + string.Join(", ", missing));
            Assert.True(unknown.Count == 0,
                "Contract cases reference names that aren't Phileas filter properties: " + string.Join(", ", unknown));
        }

        public static IEnumerable<object[]> CaseNames() => Cases.Select(c => new object[] { c.Property });

        [SkippableTheory]
        [MemberData(nameof(CaseNames))]
        public void EnabledFilter_RedactsMatchingSample(string property)
        {
            Case c = Cases.Single(x => x.Property == property);
            Skip.If(c.RequiresModel && !PhEyeModel.IsAvailable, "PhEye model not bundled (run scripts/download-pheye-model.ps1 or set PHEYE_MODEL_DIR).");

            var identifiers = new Identifiers();
            c.Enable(identifiers);
            var policy = new PhileasPolicy { Name = "contract", Identifiers = identifiers };

            // Round-trip through the same JSON path the editor and redactor use, then filter the sample.
            PhileasPolicy loaded = PolicySerializer.DeserializeFromJson(PolicySerializer.SerializeToJson(policy));
            PhEyeModel.Prepare(loaded); // injects the bundled model path for PhEye; no-op for the rest

            var result = new FilterService().Filter(loaded, "ctx", 0, c.Sample);

            Assert.DoesNotContain(c.Sensitive, result.FilteredText);
        }

        private static ISet<string> FilterPropertyNames() =>
            typeof(Identifiers).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => IsFilter(p.PropertyType))
                .Select(p => p.Name)
                .ToHashSet();

        // A filter property is one whose type (or List element type) is a Phileas policy-filter config.
        private static bool IsFilter(Type t)
        {
            Type element = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>)
                ? t.GetGenericArguments()[0]
                : t;
            return typeof(AbstractPolicyFilter).IsAssignableFrom(element);
        }
    }
}
