using Lykke.Service.Limitations.Core.Repositories;
using Lykke.Service.Limitations.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Services
{
    public class AccumulatedDepositAggregator : IAccumulatedDepositAggregator
    {
        private IAccumulatedDepositRepository _accumulatedDepositRepository;

        public AccumulatedDepositAggregator(
            IAccumulatedDepositRepository accumulatedDepositRepository
            )
        {
            _accumulatedDepositRepository = accumulatedDepositRepository;
        }

        public async Task AggregateTotalAsync(string clientId, string assetId, double amount)
        {
            await _accumulatedDepositRepository.AggregateTotalAsync(clientId, assetId, amount);
        }
    }
}
