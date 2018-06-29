using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Core.Repositories
{
    public interface IClientStateRepository<T>
    {
        Task SaveClientStateAsync(string clientId, T state);
        Task<T> LoadClientStateAsync(string clientId);
    }
}
