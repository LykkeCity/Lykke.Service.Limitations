using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Limitations.Client.Models.Request;
using Lykke.Service.Limitations.Client.Models.Response;
using Refit;

namespace Lykke.Service.Limitations.Client.Api
{
    public interface ISwiftLimitationsApi
    {
        [Get("/api/SwiftLimitations")]
        Task<IReadOnlyCollection<SwiftTransferLimitationResponse>> GetAllAsync();

        [Get("/api/SwiftLimitations/{asset}")]
        Task<SwiftTransferLimitationResponse> GetAsync(string asset);

        [Post("/api/SwiftLimitations/item")]
        Task SaveAsync(SwiftTransferLimitationRequest limitation);

        [Post("/api/SwiftLimitations")]
        Task SaveRangeAsync(IReadOnlyList<SwiftTransferLimitationRequest> limitations);

        [Delete("/api/SwiftLimitations/{asset}")]
        Task DeleteAsync(string asset);
    }
}
