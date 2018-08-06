using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Services;
using Lykke.Service.Limitations.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Controllers
{
    public class LimitationsController : Controller
    {
        private readonly ILimitationCheck _limitationChecker;

        public LimitationsController(ILimitationCheck limitationChecker)
        {
            _limitationChecker = limitationChecker;
        }

        [Route("api/[controller]")]
        [HttpPost]
        [Produces("application/json", Type = typeof(LimitationCheckResult))]
        [ProducesResponseType(typeof(LimitationCheckResult), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(LimitationCheckResult), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Check([FromBody] LimitCheckRequestModel postModel)
        {
            var context = new ValidationContext(postModel, serviceProvider: null, items: null);
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(postModel, context, validationResults);
            if (!isValid)
                return BadRequest(
                    new LimitationCheckResult
                    {
                        IsValid = false,
                        FailMessage = "InvalidInput: " + string.Join(";", validationResults.Select(e => e.ErrorMessage)),
                    });

            LimitationCheckResult result = await _limitationChecker.CheckCashOperationLimitAsync(
                postModel.ClientId,
                postModel.Asset,
                postModel.Amount,
                postModel.OperationType);

            return Ok(result);
        }

        [Route("api/[controller]/GetClientData")]
        [HttpPost]
        [Produces("application/json", Type = typeof(ClientData))]
        [ProducesResponseType(typeof(ClientData), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ClientData), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetClientData(string clientId, LimitationPeriod period)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return BadRequest(new ClientData());

            ClientData result = await _limitationChecker.GetClientDataAsync(clientId, period);

            return Ok(result);
        }

        [Route("api/[controller]/RemoveClientOperation")]
        [HttpDelete]
        public async Task<IActionResult> RemoveClientOperation(string clientId, string operationId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return BadRequest();

            await _limitationChecker.RemoveClientOperationAsync(clientId, operationId);

            return Ok();
        }

        [Route("api/[controller]/GetAccumulatedDeposits")]
        [HttpPost]
        [Produces("application/json", Type = typeof(AccumulatedDepositsModel))]
        [ProducesResponseType(typeof(AccumulatedDepositsModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(AccumulatedDepositsModel), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetAccumulatedDeposits(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return BadRequest(new ClientData());

            AccumulatedDepositsModel result = await _limitationChecker.GetAccumulatedDepositsAsync(clientId);

            return Ok(result);
        }
    }
}
