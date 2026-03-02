using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class PhoneNumber : BaseIdentifier
    {
        [JsonProperty("phoneNumberFilterStrategies")]
        public List<PhoneNumberFilterStrategy> PhoneNumberFilterStrategies { get; set; } = new List<PhoneNumberFilterStrategy>();

        public static string GetName()
        {
            return "Phone Number";
        }

        public static string GetDescription()
        {
            return "phone numbers";
        }

    }
}
