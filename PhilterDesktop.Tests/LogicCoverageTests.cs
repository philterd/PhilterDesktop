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
using PhilterDesktop.PolicyEditing;
using Xunit;

namespace PhilterDesktop.Tests
{
    public class ConditionalTests
    {
        [Fact]
        public void GetConditionalString_BuildsExpression()
        {
            var c = new Conditional("confidence", "greater_than", "0.8");
            Assert.Equal("confidence greater_than \"0.8\"", c.GetConditionalString());
            Assert.Equal(c.GetConditionalString(), c.ToString());
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void GetConditionalString_EmptyExpression_IsEmpty(string expression)
        {
            Assert.Equal(string.Empty, new Conditional(expression, "equals", "x").GetConditionalString());
        }
    }

    public class LinksTests
    {
        [Theory]
        [InlineData("toolbar")]
        [InlineData("about")]
        public void SupportUrl_PointsAtProductPage_WithUtm(string medium)
        {
            Assert.Equal(
                $"https://www.philterd.ai/philter-desktop?utm_source=desktop&utm_medium={medium}",
                Links.SupportUrl(medium));
        }

        [Fact]
        public void EachOffering_HasItsOwnTaggedUrl()
        {
            Assert.Equal("https://www.philterd.ai/philter?utm_source=desktop&utm_medium=about", Links.PhilterUrl("about"));
            Assert.Equal("https://www.philterd.ai/philter-scope?utm_source=desktop&utm_medium=verification", Links.ScopeUrl("verification"));
            Assert.Equal("https://www.philterd.ai/philter-diffuse?utm_source=desktop&utm_medium=details", Links.DiffuseUrl("details"));
            Assert.Equal("https://www.philterd.ai/consulting?utm_source=desktop&utm_medium=policy-editor", Links.ConsultingUrl("policy-editor"));
            Assert.Equal("https://philterd.ai/open-source-software?utm_source=desktop&utm_medium=help-menu", Links.AllProductsUrl("help-menu"));
        }

        [Fact]
        public void Medium_IsUrlEscaped()
        {
            Assert.EndsWith("utm_medium=policy%20editor", Links.ConsultingUrl("policy editor"));
        }
    }
}
