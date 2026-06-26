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

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// Short, plain-language examples shown under each filter's name in the Policy Editor, keyed by the
    /// Phileas <c>Identifiers</c> property name. Anything without an entry simply shows no example.
    /// </summary>
    internal static class FilterExamples
    {
        private static readonly IReadOnlyDictionary<string, string> Examples =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["FirstName"] = "e.g. Jane",
                ["Surname"] = "e.g. Smith",
                ["Age"] = "e.g. 64 years old",
                ["EmailAddress"] = "e.g. jane@example.com",
                ["PhoneNumber"] = "e.g. (555) 123-4567",
                ["PhoneNumberExtension"] = "e.g. ext. 4567",
                ["City"] = "e.g. Springfield",
                ["County"] = "e.g. Cook County",
                ["State"] = "e.g. California",
                ["StateAbbreviation"] = "e.g. CA",
                ["ZipCode"] = "e.g. 90210",
                ["StreetAddress"] = "e.g. 123 Main St",
                ["CreditCard"] = "e.g. 4111 1111 1111 1111",
                ["BankRoutingNumber"] = "e.g. 021000021",
                ["IbanCode"] = "e.g. DE89 3704 0044 0532 0130 00",
                ["BitcoinAddress"] = "e.g. 1A1zP1eP5QGefi2DMPTfTL5SLmv7Divf",
                ["Currency"] = "e.g. $1,250.00",
                ["Ssn"] = "e.g. 123-45-6789",
                ["DriversLicense"] = "e.g. D1234567",
                ["PassportNumber"] = "e.g. X12345678",
                ["Vin"] = "e.g. 1HGCM82633A004352",
                ["TrackingNumber"] = "e.g. 1Z999AA10123456784",
                ["IpAddress"] = "e.g. 192.168.0.1",
                ["MacAddress"] = "e.g. 00:1A:2B:3C:4D:5E",
                ["Url"] = "e.g. https://example.com",
                ["Hospital"] = "e.g. Mercy Hospital",
                ["Date"] = "e.g. 03/04/2020",
            };

        /// <summary>Returns a short example for the filter, or null if there isn't one.</summary>
        public static string? For(string propertyName) =>
            Examples.TryGetValue(propertyName, out string? example) ? example : null;
    }
}
