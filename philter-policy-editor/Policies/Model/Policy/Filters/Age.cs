using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class Age : BaseIdentifier
    {

        [JsonProperty("ageFilterStrategies")]
        public List<AgeFilterStrategy> AgeFilterStrategies { get; set; } = new List<AgeFilterStrategy>();

        public static string GetName()
        {
            return "Age";
        }

        public static string GetDescription()
        {
            return "ages";
        }

    }
}
