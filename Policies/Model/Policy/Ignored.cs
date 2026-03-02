using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Philter.Model.Policy
{
    public class Ignored
    {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("terms")]
        public List<string> Terms { get; set; }

    }
}
