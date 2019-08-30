using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Lykke.Common.ApiLibrary.Exceptions;
using Lykke.Service.Limitations.Client.Api;
using Lykke.Service.Limitations.Client.Models.Request;
using Lykke.Service.Limitations.Client.Models.Response;
using MoreLinq;

namespace Lykke.Service.Limitations.Controllers
{
    [Route("api/SwiftLimitations")]
    [Produces("application/json")]
    public class SwiftLimitationsController : Controller, ISwiftLimitationsApi
    {
        private readonly ISwiftTransferLimitationsRepository _swiftTransferLimitationsRepository;
        private readonly IMapper _mapper;

        public SwiftLimitationsController(
            ISwiftTransferLimitationsRepository swiftTransferLimitationsRepository,
            IMapper mapper)
        {
            _swiftTransferLimitationsRepository = swiftTransferLimitationsRepository;
            _mapper = mapper;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyCollection<SwiftTransferLimitation>), (int)HttpStatusCode.OK)]
        public async Task<IReadOnlyCollection<SwiftTransferLimitationResponse>> GetAllAsync()
        {
            var result = await _swiftTransferLimitationsRepository.GetAllAsync();
            return _mapper.Map<IReadOnlyCollection<SwiftTransferLimitationResponse>>(result);
        }

        [HttpGet("{asset}")]
        [ProducesResponseType(typeof(SwiftTransferLimitation), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        public async Task<SwiftTransferLimitationResponse> GetAsync([FromRoute]string asset)
        {
            var result = await _swiftTransferLimitationsRepository.GetAsync(asset);

            return _mapper.Map<SwiftTransferLimitationResponse>(result);
        }

        [HttpDelete("{asset}")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NoContent)]
        public async Task DeleteAsync([FromRoute]string asset)
        {
            var limitation = await _swiftTransferLimitationsRepository.GetAsync(asset);

            if (limitation == null)
                return;

            await _swiftTransferLimitationsRepository.DeleteIfExistAsync(asset);
        }

        [HttpPost("item")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        public Task SaveAsync([FromBody]SwiftTransferLimitationRequest limitation)
        {
            if (limitation == null)
                throw new ValidationApiException($"{nameof(limitation)} can't be empty");

            limitation.Asset = limitation.Asset.Trim();

            return _swiftTransferLimitationsRepository.SaveRangeAsync(
                _mapper.Map<IEnumerable<SwiftTransferLimitation>>(new List<SwiftTransferLimitationRequest>{limitation}));
        }

        [HttpPost]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        public Task SaveRangeAsync([FromBody]IReadOnlyList<SwiftTransferLimitationRequest> limitations)
        {
            if (limitations == null || !limitations.Any())
                throw new ValidationApiException($"{nameof(limitations)} can't be empty");

            limitations.ForEach(x => x.Asset = x.Asset.Trim());

            return _swiftTransferLimitationsRepository.SaveRangeAsync(
                _mapper.Map<IEnumerable<SwiftTransferLimitation>>(limitations));
        }
    }
}
