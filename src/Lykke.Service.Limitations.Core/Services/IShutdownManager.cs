using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Core.Services
{
    public interface IShutdownManager
    {
        Task StopAsync();
    }
}
