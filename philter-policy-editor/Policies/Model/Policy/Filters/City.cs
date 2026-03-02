using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class City : BaseIdentifier
    {
        [JsonProperty("cityFilterStrategies")]
        public List<CityFilterStrategy> CityFilterStrategies { get; set; } = new List<CityFilterStrategy>();

        [JsonProperty("sensitivity")]
        public string Sensitivity { get; set; } = SENSITIVITY_MEDIUM;

        public static string GetName()
        {
            return "City";
        }

        public static string GetDescription()
        {
            return "cities";
        }

    }
}
