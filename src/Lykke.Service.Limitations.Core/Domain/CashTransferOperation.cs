using System;

namespace Lykke.Service.Limitations.Core.Domain
{
    public class CashTransferOperation : ICashOperation
    {
        public string Id { get; set; }

        public string FromClientId { get; set; }

        public string ToClientId { get; set; }

        public DateTime DateTime { get; set; }

        public double Volume { get; set; }

        public double? RateToUsd { get; set; }

        public string Asset { get; set; }

        public string ClientId => ToClientId;

        public CurrencyOperationType? OperationType { get; set; }
    }
}
