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

        // --- the builder must not offer operators/values the engine silently ignores -----------

        [Fact]
        public void TypeField_DoesNotOfferStartsWith()
        {
            // EvaluateType only handles ==/!=; startswith would be an always-true (over-applying) condition.
            Assert.DoesNotContain(ConditionBuilder.OperatorsFor(Field("type")), o => o.Symbol == "startswith");
        }

        [Theory]
        [InlineData("token")]
        [InlineData("context")]
        public void TextFields_OfferStartsWith(string keyword)
        {
            // token and context genuinely implement startswith in the engine.
            Assert.Contains(ConditionBuilder.OperatorsFor(Field(keyword)), o => o.Symbol == "startswith");
        }

        [Fact]
        public void TypeStartsWith_IsRejectedByEngine_WhichIsWhyItIsExcluded()
        {
            // The engine doesn't support startswith on type, so a policy holding it won't load.
            Assert.Throws<InvalidConditionException>(() =>
                ConditionEvaluator.Evaluate("type startswith \"SS\"", "ctx", "x", 0.9, "SSN"));
            Assert.NotNull(ConditionParser.GetError("type startswith \"SS\""));
        }

        [Fact]
        public void TypeEquals_ActuallyNarrowsInEngine()
        {
            string c = ConditionBuilder.Build(Field("type"), Op(Field("type"), "=="), "SSN");
            Assert.True(ConditionEvaluator.Evaluate(c, "ctx", "x", 0.9, "SSN"));
            Assert.False(ConditionEvaluator.Evaluate(c, "ctx", "x", 0.9, "EMAIL_ADDRESS"));
        }

        [Fact]
        public void TryParse_TypeStartsWith_ReturnsFalse_SoItFallsBackToAdvanced()
        {
            // The guided editor can't show it; LegacyConditions removes it when the policy is opened.
            Assert.False(ConditionBuilder.TryParse("type startswith \"SSN\"", out _, out _, out _));
        }

        [Theory]
        [InlineData("0")]
        [InlineData("100")]
        [InlineData("0.85")]
        [InlineData(" 0.5 ")] // trimmed
        public void IsValidNumericValue_AcceptsEngineGrammar(string value) =>
            Assert.True(ConditionBuilder.IsValidNumericValue(value));

        [Theory]
        [InlineData("1E3")]   // exponent — double.TryParse accepts, engine rejects
        [InlineData("1e2")]
        [InlineData("1,000")] // thousands separator
        [InlineData("-1")]    // sign
        [InlineData(".5")]    // leading dot
        [InlineData("1.")]    // trailing dot
        [InlineData("abc")]
        [InlineData("")]
        [InlineData(null)]
        public void IsValidNumericValue_RejectsWhatEngineRejects(string? value) =>
            Assert.False(ConditionBuilder.IsValidNumericValue(value));

        [Fact]
        public void RejectedNumericForm_IsRejectedByEngine_WhichIsWhyItIsRejected()
        {
            // "1E3" isn't a number the engine reads, so the condition doesn't parse.
            Assert.Throws<InvalidConditionException>(() =>
                ConditionEvaluator.Evaluate("confidence == 1E3", "ctx", "x", 0.5, "t"));
            Assert.NotNull(ConditionParser.GetError("confidence == 1E3"));
        }

        [Fact]
        public void AcceptedNumericForm_ActuallyNarrowsInEngine()
        {
            string c = ConditionBuilder.Build(Field("confidence"), Op(Field("confidence"), ">="), "0.8");
            Assert.True(ConditionEvaluator.Evaluate(c, "ctx", "x", 0.9, "t"));
            Assert.False(ConditionEvaluator.Evaluate(c, "ctx", "x", 0.5, "t"));
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
