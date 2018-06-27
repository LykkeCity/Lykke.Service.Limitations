using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Controllers
{
    [Route("api/[controller]")]
    public class WithdrawLimitsController : Controller
    {
        private readonly IWithdrawLimitsRepository _withdrawLimitsRepository;

        public WithdrawLimitsController(IWithdrawLimitsRepository withdrawLimitsRepository)
        {
            _withdrawLimitsRepository = withdrawLimitsRepository;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<IWithdrawLimit>), (int)HttpStatusCode.OK)]
        public async Task<IEnumerable<WithdrawLimit>> Get()
        {
            return (await _withdrawLimitsRepository.GetDataAsync()).Select(l =>
                new WithdrawLimit { AssetId = l.AssetId, LimitAmount = l.LimitAmount });
        }

        [HttpPost]
        public async Task Post([FromBody] WithdrawLimit request)
        {
            await _withdrawLimitsRepository.AddAsync(request);
        }

        [HttpDelete]
        public async Task Delete(string assetId)
        {
            await _withdrawLimitsRepository.DeleteAsync(assetId);
        }

        [HttpGet]
        [Route("{assetId}")]
        public async Task<double> GetLimit(string assetId)
        {
            return await _withdrawLimitsRepository.GetLimitByAssetAsync(assetId);
        }
    }
}
