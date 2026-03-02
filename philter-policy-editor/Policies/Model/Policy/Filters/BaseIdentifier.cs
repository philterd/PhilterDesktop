using Newtonsoft.Json;
using System.ComponentModel;

namespace Philter.Model.Policy.Filters
{
    public abstract class BaseIdentifier
    {

        public const string SENSITIVITY_LOW = "LOW";
        public const string SENSITIVITY_MEDIUM = "MEDIUM";
        public const string SENSITIVITY_HIGH = "HIGH";

        [Browsable(true)]
        [ReadOnly(false)]
        [Description("Enable or disable the filter type.")]
        [Category("Properties")]
        [DisplayName("Enabled")]
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = false;

    }
}
