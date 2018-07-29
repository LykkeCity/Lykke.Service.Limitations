using Autofac;
using Lykke.Service.Limitations.Core.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Services
{
    public class StartupManager : IStartupManager
    {
        private readonly IAntiFraudCollector _antiFraudCollector;
        private readonly ICashOperationsCollector _cashOperationsCollector;
        private readonly ICashTransfersCollector _cashTransfersCollector;
        private readonly List<IStartable> _startables = new List<IStartable>();

        public StartupManager(IAntiFraudCollector antiFraudCollector, ICashOperationsCollector cashOperationsCollector, ICashTransfersCollector cashTransfersCollector)
        {
            _antiFraudCollector = antiFraudCollector;
            _cashOperationsCollector = cashOperationsCollector;
            _cashTransfersCollector = cashTransfersCollector;
        }

        public void Register(IStartable startable)
        {
            _startables.Add(startable);
        }

        public async Task StartAsync()
        {
            foreach (var item in _startables)
            {
                item.Start();
            }

            await _antiFraudCollector.PerformStartupCleanupAsync();
            await _cashOperationsCollector.PerformStartupCleanupAsync();
            await _cashTransfersCollector.PerformStartupCleanupAsync();
        }
    }
}
