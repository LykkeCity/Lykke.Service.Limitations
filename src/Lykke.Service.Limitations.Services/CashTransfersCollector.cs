using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using Lykke.Service.Limitations.Core.Services;
using StackExchange.Redis;

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
            string redisInstanceName)
            : base(
                stateRepository,
                antifraudCollector,
                connectionMultiplexer,
                accumulatedDepositAggregator,
                redisInstanceName,
                nameof(CashTransferOperation),
                currencyConverter)
        {
        }

        public async Task<(double, bool)> GetCurrentAmountAsync(
            string clientId,
            string asset,
            CashOperationLimitation limit,
            CurrencyOperationType operationType)
        {
            Dictionary<string, double> cachedRates = new Dictionary<string, double>();
            (var items, bool notCached) = await _data.GetClientDataAsync(clientId, operationType);
            double result = 0;
            DateTime now = DateTime.UtcNow;
            foreach (var item in items)
            {
                if (item.Asset != asset)
                    continue;
                if (limit.Period == LimitationPeriod.Day
                    && now.Subtract(item.DateTime).TotalHours >= 24)
                    continue;

                //  limit asset is USD - convert item amount to USD
                if (limit.Asset == _currencyConverter.DefaultAsset)
                {
                    double rateToUsd = await _currencyConverter.GetRateToUsd(cachedRates, item.Asset, item.RateToUsd);
                    result += item.Volume * rateToUsd;
                }
                else
                {
                    //  limit asset is not USD - limitation for specific asset
                    if (item.Asset == asset)
                    {
                        result += item.Volume;
                    }
                }
            }
            return (result, notCached);
        }

        public async Task<List<CashTransferOperation>> GetClientDataAsync(string clientId, LimitationPeriod period)
        {
            var (result, _) = await _data.GetClientDataAsync(clientId);
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
