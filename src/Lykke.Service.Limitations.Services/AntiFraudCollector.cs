using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Services;
using StackExchange.Redis;

namespace Lykke.Service.Limitations.Services
{
    public class AntiFraudCollector : IAntiFraudCollector
    {
        private const double _minDiff = 0.00000001;
        private const string _dataFormat = "yyyy-MM-dd-HH-mm-ss-fffffff";

        private const string _clientSetKeyPattern = "{0}:attempts:client:{1}";
        private const string _attemptKeySuffixPattern = "opType:{0}:time:{1}";

        private readonly TimeSpan _cashOperationsTimeout = TimeSpan.FromMinutes(10);
        private readonly IDatabase _db;
        private readonly string _instanceName;
        private readonly ICurrencyConverter _currencyConverter;

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
            };

            string clientKey = string.Format(_clientSetKeyPattern, _instanceName, clientId);
            string attemptSuffix = string.Format(_attemptKeySuffixPattern, operationType, now.ToString(_dataFormat));
            var attemptKey = $"{clientKey}:{attemptSuffix}";

            var tx = _db.CreateTransaction();
            var tasks = new List<Task>
            {
                tx.SortedSetAddAsync(clientKey, attemptSuffix, DateTime.UtcNow.Ticks)
            };
            var setKeyTask = tx.StringSetAsync(attemptKey, attempt.ToJson(), TimeSpan.FromMinutes(ttlInMinutes));
            tasks.Add(setKeyTask);
            if (!await tx.ExecuteAsync())
                throw new InvalidOperationException($"Error during attempt adding for client {clientId}");
            await Task.WhenAll(tasks);
            if (!setKeyTask.Result)
                throw new InvalidOperationException($"Error during attempt adding for client {clientId}");
        }

        public async Task RemoveOperationAsync(
            string clientId,
            string asset,
            double amount,
            CurrencyOperationType operationType)
        {
            var keys = await GetClientAttemptKeysAsync(clientId);
            if (keys.Length == 0)
                return;

            var attemptJsons = await _db.StringGetAsync(keys);
            var attemptToDelete = attemptJsons
                .Where(a => a.HasValue)
                .Select(a => a.ToString().DeserializeJson<CurrencyOperationAttempt>())
                .Where(i => i.OperationType == operationType && i.Asset == asset && Math.Abs(amount - i.Amount) < _minDiff)
                .OrderBy(i => i.DateTime)
                .FirstOrDefault();
            if (attemptToDelete == null)
                return;

            string clientKey = string.Format(_clientSetKeyPattern, _instanceName, clientId);
            string timeStr = attemptToDelete.DateTime.ToString(_dataFormat);
            string attemptSuffix = string.Format(_attemptKeySuffixPattern, operationType, timeStr);
            var attemptKey = $"{clientKey}:{attemptSuffix}";
            var tx = _db.CreateTransaction();
            var tasks = new List<Task>
            {
                tx.SortedSetRemoveAsync(clientKey, attemptSuffix),
                tx.KeyDeleteAsync(attemptKey),
            };
            if (await tx.ExecuteAsync())
                await Task.WhenAll(tasks);
        }

        public async Task<double> GetAttemptsValueAsync(
            string clientId,
            string asset,
            LimitationType limitType)
        {
            var keys = await GetClientAttemptKeysAsync(clientId);
            if (keys.Length == 0)
                return 0;

            var attemptJsons = await _db.StringGetAsync(keys);

            var opTypes = LimitMapHelper.MapLimitationType(limitType);
            var attempts = attemptJsons
                .Where(a => a.HasValue)
                .Select(a => a.ToString().DeserializeJson<CurrencyOperationAttempt>())
                .Where(a => opTypes.Contains(a.OperationType))
                .ToList();

            double result = 0;
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
            return result;
        }

        public async Task<List<CurrencyOperationAttempt>> GetClientDataAsync(string clientId)
        {
            var keys = await GetClientAttemptKeysAsync(clientId);
            if (keys.Length == 0)
                return new List<CurrencyOperationAttempt>(0);

            var attemptJsons = await _db.StringGetAsync(keys);
            var attempts = attemptJsons
                .Where(a => a.HasValue)
                .Select(a => a.ToString().DeserializeJson<CurrencyOperationAttempt>())
                .ToList();
            return attempts;
        }

        public Task PerformStartupCleanupAsync()
        {
            return Task.CompletedTask;
        }

        private async Task<RedisKey[]> GetClientAttemptKeysAsync(string clientId)
        {
            string clientKey = string.Format(_clientSetKeyPattern, _instanceName, clientId);
            if (!await _db.KeyExistsAsync(clientKey))
                return new RedisKey[0];

            var actualPeriodStartScore = DateTime.UtcNow.Subtract(_cashOperationsTimeout).Ticks;
            var tx = _db.CreateTransaction();
            var tasks = new List<Task>
            {
                tx.SortedSetRemoveRangeByScoreAsync(clientKey, 0, actualPeriodStartScore)
            };
            var getKeysTask = tx.SortedSetRangeByScoreAsync(clientKey, actualPeriodStartScore, double.MaxValue);
            tasks.Add(getKeysTask);
            if (await tx.ExecuteAsync())
                await Task.WhenAll(tasks);

            return getKeysTask.Result
                .Select(i => (RedisKey)$"{clientKey}:{i.ToString()}")
                .ToArray();
        }
    }
}
