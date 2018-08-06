using System.Threading.Tasks;
using Lykke.Service.Limitations.Client.Models;

namespace Lykke.Service.Limitations.Client
{
    public interface ILimitationsServiceClient
    {
        Task<LimitationCheckResponse> CheckAsync(
            string clientId,
            string asset,
            double amount,
            CurrencyOperationType operationType);

        Task<ClientDataResponse> GetClientDataAsync(string clientId, LimitationPeriod period);

        Task RemoveClientOperationAsync(string clientId, string operationId);

        Task<AccumulatedAmountsResponse> GetAccumulatedAmountsAsync(string clientId);
    }
}
