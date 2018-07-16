using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Lykke.Service.Limitations.Core.Domain
{
    public class CurrencyOperationAttempt
    {
        public string ClientId { get; set; }

        public double Amount { get; set; }

        public string Asset { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public CurrencyOperationType OperationType { get; set; }

        /// <summary>Operation UTC timesamp</summary>
        public DateTime DateTime { get; set; }
    }
}
