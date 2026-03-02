using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class State : BaseIdentifier
    {
        [JsonProperty("stateFilterStrategies")]
        public List<StateFilterStrategy> StateFilterStrategies { get; set; } = new List<StateFilterStrategy>();

        [JsonProperty("sensitivity")]
        public string Sensitivity { get; set; } = SENSITIVITY_MEDIUM;

        public static string GetName()
        {
            return "State";
        }

        public static string GetDescription()
        {
            return "states";
        }

    }
}
