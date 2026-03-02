using Newtonsoft.Json;
using Philter.Model.Policy.Filters.Strategies;
using System.Collections.Generic;

namespace Philter.Model.Policy.Filters
{
    public class CreditCard : BaseIdentifier
    {
        [JsonProperty("creditCardFilterStrategies")]
        public List<CreditCardFilterStrategy> CreditCardFilterStrategies { get; set; } = new List<CreditCardFilterStrategy>();

        public static string GetName()
        {
            return "Credit Card";
        }

        public static string GetDescription()
        {
            return "credit cards";
        }

    }
}
