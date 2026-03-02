using Newtonsoft.Json;
using System.ComponentModel;

namespace Philter.Model.Policy.Filters.Strategies
{
    public abstract class BaseFilterStrategy
    {

        public static string REDACT = "REDACT";
        public static string STATIC_REPLACEMENT = "STATIC_REPLACE";
        public static string RANDOM_REPLACEMENT = "RANDOM_REPLACE";

        public static string SCOPE_CONTEXT = "context";
        public static string SCOPE_DOCUMENT = "document";

        public abstract string GetIdentifierType();
        public abstract string GetIdentifierDescription();

        [Browsable(true)]
        [ReadOnly(false)]
        [Description("Method of replacement.")]
        [Category("Properties")]
        [DisplayName("Strategy")]
        [JsonProperty("strategy")]
        public string Strategy { get; set; } = REDACT;

        [Browsable(true)]
        [ReadOnly(false)]
        [Description("How to redact the value.")]
        [Category("Properties")]
        [DisplayName("Redaction Format")]
        [JsonProperty("redactionFormat")]
        public string RedactionFormat { get; set; } = "{{{REDACTED-%t}}}";

        [Browsable(true)]
        [ReadOnly(false)]
        [Description("The scope of the replacement. Valid values are context or document.")]
        [Category("General")]
        [DisplayName("Scope")]
        [JsonProperty("replacementScope")]
        public string ReplacementScope { get; set; } = SCOPE_DOCUMENT;

        [Browsable(true)]
        [ReadOnly(false)]
        [Description("A static replacement value.")]
        [Category("Properties")]
        [DisplayName("Static Replacement Value")]
        [JsonProperty("staticReplacement")]
        public string StaticReplacement { get; set; }

        [Browsable(true)]
        [ReadOnly(false)]
        [Description("A conditional statement that defines when the value is replaced.")]
        [Category("General")]
        [DisplayName("Condition")]
        [JsonProperty("condition")]
        public string Condition { get; set; } = string.Empty;

        public override string ToString()
        {
            if (Strategy == REDACT)
            {
                return "Redact " + GetIdentifierDescription() + " with value " + RedactionFormat + " (" + Condition + ")";
            }
            else if (Strategy == STATIC_REPLACEMENT)
            {
                return "Replace " + GetIdentifierDescription() + " with static value " + StaticReplacement + " (" + Condition + ")";
            }
            else if (Strategy == RANDOM_REPLACEMENT)
            {
                return "Replace " + GetIdentifierDescription() + " with random values in scope " + ReplacementScope + " (" + Condition + ")";
                
            }
            else
            {
                return "Invalid";
            }
        }

        public override bool Equals(object obj)
        {

            BaseFilterStrategy baseFilterStrategy = obj as BaseFilterStrategy;

            if(baseFilterStrategy == null)
            {
                return false;
            }

            return (obj != null)
                && (Strategy == baseFilterStrategy.Strategy)
                && (RedactionFormat == baseFilterStrategy.RedactionFormat)
                && (ReplacementScope == baseFilterStrategy.ReplacementScope)
                && (StaticReplacement == baseFilterStrategy.StaticReplacement)
                && (Condition == baseFilterStrategy.Condition);

        }

        public override int GetHashCode()
        {
            // See https://stackoverflow.com/a/263416
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + Strategy.GetHashCode();
                hash = hash * 23 + RedactionFormat.GetHashCode();
                hash = hash * 23 + ReplacementScope.GetHashCode();
                hash = hash * 23 + StaticReplacement.GetHashCode();
                hash = hash * 23 + Condition.GetHashCode();
                return hash;
            }
        }

        public string BuildStrategyText()
        {
            return this.ToString();
        }

    }
}
