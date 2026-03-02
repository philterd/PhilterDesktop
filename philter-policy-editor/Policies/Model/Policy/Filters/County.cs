using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class County : BaseIdentifier
    {
        [JsonProperty("countyFilterStrategies")]
        public List<CountyFilterStrategy> CountyFilterStrategies { get; set; } = new List<CountyFilterStrategy>();

        [JsonProperty("sensitivity")]
        public string Sensitivity { get; set; } = SENSITIVITY_MEDIUM;

        public static string GetName()
        {
            return "County";
        }

        public static string GetDescription()
        {
            return "counties";
        }

    }
}
