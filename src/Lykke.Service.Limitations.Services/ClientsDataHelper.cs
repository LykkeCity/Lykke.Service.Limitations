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
using Lykke.Common.Log;

namespace Lykke.Service.Limitations.Services
{
    public class ClientsDataHelper<T>
        where T : ICashOperation
    {
        private const string _oldAllClientsDataKeyPattern = "{0}:{1}:*";
        private const string _allClientsDataKeyPatternPrefix = "{0}:{1}:client:";
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
            ILogFactory logFactory,
            IConnectionMultiplexer connectionMultiplexer,
            Func<T, CurrencyOperationType> opTypeResolver,
            string redisInstanceName,
            string cashType)
        {
            _stateRepository = stateRepository;
            _log = logFactory.CreateLog(this);
            _db = connectionMultiplexer.GetDatabase();
            _opTypeResolver = opTypeResolver;
            _instanceName = redisInstanceName;
            _cacheType = cashType;
        }

        internal async Task<(List<T>, bool)> GetClientDataAsync(string clientId, CurrencyOperationType? operationType = null)
        {
            var (list, notCached) = await FetchClientDataAsync(clientId, operationType);
            return (list, notCached);
        }

        internal async Task<bool> AddDataItemAsync(T item)
        {
            var now = DateTime.UtcNow;
            var ttl = item.DateTime.AddMonths(1).Subtract(now);
            if (ttl.Ticks <= 0)
                return true;

            if (!item.OperationType.HasValue)
                item.OperationType = _opTypeResolver(item);

            await _lock.WaitAsync();
            try
            {
                var (clientData, _) = await FetchClientDataAsync(item.ClientId, item.OperationType);
                if (clientData.Any(i => i.Id == item.Id))
                    return false;

                var key = string.Format(_opKeyPattern, _instanceName, _cacheType, item.ClientId, item.OperationType.Value, item.Id);
                bool setResult = await _db.StringSetAsync(key, item.ToJson(), ttl);
                if (!setResult)
                    throw new InvalidOperationException($"Error during operations update for client {item.ClientId} with operation type {item.OperationType}");

                clientData.Add(item);

                await _stateRepository.SaveClientStateAsync($"{item.ClientId}-{item.OperationType.Value}", clientData);
            }
            finally
            {
                _lock.Release();
            }
            return true;
        }

        internal async Task<bool> RemoveClientOperationAsync(string clientId, string operationId)
        {
            await _lock.WaitAsync();
            try
            {
                var (clientData, _) = await FetchClientDataAsync(clientId);
                if (clientData.Count == 0)
                    return false;

                foreach (var operation in clientData)
                {
                    if (operation.Id != operationId)
                        continue;

                    if (!operation.OperationType.HasValue)
                        operation.OperationType = _opTypeResolver(operation);

                    string opKey = string.Format(_opKeyPattern, _instanceName, _cacheType, clientId, operation.OperationType.Value, operation.Id);
                    await _db.KeyDeleteAsync(opKey);

                    var (clientOpTypeData, _) = await FetchClientDataAsync(clientId, operation.OperationType);
                    clientOpTypeData = clientOpTypeData.Where(o => o.Id != operationId).ToList();
                    await _stateRepository.SaveClientStateAsync($"{clientId}-{operation.OperationType.Value}", clientOpTypeData);

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

            var clientState = await _stateRepository.LoadClientStateAsync($"{clientId}-{operationType}")
                ?? await _stateRepository.LoadClientStateAsync(clientId);
            if (clientState == null)
                return;

            var now = DateTime.UtcNow;
            foreach (var item in clientState)
            {
                var ttl = item.DateTime.AddMonths(1).Subtract(now);
                if (ttl.Ticks <= 0)
                    continue;

                if (!item.OperationType.HasValue)
                    item.OperationType = _opTypeResolver(item);
                if (item.OperationType.Value != operationType)
                    continue;

                var key = string.Format(_opKeyPattern, _instanceName, _cacheType, item.ClientId, item.OperationType.Value, item.Id);
                bool setResult = await _db.StringSetAsync(key, item.ToJson(), ttl);
                if (!setResult)
                    throw new InvalidOperationException($"Error during operations update for client {clientId} with operation type {operationType}");
            }
        }

        internal async Task PerformStartupCleanupAsync()
        {
            var keysPattern = string.Format(_oldAllClientsDataKeyPattern, _instanceName, _cacheType);
            RedisResult data = await _db.ScriptEvaluateAsync($"return redis.call('keys', '{keysPattern}')");
            var allClientPrefix = string.Format(_allClientsDataKeyPatternPrefix, _instanceName, _cacheType);
            if (data.IsNull)
                return;

            var keys = (string[])data;
            if (keys.Length == 0)
                return;

            int deletedKeysCount = 0;
            foreach (var key in keys)
            {
                if (key.StartsWith(allClientPrefix))
                    continue;

                await _db.KeyDeleteAsync(key);
                ++deletedKeysCount;
            }

            _log.WriteWarning(nameof(PerformStartupCleanupAsync), null, $"Deleted {deletedKeysCount} old format items for {_cacheType}");
        }

        private async Task<(List<T>, bool)> FetchClientDataAsync(string clientId, CurrencyOperationType? operationType = null)
        {
            var clientData = new List<T>();
            bool notCached = false;
            var monthAgo = DateTime.UtcNow.Date.AddMonths(-1);

            var opTypes = operationType.HasValue
                ? new List<CurrencyOperationType>(1) { operationType.Value }
                : Enum.GetValues(typeof(CurrencyOperationType)).Cast<CurrencyOperationType>().ToList();
            List<T> oldAllData = null;

            foreach (var opType in opTypes)
            {
                var keysPattern = string.Format(_opTypeKeyPattern, _instanceName, _cacheType, clientId, opType);
                RedisResult data = await _db.ScriptEvaluateAsync($"return redis.call('keys', '{keysPattern}')");
                if (!data.IsNull)
                {
                    var keys = (string[])data;
                    if (keys.Length > 0)
                    {
                        var operationJsons = await _db.StringGetAsync(keys.Select(k => (RedisKey)k).ToArray());
                        var operations = operationJsons
                            .Select(o => o.ToString().DeserializeJson<T>())
                            .ToList();
                        clientData.AddRange(operations);
                        continue;
                    }
                }
                var clientState = await _stateRepository.LoadClientStateAsync($"{clientId}-{opType}");
                if (clientState == null)
                {
                    if (oldAllData == null)
                        oldAllData = await _stateRepository.LoadClientStateAsync(clientId);
                    if (oldAllData == null || oldAllData.Count == 0)
                        continue;

                    var notExpired = oldAllData.Where(i => i.DateTime > monthAgo);
                    foreach (var item in notExpired)
                    {
                        if (!item.OperationType.HasValue)
                            item.OperationType = _opTypeResolver(item);
                        if (item.OperationType.Value != opType)
                            continue;

                        clientData.Add(item);
                        notCached = true;
                    }
                }
                else
                {
                    var notExpired = clientState.Where(i => i.DateTime > monthAgo);
                    if (notExpired.Any())
                    {
                        clientData.AddRange(clientState.Where(i => i.DateTime > monthAgo));
                        notCached = true;
                    }
                }
            }
            return (clientData, notCached);
        }
    }
}
