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
using PhilterDesktop.PolicyEditing;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The PhEye model dialog turns free-typed entity types into a clean label list and keeps the
    /// confidence threshold within [0, 1]. These guard the parsing/normalization the dialog relies on.
    /// </summary>
    public sealed class PhEyeModelFormTests
    {
        private static List<string> ParseLabels(string text)
        {
            MethodInfo m = typeof(PhEyeModelForm).GetMethod("ParseLabels", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("ParseLabels not found");
            return (List<string>)m.Invoke(null, new object[] { text })!;
        }

        private static decimal ClampThreshold(double value)
        {
            MethodInfo m = typeof(PhEyeModelForm).GetMethod("ClampThreshold", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("ClampThreshold not found");
            return (decimal)m.Invoke(null, new object[] { value })!;
        }

        [Fact]
        public void ParseLabels_SplitsTrimsAndDropsBlanks()
        {
            Assert.Equal(new[] { "person", "location", "organization" },
                ParseLabels("person,  location ,,organization,"));
        }

        [Fact]
        public void ParseLabels_HandlesNewlinesAndDeduplicatesCaseInsensitively()
        {
            // De-duplication is case-insensitive but keeps the first occurrence's casing.
            Assert.Equal(new[] { "Person", "location" },
                ParseLabels("Person\nlocation\r\nPERSON, Location"));
        }

        [Fact]
        public void ParseLabels_EmptyOrWhitespace_ReturnsEmpty()
        {
            Assert.Empty(ParseLabels("   ,  , \n "));
            Assert.Empty(ParseLabels(""));
        }

        [Theory]
        [InlineData(0.0, 0.5)]   // zero/unset falls back to the model default
        [InlineData(-1.0, 0.5)]
        [InlineData(0.35, 0.35)] // in-range value preserved
        [InlineData(1.0, 1.0)]
        [InlineData(2.5, 1.0)]   // above one clamps to one
        public void ClampThreshold_KeepsWithinUnitRange(double input, double expected)
        {
            Assert.Equal((decimal)expected, ClampThreshold(input));
        }
    }
}
