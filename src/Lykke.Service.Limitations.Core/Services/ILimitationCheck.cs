using Lykke.Service.Limitations.Core.Domain;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Core.Services
{
    public interface ILimitationCheck
    {
        Task<LimitationCheckResult> CheckCashOperationLimitAsync(
            string clientId,
            string assetId,
            double amount,
            CurrencyOperationType direction);

        Task<ClientData> GetClientDataAsync(string clientData, LimitationPeriod period);

        Task RemoveClientOperationAsync(string clientId, string operationId);

        Task<AccumulatedDepositsModel> GetAccumulatedDepositsAsync(string clientId);

        
    }
}
