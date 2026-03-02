using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class Vin : BaseIdentifier
    {
        [JsonProperty("vinFilterStrategies")]
        public List<VinFilterStrategy> VinFilterStrategies { get; set; } = new List<VinFilterStrategy>();

        public static string GetName()
        {
            return "VIN";
        }

        public static string GetDescription()
        {
            return "VINs";
        }
    }
}
