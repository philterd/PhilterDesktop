using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class EmailAddress : BaseIdentifier
    {
        [JsonProperty("emailAddressFilterStrategies")]
        public List<EmailAddressFilterStrategy> EmailAddressFilterStrategies { get; set; } = new List<EmailAddressFilterStrategy>();

        public static string GetName()
        {
            return "Email Address";
        }

        public static string GetDescription()
        {
            return "email addresses";
        }

    }
}
