using Newtonsoft.Json;
using Philter.Model.Policy;
using System.Collections.Generic;

namespace Philter.Model.Policy
{
    public class Policy
    {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("identifiers")]
        public Identifiers Identifiers { get; set; }

        [JsonProperty("ignored")]
        public List<Ignored> Ignored { get; set; }

        public override string ToString()
        {
            JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings();
            JsonSerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            return JsonConvert.SerializeObject(this, Formatting.Indented, JsonSerializerSettings);
        }

    }
}
