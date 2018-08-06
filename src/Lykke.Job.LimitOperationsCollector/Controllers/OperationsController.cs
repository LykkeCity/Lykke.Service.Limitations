using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.JobClient;
using Lykke.Service.Limitations.Core.Services;

namespace Lykke.Job.LimitOperationsCollector.Controllers
{
    [Route("api/[controller]")]
    public class OperationsController : Controller, ILimitOperationsApi
    {
        private readonly IAntiFraudCollector _antiFraudCollector;
        private readonly ICashOperationsCollector _cashOperationsCollector;
        private readonly ICashTransfersCollector _cashTransfersCollector;

        public OperationsController(
            IAntiFraudCollector antiFraudCollector,
            ICashOperationsCollector cashOperationsCollector,
            ICashTransfersCollector cashTransfersCollector)
        {
            _antiFraudCollector = antiFraudCollector;
            _cashOperationsCollector = cashOperationsCollector;
            _cashTransfersCollector = cashTransfersCollector;
        }

        /// <inheritdoc />
        [HttpPost("operationattempt")]
        [SwaggerOperation("OperationAttempt")]
        public async Task AddOperationAttemptAsync(
            string clientId,
            string assetId,
            double amount,
            int ttlInMinutes,
            CurrencyOperationType currencyOperationType)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException(nameof(clientId));

            if (string.IsNullOrWhiteSpace(assetId))
                throw new ArgumentException(nameof(assetId));

            if (amount < 0)
                throw new ArgumentException(nameof(amount));

            if (ttlInMinutes < 0)
                throw new ArgumentException(nameof(ttlInMinutes));

            await _antiFraudCollector.AddDataAsync(
                clientId,
                assetId,
                amount,
                ttlInMinutes,
                currencyOperationType);
        }

        /// <inheritdoc />
        [HttpDelete("removeoperation")]
        [SwaggerOperation("RemoveOperation")]
        public async Task RemoveOperationAsync(string clientId, string operationId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException(nameof(clientId));

            if (string.IsNullOrWhiteSpace(operationId))
                throw new ArgumentException(nameof(operationId));

            bool removed = await _cashOperationsCollector.RemoveClientOperationAsync(clientId, operationId);
            if (!removed)
                await _cashTransfersCollector.RemoveClientOperationAsync(clientId, operationId);
        }

        /// <inheritdoc />
        [HttpPost("cacheclientdata")]
        [SwaggerOperation("CacheClientData")]
        public Task CacheClientDataAsync(string clientId, CurrencyOperationType operationType)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException(nameof(clientId));

            Task.Run(() => _cashOperationsCollector.CacheClientDataAsync(clientId, operationType));
            Task.Run(() => _cashTransfersCollector.CacheClientDataAsync(clientId, operationType));

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        [HttpPost("add")]
        [SwaggerOperation("Add")]
        public async Task Add(
            string clientId,
            string assetId,
            double amount,
            CurrencyOperationType currencyOperationType)
        {
            CashOperation item = new CashOperation();
            item.ClientId = clientId;
            item.Asset = assetId;
            item.Volume = amount;
            item.OperationType = currencyOperationType;

            await _cashOperationsCollector.AddDataItemAsync(item);
        }

    }
}
