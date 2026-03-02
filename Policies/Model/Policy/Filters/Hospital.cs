using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class Hospital : BaseIdentifier
    {
        [JsonProperty("hospitalFilterStrategies")]
        public List<HospitalFilterStrategy> HospitalFilterStrategies { get; set; } = new List<HospitalFilterStrategy>();

        [JsonProperty("sensitivity")]
        public string sensitivity { get; set; } = SENSITIVITY_MEDIUM;

        public static string GetName()
        {
            return "Hospital";
        }

        public static string GetDescription()
        {
            return "hospitals";
        }

    }
}
