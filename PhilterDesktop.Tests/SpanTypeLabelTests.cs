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

using PhilterData;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The "Type" column shown for a captured span must name the entity: the engine's classification
    /// when present, otherwise the humanized filter that matched — falling back to "Detected" only when
    /// neither is known.
    /// </summary>
    public sealed class SpanTypeLabelTests
    {
        [Fact]
        public void For_PrefersClassification_WhenSet()
        {
            // The on-device name model sets Classification = "name"; that wins over the filter type.
            var span = new RedactionSpanEntity { Classification = "name", FilterType = "PhEye" };
            Assert.Equal("name", SpanTypeLabel.For(span));
        }

        [Theory]
        [InlineData("FirstName", "First Name")]
        [InlineData("Surname", "Surname")]
        [InlineData("EmailAddress", "Email Address")]
        [InlineData("Ssn", "SSN")]
        public void For_HumanizesFilterType_WhenClassificationEmpty(string filterType, string expected)
        {
            // Pattern-based filters (first-name, surname, …) leave Classification null but carry FilterType,
            // so the label should read the type rather than a bare "Detected".
            var span = new RedactionSpanEntity { Classification = string.Empty, FilterType = filterType };
            Assert.Equal(expected, SpanTypeLabel.For(span));
        }

        [Fact]
        public void For_FallsBackToDetected_WhenNothingKnown()
        {
            // Older records (and hand-added spans) may carry neither field.
            var span = new RedactionSpanEntity { Classification = string.Empty, FilterType = string.Empty };
            Assert.Equal("Detected", SpanTypeLabel.For(span));
        }
    }
}
