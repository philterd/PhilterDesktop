using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class CustomDictionary : BaseIdentifier
    {
        [JsonProperty("customDictionaryFilterStrategies")]
        public List<CustomDictionaryFilterStrategy> CustomDictionaryFilterStrategies { get; set; } = new List<CustomDictionaryFilterStrategy>();

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("terms")]
        public List<string> Terms { get; set; } = new List<string>();

        [JsonProperty("sensitivity")]
        public string Sensitivity { get; set; } = SENSITIVITY_MEDIUM;

        public static string GetName()
        {
            return "Custom Dictionary Value";
        }

        public static string GetDescription()
        {
            return "custom dictionary values";
        }

    }
}
