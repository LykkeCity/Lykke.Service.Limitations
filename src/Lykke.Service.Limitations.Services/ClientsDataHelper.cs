using Common;
using Common.Log;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Lykke.Service.Limitations.Services
{
    public class ClientsDataHelper<T>
        where T : ICashOperation
    {
        private const string _allClientDataKeyPattern = "{0}:{1}:{2}";
        private const string _clientDataKeyPattern = "{0}:{1}:client:{2}:opType:*";
        private const string _opTypeKeyPattern = "{0}:{1}:client:{2}:opType:{3}:id:*";
        private const string _opKeyPattern = "{0}:{1}:client:{2}:opType:{3}:id:{4}";

        private readonly IClientStateRepository<List<T>> _stateRepository;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly ILog _log;
        private readonly IDatabase _db;
        private readonly Func<T, CurrencyOperationType> _opTypeResolver;
        private readonly string _instanceName;
        private readonly string _cacheType;

        internal ClientsDataHelper(
            IClientStateRepository<List<T>> stateRepository,
            ILog log,
            IConnectionMultiplexer connectionMultiplexer,
            Func<T, CurrencyOperationType> opTypeResolver,
            string redisInstanceName,
            string cashType)
        {
            _stateRepository = stateRepository;
            _log = log;
            _db = connectionMultiplexer.GetDatabase();
            _opTypeResolver = opTypeResolver;
            _instanceName = redisInstanceName;
            _cacheType = cashType;
        }

        internal async Task<(List<T>, bool)> GetClientDataAsync(string clientId, CurrencyOperationType? operationType = null)
        {
            //TODO remove lock after new Limitation service is deployed
            await _lock.WaitAsync();
            try
            {
                (var list, var notCached) = await FetchClientDataAsync(clientId, operationType);
                return (list, notCached);
            }
            finally
            {
                _lock.Release();
            }
        }

        internal async Task AddDataItemAsync(T item)
        {
            var now = DateTime.UtcNow;
            var ttl = item.DateTime.AddMonths(1).Subtract(now);
            if (ttl.Ticks <= 0)
                return;

            if (!item.OperationType.HasValue)
                item.OperationType = _opTypeResolver(item);

            await _lock.WaitAsync();
            try
            {
                (var clientData, _) = await FetchClientDataAsync(item.ClientId, item.OperationType);

                var key = string.Format(_opKeyPattern, _instanceName, _cacheType, item.ClientId, item.OperationType.Value, item.Id);
                bool setResult = await _db.StringSetAsync(key, item.ToJson(), ttl);
                if (!setResult)
                    throw new InvalidOperationException($"Error during operations update for client {item.ClientId} with operation type {item.OperationType}");

                clientData.Add(item);
                await _stateRepository.SaveClientStateAsync($"{item.ClientId}-{item.OperationType.Value}", clientData);

                //TODO Remove code below after new Limitations service is deployed + delete lock
                (clientData, _) = await FetchClientDataAsync(item.ClientId);
                clientData.Add(item);

                key = string.Format(_allClientDataKeyPattern, _instanceName, _cacheType, item.ClientId);
                setResult = await _db.StringSetAsync(key, clientData.ToJson());
                if (!setResult)
                    throw new InvalidOperationException($"Error during operations update for client {item.ClientId}");

                await _stateRepository.SaveClientStateAsync(item.ClientId, clientData);
                // remove till here
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
                    var operation = clientData[i];
                    if (operation.Id != operationId)
                        continue;

                    if (!operation.OperationType.HasValue)
                        operation.OperationType = _opTypeResolver(operation);

                    string opKey = string.Format(_opKeyPattern, _instanceName, _cacheType, clientId, operation.OperationType.Value, operation.Id);
                    await _db.KeyDeleteAsync(opKey);

                    (var clientOpTypeData, _) = await FetchClientDataAsync(clientId, operation.OperationType);
                    clientOpTypeData = clientOpTypeData.Where(o => o.Id != operationId).ToList();
                    await _stateRepository.SaveClientStateAsync($"{clientId}-{operation.OperationType.Value}", clientOpTypeData);

                    //TODO Remove code below after new Limitations service is deployed + delete lock
                    clientData.RemoveAt(i);
                    string clientKey = string.Format(_allClientDataKeyPattern, _instanceName, _cacheType, clientId);
                    bool setResult = await _db.StringSetAsync(clientKey, clientData.ToJson());
                    if (!setResult)
                        throw new InvalidOperationException($"Error during operations update for client {clientId}");

                    await _stateRepository.SaveClientStateAsync(clientId, clientData);
                    // remove till here

                    return true;
                }
            }
            finally
            {
                _lock.Release();
            }

            return false;
        }

        internal async Task CacheClientDataIfRequiredAsync(string clientId, CurrencyOperationType operationType)
        {
            var keysPattern = string.Format(_opTypeKeyPattern, _instanceName, _cacheType, clientId, operationType);
            RedisResult data = await _db.ScriptEvaluateAsync($"return redis.call('keys', '{keysPattern}')");
            if (!data.IsNull)
            {
                var keys = (string[])data;
                if (keys.Length > 0)
                    return;
            }

            var clientState = await _stateRepository.LoadClientStateAsync($"{clientId}-{operationType}");
            if (clientState == null)
                clientState = await _stateRepository.LoadClientStateAsync(clientId);

            var now = DateTime.UtcNow;
            foreach (var item in clientState)
            {
                if (!item.OperationType.HasValue)
                    item.OperationType = _opTypeResolver(item);

                if (item.OperationType.Value != operationType)
                    continue;

                var ttl = item.DateTime.AddMonths(1).Subtract(now);
                if (ttl.Ticks <= 0)
                    continue;

                var key = string.Format(_opKeyPattern, _instanceName, _cacheType, item.ClientId, item.OperationType.Value, item.Id);
                bool setResult = await _db.StringSetAsync(key, item.ToJson(), ttl);
                if (!setResult)
                    throw new InvalidOperationException($"Error during operations update for client {clientId} with operation type {operationType}");
            }
        }

        private async Task<(List<T>, bool)> FetchClientDataAsync(string clientId, CurrencyOperationType? operationType = null)
        {
            var clientData = new List<T>();
            bool notCached = false;
            var monthAgo = DateTime.UtcNow.Date.AddMonths(-1);

            var opTypes = operationType.HasValue
                ? new List<CurrencyOperationType>(1) { operationType.Value }
                : Enum.GetValues(typeof(CurrencyOperationType)).Cast<CurrencyOperationType>().ToList();
            foreach (var opType in opTypes)
            {
                var keysPattern = string.Format(_opTypeKeyPattern, _instanceName, _cacheType, clientId, operationType);
                RedisResult data = await _db.ScriptEvaluateAsync($"return redis.call('keys', '{keysPattern}')");
                if (!data.IsNull)
                {
                    var keys = (string[])data;
                    if (keys.Length > 0)
                    {
                        var operationJsons = await _db.StringGetAsync(keys.Select(k => (RedisKey)k).ToArray());
                        var operations = operationJsons
                            .Select(o => o.ToString().DeserializeJson<T>())
                            .Where(o => !operationType.HasValue || o.OperationType == operationType.Value)
                            .ToList();
                        clientData.AddRange(operations);
                        continue;
                    }
                }
                var clientState = await _stateRepository.LoadClientStateAsync($"{clientId}-{opType}");
                if (clientState == null)
                {
                    clientState = await _stateRepository.LoadClientStateAsync(clientId);
                    var notExpired = clientState.Where(i => i.DateTime > monthAgo);
                    if (operationType.HasValue)
                    {
                        clientData = new List<T>();
                        foreach (var item in notExpired)
                        {
                            if (!item.OperationType.HasValue)
                                item.OperationType = _opTypeResolver(item);
                            if (item.OperationType.Value != operationType.Value)
                                continue;
                            clientData.Add(item);
                        }
                    }
                    else
                    {
                        clientData = notExpired.ToList();
                    }
                    return (clientData, true);
                }
                notCached = true;
                clientData.AddRange(clientState.Where(i => i.DateTime > monthAgo));
            }
            return (clientData, notCached);
        }
    }
}
