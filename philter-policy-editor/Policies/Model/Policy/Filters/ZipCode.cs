using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class ZipCode : BaseIdentifier
    {
        [JsonProperty("zipCodeFilterStrategies")]
        public List<ZipCodeFilterStrategy> ZipCodeFilterStrategies { get; set; } = new List<ZipCodeFilterStrategy>();

        public static string GetName()
        {
            return "Zip Code";
        }

        public static string GetDescription()
        {
            return "zip codes";
        }
    }
}
