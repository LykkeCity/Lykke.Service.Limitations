using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Controllers
{
    public class TiersController : Controller
    {
        private readonly ITierRepository _tierRepository;
        private readonly IClientTierRepository _clientTierRepository;

        public TiersController(
            ITierRepository tierRepository,
            IClientTierRepository clientTierRepository
            )
        {
            _tierRepository = tierRepository;
            _clientTierRepository = clientTierRepository;
        }

        [Route("api/[controller]/SetTierToClient")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SetTierToClient([FromBody] ClientTier clientTier)
        {
            await _clientTierRepository.SetClientTierAsync(clientTier.ClientId, clientTier.TierId);
            return Ok();
        }

        [Route("api/[controller]/SaveTier")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SaveTier([FromBody] Tier tier)
        {
            await _tierRepository.SaveTierAsync(tier);
            return Ok();
        }

        [Route("api/[controller]/LoadTiers")]
        [HttpPost]
        [ProducesResponseType(typeof(IEnumerable<Tier>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> LoadTiers()
        {
            return Ok(await _tierRepository.LoadTiersAsync());
        }

        [Route("api/[controller]/LoadTier")]
        [HttpPost]
        [ProducesResponseType(typeof(Tier), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> LoadTier(string id)
        {
            return Ok(await _tierRepository.LoadTierAsync(id));
        }

    }
}
