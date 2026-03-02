using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class Date : BaseIdentifier
    {
        [JsonProperty("dateFilterStrategies")]
        public List<DateFilterStrategy> DateFilterStrategies { get; set; } = new List<DateFilterStrategy>();

        public static string GetName()
        {
            return "Date";
        }

        public static string GetDescription()
        {
            return "dates";
        }

    }
}
