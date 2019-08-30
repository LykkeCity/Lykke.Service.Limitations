using System;

namespace Lykke.Service.Limitations.Core.Domain
{
    public interface ICashOperation
    {
        string Id { get; }
        string ClientId { get; }
        DateTime DateTime { get; }
        double Volume { get; set; }
        string Asset { get; set; }
        string BaseAsset { get; set; }
        double BaseVolume { get; set; }
        CurrencyOperationType? OperationType { get; set; }
    }
}
