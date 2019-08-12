using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lykke.Service.Limitations.Client.Models
{
    /// <summary>
    /// Cash operation.
    /// </summary>
    public class CashOperation
    {
        /// <summary>Operation id</summary>
        public string Id { get; set; }

        /// <summary>Client id</summary>
        public string ClientId { get; set; }

        /// <summary>Currency amount</summary>
        public double Amount { get; set; }

        /// <summary>Operation asset</summary>
        public string Asset { get; set; }

        /// <summary>Operation UTC timesamp</summary>
        public DateTime DateTime { get; set; }

        /// <summary>Operation type</summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public CurrencyOperationType OperationType { get; set; }
    }
}
