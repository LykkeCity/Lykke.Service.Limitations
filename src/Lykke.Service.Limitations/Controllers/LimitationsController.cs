using System;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Service.Limitations.Client.Api;
using Lykke.Service.Limitations.Client.Models.Request;
using Lykke.Service.Limitations.Client.Models.Response;
using CurrencyOperationType = Lykke.Service.Limitations.Core.Domain.CurrencyOperationType;
using LimitationPeriod = Lykke.Service.Limitations.Core.Domain.LimitationPeriod;
using ValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;

namespace Lykke.Service.Limitations.Controllers
{
    [Route("api/limitations")]
    public class LimitationsController : Controller, ILimitationsApi
    {
        private readonly ILimitationCheck _limitationChecker;
        private readonly IMapper _mapper;

        public LimitationsController(
            ILimitationCheck limitationChecker,
            IMapper mapper)
        {
            _limitationChecker = limitationChecker;
            _mapper = mapper;
        }

        [HttpPost]
        [ProducesResponseType(typeof(LimitationCheckResponse), (int)HttpStatusCode.OK)]
        public async Task<LimitationCheckResponse> CheckAsync([FromBody] LimitationCheckRequest postModel)
        {
            var context = new ValidationContext(postModel, serviceProvider: null, items: null);
            var validationResults = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(postModel, context, validationResults);

            if (!isValid)
                return new LimitationCheckResponse
                {
                    IsValid = false,
                    FailMessage =
                        "InvalidInput: " + string.Join(";", validationResults.Select(e => e.ErrorMessage)),
                };

            LimitationCheckResult result = await _limitationChecker.CheckCashOperationLimitAsync(
                postModel.ClientId,
                postModel.Asset,
                postModel.Amount,
                _mapper.Map<CurrencyOperationType>(postModel.OperationType));

            return _mapper.Map<LimitationCheckResponse>(result);
        }

//        [HttpGet("{clientId}")]
//        public async Task<ClientDataResponse> GetClientDataAsync(string clientId)
//        {
//            throw new NotImplementedException();
//        }

        [Obsolete("Use GET /api/limitations/{clientId} instead")]
        [HttpPost("GetClientData")]
        [ProducesResponseType(typeof(ClientData), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ClientData), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetClientData(string clientId, LimitationPeriod period)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                return BadRequest(new ClientData());

            ClientData result = await _limitationChecker.GetClientDataAsync(clientId, period);

            return Ok(result);
        }

        [Route("RemoveClientOperation")]
        [HttpDelete]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public Task RemoveClientOperationAsync(string clientId, string operationId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ValidationApiException($"{clientId} can't be empty");

            return _limitationChecker.RemoveClientOperationAsync(clientId, operationId);
        }
    }
}
