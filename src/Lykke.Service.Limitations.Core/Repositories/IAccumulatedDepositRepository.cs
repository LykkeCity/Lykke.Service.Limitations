using Lykke.Service.Limitations.Core.Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Core.Repositories
{
    public interface IAccumulatedDepositRepository
    {
        Task AggregateTotalAsync(string clientId, string assetId, double amount, CurrencyOperationType operationType);
        Task<double> GetAccumulatedAmountAsync(string clientId, CurrencyOperationType operationType);
    }
}
