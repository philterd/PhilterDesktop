using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class Ner : BaseIdentifier
    {
        [JsonProperty("nerFilterStrategies")]
        public List<NerFilterStrategy> nerFilterStrategies { get; set; } = new List<NerFilterStrategy>();

        public static string GetName()
        {
            return "Entity";
        }

        public static string GetDescription()
        {
            return "entities";
        }

    }
}
