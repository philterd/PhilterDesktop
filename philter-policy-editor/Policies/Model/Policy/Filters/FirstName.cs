using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class FirstName : BaseIdentifier
    {
        [JsonProperty("firstNameFilterStrategies")]
        public List<FirstNameFilterStrategy> FirstNameFilterStrategies { get; set; } = new List<FirstNameFilterStrategy>();

        [JsonProperty("sensitivity")]
        public string Sensitivity { get; set; } = SENSITIVITY_MEDIUM;

        public static string GetName()
        {
            return "First Name";
        }

        public static string GetDescription()
        {
            return "first names";
        }

    }
}
