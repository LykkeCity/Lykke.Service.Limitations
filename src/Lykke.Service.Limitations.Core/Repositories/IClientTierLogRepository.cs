using Lykke.Service.Limitations.Core.Domain;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Core.Repositories
{
    public interface IClientTierLogRepository
    {
        Task WriteLogAsync(string clientId, string oldTierId, string newTierId, string changer);
        Task<IEnumerable<IClientTierLogRecord>> GetLogAsync(string clientId);
    }
}
