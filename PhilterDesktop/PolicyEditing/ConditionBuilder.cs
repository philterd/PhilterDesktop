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

using System.Text.RegularExpressions;

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// Builds and parses a single filter-strategy condition (e.g. <c>token == "Smith"</c>) for the
    /// guided condition editor. Mirrors the grammar phileas-dotnet's ConditionEvaluator accepts:
    /// a field (token/context/type/confidence/population), an operator, and a quoted-string or numeric
    /// value. (The engine also allows chaining with "and"; that multi-condition case isn't built here.)
    /// </summary>
    internal static class ConditionBuilder
    {
        // AllowsPrefix: whether the field supports the "starts with" operator. The engine's
        // ConditionEvaluator implements startswith for token and context, but NOT for type (EvaluateType
        // handles only ==/!= and returns true for anything else) — so offering it there would build an
        // always-true condition that over-applies the strategy. Numeric fields never use prefix ops.
        internal sealed record ConditionField(string Display, string Keyword, bool Numeric, bool AllowsPrefix);
        internal sealed record ConditionOperator(string Display, string Symbol);

        public static readonly IReadOnlyList<ConditionField> Fields = new[]
        {
            new ConditionField("Matched text", "token", Numeric: false, AllowsPrefix: true),
            new ConditionField("Context", "context", Numeric: false, AllowsPrefix: true),
            new ConditionField("Detected type", "type", Numeric: false, AllowsPrefix: false),
            new ConditionField("Confidence (0 to 1)", "confidence", Numeric: true, AllowsPrefix: false),
            new ConditionField("Population", "population", Numeric: true, AllowsPrefix: false),
        };

        public static readonly IReadOnlyList<ConditionOperator> EqualityOperators = new[]
        {
            new ConditionOperator("equals", "=="),
            new ConditionOperator("does not equal", "!="),
        };

        public static readonly IReadOnlyList<ConditionOperator> TextOperators = new[]
        {
            new ConditionOperator("equals", "=="),
            new ConditionOperator("does not equal", "!="),
            new ConditionOperator("starts with", "startswith"),
        };

        public static readonly IReadOnlyList<ConditionOperator> NumberOperators = new[]
        {
            new ConditionOperator("equals", "=="),
            new ConditionOperator("does not equal", "!="),
            new ConditionOperator("is greater than", ">"),
            new ConditionOperator("is greater than or equal to", ">="),
            new ConditionOperator("is less than", "<"),
            new ConditionOperator("is less than or equal to", "<="),
        };

        public static IReadOnlyList<ConditionOperator> OperatorsFor(ConditionField field) =>
            field.Numeric ? NumberOperators
            : field.AllowsPrefix ? TextOperators
            : EqualityOperators;

        // The engine's condition grammar accepts numbers as \d+(?:\.\d+)? — no sign, exponent, or
        // thousands separator. A value outside this (e.g. "1E3", "1,000") makes the whole condition
        // unparseable, and an unparseable condition evaluates to true, so the strategy over-applies.
        private static readonly Regex NumericValue = new(@"^\d+(?:\.\d+)?$", RegexOptions.Compiled);

        /// <summary>Whether <paramref name="value"/> is a number the engine's condition grammar accepts.</summary>
        public static bool IsValidNumericValue(string? value) =>
            value is not null && NumericValue.IsMatch(value.Trim());

        /// <summary>Builds a condition string. Numeric values are bare; text values are quoted.</summary>
        public static string Build(ConditionField field, ConditionOperator op, string value) =>
            field.Numeric
                ? $"{field.Keyword} {op.Symbol} {value.Trim()}"
                : $"{field.Keyword} {op.Symbol} \"{value}\"";

        private static readonly Regex SingleCondition = new(
            "^\\s*(?<field>token|context|confidence|type|population)\\s+" +
            "(?<op>>=|<=|==|!=|>|<|startswith)\\s+" +
            "(?<value>\"[^\"]*\"|\\d+(?:\\.\\d+)?)\\s*$",
            RegexOptions.Compiled);

        /// <summary>
        /// Parses a single condition back into field/operator/value for editing. Returns false for an
        /// empty, chained ("and"), or otherwise unrecognized condition so the caller can fall back.
        /// </summary>
        public static bool TryParse(string? condition, out ConditionField field, out ConditionOperator op, out string value)
        {
            field = Fields[0];
            op = TextOperators[0];
            value = string.Empty;

            if (string.IsNullOrWhiteSpace(condition))
            {
                return false;
            }

            Match m = SingleCondition.Match(condition);
            if (!m.Success)
            {
                return false;
            }

            ConditionField? f = Fields.FirstOrDefault(x => x.Keyword == m.Groups["field"].Value);
            if (f is null)
            {
                return false;
            }

            ConditionOperator? o = OperatorsFor(f).FirstOrDefault(x => x.Symbol == m.Groups["op"].Value);
            if (o is null)
            {
                return false;
            }

            string raw = m.Groups["value"].Value;
            value = raw.Length >= 2 && raw[0] == '"' && raw[^1] == '"' ? raw[1..^1] : raw;
            field = f;
            op = o;
            return true;
        }
    }
}
