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

using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    public class ComboSelectionTests
    {
        private static readonly string[] Items = { "alpha", "default", "beta" };

        [Fact]
        public void ResolveIndex_PrefersTheLastUsedValue()
        {
            Assert.Equal(2, ComboSelection.ResolveIndex(Items, "beta"));
        }

        [Fact]
        public void ResolveIndex_FallsBackToDefault_WhenPreferredMissing()
        {
            Assert.Equal(1, ComboSelection.ResolveIndex(Items, "does-not-exist"));
        }

        [Fact]
        public void ResolveIndex_FallsBackToDefault_WhenPreferredEmpty()
        {
            Assert.Equal(1, ComboSelection.ResolveIndex(Items, null));
            Assert.Equal(1, ComboSelection.ResolveIndex(Items, ""));
        }

        [Fact]
        public void ResolveIndex_FallsBackToFirst_WhenNoDefaultAndNoPreferred()
        {
            Assert.Equal(0, ComboSelection.ResolveIndex(new[] { "one", "two" }, null));
        }

        [Fact]
        public void ResolveIndex_ReturnsMinusOne_WhenEmpty()
        {
            Assert.Equal(-1, ComboSelection.ResolveIndex(System.Array.Empty<string>(), "anything"));
        }

        [Fact]
        public void ResolveIndex_IsCaseSensitive()
        {
            // "Default" (capital D) shouldn't match "default", so it falls back to the literal "default".
            Assert.Equal(1, ComboSelection.ResolveIndex(Items, "BETA"));
        }
    }
}
