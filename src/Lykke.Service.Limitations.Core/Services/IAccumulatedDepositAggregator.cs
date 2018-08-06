using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Core.Services
{
    public interface IAccumulatedDepositAggregator
    {
         Task AggregateTotalAsync(string clientId, string assetId, double amount);
    }
}
