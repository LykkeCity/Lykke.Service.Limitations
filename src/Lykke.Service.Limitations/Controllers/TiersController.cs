using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [Route("api/[controller]/GetClientTier")]
        [HttpGet]
        [ProducesResponseType(typeof(Tier), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetClientTier(string clientId)
        {
            ITier result = null;
            var id = await _clientTierRepository.GetClientTierIdAsync(clientId);
            if (id != null)
            {
                result = await _tierRepository.LoadTierAsync(id);
                return Ok(result);
            }
            return Ok();
        }

        [Route("api/[controller]/SaveTier")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SaveTier([FromBody] Tier tier)
        {
            var id = await _tierRepository.SaveTierAsync(tier);
            var defaultTierId = await _clientTierRepository.GetDefaultTierIdAsync();
            if (tier.IsDefault || defaultTierId == null)
            {
                await _clientTierRepository.SetDefaultTierAsync(id);
            }
            return Ok();
        }

        [Route("api/[controller]/LoadTiers")]
        [HttpPost]
        [ProducesResponseType(typeof(IEnumerable<Tier>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> LoadTiers()
        {
            var tiers = await _tierRepository.LoadTiersAsync();
            var defaultTierId = await _clientTierRepository.GetDefaultTierIdAsync();
            if (defaultTierId != null)
            {
                tiers.Where(tier => tier.Id == defaultTierId).ToList().ForEach(tier => tier.IsDefault = true);
            }
            return Ok(tiers);
        }

        [Route("api/[controller]/LoadTier")]
        [HttpPost]
        [ProducesResponseType(typeof(Tier), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> LoadTier(string id)
        {
            var tier = await _tierRepository.LoadTierAsync(id);
            var defaultTierId = await _clientTierRepository.GetDefaultTierIdAsync();
            if (tier != null && defaultTierId != null && tier.Id == defaultTierId)
            {
                tier.IsDefault = true;
            }
            return Ok(tier);
        }

    }
}
