using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Controllers
{
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SwiftLimitationsController : Controller
    {
        private readonly ISwiftTransferLimitationsRepository _swiftTransferLimitationsRepository;

        public SwiftLimitationsController(ISwiftTransferLimitationsRepository swiftTransferLimitationsRepository)
        {
            _swiftTransferLimitationsRepository = swiftTransferLimitationsRepository;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyCollection<SwiftTransferLimitation>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _swiftTransferLimitationsRepository.GetAllAsync();
            return Ok(result);
        }

        [HttpGet("{asset}")]
        [ProducesResponseType(typeof(SwiftTransferLimitation), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Get([FromRoute]string asset)
        {
            var result = await _swiftTransferLimitationsRepository.GetAsync(asset);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpDelete("{asset}")]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> Delete([FromRoute]string asset)
        {
            var limitation = await _swiftTransferLimitationsRepository.GetAsync(asset);

            if (limitation == null)
            {
                return NotFound();
            }

            await _swiftTransferLimitationsRepository.DeleteIfExistAsync(asset);
            return Ok();
        }

        [HttpPost]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(void), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> Save([FromBody]List<SwiftTransferLimitation> limitations)
        {
            if (limitations == null || limitations.Count == 0)
            {
                return BadRequest();
            }

            limitations.ForEach(x => x.Asset = x.Asset.Trim());

            await _swiftTransferLimitationsRepository.SaveRangeAsync(limitations);
            return Ok();
        }
    }
}
