using Common.Log;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using Lykke.Service.Limitations.Core.Services;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Services
{
    public class CashTransfersCollector : CashOperationsCollectorBase<CashTransferOperation>, ICashTransfersCollector
    {
        public CashTransfersCollector(
            ICashTransfersRepository stateRepository,
            IConnectionMultiplexer connectionMultiplexer,
            IAccumulatedDepositAggregator accumulatedDepositAggregator,
            IAntiFraudCollector antifraudCollector,
            ICurrencyConverter currencyConverter,
            string redisInstanceName,
            ILog log)
            : base(
                stateRepository,
                antifraudCollector,
                connectionMultiplexer,
                accumulatedDepositAggregator,
                redisInstanceName,
                nameof(CashTransferOperation),
                currencyConverter,
                log)
        {
        }

        public async Task<(double, bool)> GetCurrentAmountAsync(
            string clientId,
            string asset,
            LimitationPeriod period,
            CurrencyOperationType operationType)
        {
            (var items, bool notCached) = await _data.GetClientDataAsync(clientId, operationType);
            double result = 0;
            DateTime now = DateTime.UtcNow;
            foreach (var item in items)
            {
                if (item.Asset != asset)
                    continue;
                if (period == LimitationPeriod.Day
                    && now.Subtract(item.DateTime).TotalHours >= 24)
                    continue;

                result += item.Volume;
            }
            return (result, notCached);
        }

        public async Task<List<CashTransferOperation>> GetClientDataAsync(string clientId, LimitationPeriod period)
        {
            (var result, _) = await _data.GetClientDataAsync(clientId);
            if (period == LimitationPeriod.Day)
            {
                var now = DateTime.UtcNow;
                result.RemoveAll(i => now.Subtract(i.DateTime).TotalHours >= 24);
            }
            foreach (var item in result)
            {
                if (item.OperationType.HasValue)
                    continue;

                item.OperationType = GetOperationType(item);
            }
            return result;
        }

        protected override CurrencyOperationType GetOperationType(CashTransferOperation item)
        {
            return item.Volume >= 0 ? CurrencyOperationType.SwiftTransfer : CurrencyOperationType.SwiftTransferOut;
        }
    }
}
