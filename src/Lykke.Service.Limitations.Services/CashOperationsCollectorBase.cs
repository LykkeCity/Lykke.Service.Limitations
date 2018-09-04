using Common.Log;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using Lykke.Service.Limitations.Core.Services;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Common.Log;

namespace Lykke.Service.Limitations.Services
{
    public abstract class CashOperationsCollectorBase<T>
        where T : ICashOperation
    {
        protected readonly ClientsDataHelper<T> _data;
        protected readonly ICurrencyConverter _currencyConverter;
        protected readonly IAntiFraudCollector _antiFraudCollector;
        protected readonly IAccumulatedDepositAggregator _accumulatedDepositAggregator;
        protected readonly ILog _log;

        public CashOperationsCollectorBase(
            IClientStateRepository<List<T>> stateRepository,
            IAntiFraudCollector antiFraudCollector,
            IConnectionMultiplexer connectionMultiplexer,
            IAccumulatedDepositAggregator accumulatedDepositAggregator,
            string redisInstanceName,
            string cashPrefix,
            ICurrencyConverter currencyConverter,
            ILogFactory logFactory)
        {
            _currencyConverter = currencyConverter;
            _antiFraudCollector = antiFraudCollector;
            _accumulatedDepositAggregator = accumulatedDepositAggregator;
            _data = new ClientsDataHelper<T>(
                stateRepository,
                logFactory,
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

            // no conversion happened
            // source asset and volume are saved to DB
            // rate to USD should be calculated and saved too
            if (item.Asset == originAsset && originAsset != _currencyConverter.DefaultAsset) 
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

                switch(item.OperationType)
                {
                    case CurrencyOperationType.CardCashIn:
                    case CurrencyOperationType.SwiftTransfer:
                    case CurrencyOperationType.SwiftTransferOut:
                    //case CurrencyOperationType.CryptoCashOut:
                        if (_currencyConverter.IsNotConvertible(item.Asset))
                        {
                            // force convertation fot crypto, tokens, etc
                            converted = await _currencyConverter.ConvertAsync(item.Asset, _currencyConverter.DefaultAsset, item.Volume, true);
                            item.Asset = converted.Item1;
                            item.Volume = converted.Item2;
                        }
                        await _accumulatedDepositAggregator.AggregateTotalAsync(
                            item.ClientId,
                            item.Asset,
                            item.Volume,
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
            return _data.PerformStartupCleanupAsync();
        }

        protected abstract CurrencyOperationType GetOperationType(T item);
    }
}
