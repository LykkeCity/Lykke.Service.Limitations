using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Limitations.Client.AutorestClient.Models;

namespace Lykke.Service.Limitations.Client
{
    public interface ISwiftLimitationServiceClient
    {
        Task<IReadOnlyCollection<SwiftTransferLimitation>> GetAllAsync();

        Task<SwiftTransferLimitation> GetAsync(string asset);

        Task SaveAsync(SwiftTransferLimitation limitation);

        Task SaveRangeAsync(IEnumerable<SwiftTransferLimitation> limitations);

        Task DeleteIfExistAsync(string asset);
    }
}
