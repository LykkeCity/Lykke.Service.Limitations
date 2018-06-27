using Common;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Services;
using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Services
{
    public class AntiFraudCollector : IAntiFraudCollector
    {
        private const double _minDiff = 0.00000001;

        private readonly IDistributedCache _distributedCache;
        private readonly ICurrencyConverter _currencyConverter;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        private const string _cacheKeyPrefix = ":attempts:";

        public AntiFraudCollector(

            IDistributedCache distributedCache,
            ICurrencyConverter currencyConverter)
        {
            _distributedCache = distributedCache;
            _currencyConverter = currencyConverter;
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
            await _lock.WaitAsync();
            try
            {
                var clientData = await FetchClientDataAsync(clientId);
                clientData.Add(attempt);
                string json = clientData.ToJson();
                await _distributedCache.SetStringAsync(_cacheKeyPrefix + clientId, json);
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
        }

        public async Task RemoveOperationAsync(
            string clientId,
            string asset,
            double amount,
            CurrencyOperationType operationType)
        {
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
                        await _distributedCache.SetStringAsync(_cacheKeyPrefix + clientId, json);
                    }
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<double> GetAttemptsValueAsync(
            string clientId,
            string asset,
            LimitationType limitType)
        {
            await _lock.WaitAsync();
            try
            {
                double result = 0;
                var clientData = await FetchClientDataAsync(clientId);
                if (clientData.Count > 0)
                {
                    var opTypes = LimitMapHelper.MapLimitationType(limitType);
                    foreach (var item in clientData)
                    {
                        if (!opTypes.Contains(item.OperationType))
                            continue;

                        if (_currencyConverter.IsNotConvertible(asset) && item.Asset == asset)
                        {
                            result += item.Amount;
                        }
                        else if (!_currencyConverter.IsNotConvertible(asset) && !_currencyConverter.IsNotConvertible(item.Asset))
                        {
                            var converted = await _currencyConverter.ConvertAsync(item.Asset, asset, item.Amount);
                            result += converted.Item2;
                        }
                    }
                }
                return result;
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<List<CurrencyOperationAttempt>> GetClientDataAsync(string clientId)
        {
            await _lock.WaitAsync();
            try
            {
                return await FetchClientDataAsync(clientId);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task<List<CurrencyOperationAttempt>> FetchClientDataAsync(string clientId)
        {
            var clientDataJson = await _distributedCache.GetStringAsync(_cacheKeyPrefix + clientId);
            if (string.IsNullOrWhiteSpace(clientDataJson))
                return new List<CurrencyOperationAttempt>(0);
            var clientData = clientDataJson.DeserializeJson<List<CurrencyOperationAttempt>>();
            if (clientData == null)
                return new List<CurrencyOperationAttempt>(0);
            if (clientData.Count > 0)
            {
                var now = DateTime.UtcNow;
                for (int i = 0; i < clientData.Count; i++)
                {
                    if (now > clientData[i].ExpireAt)
                        continue;

                    clientData.RemoveAt(i);
                    --i;
                }
            }
            return clientData;
        }
    }
}
