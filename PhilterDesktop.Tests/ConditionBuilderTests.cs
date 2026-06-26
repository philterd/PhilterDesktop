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

using Phileas.Filters.Conditions;
using PhilterDesktop.PolicyEditing;
using Xunit;

namespace PhilterDesktop.Tests
{
    public class ConditionBuilderTests
    {
        private static ConditionBuilder.ConditionField Field(string keyword) =>
            ConditionBuilder.Fields.First(f => f.Keyword == keyword);

        private static ConditionBuilder.ConditionOperator Op(ConditionBuilder.ConditionField field, string symbol) =>
            ConditionBuilder.OperatorsFor(field).First(o => o.Symbol == symbol);

        [Fact]
        public void Build_TextField_QuotesValue()
        {
            string c = ConditionBuilder.Build(Field("token"), Op(Field("token"), "=="), "Smith");
            Assert.Equal("token == \"Smith\"", c);
        }

        [Fact]
        public void Build_NumericField_DoesNotQuoteValue()
        {
            string c = ConditionBuilder.Build(Field("confidence"), Op(Field("confidence"), ">"), "0.8");
            Assert.Equal("confidence > 0.8", c);
        }

        [Theory]
        [InlineData("token == \"Smith\"", "token", "==", "Smith")]
        [InlineData("context != \"case-42\"", "context", "!=", "case-42")]
        [InlineData("token startswith \"Dr\"", "token", "startswith", "Dr")]
        [InlineData("confidence >= 0.5", "confidence", ">=", "0.5")]
        [InlineData("population < 100", "population", "<", "100")]
        public void TryParse_RoundTrips(string condition, string field, string op, string value)
        {
            Assert.True(ConditionBuilder.TryParse(condition, out var f, out var o, out var v));
            Assert.Equal(field, f.Keyword);
            Assert.Equal(op, o.Symbol);
            Assert.Equal(value, v);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("token == \"a\" and confidence > 0.5")] // chained — not representable in the builder
        [InlineData("garbage")]
        [InlineData("unknownField == \"x\"")]
        public void TryParse_ReturnsFalse_ForUnrepresentable(string? condition)
        {
            Assert.False(ConditionBuilder.TryParse(condition, out _, out _, out _));
        }

        [Fact]
        public void BuiltConditions_AreAcceptedByPhileasEvaluator()
        {
            // Build with the UI, then prove the engine actually parses/evaluates it (true, not the
            // silent always-true fallback that a malformed condition would also give — we check a
            // case the engine should evaluate to false).
            string match = ConditionBuilder.Build(Field("token"), Op(Field("token"), "=="), "Smith");
            string noMatch = ConditionBuilder.Build(Field("token"), Op(Field("token"), "=="), "Jones");

            Assert.True(ConditionEvaluator.Evaluate(match, "ctx", "Smith", 0.9, "surname"));
            Assert.False(ConditionEvaluator.Evaluate(noMatch, "ctx", "Smith", 0.9, "surname"));
        }
    }
}
