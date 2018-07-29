using System;
using System.Threading.Tasks;
using Lykke.Service.Limitations.Core.Domain;

namespace Lykke.Service.Limitations.Core.Repositories
{
    public interface ILimitSettingsRepository
    {        
        Task<LimitSettings> GetAsync();
    }
}
