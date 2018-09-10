using Common;
using Common.Log;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.JobClient;
using Lykke.Service.Limitations.Core.Repositories;
using Lykke.Service.Limitations.Core.Services;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Common.Cache;
using Lykke.Common.Log;

namespace Lykke.Service.Limitations.Services
{
    public class LimitationChecker : ILimitationCheck
    {
        //private const string _swiftLifetimeDepositLimitError = "Operation is not allowed because of Swift Lifetime deposits limitation.";
        //private const string _swiftLifetimeWithdrawalsLimitError = "Operation is not allowed because of Swift Lifetime withdrawals limitation.";
        //private const string _creditCardLifetimeDepositLimitError = "Operation is not allowed because of Credt cards Lifetime deposits limitation.";
        //private const string _cryptoLifetimeWithdrawalsLimitError = "Operation is not allowed because of Crypto currency Lifetime withdrawals limitation.";
        //private const string _totalLifetimeDepositLimitError = "Operation is not allowed because of Total Lifetime deposits limitation.";
        //private const string _totalLifetimeWithdrawalsLimitError = "Operation is not allowed because of Total Lifetime withdrawals limitation.";

        private const string _swiftLifetimeDepositLimitError = "SwiftLifetimeDepositsErrMsg";
        private const string _swiftLifetimeWithdrawalsLimitError = "SwiftLifetimeWithdrawalsErrMsg";
        private const string _creditCardLifetimeDepositLimitError = "CredtCardsLifetimeDepositsErrMsg";
        private const string _cryptoLifetimeWithdrawalsLimitError = "CryptoCurrencyLifetimeWithdrawalsErrMsg";
        private const string _totalLifetimeDepositLimitError = "TotalLifetimeDepositsErrMsg";
        private const string _totalLifetimeWithdrawalsLimitError = "TotalLifetimeWithdrawalsErrMsg";

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
        private readonly IAccumulatedDepositRepository _accumulatedDepositRepository;
        private readonly List<string> _convertibleCurrencies;
        private readonly ITierRepository _tierRepository;
        private readonly IClientTierRepository _clientTierRepository;
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
            List<string> convertibleCurrencies,
            int attemptRetainInMinutes,
            ISwiftTransferLimitationsRepository swiftTransferLimitationsRepository,
            IAccumulatedDepositRepository accumulatedDepositRepository,
            ITierRepository tierRepository,
            IClientTierRepository clientTierRepository,
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
            _accumulatedDepositRepository = accumulatedDepositRepository;
            _tierRepository = tierRepository;
            _clientTierRepository = clientTierRepository;
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
                        _log.WriteWarning(nameof(LimitationChecker), "C-tor", "Invalid limit in settings: " + limit.ToJson());
                        continue;
                    }
                    _limits.Add(limit);
                }
            }
            _convertibleCurrencies = convertibleCurrencies;
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

            var limitationTypes = LimitMapHelper.MapOperationType(currencyOperationType);
            List<CashOperationLimitation> typeLimits = _limits.Where(l => limitationTypes.Contains(l.LimitationType)).ToList();
            List<CashOperationLimitation> totalLimits = new List<CashOperationLimitation>();

            /*
            if (currencyOperationType != CurrencyOperationType.CryptoCashIn && currencyOperationType != CurrencyOperationType.CryptoCashOut)
            {
                (assetId, amount) = await _currencyConverter.ConvertAsync(
                    assetId,
                    _currencyConverter.DefaultAsset,
                    amount,
                    forceConvesion: true
                    );
            }
            */

            ITier clientTier = await GetClientTierAsync(clientId, originalAsset);
            if (clientTier != null)  // check all time deposit limits
            {
                LimitationCheckResult alltimeLimitCheckResult = await CheckAllTimeLimits(clientId, originalAsset, originalAmount, currencyOperationType, clientTier);
                if (alltimeLimitCheckResult != null)
                {
                    return alltimeLimitCheckResult;
                }

                // replace limits with new ones from tier
                typeLimits = CreateLimitsFromTier(clientTier, typeLimits, clientId, assetId, _currencyConverter.DefaultAsset, currencyOperationType);
            }


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
                string error = await DoPeriodCheckAsync(
                    typeLimits,
                    clientId,
                    assetId,
                    amount,
                    currencyOperationType,
                    false);
                if (error != null)
                    return new LimitationCheckResult { IsValid = false, FailMessage = error };

                /*
                // check for maximum values in asset units or USD for convertible assets
                var assetLimits = typeLimits.Where(l => l.Asset == assetId);
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

                // check for maximum values in USD for nonn-convertible (crypto) assets
                if (currencyOperationType == CurrencyOperationType.CryptoCashOut)
                {
                    assetLimits = typeLimits.Where(l => l.Asset == _currencyConverter.DefaultAsset);

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
                */

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

        private async Task<LimitationCheckResult> CheckAllTimeLimits(string clientId, string asset, double amount, CurrencyOperationType currencyOperationType, ITier clientTier)
        {
            var amountInUsd = amount;
            if (asset != _currencyConverter.DefaultAsset) // convert to USD
            {
                amountInUsd = (await _currencyConverter.ConvertAsync(asset, _currencyConverter.DefaultAsset, amount, forceConvesion: true)).convertedAmount;
            }

            switch (currencyOperationType)
            {
                case CurrencyOperationType.CardCashIn:
                case CurrencyOperationType.SwiftTransfer:
                    var accumulatedSwiftDepositsTask = _accumulatedDepositRepository.GetAccumulatedAmountAsync(clientId, CurrencyOperationType.SwiftTransfer);
                    var accumulatedCardsDepositsTask = _accumulatedDepositRepository.GetAccumulatedAmountAsync(clientId, CurrencyOperationType.CardCashIn);
                    await Task.WhenAll(new Task[] { accumulatedSwiftDepositsTask, accumulatedCardsDepositsTask });
                    var accumulatedSwiftDeposits = Math.Abs(accumulatedSwiftDepositsTask.Result);
                    var accumulatedCardDeposits = Math.Abs(accumulatedCardsDepositsTask.Result);

                    switch (currencyOperationType)
                    {
                        case CurrencyOperationType.CardCashIn:
                            if (clientTier.LimitCreditCardsCashInAllTime > 0 && clientTier.LimitCreditCardsCashInAllTime < accumulatedCardDeposits + amountInUsd)
                            {
                                return new LimitationCheckResult { IsValid = false, FailMessage = _creditCardLifetimeDepositLimitError };
                            }
                            break;
                        case CurrencyOperationType.SwiftTransfer:
                            if (clientTier.LimitSwiftCashInAllTime > 0 && clientTier.LimitSwiftCashInAllTime < accumulatedSwiftDeposits + amountInUsd)
                            {
                                return new LimitationCheckResult { IsValid = false, FailMessage = _swiftLifetimeDepositLimitError };
                            }
                            break;
                    }

                    // check Alltime CashIn limit
                    if (clientTier.LimitTotalCashInAllTime > 0)
                    {
                        if (clientTier.LimitTotalCashInAllTime < accumulatedCardDeposits + accumulatedSwiftDeposits + amountInUsd)
                        {
                            return new LimitationCheckResult { IsValid = false, FailMessage = _totalLifetimeDepositLimitError };
                        }
                    }
                    break;

                case CurrencyOperationType.CryptoCashOut:
                case CurrencyOperationType.SwiftTransferOut:
                    var accumulatedSwiftWithdrawalsTask = _accumulatedDepositRepository.GetAccumulatedAmountAsync(clientId, CurrencyOperationType.SwiftTransferOut);
                    var accumulatedCryptoWithdrawalsTask = _accumulatedDepositRepository.GetAccumulatedAmountAsync(clientId, CurrencyOperationType.CryptoCashOut);
                    await Task.WhenAll(new Task[] { accumulatedSwiftWithdrawalsTask, accumulatedCryptoWithdrawalsTask });
                    var accumulatedSwiftWithdrawals = Math.Abs(accumulatedSwiftWithdrawalsTask.Result);
                    var accumulatedCryptoWithdrawals = Math.Abs(accumulatedCryptoWithdrawalsTask.Result);

                    switch (currencyOperationType)
                    {
                        case CurrencyOperationType.CryptoCashOut:
                            if (clientTier.LimitCryptoCashOutAllTime > 0 && clientTier.LimitCryptoCashOutAllTime < accumulatedCryptoWithdrawals + amountInUsd)
                            {
                                return new LimitationCheckResult { IsValid = false, FailMessage = _cryptoLifetimeWithdrawalsLimitError };
                            }
                            break;
                        case CurrencyOperationType.SwiftTransferOut:
                            if (clientTier.LimitSwiftCashOutAllTime > 0 && clientTier.LimitSwiftCashOutAllTime < accumulatedSwiftWithdrawals + amountInUsd)
                            {
                                return new LimitationCheckResult { IsValid = false, FailMessage = _swiftLifetimeWithdrawalsLimitError };
                            }
                            break;
                    }

                    // check Alltime CashIn limit
                    if (clientTier.LimitTotalCashOutAllTime > 0)
                    {
                        if (clientTier.LimitTotalCashOutAllTime < accumulatedCryptoWithdrawals + accumulatedSwiftWithdrawals + amountInUsd)
                        {
                            return new LimitationCheckResult { IsValid = false, FailMessage = _totalLifetimeWithdrawalsLimitError };
                        }
                    }
                    break;
            }
            return null;
        }

        private async Task<ITier> GetClientTierAsync(string clientId, string asset)
        {
            var clientTierId = await _clientTierRepository.GetClientTierIdAsync(clientId);
            if (clientTierId != null)
            {
                return await _tierRepository.LoadTierAsync(clientTierId);
            }
            return null;
        }

        private List<CashOperationLimitation> CreateLimitsFromTier(ITier clientTier, List<CashOperationLimitation> typeLimits, string clientId, string originalAsset, string asset, CurrencyOperationType currencyOperationType)
        {
            if (clientTier != null)
            {
                switch (currencyOperationType)
                {
                    case CurrencyOperationType.CardCashIn:
                        typeLimits = typeLimits.Where(limit => limit.Asset == originalAsset && limit.LimitationType == LimitationType.CardCashIn).ToList();

                        CreateLimit(typeLimits, clientId, asset, clientTier.LimitCreditCardsCashIn24Hours, LimitationType.CardCashIn, LimitationPeriod.Day);
                        CreateLimit(typeLimits, clientId, asset, clientTier.LimitCreditCardsCashIn30Days, LimitationType.CardCashIn, LimitationPeriod.Month);
                        CreateLimit(typeLimits, clientId, asset, clientTier.LimitTotalCashIn24Hours, LimitationType.OverallCashIn, LimitationPeriod.Day);
                        CreateLimit(typeLimits, clientId, asset, clientTier.LimitTotalCashIn30Days, LimitationType.OverallCashIn, LimitationPeriod.Month);
                        break;
                    case CurrencyOperationType.CryptoCashOut:
                        typeLimits = typeLimits.Where(limit => limit.Asset == originalAsset && limit.LimitationType == LimitationType.CryptoCashOut).ToList();

                        CreateLimit(typeLimits, clientId, asset, clientTier.LimitCryptoCashOut24Hours, LimitationType.CryptoCashOut, LimitationPeriod.Day);
                        CreateLimit(typeLimits, clientId, asset, clientTier.LimitCryptoCashOut30Days, LimitationType.CryptoCashOut, LimitationPeriod.Month);
                        CreateLimit(typeLimits, clientId, asset, clientTier.LimitTotalCashOut24Hours, LimitationType.OverallCashOut, LimitationPeriod.Day);
                        CreateLimit(typeLimits, clientId, asset, clientTier.LimitTotalCashOut30Days, LimitationType.OverallCashOut, LimitationPeriod.Month);
                        break;
                    case CurrencyOperationType.SwiftTransfer:
                        typeLimits = typeLimits.Where(limit => limit.Asset == originalAsset && limit.LimitationType == LimitationType.SwiftCashIn).ToList();

                        CreateLimit(typeLimits, clientId, asset, clientTier.LimitSwiftCashIn24Hours, LimitationType.SwiftCashIn, LimitationPeriod.Day);
                        CreateLimit(typeLimits, clientId, asset, clientTier.LimitSwiftCashIn30Days, LimitationType.SwiftCashIn, LimitationPeriod.Month);
                        CreateLimit(typeLimits, clientId, asset, clientTier.LimitTotalCashIn24Hours, LimitationType.OverallCashIn, LimitationPeriod.Day);
                        CreateLimit(typeLimits, clientId, asset, clientTier.LimitTotalCashIn30Days, LimitationType.OverallCashIn, LimitationPeriod.Month);
                        break;
                    case CurrencyOperationType.SwiftTransferOut:
                        typeLimits = typeLimits.Where(limit => limit.Asset == originalAsset && limit.LimitationType == LimitationType.SwiftCashOut).ToList();

                        CreateLimit(typeLimits, clientId, asset, clientTier.LimitSwiftCashOut24Hours, LimitationType.SwiftCashOut, LimitationPeriod.Day);
                        CreateLimit(typeLimits, clientId, asset, clientTier.LimitSwiftCashOut30Days, LimitationType.SwiftCashOut, LimitationPeriod.Month);
                        CreateLimit(typeLimits, clientId, asset, clientTier.LimitTotalCashOut24Hours, LimitationType.OverallCashOut, LimitationPeriod.Day);
                        CreateLimit(typeLimits, clientId, asset, clientTier.LimitTotalCashOut30Days, LimitationType.OverallCashOut, LimitationPeriod.Month);
                        break;
                    default:
                        throw new NotSupportedException($"Currency operation type period {currencyOperationType} is not supported");
                }

            }

            return typeLimits;
        }

        private List<CashOperationLimitation> CreateLimitsFromTier(ITier clientTier, string clientId, string asset, LimitationPeriod period)
        {
            var result = new List<CashOperationLimitation>();

            if (clientTier != null)
            {
                switch (period)
                {
                    case LimitationPeriod.Day:
                        CreateLimit(result, clientId, asset, clientTier.LimitCreditCardsCashIn24Hours, LimitationType.CardCashIn, LimitationPeriod.Day);

                        long cardAndSwiftCashIn24HoursLimit = clientTier.LimitCreditCardsCashIn30Days + clientTier.LimitSwiftCashIn30Days;
                        if (cardAndSwiftCashIn24HoursLimit > 0 && clientTier.LimitTotalCashIn30Days > 0 && cardAndSwiftCashIn24HoursLimit > clientTier.LimitTotalCashIn30Days)
                        {
                            cardAndSwiftCashIn24HoursLimit = clientTier.LimitTotalCashIn30Days;
                        }
                        CreateLimit(result, clientId, asset, cardAndSwiftCashIn24HoursLimit, LimitationType.OverallCashIn, LimitationPeriod.Day);
                        break;
                    case LimitationPeriod.Month:
                        CreateLimit(result, clientId, asset, clientTier.LimitCreditCardsCashIn30Days, LimitationType.CardCashIn, LimitationPeriod.Month);

                        long cardAndSwiftCashIn30DaysLimit = clientTier.LimitCreditCardsCashIn30Days + clientTier.LimitSwiftCashIn30Days;
                        if (cardAndSwiftCashIn30DaysLimit > 0 && clientTier.LimitTotalCashIn30Days > 0 && cardAndSwiftCashIn30DaysLimit > clientTier.LimitTotalCashIn30Days)
                        {
                            cardAndSwiftCashIn30DaysLimit = clientTier.LimitTotalCashIn30Days;
                        }
                        CreateLimit(result, clientId, asset, cardAndSwiftCashIn30DaysLimit, LimitationType.OverallCashIn, LimitationPeriod.Month);
                        break;
                }
            }

            return result;
        }

        private static void CreateLimit(List<CashOperationLimitation> typeLimits, string clientId, string asset, long value, LimitationType limitationType, LimitationPeriod limitationPeriod)
        {
            if (value > 0)
            {
                CashOperationLimitation l = new CashOperationLimitation()
                {
                    ClientId = clientId,
                    Asset = asset,
                    Limit = value,
                    LimitationType = limitationType,
                    Period = limitationPeriod
                };
                typeLimits.Add(l);
            }
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

            ITier clientTier = await GetClientTierAsync(clientId, _currencyConverter.DefaultAsset);
            if (clientTier != null)  // check all time deposit limits
            {
                // remove old limits
                periodLimits = periodLimits.Where(l => l.LimitationType != LimitationType.CardCashIn && l.LimitationType != LimitationType.OverallCashIn);

                // replace with new limits from tiers
                var additionalPeriodLimits = CreateLimitsFromTier(clientTier, clientId, _currencyConverter.DefaultAsset, period);
                periodLimits = periodLimits.Union(additionalPeriodLimits);
            }

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
                else if (periodLimit.LimitationType == LimitationType.OverallCashIn)
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
                LimitAmount = limitation.Limit
            };
        }

        private async Task<string> DoPeriodCheckAsync(
            IEnumerable<CashOperationLimitation> limits,
            string clientId,
            string asset,
            double amount,
            CurrencyOperationType currencyOperationType,
            bool checkAllCrypto)
        {
            foreach (var limit in limits)
            {
                CurrencyOperationType[] operationTypes;
                switch (limit.LimitationType)
                {
                    case LimitationType.OverallCashIn:
                        operationTypes = new CurrencyOperationType[] {
                            CurrencyOperationType.CardCashIn,
                            CurrencyOperationType.SwiftTransfer
                        };
                        break;
                    case LimitationType.OverallCashOut:
                        operationTypes = new CurrencyOperationType[] {
                            CurrencyOperationType.CryptoCashOut,
                            CurrencyOperationType.SwiftTransferOut
                        };
                        break;
                    default:
                        operationTypes = new CurrencyOperationType[] {
                            currencyOperationType
                        };
                        break;
                }

                double sumCashPeriodValue = 0;
                double sumTransferPeriodValue = 0;

                foreach (CurrencyOperationType operationType in operationTypes)
                {

                    (double cashPeriodValue, bool cashOperationsNotCached) = await _cashOperationsCollector.GetCurrentAmountAsync(
                        clientId,
                        asset,
                        limit,
                        operationType,
                        checkAllCrypto);
                    sumCashPeriodValue += cashPeriodValue;

                    (double transferPeriodValue, bool cashTransfersNotCached) = await _cashTransfersCollector.GetCurrentAmountAsync(
                        clientId,
                        asset,
                        limit,
                        operationType);
                    sumTransferPeriodValue += transferPeriodValue;

                    if (cashOperationsNotCached || cashTransfersNotCached)
                    {
                        try
                        {
                            await _limitOperationsApi.CacheClientDataAsync(clientId, operationType);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, context: new { Type = "CachOp", clientId, currencyOperationType });
                        }
                    }
                }

                var periodValue = sumCashPeriodValue + sumTransferPeriodValue;

                string checkMessage = await CheckLimitAsync(
                    periodValue,
                    limit,
                    clientId,
                    asset,
                    amount);
                if (!string.IsNullOrWhiteSpace(checkMessage))
                    return checkMessage;
            }

            return null;
        }

        private async Task<string> CheckLimitAsync(
            double periodValue,
            CashOperationLimitation limit,
            string clientId,
            string asset,
            double amount)
        {
            double convertedToLimitAssetAmount = limit.Asset == asset ? amount : (await _currencyConverter.ConvertAsync(asset, limit.Asset, amount, true)).convertedAmount;

            if (limit.Limit < periodValue + convertedToLimitAssetAmount)
                return GetPeriodLimitFailMessage(limit.Period);

            double antiFraudValue = await _antiFraudCollector.GetAttemptsValueAsync(
                clientId,
                asset,
                limit.LimitationType);

            if (limit.Limit < periodValue + convertedToLimitAssetAmount + antiFraudValue)
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

        public async Task<AccumulatedAmountsModel> GetAccumulatedDepositsAsync(string clientId)
        {
            var cachedRates = new Dictionary<string, double>();

            AccumulatedAmountsModel result = new AccumulatedAmountsModel();

            var accumulatedSwiftDepositTask = _accumulatedDepositRepository.GetAccumulatedAmountAsync(clientId, CurrencyOperationType.SwiftTransfer);
            var accumulatedCardDepositTask = _accumulatedDepositRepository.GetAccumulatedAmountAsync(clientId, CurrencyOperationType.CardCashIn);
            var accumulatedSwiftWithdrawalTask = _accumulatedDepositRepository.GetAccumulatedAmountAsync(clientId, CurrencyOperationType.SwiftTransferOut);
            //var accumulatedCryptoWithdrawalTask = _accumulatedDepositRepository.GetAccumulatedAmountAsync(clientId, CurrencyOperationType.CryptoCashOut);

            await Task.WhenAll(new Task[] {
                accumulatedSwiftDepositTask,
                accumulatedSwiftDepositTask,
                accumulatedSwiftWithdrawalTask,
                //accumulatedCryptoWithdrawalTask
            });

            result.DepositTotalFiat = Math.Round(accumulatedSwiftDepositTask.Result + accumulatedCardDepositTask.Result, 15);
            result.WithdrawalTotalFiat = accumulatedSwiftWithdrawalTask.Result;
            //result.WithdrawalTotalNonFiat = accumulatedCryptoWithdrawalTask.Result;

            var dayOperations = (await LoadOperationsAsync(clientId, LimitationPeriod.Day));
            foreach (var op in dayOperations)
            {
                await SetRateToUsd(cachedRates, op);

                switch (op.OperationType)
                {
                    case CurrencyOperationType.CardCashIn:
                        result.Deposit1DayFiat += Math.Abs(op.Volume * op.RateToUsd);
                        result.Deposit1DayCards += Math.Abs(op.Volume * op.RateToUsd);
                        break;
                    case CurrencyOperationType.SwiftTransfer:
                        result.Deposit1DayFiat += Math.Abs(op.Volume * op.RateToUsd);
                        result.Deposit1DaySwift += Math.Abs(op.Volume * op.RateToUsd);
                        break;
                    case CurrencyOperationType.SwiftTransferOut:
                        result.Withdrawal1DayFiat += Math.Abs(op.Volume * op.RateToUsd);
                        result.Withdrawal1DaySwift += Math.Abs(op.Volume * op.RateToUsd);
                        break;
                    case CurrencyOperationType.CryptoCashOut:
                        result.Withdrawal1DayNonFiat += Math.Abs(op.Volume * op.RateToUsd);
                        break;
                }
            }

            var monthOperations = (await LoadOperationsAsync(clientId, LimitationPeriod.Month));
            foreach (var op in monthOperations)
            {
                await SetRateToUsd(cachedRates, op);

                switch (op.OperationType)
                {
                    case CurrencyOperationType.CardCashIn:
                        result.Deposit30DaysFiat += Math.Abs(op.Volume * op.RateToUsd);
                        result.Deposit30DaysCards += Math.Abs(op.Volume * op.RateToUsd);
                        break;
                    case CurrencyOperationType.SwiftTransfer:
                        result.Deposit30DaysFiat += Math.Abs(op.Volume * op.RateToUsd);
                        result.Deposit30DaysSwift += Math.Abs(op.Volume * op.RateToUsd);
                        break;
                    case CurrencyOperationType.SwiftTransferOut:
                        result.Withdrawal30DaysFiat += Math.Abs(op.Volume * op.RateToUsd);
                        result.Withdrawal30DaysSwift += Math.Abs(op.Volume * op.RateToUsd);
                        break;
                    case CurrencyOperationType.CryptoCashOut:
                        result.Withdrawal30DaysNonFiat += Math.Abs(op.Volume * op.RateToUsd);
                        break;
                }
            }

            return result;
        }

        private async Task SetRateToUsd(Dictionary<string, double> cachedRates, ICashOperation op)
        {
            if (op.RateToUsd == 0)
            {
                if (op.Asset != _currencyConverter.DefaultAsset)
                {
                    string asset;
                    double rate = 1;
                    if (!cachedRates.TryGetValue(op.Asset, out rate))
                    {
                        (asset, rate) = await _currencyConverter.ConvertAsync(op.Asset, _currencyConverter.DefaultAsset, 1, forceConvesion: true);
                        cachedRates[op.Asset] = rate;
                    }
                    op.RateToUsd = rate;
                }
                else
                {
                    op.RateToUsd = 1;
                }
            }
        }

        private async Task<IEnumerable<CashOperation>> LoadOperationsAsync(string clientId, LimitationPeriod period)
        {
            return (await _cashOperationsCollector.GetClientDataAsync(clientId, period)).Union(
                (await _cashTransfersCollector.GetClientDataAsync(clientId, period))
                    .Select(i => new CashOperation
                    {
                        Id = i.Id,
                        ClientId = i.ClientId,
                        Asset = i.Asset,
                        Volume = i.Volume,
                        DateTime = i.DateTime,
                        OperationType = i.OperationType,
                    })
                    .ToList());
        }
    }
}
