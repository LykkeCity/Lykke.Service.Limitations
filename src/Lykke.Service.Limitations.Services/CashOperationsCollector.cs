using Common.Log;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using Lykke.Service.Limitations.Core.Services;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Common.Log;

namespace Lykke.Service.Limitations.Services
{
    public class CashOperationsCollector : CashOperationsCollectorBase<CashOperation>, ICashOperationsCollector
    {
        public CashOperationsCollector(
            ICashOperationsRepository stateRepository,
            IConnectionMultiplexer connectionMultiplexer,
            IAccumulatedDepositAggregator accumulatedDepositAggregator,
            IAntiFraudCollector antifraudCollector,
            ICurrencyConverter currencyConverter,
            string redisInstanceName,
            ILogFactory logFactory)
            : base(
                stateRepository,
                antifraudCollector,
                connectionMultiplexer,
                accumulatedDepositAggregator,
                redisInstanceName,
                nameof(CashOperation),
                currencyConverter,
                logFactory)
        {
        }

        public async Task<(double, bool)> GetCurrentAmountAsync(
            string clientId,
            string asset,
            CashOperationLimitation limit,
            CurrencyOperationType operationType,
            bool checkAllCrypto = false)
        {
            int sign;
            switch (operationType)
            {
                case CurrencyOperationType.CardCashIn:
                case CurrencyOperationType.CryptoCashIn:
                case CurrencyOperationType.SwiftTransfer:
                    sign = 1;
                    break;
                case CurrencyOperationType.CardCashOut:
                case CurrencyOperationType.CryptoCashOut:
                case CurrencyOperationType.SwiftTransferOut:
                    sign = -1;
                    break;
                default:
                    throw new NotSupportedException($"Operation type {operationType} can't be mapped to CashFlowDirection!");
            }

            Dictionary<string, double> cachedRates = new Dictionary<string, double>();
            (var items, bool notCached) = await _data.GetClientDataAsync(clientId, operationType);

            DateTime now = DateTime.UtcNow;
            double result = 0;
            foreach (var item in items)
            {
                if (limit.Period == LimitationPeriod.Day
                    && now.Subtract(item.DateTime).TotalHours >= 24)
                    continue;

                if (Math.Sign(item.Volume) != Math.Sign(sign))
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
            return (Math.Abs(result), notCached);
        }

        public async Task<List<CashOperation>> GetClientDataAsync(string clientId, LimitationPeriod period)
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

        protected override CurrencyOperationType GetOperationType(CashOperation item)
        {
            if (item.Volume >= 0)
                return _currencyConverter.IsNotConvertible(item.Asset)
                    ? CurrencyOperationType.CryptoCashIn : CurrencyOperationType.CardCashIn;
            return _currencyConverter.IsNotConvertible(item.Asset)
                ? CurrencyOperationType.CryptoCashOut : CurrencyOperationType.CardCashOut;
        }
    }
}
