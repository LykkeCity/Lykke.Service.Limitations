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
            IAntiFraudCollector antifraudCollector,
            ICurrencyConverter currencyConverter,
            string redisInstanceName,
            ILogFactory logFactory)
            : base(
                stateRepository,
                antifraudCollector,
                connectionMultiplexer,
                redisInstanceName,
                nameof(CashOperation),
                currencyConverter,
                logFactory)
        {
        }

        public async Task<(double, bool)> GetCurrentAmountAsync(
            string clientId,
            string asset,
            LimitationPeriod period,
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
            (var items, bool notCached) = await _data.GetClientDataAsync(clientId, operationType);
            DateTime now = DateTime.UtcNow;
            double result = 0;
            foreach (var item in items)
            {
                if (period == LimitationPeriod.Day
                    && now.Subtract(item.DateTime).TotalHours >= 24)
                    continue;
                if (Math.Sign(item.Volume) != Math.Sign(sign))
                    continue;

                double amount;

                if (checkAllCrypto)
                {
                    if (!_currencyConverter.IsNotConvertible(item.Asset))
                        continue;

                    var converted = await _currencyConverter.ConvertAsync(
                        item.Asset,
                        _currencyConverter.DefaultAsset,
                        item.Volume,
                        true);

                    amount = converted.Item2;
                }
                else
                {
                    if (item.Asset != asset)
                        continue;

                    amount = item.Volume;
                }

                result += amount;
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
