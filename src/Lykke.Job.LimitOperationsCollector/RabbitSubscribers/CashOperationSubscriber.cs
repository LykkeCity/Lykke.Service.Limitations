using System;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Lykke.Common.Log;
using Lykke.MatchingEngine.Connector.Models.Events;
using Lykke.MatchingEngine.Connector.Models.Events.Common;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.Limitations.Core.Services;

namespace Lykke.Job.LimitOperationsCollector.RabbitSubscribers
{
    public class CashOperationSubscriber : IStartable, IDisposable
    {
        private readonly ILog _log;
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private readonly ICashOperationsCollector _cashOperationsCollector;
        private readonly ILogFactory _logFactory;

        private RabbitMqSubscriber<CashInEvent> _cashinSubscriber;
        private RabbitMqSubscriber<CashOutEvent> _cashoutSubscriber;

        public CashOperationSubscriber(
            ICashOperationsCollector cashOperationsCollector,
            ILogFactory logFactory,
            string connectionString,
            string exchangeName)
        {
            _cashOperationsCollector = cashOperationsCollector;
            _logFactory = logFactory;
            _log = logFactory.CreateLog(this);
            _connectionString = connectionString;
            _exchangeName = exchangeName;
        }

        public void Start()
        {
            var cashinSettings = RabbitMqSubscriptionSettings
                .ForSubscriber(_connectionString, _exchangeName, "limitoperationscollector-cashin")
                .MakeDurable()
                .UseRoutingKey(((int)MessageType.CashIn).ToString());

            _cashinSubscriber = new RabbitMqSubscriber<CashInEvent>(_logFactory,
                    cashinSettings,
                    new ResilientErrorHandlingStrategy(_logFactory, cashinSettings,
                        retryTimeout: TimeSpan.FromSeconds(10),
                        next: new DeadQueueErrorHandlingStrategy(_logFactory, cashinSettings)))
                .SetMessageDeserializer(new ProtobufMessageDeserializer<CashInEvent>())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .Start();

            var cashoutSettings = RabbitMqSubscriptionSettings
                .ForSubscriber(_connectionString, _exchangeName, "limitoperationscollector-cashout")
                .MakeDurable()
                .UseRoutingKey(((int)MessageType.CashOut).ToString());

            _cashoutSubscriber = new RabbitMqSubscriber<CashOutEvent>(_logFactory,
                    cashoutSettings,
                    new ResilientErrorHandlingStrategy(_logFactory, cashoutSettings,
                        retryTimeout: TimeSpan.FromSeconds(10),
                        next: new DeadQueueErrorHandlingStrategy(_logFactory, cashoutSettings)))
                .SetMessageDeserializer(new ProtobufMessageDeserializer<CashOutEvent>())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .Start();
        }

        private async Task ProcessMessageAsync(CashInEvent item)
        {
            try
            {
                await _cashOperationsCollector.AddDataItemAsync(
                    new Service.Limitations.Core.Domain.CashOperation
                    {
                        Id = item.Header.MessageId ?? item.Header.RequestId,
                        ClientId = item.CashIn.WalletId,
                        Asset = item.CashIn.AssetId,
                        Volume = double.Parse(item.CashIn.Volume),
                        DateTime = item.Header.Timestamp,
                    });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: item);
                throw;
            }
        }

        private async Task ProcessMessageAsync(CashOutEvent item)
        {
            try
            {
                await _cashOperationsCollector.AddDataItemAsync(
                    new Service.Limitations.Core.Domain.CashOperation
                    {
                        Id = item.Header.MessageId ?? item.Header.RequestId,
                        ClientId = item.CashOut.WalletId,
                        Asset = item.CashOut.AssetId,
                        Volume = double.Parse(item.CashOut.Volume),
                        DateTime = item.Header.Timestamp
                    });
            }
            catch (Exception ex)
            {
                _log.Error(ex, context: item);
                throw;
            }
        }

        public void Dispose()
        {
            _cashinSubscriber?.Stop();
            _cashoutSubscriber?.Stop();
        }
    }
}
