using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.Limitations.Core.Domain
{
    public class RemainingLimitation
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public LimitationType LimitationType { get; set; }

        public string Asset { get; set; }

        public double RemainingAmount { get; set; }

        public double LimitAmount { get; set; }
    }
}
