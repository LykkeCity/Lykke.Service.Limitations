using Lykke.Service.Limitations.Core.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Core.Repositories
{
    public interface IWithdrawLimitsRepository
    {
        Task<IEnumerable<IWithdrawLimit>> GetDataAsync();
        Task<bool> AddAsync(IWithdrawLimit item);
        Task<bool> DeleteAsync(string assetId);
        Task<double> GetLimitByAssetAsync(string assetId);
    }
}
