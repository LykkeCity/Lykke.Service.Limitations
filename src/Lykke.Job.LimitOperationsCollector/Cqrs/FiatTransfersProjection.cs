using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Services;
using Lykke.Service.Limitations.Services.Contracts.FxPaygate;

namespace Lykke.Job.LimitOperationsCollector.Cqrs
{
    public class FiatTransfersProjection
    {
        private readonly ICashOperationsCollector _collector;

        public FiatTransfersProjection(ICashOperationsCollector collector)
        {
            _collector = collector;
        }

        [UsedImplicitly]
        private async Task Handle(TransferCreatedEvent evt)
        {
            await _collector.AddDataItemAsync(new CashOperation
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
