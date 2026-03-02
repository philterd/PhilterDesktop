using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class Surname : BaseIdentifier
    {
        [JsonProperty("surnameFilterStrategies")]
        public List<SurnameFilterStrategy> SurnameFilterStrategies { get; set; } = new List<SurnameFilterStrategy>();

        [JsonProperty("sensitivity")]
        public string Sensitivity { get; set; } = SENSITIVITY_MEDIUM;

        public static string GetName()
        {
            return "Surname";
        }

        public static string GetDescription()
        {
            return "surnames";
        }

    }
}
