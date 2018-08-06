using Lykke.Service.Limitations.Core.Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Core.Repositories
{
    public interface IClientTierRepository
    {
        Task SetClientTierAsync(string clientId, string tierId);

        Task<string> GetClientTierIdAsync(string clientId);

        Task SetDefaultTierAsync(string tierId);

        Task<string> GetDefaultTierIdAsync();
    }
}
