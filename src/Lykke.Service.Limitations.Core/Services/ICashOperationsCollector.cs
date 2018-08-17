using Lykke.Service.Limitations.Core.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Core.Services
{
    public interface ICashOperationsCollector
    {
        Task<(double, bool)> GetCurrentAmountAsync(
            string clientId,
            string asset,
            LimitationPeriod period,
            CurrencyOperationType operationType,
            bool checkAllCrypto = false);

        Task<List<CashOperation>> GetClientDataAsync(string clientId, LimitationPeriod period);

        Task<bool> RemoveClientOperationAsync(string clientId, string operationId);

        Task CacheClientDataAsync(string clientId, CurrencyOperationType operationType);

        Task AddDataItemAsync(CashOperation item);

        Task PerformStartupCleanupAsync();
    }
}
