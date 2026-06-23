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
using Phileas.Policy.Filters.Strategies;
using PhilterDesktop.PolicyEditing;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    public sealed class FilterLabelTests
    {
        [Theory]
        [InlineData("Age", "Age")]
        [InlineData("CreditCard", "Credit Card")]
        [InlineData("EmailAddress", "Email Address")]
        [InlineData("Ssn", "SSN")]
        [InlineData("Vin", "VIN")]
        [InlineData("Url", "URL")]
        [InlineData("IpAddress", "IP Address")]
        [InlineData("MacAddress", "MAC Address")]
        [InlineData("IbanCode", "IBAN Code")]
        [InlineData("StreetAddress", "Street Address")]
        [InlineData("PhoneNumberExtension", "Phone Number Extension")]
        public void Humanize_FormatsNames(string input, string expected)
        {
            Assert.Equal(expected, FilterLabel.Humanize(input));
        }
    }

    /// <summary>
    /// Guards the reflection-driven editor: every single-instance filter on the Phileas
    /// Identifiers model must be discoverable, creatable, and round-trip through the
    /// serializer — which is exactly what the editor relies on to generate its UI.
    /// </summary>
    public sealed class IdentifierReflectionTests
    {
        private static IEnumerable<PropertyInfo> SingleFilterProperties() =>
            typeof(Identifiers)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => typeof(AbstractPolicyFilter).IsAssignableFrom(p.PropertyType));

        [Fact]
        public void ThereAreManySingleFilters()
        {
            Assert.True(SingleFilterProperties().Count() >= 15);
        }

        [Fact]
        public void EverySingleFilter_IsCreatable_AndHasCreatableStrategyType()
        {
            foreach (PropertyInfo prop in SingleFilterProperties())
            {
                object? filter = Activator.CreateInstance(prop.PropertyType);
                Assert.NotNull(filter);

                PropertyInfo? strategies = prop.PropertyType.GetProperty("Strategies");
                Assert.NotNull(strategies); // every filter exposes a Strategies list

                Type strategyType = strategies!.PropertyType.GetGenericArguments()[0];
                Assert.True(typeof(AbstractFilterStrategy).IsAssignableFrom(strategyType));
                Assert.IsAssignableFrom<AbstractFilterStrategy>(Activator.CreateInstance(strategyType)!);
            }
        }

        [Fact]
        public void EnablingAllSingleFilters_RoundTripsThroughSerializer()
        {
            var identifiers = new Identifiers();
            foreach (PropertyInfo prop in SingleFilterProperties())
            {
                prop.SetValue(identifiers, Activator.CreateInstance(prop.PropertyType));
            }
            var policy = new PhileasPolicy { Name = "all", Identifiers = identifiers };

            string json = PolicySerializer.SerializeToJson(policy);
            PhileasPolicy loaded = PolicySerializer.DeserializeFromJson(json);

            foreach (PropertyInfo prop in SingleFilterProperties())
            {
                Assert.NotNull(prop.GetValue(loaded.Identifiers));
            }
        }
    }
}
