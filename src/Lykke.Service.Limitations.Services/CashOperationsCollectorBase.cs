using Common.Log;
using Microsoft.Extensions.Caching.Distributed;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using Lykke.Service.Limitations.Core.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Services
{
    public abstract class CashOperationsCollectorBase<T>
        where T : ICashOperation
    {
        protected readonly ClientsDataHelper<T> _data;
        protected readonly ICurrencyConverter _currencyConverter;
        protected readonly IAntiFraudCollector _antiFraudCollector;
        protected readonly ILog _log;

        public CashOperationsCollectorBase(
            IClientStateRepository<List<T>> stateRepository,
            IDistributedCache distributedCache,
            IAntiFraudCollector antiFraudCollector,
            string cashPrefix,
            ICurrencyConverter currencyConverter,
            ILog log)
        {
            _currencyConverter = currencyConverter;
            _antiFraudCollector = antiFraudCollector;
            _log = log;
            _data = new ClientsDataHelper<T>(
                stateRepository,
                distributedCache,
                _log,
                cashPrefix);
        }

        public virtual async Task AddDataItemAsync(T item)
        {
            string originAsset = item.Asset;
            double originVolume = item.Volume;

            var converted = await _currencyConverter.ConvertAsync(item.Asset, _currencyConverter.DefaultAsset, item.Volume);

            item.Asset = converted.Item1;
            item.Volume = converted.Item2;

            await _data.AddDataItemAsync(item);

            CurrencyOperationType operationType = GetOperationType(item);
            await _antiFraudCollector.RemoveOperationAsync(
                item.ClientId,
                item.Asset,
                item.Volume,
                operationType);
        }

        public Task<bool> RemoveClientOperationAsync(string clientId, string operationId)
        {
            return _data.RemoveClientOperationAsync(clientId, operationId);
        }

        public Task CacheClientDataAsync(string clientId)
        {
            return _data.CacheClientDataIfRequiredAsync(clientId);
        }

        protected abstract CurrencyOperationType GetOperationType(T item);
    }
}
