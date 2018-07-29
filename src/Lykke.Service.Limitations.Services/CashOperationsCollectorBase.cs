﻿using Common.Log;
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
        
        public CashOperationsCollectorBase(
            IClientStateRepository<List<T>> stateRepository,
            IAntiFraudCollector antiFraudCollector,
            IConnectionMultiplexer connectionMultiplexer,
            string redisInstanceName,
            string cashPrefix,
            ICurrencyConverter currencyConverter,
            ILogFactory logFactory)
        {
            _currencyConverter = currencyConverter;
            _antiFraudCollector = antiFraudCollector;            
            _data = new ClientsDataHelper<T>(
                stateRepository,
                logFactory,
                connectionMultiplexer,
                GetOperationType,
                redisInstanceName,
                cashPrefix);
        }

        public virtual async Task AddDataItemAsync(T item)
        {
            string originAsset = item.Asset;
            double originVolume = item.Volume;

            var converted = await _currencyConverter.ConvertAsync(item.Asset, _currencyConverter.DefaultAsset, item.Volume);

            item.Asset = converted.Item1;
            item.Volume = converted.Item2;
            item.OperationType = GetOperationType(item);

            bool isNewItem = await _data.AddDataItemAsync(item);

            if (isNewItem)
                await _antiFraudCollector.RemoveOperationAsync(
                    item.ClientId,
                    originAsset,
                    originVolume,
                    item.OperationType.Value);
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
