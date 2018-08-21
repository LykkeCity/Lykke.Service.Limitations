using Lykke.Service.Limitations.Core.Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Core.Repositories
{
    public interface ITierRepository
    {
        Task<string> SaveTierAsync(ITier tier);
        Task<IEnumerable<ITier>> LoadTiersAsync();
        Task<ITier> LoadTierAsync(string id);
        Task DeleteTierAsync(string id);
    }
}
