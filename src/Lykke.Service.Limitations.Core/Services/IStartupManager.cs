using Autofac;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Core.Services
{
    public interface IStartupManager
    {
        Task StartAsync();

        void Register(IStartable startable);
    }
}
