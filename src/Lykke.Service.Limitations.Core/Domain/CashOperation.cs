using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Lykke.Service.Limitations.Core.Domain
{
    public class CashOperation : ICashOperation
    {
        public string Id { get; set; }

        public string ClientId { get; set; }

        public double Volume { get; set; }

        public double? RateToUsd { get; set; }

        public string Asset { get; set; }

        public DateTime DateTime { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public CurrencyOperationType? OperationType { get; set; }
    }
}
