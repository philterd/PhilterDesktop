using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class Identifier : BaseIdentifier
    {
        [JsonProperty("identifierFilterStrategies")]
        public List<IdentifierFilterStrategy> IdentifierFilterStrategies { get; set; } = new List<IdentifierFilterStrategy>();

        [JsonProperty("pattern")]
        public string Pattern { get; set; } = "\\b[A-Z0-9_-]{4,}\\b";

        [JsonProperty("caseSensitive")]
        public bool CaseSensitive { get; set; } = false;

        [JsonProperty("label")]
        public string Label { get; set; }

        public static string GetName()
        {
            return "Identifier";
        }

        public static string GetDescription()
        {
            return "identifiers";
        }

    }
}
