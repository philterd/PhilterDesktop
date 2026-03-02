using Newtonsoft.Json;

namespace Philter.Model.Policy.Filters.Strategies
{
    public class ZipCodeFilterStrategy : BaseFilterStrategy
    {

        public override string GetIdentifierType()
        {
            return "Zip Code";
        }

        public override string GetIdentifierDescription()
        {
            return ZipCode.GetDescription();
        }

        [JsonProperty("truncateDigits")]
        public int TruncateDigits { get; set; }
    }
}
