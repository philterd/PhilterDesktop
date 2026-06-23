namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// Helper for building a filter-strategy condition expression string.
    /// Ported from the former VB editor's Conditional class.
    /// </summary>
    internal sealed class Conditional
    {
        public string? Expression { get; set; }
        public string? ConditionalOperator { get; set; }
        public string? Value { get; set; }

        public Conditional(string expression, string conditionalOperator, string value)
        {
            Expression = expression;
            ConditionalOperator = conditionalOperator;
            Value = value;
        }

        public string GetConditionalString()
        {
            return string.IsNullOrWhiteSpace(Expression)
                ? string.Empty
                : $"{Expression} {ConditionalOperator} \"{Value}\"";
        }

        public override string ToString() => GetConditionalString();
    }
}
