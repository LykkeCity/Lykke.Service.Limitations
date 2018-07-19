using Lykke.Service.Limitations.Core.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Core.Services
{
    public interface IAntiFraudCollector
    {
        Task AddDataAsync(
            string clientId,
            string asset,
            double amount,
            int ttlInMinutes,
            CurrencyOperationType operationType);

        Task<double> GetAttemptsValueAsync(
            string clientId,
            string asset,
            LimitationType limitType);

        Task RemoveOperationAsync(
            string clientId,
            string asset,
            double amount,
            CurrencyOperationType operationType);

        Task<List<CurrencyOperationAttempt>> GetClientDataAsync(string clientId);

        Task PerformStartupCleanupAsync();
    }
}
