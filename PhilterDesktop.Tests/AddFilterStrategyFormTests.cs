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

using PhilterDesktop.PolicyEditing;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Guards the static-value validation in the filter-strategy editor: an empty (or
    /// whitespace-only) "Replace with a static value" must be rejected, because it would silently
    /// delete matched PII with no visible marker.
    /// </summary>
    public sealed class AddFilterStrategyFormTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t")]
        [InlineData("\r\n")]
        public void ValidateStaticReplacement_RejectsEmptyOrWhitespace(string? value)
        {
            string? error = AddFilterStrategyForm.ValidateStaticReplacement(value);
            Assert.NotNull(error);
        }

        [Theory]
        [InlineData("REDACTED")]
        [InlineData("[REDACTED]")]
        [InlineData("x")]
        [InlineData(" name ")] // surrounding spaces but real content
        public void ValidateStaticReplacement_AllowsNonEmptyValue(string value)
        {
            string? error = AddFilterStrategyForm.ValidateStaticReplacement(value);
            Assert.Null(error);
        }
    }
}
