using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Common;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using StackExchange.Redis;

namespace Lykke.Service.Limitations.Services
{
    public class ClientsDataHelper<T>
        where T : ICashOperation
    {
        private const string _clientSetKeyPattern = "{0}:{1}:client:{2}";
        private const string _opKeySuffixPattern = "opType:{0}:id:{1}";

        private readonly IClientStateRepository<List<T>> _stateRepository;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly IDatabase _db;
        private readonly Func<T, CurrencyOperationType> _opTypeResolver;
        private readonly string _instanceName;
        private readonly string _cacheType;

        internal ClientsDataHelper(
            IClientStateRepository<List<T>> stateRepository,
            IConnectionMultiplexer connectionMultiplexer,
            Func<T, CurrencyOperationType> opTypeResolver,
            string redisInstanceName,
            string cashType)
        {
            _stateRepository = stateRepository;
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

                clientData.Add(item);

                string clientKey = string.Format(_clientSetKeyPattern, _instanceName, _cacheType, item.ClientId);
                string operationSuffix = string.Format(_opKeySuffixPattern, item.OperationType.Value, item.Id);
                var operationKey = $"{clientKey}:{operationSuffix}";

                var tx = _db.CreateTransaction();
                var tasks = new List<Task>
                {
                    tx.SortedSetAddAsync(clientKey, operationSuffix, DateTime.UtcNow.Ticks),
                    _stateRepository.SaveClientStateAsync($"{item.ClientId}-{item.OperationType.Value}", clientData),
                };
                var setKeyTask = tx.StringSetAsync(operationKey, item.ToJson(), ttl);
                tasks.Add(setKeyTask);
                if (!await tx.ExecuteAsync())
                    throw new InvalidOperationException($"Error during operations update for client {item.ClientId} with operation type {item.OperationType}");
                await Task.WhenAll(tasks);
                if (!setKeyTask.Result)
                    throw new InvalidOperationException($"Error during operations update for client {item.ClientId} with operation type {item.OperationType}");
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

                    string clientKey = string.Format(_clientSetKeyPattern, _instanceName, _cacheType, clientId);
                    string operationSuffix = string.Format(_opKeySuffixPattern, operation.OperationType.Value, operation.Id);
                    var operationKey = $"{clientKey}:{operationSuffix}";
                    var tx = _db.CreateTransaction();
                    var tasks = new List<Task>
                    {
                        tx.SortedSetRemoveAsync(clientKey, operationSuffix),
                        tx.KeyDeleteAsync(operationKey),
                    };
                    if (await tx.ExecuteAsync())
                        await Task.WhenAll(tasks);

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
            var keys = await GetClientOperationsKeysAsync(clientId);
            string opTypeStr = operationType.ToString();
            if (keys.Length > 0 && keys.Any(k => k.ToString().Contains(opTypeStr)))
                return;

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

                string clientKey = string.Format(_clientSetKeyPattern, _instanceName, _cacheType, item.ClientId);
                string operationSuffix = string.Format(_opKeySuffixPattern, item.OperationType.Value, item.Id);
                var operationKey = $"{clientKey}:{operationSuffix}";

                var tx = _db.CreateTransaction();
                var tasks = new List<Task>
                {
                    tx.SortedSetAddAsync(clientKey, operationSuffix, DateTime.UtcNow.Ticks),
                };
                var setKeyTask = tx.StringSetAsync(operationKey, item.ToJson(), ttl);
                tasks.Add(setKeyTask);
                if (!await tx.ExecuteAsync())
                    throw new InvalidOperationException($"Error during operations update for client {clientId} with operation type {operationType}");
                await Task.WhenAll(tasks);
                if (!setKeyTask.Result)
                    throw new InvalidOperationException($"Error during operations update for client {clientId} with operation type {operationType}");
            }
        }

        private async Task<(List<T>, bool)> FetchClientDataAsync(string clientId, CurrencyOperationType? operationType = null)
        {
            var clientData = new List<T>();
            bool notCached = false;
            var monthAgo = DateTime.UtcNow.Date.AddMonths(-1);

            List<T> oldAllData = null;

            var opTypes = operationType.HasValue
                ? new List<CurrencyOperationType>(1) { operationType.Value }
                : Enum.GetValues(typeof(CurrencyOperationType)).Cast<CurrencyOperationType>();

            var clientKeys = await GetClientOperationsKeysAsync(clientId);
            var keysDict = clientKeys
                .GroupBy(k => k.ToString().Split(':')[1])
                .ToDictionary(i => i.Key, i => i.ToArray());

            foreach (var opType in opTypes)
            {
                string opTypeStr = opType.ToString();
                if (keysDict.ContainsKey(opTypeStr))
                {
                    var operationJsons = await _db.StringGetAsync(keysDict[opTypeStr]);
                    var operations = operationJsons
                        .Where(i => i.HasValue)
                        .Select(o => o.ToString().DeserializeJson<T>())
                        .ToList();
                    clientData.AddRange(operations);
                    continue;
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

        private async Task<RedisKey[]> GetClientOperationsKeysAsync(string clientId)
        {
            var actualPeriodStartScore = DateTime.UtcNow.AddMonths(-1).Ticks;
            string clientKey = string.Format(_clientSetKeyPattern, _instanceName, _cacheType, clientId);
            var tx = _db.CreateTransaction();
            tx.AddCondition(Condition.KeyExists(clientKey));
            var tasks = new List<Task>
            {
                tx.SortedSetRemoveRangeByScoreAsync(clientKey, 0, actualPeriodStartScore)
            };
            var getKeysTask = tx.SortedSetRangeByScoreAsync(clientKey, actualPeriodStartScore, double.MaxValue);
            tasks.Add(getKeysTask);
            if (await tx.ExecuteAsync())
                await Task.WhenAll(tasks);
            else
                return new RedisKey[0];

            return getKeysTask.Result
                .Select(i => (RedisKey)$"{clientKey}:{i.ToString()}")
                .ToArray();
        }
    }
}
