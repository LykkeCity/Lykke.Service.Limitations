using Common;
using Common.Log;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Services;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Services
{
    public class AntiFraudCollector : IAntiFraudCollector
    {
        private const double _minDiff = 0.00000001;
        private const string _dataFormat = "yyyy-MM-dd-HH-mm-ss-fffffff";
        private const string _allOldClientsDataKeyPattern = "{0}:attempts:*";
        private const string _oldClientDataKeyPattern = "{0}:attempts:{1}";
        private const string _allAttemptsKeyPattern = "{0}:attempts:client:{1}:opType:*";
        private const string _opTypeKeyPattern = "{0}:attempts:client:{1}:opType:{2}:time:*";
        private const string _attemptKeyPattern = "{0}:attempts:client:{1}:opType:{2}:time:{3}";

        private readonly IDatabase _db;
        private readonly string _instanceName;
        private readonly ICurrencyConverter _currencyConverter;
        private readonly ILog _log;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public AntiFraudCollector(
            IConnectionMultiplexer connectionMultiplexer,
            ICurrencyConverter currencyConverter,
            string redisInstanceName,
            ILog log)
        {
            _currencyConverter = currencyConverter;
            _log = log;
            _db = connectionMultiplexer.GetDatabase();
            _instanceName = redisInstanceName;
        }

        public async Task AddDataAsync(
            string clientId,
            string asset,
            double amount,
            int ttlInMinutes,
            CurrencyOperationType operationType)
        {
            amount = Math.Abs(amount);
            var now = DateTime.UtcNow;
            var attempt = new CurrencyOperationAttempt
            {
                ClientId = clientId,
                Asset = asset,
                Amount = amount,
                OperationType = operationType,
                DateTime = now,
            };

            string key = string.Format(_attemptKeyPattern, _instanceName, clientId, operationType, now.ToString(_dataFormat));
            bool result = await _db.StringSetAsync(key, attempt.ToJson(), TimeSpan.FromMinutes(ttlInMinutes));
            if (!result)
                throw new InvalidOperationException($"Error during attempt adding for client {clientId}");

            //TODO remove part below after new Limitations service is deployed
            await _lock.WaitAsync();
            try
            {
                var clientData = await FetchOldClientDataAsync(clientId);
                clientData.Add(attempt);
                string json = clientData.ToJson();
                string clientKey = string.Format(_oldClientDataKeyPattern, _instanceName, clientId);
                bool setResult = await _db.StringSetAsync(clientKey, json);
                if (!setResult)
                    throw new InvalidOperationException($"Error during attempts update for client {clientId}");
            }
            finally
            {
                _lock.Release();
            }

#pragma warning disable CS4014
            Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(ttlInMinutes));
                await RemoveOperationAsync(
                    attempt.ClientId,
                    attempt.Asset,
                    attempt.Amount,
                    attempt.OperationType);
            });
#pragma warning restore CS4014
            // till here
        }

        public async Task RemoveOperationAsync(
            string clientId,
            string asset,
            double amount,
            CurrencyOperationType operationType)
        {
            string keysPattern = string.Format(_opTypeKeyPattern, _instanceName, clientId, operationType);
            var data = await _db.ScriptEvaluateAsync($"return redis.call('keys', '{keysPattern}')");
            if (!data.IsNull)
            {
                var keys = (string[])data;
                if (keys.Length > 0)
                {
                    var attemptJsons = await _db.StringGetAsync(keys.Select(key => (RedisKey)key).ToArray());
                    var attemptToDelete = attemptJsons.Where(a => a.HasValue)
                        .Select(a => a.ToString().DeserializeJson<CurrencyOperationAttempt>())
                        .Where(i => i.Asset == asset && Math.Abs(amount - i.Amount) < _minDiff)
                        .OrderBy(i => i.DateTime)
                        .FirstOrDefault();
                    if (attemptToDelete != null)
                    {
                        string key = string.Format(_attemptKeyPattern, _instanceName, clientId, operationType, attemptToDelete.DateTime.ToString(_dataFormat));
                        await _db.KeyDeleteAsync(key);
                    }
                }
            }

            //TODO remove part below after new Limitations service is deployed
            await _lock.WaitAsync();
            try
            {
                var clientData = await FetchOldClientDataAsync(clientId);
                if (clientData.Count > 0)
                {
                    bool removed = false;
                    for (int i = 0; i < clientData.Count; ++i)
                    {
                        if (clientData[i].Asset != asset
                            || Math.Abs(clientData[i].Amount - amount) >= _minDiff
                            || clientData[i].OperationType != operationType)
                            continue;

                        clientData.RemoveAt(i);
                        removed = true;
                        break;
                    }
                    if (removed)
                    {
                        string json = clientData.ToJson();
                        string clientKey = string.Format(_oldClientDataKeyPattern, _instanceName, clientId);
                        bool setResult = await _db.StringSetAsync(clientKey, json);
                        if (!setResult)
                            throw new InvalidOperationException($"Error during attempts update for client {clientId}");
                    }
                }
            }
            finally
            {
                _lock.Release();
            }
            // till here
        }

        public async Task<double> GetAttemptsValueAsync(
            string clientId,
            string asset,
            LimitationType limitType)
        {
            double result = 0;
            var opTypes = LimitMapHelper.MapLimitationType(limitType);
            foreach (var opType in opTypes)
            {
                string keysPattern = string.Format(_opTypeKeyPattern, _instanceName, clientId, opType);
                var data = await _db.ScriptEvaluateAsync($"return redis.call('keys', '{keysPattern}')");
                if (data.IsNull)
                    continue;

                var keys = (string[])data;
                if (keys.Length == 0)
                    continue;

                var attemptJsons = await _db.StringGetAsync(keys.Select(key => (RedisKey)key).ToArray());
                var attempts = attemptJsons.Where(a => a.HasValue)
                    .Select(a => a.ToString().DeserializeJson<CurrencyOperationAttempt>())
                    .ToList();

                foreach (var attempt in attempts)
                {
                    if (_currencyConverter.IsNotConvertible(asset) && attempt.Asset == asset)
                    {
                        result += attempt.Amount;
                    }
                    else if (!_currencyConverter.IsNotConvertible(asset) && !_currencyConverter.IsNotConvertible(attempt.Asset))
                    {
                        var converted = await _currencyConverter.ConvertAsync(attempt.Asset, asset, attempt.Amount);
                        result += converted.Item2;
                    }
                }
            }
            return result;
        }

        public async Task<List<CurrencyOperationAttempt>> GetClientDataAsync(string clientId)
        {
            string keysPattern = string.Format(_allAttemptsKeyPattern, _instanceName, clientId);
            var data = await _db.ScriptEvaluateAsync($"return redis.call('keys', '{keysPattern}')");
            if (data.IsNull)
                return new List<CurrencyOperationAttempt>(0);

            var keys = (string[])data;
            if (keys.Length == 0)
                return new List<CurrencyOperationAttempt>(0);

            var attemptJsons = await _db.StringGetAsync(keys.Select(key => (RedisKey)key).ToArray());
            var attempts = attemptJsons
                .Where(a => a.HasValue)
                .Select(a => a.ToString().DeserializeJson<CurrencyOperationAttempt>())
                .ToList();
            return attempts;
        }

        public async Task PerformStartupCleanupAsync()
        {
            await RemoveNotStringValuesAsync();
            //await CleanupOldFormatAttemptsAsync()
        }

        //TODO remove this method after new Limitations service is deployed
        private async Task<List<CurrencyOperationAttempt>> FetchOldClientDataAsync(string clientId)
        {
            string clientKey = string.Format(_oldClientDataKeyPattern, _instanceName, clientId);
            var getResult = await _db.StringGetAsync(clientKey);
            if (!getResult.HasValue)
                return new List<CurrencyOperationAttempt>();

            var result = getResult.ToString().DeserializeJson<List<CurrencyOperationAttempt>>();
            return result ?? new List<CurrencyOperationAttempt>();
        }

        private async Task CleanupOldFormatAttemptsAsync()
        {
            string keysPattern = string.Format(_allOldClientsDataKeyPattern, _instanceName);
            var data = await _db.ScriptEvaluateAsync($"return redis.call('keys', '{keysPattern}')");
            if (data.IsNull)
                return;

            var keys = (string[])data;
            if (keys.Length == 0)
                return;

            foreach (var key in keys)
            {
                await _db.KeyDeleteAsync(key);
            }
        }

        private async Task RemoveNotStringValuesAsync()
        {
            string allKeysPattern = $"{_instanceName}:*";
            var data = await _db.ScriptEvaluateAsync($"return redis.call('keys', '{allKeysPattern}')");
            if (data.IsNull)
                return;

            var keys = (string[])data;
            if (keys.Length == 0)
                return;

            var deletedTypes = new HashSet<string>();
            var keysToDelete = new List<string>();
            foreach (var key in keys)
            {
                var type = await _db.KeyTypeAsync(key);
                if (type == RedisType.String)
                    continue;

                keysToDelete.Add(key);
                deletedTypes.Add(type.ToString());
            }

            if (keysToDelete.Count > 0)
            {
                await _db.KeyDeleteAsync(keysToDelete.Select(i => (RedisKey)i).ToArray());
                _log.WriteWarning(nameof(RemoveNotStringValuesAsync), null, $"Deleted {keysToDelete.Count} items of types {string.Join(',', deletedTypes)}");
            }
        }
    }
}
