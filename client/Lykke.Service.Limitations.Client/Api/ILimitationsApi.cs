using System.Threading.Tasks;
using Lykke.Service.Limitations.Client.Models.Request;
using Lykke.Service.Limitations.Client.Models.Response;
using Refit;

namespace Lykke.Service.Limitations.Client.Api
{
    public interface ILimitationsApi
    {
        [Post("/api/limitations")]
        Task<LimitationCheckResponse> CheckAsync(LimitationCheckRequest request);

        [Delete("/api/limitations/RemoveClientOperation")]
        Task RemoveClientOperationAsync(string clientId, string operationId);
    }
}
