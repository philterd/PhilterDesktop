using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class StateAbbreviation : BaseIdentifier
    {
        [JsonProperty("stateAbbreviationFilterStrategies")]
        public List<StateAbbreviationFilterStrategy> StateAbbreviationFilterStrategies { get; set; } = new List<StateAbbreviationFilterStrategy>();

        public static string GetName()
        {
            return "State Abbreviation";
        }

        public static string GetDescription()
        {
            return "state abbreviations";
        }

    }
}
