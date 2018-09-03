using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Lykke.Service.Limitations.Client.AutorestClient;
using Lykke.Service.Limitations.Client.Models;

namespace Lykke.Service.Limitations.Client
{
    public class LimitationsServiceClient : ILimitationsServiceClient, IDisposable
    {
        private readonly LykkelimitationsService _service;

        public LimitationsServiceClient(string serviceUrl)
        {
            if (string.IsNullOrWhiteSpace(serviceUrl))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(serviceUrl));

            _service = new LykkelimitationsService(new Uri(serviceUrl), new HttpClient());
        }

        public void Dispose()
        {
            _service.Dispose();
        }

        public async Task<AutorestClient.Models.IsAliveResponse> IsAlive()
        {
            return await _service.IsAliveAsync();
        }

        public async Task<LimitationCheckResponse> CheckAsync(
            string clientId,
            string asset,
            double amount,
            CurrencyOperationType operationType)
        {
            var request = new AutorestClient.Models.LimitCheckRequestModel
            {
                ClientId = clientId,
                Asset = asset,
                Amount = amount,
                OperationType = (AutorestClient.Models.CurrencyOperationType)operationType,
            };
            var result = await _service.ApiLimitationsPostAsync(request);
            return new LimitationCheckResponse
            {
                IsValid = result.IsValid,
                FailMessage = result.FailMessage,
            };
        }

        public async Task<ClientDataResponse> GetClientDataAsync(string clientId, LimitationPeriod period)
        {
            var result = await _service.ApiLimitationsGetClientDataPostAsync((AutorestClient.Models.LimitationPeriod)period, clientId);
            return new ClientDataResponse
            {
                RemainingLimits = result.RemainingLimits.Select(i => RemainingLimitation.FromModel(i)).ToList(),
                CashOperations = result.CashOperations.Select(i => CashOperation.FromModel(i)).ToList(),
                CashTransferOperations = result.CashTransferOperations.Select(i => CashOperation.FromModel(i)).ToList(),
                OperationAttempts = result.OperationAttempts.Select(i => CurrencyOperationAttempt.FromModel(i)).ToList(),
            };
        }

        public async Task RemoveClientOperationAsync(string clientId, string operationId)
        {
            await _service.ApiLimitationsRemoveClientOperationDeleteAsync(clientId, operationId);
        }

        public async Task<AccumulatedAmountsResponse> GetAccumulatedDepositsAsync(string clientId)
        {
            var accumulatedDepositsModel = await _service.ApiLimitationsGetAccumulatedDepositsPostAsync(clientId);
            return new AccumulatedAmountsResponse
            {
                Deposit1DayFiat = accumulatedDepositsModel.Deposit1DayFiat,
                Deposit1DayNonFiat = accumulatedDepositsModel.Deposit1DayNonFiat,
                Deposit30DaysFiat = accumulatedDepositsModel.Deposit30DaysFiat,
                Deposit30DaysNonFiat = accumulatedDepositsModel.Deposit30DaysNonFiat,
                DepositTotalFiat = accumulatedDepositsModel.DepositTotalFiat,
                DepositTotalNonFiat = accumulatedDepositsModel.DepositTotalNonFiat,

                Withdrawal1DayFiat = accumulatedDepositsModel.Withdrawal1DayFiat,
                Withdrawal1DayNonFiat = accumulatedDepositsModel.Withdrawal1DayNonFiat,
                Withdrawal30DaysFiat = accumulatedDepositsModel.Withdrawal30DaysFiat,
                Withdrawal30DaysNonFiat = accumulatedDepositsModel.Withdrawal30DaysNonFiat,
                WithdrawalTotalFiat = accumulatedDepositsModel.WithdrawalTotalFiat,
                WithdrawalTotalNonFiat = accumulatedDepositsModel.WithdrawalTotalNonFiat
            };
        }
    }
}
