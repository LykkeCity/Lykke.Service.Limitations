using Autofac;
using Lykke.Service.Limitations.Core.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Services
{
    public class StartupManager : IStartupManager
    {
        private readonly List<IStartable> _startables = new List<IStartable>();

        public void Register(IStartable startable)
        {
            _startables.Add(startable);
        }

        public Task StartAsync()
        {
            foreach (var item in _startables)
            {
                item.Start();
            }

            return Task.CompletedTask;
        }
    }
}
