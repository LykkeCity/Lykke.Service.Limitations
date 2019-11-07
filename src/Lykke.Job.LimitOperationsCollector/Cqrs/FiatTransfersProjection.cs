using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Services;
using Lykke.Service.Limitations.Services.Contracts.FxPaygate;

namespace Lykke.Job.LimitOperationsCollector.Cqrs
{
    public class FiatTransfersProjection
    {
        public ICashOperationsCollector Collector { get; set; }

        [UsedImplicitly]
        private Task Handle(TransferCreatedEvent evt)
        {
            return Collector.AddDataItemAsync(new CashOperation
            {
                Id = evt.TransferId,
                ClientId = evt.ClientId,
                Volume = evt.Amount,
                Asset = evt.AssetId,
                DateTime = DateTime.UtcNow,
                OperationType = CurrencyOperationType.CardCashIn
            });
        }
    }
}
