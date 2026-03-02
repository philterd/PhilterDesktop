using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class Ssn : BaseIdentifier
    {
        [JsonProperty("ssnFilterStrategies")]
        public List<SsnFilterStrategy> ssnFilterStrategies { get; set; } = new List<SsnFilterStrategy>();

        public static string GetName()
        {
            return "SSN";
        }

        public static string GetDescription()
        {
            return "SSNs";
        }

    }
}
