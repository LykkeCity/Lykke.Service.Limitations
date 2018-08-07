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
            return await _service.ApiIsAliveGetAsync();
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
                OperationType = operationType.ToString(),
            };
            var result = await _service.ApiLimitationsPostAsync(request);
            return new LimitationCheckResponse
            {
                IsValid = result.IsValid.HasValue ? result.IsValid.Value : false,
                FailMessage = result.FailMessage,
            };
        }

        public async Task<ClientDataResponse> GetClientDataAsync(string clientId, LimitationPeriod period)
        {
            var result = await _service.ApiLimitationsGetClientDataPostAsync(clientId, period.ToString());
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

        public async Task<AccumulatedDepositsResponse> GetAccumulatedDeposits(string clientId)
        {
            //await _service.(clientId, operationId);
            return await Task.FromResult((AccumulatedDepositsResponse)null);
        }
    }
}
