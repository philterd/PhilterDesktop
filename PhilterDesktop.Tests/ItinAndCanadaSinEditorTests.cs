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
using Phileas.Policy.Filters.Strategies;
using Phileas.Services;
using PhilterDesktop.PolicyEditing;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The ITIN and Canada SIN filters appear under Identifiers with readable labels and examples, and each
    /// exposes its validity option (default off) in the Configure dialog, which the engine honors.
    /// </summary>
    public sealed class ItinAndCanadaSinEditorTests
    {
        [Theory]
        [InlineData("Itin", "ITIN")]
        [InlineData("CanadaSin", "Canada SIN")]
        public void Filter_IsUnderIdentifiers_WithALabelAndExample(string property, string label)
        {
            FilterCatalog.FilterInfo info = FilterCatalog.Grouped().SelectMany(g => g).Single(f => f.Property == property);

            Assert.Equal("Identifiers", info.Category);
            Assert.Equal(label, info.Display);
            Assert.False(string.IsNullOrWhiteSpace(FilterExamples.For(property)));
        }

        [Theory]
        [InlineData("Itin", typeof(Itin), "OnlyValidRanges")]
        [InlineData("CanadaSin", typeof(CanadaSin), "OnlyValidPrefixes")]
        public void Filter_ExposesItsOption_MatchingARealBooleanProperty_DefaultOff(string property, Type filterType, string optionProperty)
        {
            FilterOption option = Assert.Single(FilterOptions.For(property));
            Assert.Equal(optionProperty, option.Property);
            Assert.False(option.Default);
            Assert.False(string.IsNullOrWhiteSpace(option.Label));
            Assert.False(string.IsNullOrWhiteSpace(option.Description));
            Assert.DoesNotContain("—", option.Label + option.Description);

            var prop = filterType.GetProperty(optionProperty);
            Assert.NotNull(prop);
            Assert.Equal(typeof(bool), prop!.PropertyType);
            Assert.True(prop.CanWrite);
            Assert.False((bool)prop.GetValue(Activator.CreateInstance(filterType))!); // engine default matches
        }

        private static string RedactSin(bool onlyValidPrefixes, string text) =>
            new FilterService().Filter(
                new PhileasPolicy { Identifiers = new Identifiers { CanadaSin = new CanadaSin { OnlyValidPrefixes = onlyValidPrefixes } } },
                "ctx", 0, text).FilteredText;

        private static string RedactItin(bool onlyValidRanges, string text) =>
            new FilterService().Filter(
                new PhileasPolicy { Identifiers = new Identifiers { Itin = new Itin { OnlyValidRanges = onlyValidRanges } } },
                "ctx", 0, text).FilteredText;

        [Theory]
        [InlineData("046 454 286", false, true)]  // starts with 0
        [InlineData("046 454 286", true, false)]
        [InlineData("800 000 002", false, true)]  // starts with 8 (business numbers)
        [InlineData("800 000 002", true, false)]
        [InlineData("123 456 782", false, true)]
        [InlineData("123 456 782", true, true)]
        public void CanadaSin_Option_SkipsNumbersStartingWith0Or8(string sin, bool option, bool redacted)
        {
            string output = RedactSin(option, "SIN " + sin + " filed.");
            Assert.Equal(redacted, !output.Contains(sin));
        }

        [Fact]
        public void CanadaSin_NeverMatchesAnInvalidChecksum_WhateverTheOption()
        {
            Assert.Equal("SIN 123 345 678 filed.", RedactSin(false, "SIN 123 345 678 filed."));
            Assert.Equal("SIN 123 345 678 filed.", RedactSin(true, "SIN 123 345 678 filed."));
        }

        [Theory]
        [InlineData("912-70-1234", false, true)]  // 70: issued range
        [InlineData("912-70-1234", true, true)]
        [InlineData("912-93-1234", false, true)]  // 93: ATIN
        [InlineData("912-93-1234", true, false)]
        [InlineData("912-45-1234", false, true)]  // 45: not issued
        [InlineData("912-45-1234", true, false)]
        public void Itin_Option_KeepsOnlyIssuedRanges(string itin, bool option, bool redacted)
        {
            string output = RedactItin(option, "ITIN " + itin + " filed.");
            Assert.Equal(redacted, !output.Contains(itin));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Options_RoundTripAndValidate(bool value)
        {
            var policy = new PhileasPolicy
            {
                Identifiers = new Identifiers
                {
                    Itin = new Itin { OnlyValidRanges = value, Strategies = new List<ItinFilterStrategy> { new() } },
                    CanadaSin = new CanadaSin { OnlyValidPrefixes = value, Strategies = new List<CanadaSinFilterStrategy> { new() } }
                }
            };
            string json = PolicySerializer.SerializeToJson(policy);

            PolicyValidationResult result = PolicyValidator.Validate(json);
            Assert.True(result.IsValid, string.Join(" | ", result.Errors));
            PhileasPolicy loaded = PolicySerializer.DeserializeFromJson(json);
            Assert.Equal(value, loaded.Identifiers.Itin!.OnlyValidRanges);
            Assert.Equal(value, loaded.Identifiers.CanadaSin!.OnlyValidPrefixes);
        }
    }
}
