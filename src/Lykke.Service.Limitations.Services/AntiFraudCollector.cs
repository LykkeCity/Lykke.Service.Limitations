using Common;
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
        private const string _allClientDataKeyPattern = "{0}:attempts:{1}";
        private const string _allAttemptsKeyPattern = "{0}:attempts:client:{1}:opType:*";
        private const string _opTypeKeyPattern = "{0}:attempts:client:{1}:opType:{2}";
        private const string _attemptKeyPattern = "{0}:attempts:client:{1}:opType:{2}:time:{3}";

        private readonly IDatabase _db;
        private readonly string _instanceName;
        private readonly ICurrencyConverter _currencyConverter;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public AntiFraudCollector(
            IConnectionMultiplexer connectionMultiplexer,
            ICurrencyConverter currencyConverter,
            string redisInstanceName)
        {
            _currencyConverter = currencyConverter;
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
                ExpireAt = now.AddMinutes(ttlInMinutes),
            };

            string key = string.Format(_attemptKeyPattern, _instanceName, clientId, operationType, now.ToString(_dataFormat));
            bool setResult = await _db.StringSetAsync(key, attempt.ToJson(), TimeSpan.FromMinutes(ttlInMinutes));
            if (!setResult)
                throw new InvalidOperationException($"Error during attempt adding for client {clientId}");

            //TODO remove part below after new Limitations service is deployed
            await _lock.WaitAsync();
            try
            {
                var clientData = await FetchClientDataAsync(clientId);
                clientData.Add(attempt);
                string json = clientData.ToJson();
                key = string.Format(_allClientDataKeyPattern, _instanceName, clientId);
                setResult = await _db.StringSetAsync(key, json);
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
                        .FirstOrDefault(i => i.Asset == asset && Math.Abs(amount - i.Amount) < _minDiff);
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
                var clientData = await FetchClientDataAsync(clientId);
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
                        string key = string.Format(_allClientDataKeyPattern, _instanceName, clientId);
                        bool setResult = await _db.StringSetAsync(key, json);
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

        public Task<List<CurrencyOperationAttempt>> GetClientDataAsync(string clientId)
        {
            return FetchClientDataAsync(clientId);
        }

        private async Task<List<CurrencyOperationAttempt>> FetchClientDataAsync(string clientId)
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
    }
}
