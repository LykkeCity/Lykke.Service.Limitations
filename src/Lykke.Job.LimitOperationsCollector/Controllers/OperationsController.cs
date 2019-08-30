using System;
using System.Threading.Tasks;
using Lykke.Cqrs;
using Lykke.Service.Limitations.Client;
using Lykke.Service.Limitations.Client.Events;
using Microsoft.AspNetCore.Mvc;
using Lykke.Service.Limitations.Core.JobClient;
using Lykke.Service.Limitations.Core.Services;
using Swashbuckle.AspNetCore.Annotations;
using CurrencyOperationType = Lykke.Service.Limitations.Core.Domain.CurrencyOperationType;

namespace Lykke.Job.LimitOperationsCollector.Controllers
{
    [Route("api/[controller]")]
    public class OperationsController : Controller, ILimitOperationsApi
    {
        private readonly IAntiFraudCollector _antiFraudCollector;
        private readonly ICashOperationsCollector _cashOperationsCollector;
        private readonly ICashTransfersCollector _cashTransfersCollector;
        private readonly ICqrsEngine _cqrsEngine;

        public OperationsController(
            IAntiFraudCollector antiFraudCollector,
            ICashOperationsCollector cashOperationsCollector,
            ICashTransfersCollector cashTransfersCollector,
            ICqrsEngine cqrsEngine)
        {
            _antiFraudCollector = antiFraudCollector;
            _cashOperationsCollector = cashOperationsCollector;
            _cashTransfersCollector = cashTransfersCollector;
            _cqrsEngine = cqrsEngine;
        }

        /// <inheritdoc />
        [HttpPost("operationattempt")]
        [SwaggerOperation("OperationAttempt")]
        public Task AddOperationAttemptAsync(
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

            return _antiFraudCollector.AddDataAsync(
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

            bool removedCashOperation = await _cashOperationsCollector.RemoveClientOperationAsync(clientId, operationId);
            bool removedTransferOperation = false;
            if (!removedCashOperation)
                removedTransferOperation = await _cashTransfersCollector.RemoveClientOperationAsync(clientId, operationId);

            if (removedCashOperation || removedTransferOperation)
            {
                _cqrsEngine.PublishEvent(
                    new ClientOperationRemovedEvent
                    {
                        ClientId = clientId,
                        OperationId = operationId
                    },
                    LimitationsBoundedContext.Name);
            }
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
    }
}
