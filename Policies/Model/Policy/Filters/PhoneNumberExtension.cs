using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class PhoneNumberExtension : BaseIdentifier
    {
        [JsonProperty("phoneNumberExtensionFilterStrategies")]
        public List<PhoneNumberExtensionFilterStrategy> PhoneNumberExtensionFilterStrategies { get; set; } = new List<PhoneNumberExtensionFilterStrategy>();

        public static string GetName()
        {
            return "Phone Number Extension";
        }

        public static string GetDescription()
        {
            return "phone number extensions";
        }

    }
}
