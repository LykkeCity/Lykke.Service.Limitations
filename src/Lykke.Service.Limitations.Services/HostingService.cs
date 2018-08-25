using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Lykke.Service.Limitations.Core.Services;
using Microsoft.Extensions.Hosting;

namespace Lykke.Service.Limitations.Services
{
    public class HostingService : IHostedService
    {        
        private readonly IAntiFraudCollector _antiFraudCollector;
        private readonly ICashOperationsCollector _cashOperationsCollector;
        private readonly ICashTransfersCollector _cashTransfersCollector;
        
        public HostingService(            
            IAntiFraudCollector antiFraudCollector, 
            ICashOperationsCollector cashOperationsCollector, 
            ICashTransfersCollector cashTransfersCollector)
        {
            _antiFraudCollector = antiFraudCollector;
            _cashOperationsCollector = cashOperationsCollector;
            _cashTransfersCollector = cashTransfersCollector;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _antiFraudCollector.PerformStartupCleanupAsync();
            await _cashOperationsCollector.PerformStartupCleanupAsync();
            await _cashTransfersCollector.PerformStartupCleanupAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
