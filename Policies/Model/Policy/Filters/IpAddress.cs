using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class IpAddress : BaseIdentifier
    {
        [JsonProperty("ipAddressFilterStrategies")]
        public List<IpAddressFilterStrategy> IpAddressFilterStrategies { get; set; } = new List<IpAddressFilterStrategy>();

        public static string GetName()
        {
            return "IP Address";
        }

        public static string GetDescription()
        {
            return "IP addresses";
        }

    }
}
