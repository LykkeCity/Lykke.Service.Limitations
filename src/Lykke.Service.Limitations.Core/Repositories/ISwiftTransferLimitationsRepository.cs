using Lykke.Service.Limitations.Core.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Core.Repositories
{
    public interface ISwiftTransferLimitationsRepository
    {
        Task<IReadOnlyCollection<SwiftTransferLimitation>> GetAllAsync();

        Task<SwiftTransferLimitation> GetAsync(string asset);

        Task DeleteIfExistAsync(string asset);

        Task SaveRangeAsync(IEnumerable<SwiftTransferLimitation> limitations);
    }
}
