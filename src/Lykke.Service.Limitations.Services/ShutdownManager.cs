using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Sdk;
using Lykke.Service.Limitations.Core.Services;

namespace Lykke.Service.Limitations.Services
{
    public class ShutdownManager : IShutdownManager
    {
        private readonly ILog _log;
        private readonly List<IStopable> _stopables = new List<IStopable>();
        private readonly List<IStopable> _items = new List<IStopable>();

        public ShutdownManager(
            ILogFactory logFactory,
            IEnumerable<IStopable> stopables,
            IEnumerable<IStartStop> items)
        {
            _log = logFactory.CreateLog(this);
            _stopables.AddRange(stopables);
            _items.AddRange(items);
        }

        public async Task StopAsync()
        {
            Parallel.ForEach(_items, item =>
            {
                try
                {
                    item.Stop();
                }
                catch (Exception ex)
                {
                    _log.Warning($"Unable to stop {item.GetType().Name}", ex);
                }
            });

            Parallel.ForEach(_stopables, item =>
            {
                try
                {
                    item.Stop();
                }
                catch (Exception ex)
                {
                    _log.Warning($"Unable to stop {item.GetType().Name}", ex);
                }
            });

            await Task.CompletedTask;
        }
    }
}
