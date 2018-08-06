using StackExchange.Redis;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using Lykke.Service.Limitations.Core.Services;
using System;

namespace Lykke.Service.Limitations.Services
{
    public abstract class CashOperationsCollectorBase<T>
        where T : ICashOperation
    {
        protected readonly ClientsDataHelper<T> _data;
        protected readonly ICurrencyConverter _currencyConverter;
        protected readonly IAntiFraudCollector _antiFraudCollector;
        protected readonly IAccumulatedDepositAggregator _accumulatedDepositAggregator;
        internal CashOperationsCollectorBase(
            IClientStateRepository<List<T>> stateRepository,
            IAntiFraudCollector antiFraudCollector,
            IConnectionMultiplexer connectionMultiplexer,
            IAccumulatedDepositAggregator accumulatedDepositAggregator,
            string redisInstanceName,
            string cashPrefix,
            ICurrencyConverter currencyConverter)
        {
            _currencyConverter = currencyConverter;
            _antiFraudCollector = antiFraudCollector;
            _accumulatedDepositAggregator = accumulatedDepositAggregator;
            _data = new ClientsDataHelper<T>(
                stateRepository,
                connectionMultiplexer,
                GetOperationType,
                redisInstanceName,
                cashPrefix);
        }

        public virtual async Task AddDataItemAsync(T item, bool setOperationType = true)
        {
            string originAsset = item.Asset;
            double originVolume = item.Volume;

            var converted = await _currencyConverter.ConvertAsync(item.Asset, _currencyConverter.DefaultAsset, item.Volume);

            item.Asset = converted.Item1;
            item.Volume = converted.Item2;
            item.RateToUsd = 1;

            if (item.Asset != _currencyConverter.DefaultAsset)
            // no conversion to USD happend
            // rate to USD should be calculated and saved too
            {
                var rateUsdConverted = await _currencyConverter.ConvertAsync(originAsset, _currencyConverter.DefaultAsset, 1, true);
                item.RateToUsd = rateUsdConverted.convertedAmount;
            }

            if (setOperationType)
            {
                item.OperationType = GetOperationType(item);
            }

            bool isNewItem = await _data.AddDataItemAsync(item);

            if (isNewItem)
            {
                await _antiFraudCollector.RemoveOperationAsync(
                    item.ClientId,
                    originAsset,
                    originVolume,
                    item.OperationType.Value);

                // aggregate Lifetime totals in USD
                switch(item.OperationType)
                {
                    case CurrencyOperationType.CardCashIn:
                    case CurrencyOperationType.SwiftTransfer:
                    case CurrencyOperationType.SwiftTransferOut:
                    //case CurrencyOperationType.CryptoCashOut:

                        if (item.Asset != _currencyConverter.DefaultAsset) 
                            // no conversion to USD happend
                            // convert to USD for Lifetime totals calculation
                        {
                            // force convertation fot crypto, tokens, etc
                            converted = await _currencyConverter.ConvertAsync(item.Asset, _currencyConverter.DefaultAsset, item.Volume, true);
                            item.Asset = converted.Item1;
                            item.Volume = converted.Item2;
                        }

                        await _accumulatedDepositAggregator.AggregateTotalAsync(
                            item.ClientId,
                            item.Asset,
                            Math.Abs(item.Volume),
                            item.OperationType.Value
                            );
                        break;
                }

            }
        }

        public Task<bool> RemoveClientOperationAsync(string clientId, string operationId)
        {
            return _data.RemoveClientOperationAsync(clientId, operationId);
        }

        public Task CacheClientDataAsync(string clientId, CurrencyOperationType operationType)
        {
            return _data.CacheClientDataIfRequiredAsync(clientId, operationType);
        }

        public Task PerformStartupCleanupAsync()
        {
            return Task.CompletedTask;
        }

        protected abstract CurrencyOperationType GetOperationType(T item);

    }
}
