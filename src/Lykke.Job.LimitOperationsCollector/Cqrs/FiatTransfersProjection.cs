using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Services;

namespace Lykke.Job.LimitOperationsCollector.Cqrs
{
    public class FiatTransfersProjection
    {
        private readonly ICashOperationsCollector _collector;
        private readonly ILog _log;

        public FiatTransfersProjection(ICashOperationsCollector collector, ILog log)
        {
            _collector = collector;
            _log = log;
        }

        [UsedImplicitly]
        private async Task Handle(TransferCreatedEvent evt)
        {
            _log.WriteInfo(nameof(FiatTransfersProjection), nameof(Handle), evt.ToJson());

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
