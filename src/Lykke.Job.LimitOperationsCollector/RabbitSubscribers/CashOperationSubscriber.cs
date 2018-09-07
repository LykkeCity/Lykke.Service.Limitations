using System;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.MatchingEngine.Connector.Models.RabbitMq;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.Limitations.Core.Services;
using Lykke.Service.Operations.Client;

namespace Lykke.Job.LimitOperationsCollector.RabbitSubscribers
{
    public class CashOperationSubscriber : IStartable, IStopable
    {
        private readonly ILog _log;
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private readonly ICashOperationsCollector _cashOperationsCollector;
        private readonly IOperationsClient _operationsClient;

        private RabbitMqSubscriber<CashOperation> _subscriber;
        private RabbitMqSubscriber<CashOperation> _oldSubscriber;

        public CashOperationSubscriber(
            ICashOperationsCollector cashOperationsCollector,
            IOperationsClient operationsClient,
            IStartupManager startupManager,
            ILogFactory log,
            string connectionString,
            string exchangeName)
        {
            _cashOperationsCollector = cashOperationsCollector;
            _operationsClient = operationsClient;
            _log = log.CreateLog(this);
            _connectionString = connectionString;
            _exchangeName = exchangeName;

            startupManager.Register(this);
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .CreateForSubscriber(_connectionString, _exchangeName, "limitoperationscollector")
                .MakeDurable();

            _subscriber = new RabbitMqSubscriber<CashOperation>(settings,
                    new ResilientErrorHandlingStrategy(_log, settings,
                        retryTimeout: TimeSpan.FromSeconds(10),
                        next: new DeadQueueErrorHandlingStrategy(_log, settings)))
                .SetMessageDeserializer(new JsonMessageDeserializer<CashOperation>())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .SetLogger(_log)
                .SetConsole(new LogToConsole())
                .Start();

            var subscriberSettings = new RabbitMqSubscriptionSettings
            {
                ConnectionString = _connectionString,
                ExchangeName = settings.ExchangeName,
                QueueName = settings.ExchangeName + ".limitations",
                IsDurable = true,
                ReconnectionDelay = TimeSpan.FromSeconds(3),
            };
            var errorStrategy = new DefaultErrorHandlingStrategy(_log, subscriberSettings);
            _oldSubscriber = new RabbitMqSubscriber<CashOperation>(subscriberSettings, errorStrategy)
                .SetMessageDeserializer(new JsonMessageDeserializer<CashOperation>())
                .Subscribe(ProcessMessageAsync)
                .SetLogger(_log)
                .SetConsole(new LogToConsole())
                .Start();
        }

        private async Task ProcessMessageAsync(CashOperation item)
        {
            try
            {
                var op = await _operationsClient.Get(Guid.Parse(item.Id));
                if (op != null)
                {
                }

                await _cashOperationsCollector.AddDataItemAsync(
                new Service.Limitations.Core.Domain.CashOperation
                {
                    Id = item.Id,
                    ClientId = item.ClientId,
                    Asset = item.Asset,
                    Volume = item.Volume,
                    DateTime = item.DateTime,
                });
            }
            catch (Exception ex)
            {
                _log.WriteError(nameof(ProcessMessageAsync), item, ex);
                throw;
            }
        }

        public void Dispose()
        {
            _subscriber?.Dispose();
            _oldSubscriber?.Dispose();
        }

        public void Stop()
        {
            _subscriber?.Stop();
            _oldSubscriber?.Stop();
        }
    }
}
