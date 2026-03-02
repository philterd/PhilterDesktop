using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class Url : BaseIdentifier
    {
        [JsonProperty("urlFilterStrategies")]
        public List<UrlFilterStrategy> UrlFilterStrategies { get; set; } = new List<UrlFilterStrategy>();

        public static string GetName()
        {
            return "URL";
        }

        public static string GetDescription()
        {
            return "URLs";
        }
    }
}
