using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Lykke.Cqrs;
using Lykke.Sdk;
using Lykke.Service.Limitations.Core.Services;

namespace Lykke.Service.Limitations.Services
{
    public class StartupManager : IStartupManager
    {
        private readonly List<IStartable> _startables = new List<IStartable>();
        private readonly List<IStartable> _items = new List<IStartable>();
        private readonly ICqrsEngine _cqrsEngine;

        public StartupManager(
            IEnumerable<IStartable> startables,
            IEnumerable<IStartStop> items,
            ICqrsEngine cqrsEngine)
        {
            _startables.AddRange(startables);
            _items.AddRange(items);
            _cqrsEngine = cqrsEngine;
        }

        public Task StartAsync()
        {
            foreach (var item in _startables)
            {
                item.Start();
            }

            foreach (var item in _items)
            {
                item.Start();
            }

            _cqrsEngine.Start();

            return Task.CompletedTask;
        }
    }
}
