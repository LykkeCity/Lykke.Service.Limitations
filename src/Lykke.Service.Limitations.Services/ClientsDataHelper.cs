using Common;
using Common.Log;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Lykke.Service.Limitations.Services
{
    public class ClientsDataHelper<T>
        where T : ICashOperation
    {
        private readonly IClientStateRepository<List<T>> _stateRepository;
        private readonly IDistributedCache _distributedCache;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly ILog _log;
        private readonly string _cacheKeyPrefix;

        internal ClientsDataHelper(
            IClientStateRepository<List<T>> stateRepository,
            IDistributedCache distributedCache,
            ILog log,
            string cashPrefix)
        {
            _stateRepository = stateRepository;
            _distributedCache = distributedCache;
            _log = log;
            _cacheKeyPrefix = $":{cashPrefix}:";
        }

        internal async Task<(List<T>, bool)> GetClientDataAsync(string clientId)
        {
            List<T> list;
            bool notCached;
            await _lock.WaitAsync();
            try
            {
                (list, notCached) = await FetchClientDataAsync(clientId);
            }
            finally
            {
                _lock.Release();
            }
            if (list.Count == 0)
                return (list, notCached);

            var result = new List<T>(list.Count);
            result.AddRange(list);
            return (result, notCached);
        }

        internal async Task AddDataItemAsync(T item)
        {
            await _lock.WaitAsync();
            try
            {
                (var clientData, _) = await FetchClientDataAsync(item.ClientId);
                clientData.Add(item);
                await _distributedCache.SetStringAsync(_cacheKeyPrefix + item.ClientId, clientData.ToJson());
                await _stateRepository.SaveClientStateAsync(item.ClientId, clientData);
            }
            finally
            {
                _lock.Release();
            }
        }

        internal async Task<bool> RemoveClientOperationAsync(string clientId, string operationId)
        {
            await _lock.WaitAsync();
            try
            {
                (var clientData, _) = await FetchClientDataAsync(clientId);
                if (clientData.Count == 0)
                    return false;

                for (int i = 0; i < clientData.Count; ++i)
                {
                    if (clientData[i].Id != operationId)
                        continue;

                    clientData.RemoveAt(i);
                    await _distributedCache.SetStringAsync(_cacheKeyPrefix + clientId, clientData.ToJson());
                    await _stateRepository.SaveClientStateAsync(clientId, clientData);
                    return true;
                }
            }
            finally
            {
                _lock.Release();
            }

            return false;
        }

        internal async Task CacheClientDataIfRequiredAsync(string clientId)
        {
            string clientDataJson = await _distributedCache.GetStringAsync(_cacheKeyPrefix + clientId);
            if (!string.IsNullOrWhiteSpace(clientDataJson))
                return;

            await _lock.WaitAsync();
            try
            {
                var clientState = await _stateRepository.LoadClientStateAsync(clientId);
                clientDataJson = await _distributedCache.GetStringAsync(_cacheKeyPrefix + clientId);
                if (!string.IsNullOrWhiteSpace(clientDataJson))
                    return;

                await _distributedCache.SetStringAsync(_cacheKeyPrefix + clientId, clientState.ToJson());
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<(List<T>, bool)> FetchClientDataAsync(string clientId)
        {
            List<T> clientState;
            bool notCached = false;
            string clientDataJson = await _distributedCache.GetStringAsync(_cacheKeyPrefix + clientId);
            if (!string.IsNullOrWhiteSpace(clientDataJson))
            {
                clientState = clientDataJson.DeserializeJson<List<T>>();
            }
            else
            {
                clientState = await _stateRepository.LoadClientStateAsync(clientId);
                notCached = true;
            }
            var clientData = new List<T>();
            if (clientState != null)
            {
                var monthAgo = DateTime.UtcNow.Date.AddMonths(-1);
                foreach (T item in clientState)
                {
                    if (item.DateTime < monthAgo)
                        continue;

                    clientData.Add(item);
                }
            }

            return (clientData, notCached);
        }
    }
}
