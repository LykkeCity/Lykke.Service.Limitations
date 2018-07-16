using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Core.Repositories
{
    public interface IClientStateRepository<T>
    {
        Task SaveClientStateAsync(string filename, T state);
        Task<T> LoadClientStateAsync(string filename);
    }
}
