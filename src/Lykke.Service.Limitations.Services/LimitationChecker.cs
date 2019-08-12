using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Common.Cache;
using Lykke.Common.Log;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.JobClient;
using Lykke.Service.Limitations.Core.Repositories;
using Lykke.Service.Limitations.Core.Services;

namespace Lykke.Service.Limitations.Services
{
    public class LimitationChecker : ILimitationCheck
    {
        private const int _cashOperationsTimeoutInMinutes = 10;

        private readonly int _attemptRetainInMinutes;
        private readonly ICashOperationsCollector _cashOperationsCollector;
        private readonly ICashTransfersCollector _cashTransfersCollector;
        private readonly ICurrencyConverter _currencyConverter;
        private readonly IAntiFraudCollector _antiFraudCollector;
        private readonly ILimitOperationsApi _limitOperationsApi;
        private readonly List<CashOperationLimitation> _limits;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private readonly ISwiftTransferLimitationsRepository _swiftTransferLimitationsRepository;
        private readonly ILimitSettingsRepository _limitSettingsRepository;
        private readonly ICallTimeLimitsRepository _callTimeLimitsRepository;
        private readonly OnDemandDataCache<Asset> _assets;
        private readonly ILog _log;

        public LimitationChecker(
            ICashOperationsCollector cashOperationsCollector,
            ICashTransfersCollector cashTransfersCollector,
            ICurrencyConverter currencyConverter,
            IAntiFraudCollector antiFraudCollector,
            ILimitOperationsApi limitOperationsApi,
            List<CashOperationLimitation> limits,
            int attemptRetainInMinutes,
            ISwiftTransferLimitationsRepository swiftTransferLimitationsRepository,
            ILimitSettingsRepository limitSettingsRepository,
            ICallTimeLimitsRepository callTimeLimitsRepository,
            OnDemandDataCache<Asset> assets,
            ILogFactory logFactory)
        {
            _cashOperationsCollector = cashOperationsCollector;
            _cashTransfersCollector = cashTransfersCollector;
            _currencyConverter = currencyConverter;
            _antiFraudCollector = antiFraudCollector;
            _limitOperationsApi = limitOperationsApi;
            _attemptRetainInMinutes = attemptRetainInMinutes > 0 ? attemptRetainInMinutes : 1;
            _swiftTransferLimitationsRepository = swiftTransferLimitationsRepository;
            _limitSettingsRepository = limitSettingsRepository;
            _callTimeLimitsRepository = callTimeLimitsRepository;
            _assets = assets;
            _log = logFactory.CreateLog(this);
            if (limits == null)
            {
                _limits = new List<CashOperationLimitation>(0);
            }
            else
            {
                _limits = new List<CashOperationLimitation>(limits.Count);
                foreach (var limit in limits)
                {
                    if (!limit.IsValid())
                    {
                        _log.Warning("Invalid limit in settings: " + limit.ToJson());
                        continue;
                    }
                    _limits.Add(limit);
                }
            }
        }

        private async Task<string> CheckSwiftWithdrawLimitations(string asset, decimal amount)
        {
            var limitation = await _swiftTransferLimitationsRepository.GetAsync(asset);

            if (amount < limitation?.MinimalWithdraw)
                return $"The withdrawal amount should be greater than {limitation.MinimalWithdraw}";

            return null;
        }

        public async Task<LimitationCheckResult> CheckCashOperationLimitAsync(
            string clientId,
            string assetId,
            double amount,
            CurrencyOperationType currencyOperationType)
        {
            var originalAsset = assetId;
            var originalAmount = amount;

            amount = Math.Abs(amount);

            if (currencyOperationType == CurrencyOperationType.CryptoCashOut)
            {
                var asset = _assets.Get(assetId);

                if (amount < asset.CashoutMinimalAmount)
                {
                    var minimalAmount = asset.CashoutMinimalAmount.GetFixedAsString(asset.Accuracy).TrimEnd('0');

                    return new LimitationCheckResult { IsValid = false, FailMessage = $"The minimum amount to cash out is {minimalAmount}" };
                }

                if (asset.LowVolumeAmount.HasValue && amount < asset.LowVolumeAmount)
                {
                    var settings = await _limitSettingsRepository.GetAsync();

                    var timeout = TimeSpan.FromMinutes(settings.LowCashOutTimeoutMins);
                    var callHistory = await _callTimeLimitsRepository.GetCallHistoryAsync("CashOutOperation", clientId, timeout);

                    var cashoutEnabled = !callHistory.Any() || callHistory.IsCallEnabled(timeout, settings.LowCashOutLimit);

                    if (!cashoutEnabled)
                        return new LimitationCheckResult { IsValid = false, FailMessage = "You have exceeded cash out operations limit. Please try again later." };
                }
            }

            if (currencyOperationType == CurrencyOperationType.SwiftTransferOut)
            {
                var error = await CheckSwiftWithdrawLimitations(assetId, (decimal)amount);

                if (error != null)
                {
                    return new LimitationCheckResult { IsValid = false, FailMessage = error };
                }
            }

            if (currencyOperationType != CurrencyOperationType.CryptoCashIn &&
                currencyOperationType != CurrencyOperationType.CryptoCashOut)
            {
                (assetId, amount) = await _currencyConverter.ConvertAsync(
                    assetId,
                    _currencyConverter.DefaultAsset,
                    amount);
            }

            var limitationTypes = LimitMapHelper.MapOperationType(currencyOperationType);
            var typeLimits = _limits.Where(l => limitationTypes.Contains(l.LimitationType)).ToList();
            if (!typeLimits.Any())
            {
                try
                {
                    await _limitOperationsApi.AddOperationAttemptAsync(
                    clientId,
                    originalAsset,
                    originalAmount,
                    currencyOperationType == CurrencyOperationType.CardCashIn
                        ? _cashOperationsTimeoutInMinutes
                        : _attemptRetainInMinutes,
                    currencyOperationType);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, context: new { Type = "Attempt", clientId, originalAmount, originalAsset });
                }
                return new LimitationCheckResult { IsValid = true };
            }

            //To handle parallel request
            await _lock.WaitAsync();
            try
            {
                var assetLimits = typeLimits.Where(l => l.Asset == assetId).ToList();
                string error = await DoPeriodCheckAsync(
                    assetLimits,
                    LimitationPeriod.Month,
                    clientId,
                    assetId,
                    amount,
                    currencyOperationType,
                    false);
                if (error != null)
                    return new LimitationCheckResult { IsValid = false, FailMessage = error };

                error = await DoPeriodCheckAsync(
                    assetLimits,
                    LimitationPeriod.Day,
                    clientId,
                    assetId,
                    amount,
                    currencyOperationType,
                    false);
                if (error != null)
                    return new LimitationCheckResult { IsValid = false, FailMessage = error };

                if (currencyOperationType == CurrencyOperationType.CryptoCashOut)
                {
                    assetLimits = typeLimits.Where(l => l.Asset == _currencyConverter.DefaultAsset).ToList();

                    var (assetTo, convertedAmount) = await _currencyConverter.ConvertAsync(
                        assetId,
                        _currencyConverter.DefaultAsset,
                        amount,
                        true);

                    error = await DoPeriodCheckAsync(
                        assetLimits,
                        LimitationPeriod.Month,
                        clientId,
                        assetTo,
                        convertedAmount,
                        currencyOperationType,
                        true);
                    if (error != null)
                        return new LimitationCheckResult { IsValid = false, FailMessage = error };

                    error = await DoPeriodCheckAsync(
                        assetLimits,
                        LimitationPeriod.Day,
                        clientId,
                        assetTo,
                        convertedAmount,
                        currencyOperationType,
                        true);
                    if (error != null)
                        return new LimitationCheckResult { IsValid = false, FailMessage = error };
                }

                try
                {
                    await _limitOperationsApi.AddOperationAttemptAsync(
                    clientId,
                    originalAsset,
                    originalAmount,
                    currencyOperationType == CurrencyOperationType.CardCashIn
                        ? _cashOperationsTimeoutInMinutes
                        : _attemptRetainInMinutes,
                    currencyOperationType);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, context: new { Type = "Attempt", clientId, originalAmount, originalAsset });
                }
            }
            finally
            {
                _lock.Release();
            }

            return new LimitationCheckResult { IsValid = true };
        }

        public async Task<ClientData> GetClientDataAsync(string clientId, LimitationPeriod period)
        {
            var result = new ClientData
            {
                CashOperations = await _cashOperationsCollector.GetClientDataAsync(clientId, period),
                CashTransferOperations = (await _cashTransfersCollector.GetClientDataAsync(clientId, period))
                    .Select(i => new CashOperation
                    {
                        Id = i.Id,
                        ClientId = i.ClientId,
                        Asset = i.Asset,
                        Volume = i.Volume,
                        DateTime = i.DateTime,
                        OperationType = i.OperationType,
                    })
                    .ToList(),
                OperationAttempts = await _antiFraudCollector.GetClientDataAsync(clientId),
            };
            await AddRemainingLimitsAsync(clientId, period, result);
            return result;
        }

        public async Task RemoveClientOperationAsync(string clientId, string operationId)
        {
            try
            {
                await _limitOperationsApi.RemoveOperationAsync(clientId, operationId);
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: new { Type = "Remove", clientId, operationId });
            }
        }

        private async Task AddRemainingLimitsAsync(string clientId, LimitationPeriod period, ClientData clientData)
        {
            var result = new List<RemainingLimitation>();
            var periodLimits = _limits.Where(l => l.Period == period);
            foreach (var periodLimit in periodLimits)
            {
                if (!string.IsNullOrWhiteSpace(periodLimit.ClientId) && clientId != periodLimit.ClientId)
                    continue;

                if (periodLimit.LimitationType == LimitationType.CardCashIn)
                {
                    result.Add(
                        await CalculateRemainingAsync(
                            clientData.CashOperations,
                            clientData.OperationAttempts,
                            new List<CurrencyOperationType> { CurrencyOperationType.CardCashIn },
                            periodLimit));
                }
                else if (periodLimit.LimitationType == LimitationType.CryptoCashOut)
                {
                    result.Add(
                        await CalculateRemainingAsync(
                            clientData.CashOperations,
                            clientData.OperationAttempts,
                            new List<CurrencyOperationType> { CurrencyOperationType.CryptoCashOut },
                            periodLimit));
                }
                else if (periodLimit.LimitationType == LimitationType.CardAndSwiftCashIn)
                {
                    result.Add(
                        await CalculateRemainingAsync(
                            clientData.CashOperations.Concat(clientData.CashTransferOperations),
                            clientData.OperationAttempts,
                            new List<CurrencyOperationType> { CurrencyOperationType.CardCashIn, CurrencyOperationType.SwiftTransfer },
                            periodLimit));
                }
            }
            clientData.RemainingLimits = result;
        }

        private async Task<RemainingLimitation> CalculateRemainingAsync(
            IEnumerable<CashOperation> operations,
            IEnumerable<CurrencyOperationAttempt> attempts,
            List<CurrencyOperationType> operationTypes,
            CashOperationLimitation limitation)
        {
            bool checkForAllCryptoCashouts = limitation.Asset == _currencyConverter.DefaultAsset
                && operationTypes.Contains(CurrencyOperationType.CryptoCashOut);
            double sum = 0;
            foreach (var assetCashin in operations.Where(c => operationTypes.Contains(c.OperationType.Value)))
            {
                if (limitation.Asset == _currencyConverter.DefaultAsset)
                {
                    var converted = await _currencyConverter.ConvertAsync(
                        assetCashin.Asset,
                        _currencyConverter.DefaultAsset,
                        assetCashin.Volume,
                        checkForAllCryptoCashouts && _currencyConverter.IsNotConvertible(assetCashin.Asset));
                    if (converted.Item1 == limitation.Asset)
                        sum += Math.Abs(converted.Item2);
                }
                else if (limitation.Asset == assetCashin.Asset)
                {
                    sum += Math.Abs(assetCashin.Volume);
                }
            }
            foreach (var attempt in attempts.Where(a => operationTypes.Contains(a.OperationType)))
            {
                if (limitation.Asset == _currencyConverter.DefaultAsset)
                {
                    var converted = await _currencyConverter.ConvertAsync(
                        attempt.Asset,
                        _currencyConverter.DefaultAsset,
                        attempt.Amount,
                        checkForAllCryptoCashouts && _currencyConverter.IsNotConvertible(attempt.Asset));
                    if (converted.Item1 == limitation.Asset)
                        sum += Math.Abs(converted.Item2);
                }
                else if (limitation.Asset == attempt.Asset)
                {
                    sum += Math.Abs(attempt.Amount);
                }
            }
            return new RemainingLimitation
            {
                Asset = limitation.Asset,
                LimitationType = limitation.LimitationType,
                RemainingAmount = Math.Max(0, limitation.Limit - sum),
            };
        }

        private async Task<string> DoPeriodCheckAsync(
            IEnumerable<CashOperationLimitation> limits,
            LimitationPeriod period,
            string clientId,
            string asset,
            double amount,
            CurrencyOperationType currencyOperationType,
            bool checkAllCrypto)
        {
            var periodLimits = limits.Where(l => l.Period == period).ToList();
            if (!periodLimits.Any())
                return null;

            (double cashPeriodValue, bool cashOperationsNotCached) = await _cashOperationsCollector.GetCurrentAmountAsync(
                clientId,
                asset,
                period,
                currencyOperationType,
                checkAllCrypto);
            (double transferPeriodValue, bool cashTransfersNotCached) = await _cashTransfersCollector.GetCurrentAmountAsync(
                clientId,
                asset,
                period,
                currencyOperationType);
            if (cashOperationsNotCached || cashTransfersNotCached)
            {
                try
                {
                    await _limitOperationsApi.CacheClientDataAsync(clientId, currencyOperationType);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, context: new { Type = "CachOp", clientId, currencyOperationType });
                }
            }

            var clientLimit = periodLimits.FirstOrDefault(l => l.ClientId == clientId);
            if (clientLimit != null)
            {
                string checkMessage = await CheckLimitAsync(
                     cashPeriodValue,
                     transferPeriodValue,
                     clientLimit,
                     period,
                     clientId,
                     asset,
                     amount);
                if (!string.IsNullOrWhiteSpace(checkMessage))
                    return checkMessage;
            }
            else
            {
                foreach (var periodLimit in periodLimits)
                {
                    if (periodLimit.ClientId != null)
                        continue;

                    string checkMessage = await CheckLimitAsync(
                        cashPeriodValue,
                        transferPeriodValue,
                        periodLimit,
                        period,
                        clientId,
                        asset,
                        amount);
                    if (!string.IsNullOrWhiteSpace(checkMessage))
                        return checkMessage;
                }
            }

            return null;
        }

        private async Task<string> CheckLimitAsync(
            double cashPeriodValue,
            double transferPeriodValue,
            CashOperationLimitation limit,
            LimitationPeriod period,
            string clientId,
            string asset,
            double amount)
        {
            double currentValue = cashPeriodValue;
            if (limit.LimitationType == LimitationType.CardAndSwiftCashIn)
                currentValue += transferPeriodValue;
            double limitValue = (await _currencyConverter.ConvertAsync(limit.Asset, _currencyConverter.DefaultAsset, limit.Limit)).Item2;
            if (limitValue < currentValue + amount)
                return GetPeriodLimitFailMessage(period);
            double antiFraudValue = await _antiFraudCollector.GetAttemptsValueAsync(
                clientId,
                asset,
                limit.LimitationType);
            if (limitValue < currentValue + amount + antiFraudValue)
            {
                var forbidDuration = limit.LimitationType == LimitationType.CardCashIn ? _cashOperationsTimeoutInMinutes : _attemptRetainInMinutes;
                return $"Please wait {forbidDuration} minute(s) after previous payment attempt";
            }
            return null;
        }

        private string GetPeriodLimitFailMessage(LimitationPeriod period)
        {
            switch (period)
            {
                case LimitationPeriod.Day:
                    return "Operation is not allowed because of Daily limitation.";
                case LimitationPeriod.Month:
                    return "Operation is not allowed because of Monthly limitation.";
                default:
                    throw new NotSupportedException($"Limitation period {period} is not supported");
            }
        }
    }
}
