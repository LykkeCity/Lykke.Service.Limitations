using Lykke.Service.Limitations.Core.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Core.Services
{
    public interface ICashTransfersCollector
    {
        Task<(double, bool)> GetCurrentAmountAsync(
            string clientId,
            string asset,
            CashOperationLimitation limit,
            CurrencyOperationType operationType);

        Task<List<CashTransferOperation>> GetClientDataAsync(string clientId, LimitationPeriod period);

        Task<bool> RemoveClientOperationAsync(string clientId, string operationId);

        Task CacheClientDataAsync(string clientId, CurrencyOperationType operationType);

        Task AddDataItemAsync(CashTransferOperation item, bool setOperationType = true);

        Task PerformStartupCleanupAsync();
    }
}
