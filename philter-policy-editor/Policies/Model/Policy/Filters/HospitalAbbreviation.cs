using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class HospitalAbbreviation : BaseIdentifier
    {
        [JsonProperty("hospitalAbbreviationFilterStrategies")]
        public List<HospitalAbbreviationFilterStrategy> HospitalAbbreviationFilterStrategies { get; set; } = new List<HospitalAbbreviationFilterStrategy>();

        [JsonProperty("sensitivity")]
        public string Sensitivity { get; set; } = SENSITIVITY_MEDIUM;

        public static string GetName()
        {
            return "Hospital Abbreviation";
        }

        public static string GetDescription()
        {
            return "hospital abbreviations";
        }

    }
}
