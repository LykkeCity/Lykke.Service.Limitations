using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.MatchingEngine.Connector.Models.Events;
using Lykke.MatchingEngine.Connector.Models.Events.Common;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.Limitations.Core.Services;

namespace Lykke.Job.LimitOperationsCollector.RabbitSubscribers
{
    public class CashOperationSubscriber : IStartStop
    {
        private readonly ILog _log;
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private readonly ICashOperationsCollector _cashOperationsCollector;

        private RabbitMqSubscriber<CashInEvent> _cashinSubscriber;
        private RabbitMqSubscriber<CashOutEvent> _cashoutSubscriber;

        public CashOperationSubscriber(
            ICashOperationsCollector cashOperationsCollector,
            ILogFactory log,
            string connectionString,
            string exchangeName)
        {
            _cashOperationsCollector = cashOperationsCollector;
            _log = log.CreateLog(this);
            _connectionString = connectionString;
            _exchangeName = exchangeName;
        }

        public void Start()
        {
            var cashinSettings = RabbitMqSubscriptionSettings
                .ForSubscriber(_connectionString, _exchangeName, "limitoperationscollector-cashin")
                .MakeDurable()
                .UseRoutingKey(((int)MessageType.CashIn).ToString());
            _cashinSubscriber = new RabbitMqSubscriber<CashInEvent>(
                    cashinSettings,
                    new ResilientErrorHandlingStrategy(_log, cashinSettings,
                        retryTimeout: TimeSpan.FromSeconds(10),
                        next: new DeadQueueErrorHandlingStrategy(_log, cashinSettings)))
                .SetMessageDeserializer(new ProtobufMessageDeserializer<CashInEvent>())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .SetLogger(_log)
                .Start();

            var cashoutSettings = RabbitMqSubscriptionSettings
                .ForSubscriber(_connectionString, _exchangeName, "limitoperationscollector-cashout")
                .MakeDurable()
                .UseRoutingKey(((int)MessageType.CashOut).ToString());
            _cashoutSubscriber = new RabbitMqSubscriber<CashOutEvent>(
                    cashoutSettings,
                    new ResilientErrorHandlingStrategy(_log, cashoutSettings,
                        retryTimeout: TimeSpan.FromSeconds(10),
                        next: new DeadQueueErrorHandlingStrategy(_log, cashoutSettings)))
                .SetMessageDeserializer(new ProtobufMessageDeserializer<CashOutEvent>())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .SetLogger(_log)
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
                _log.WriteError(nameof(ProcessMessageAsync), item, ex);
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
                        DateTime = item.Header.Timestamp,
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
            _cashinSubscriber?.Dispose();
            _cashoutSubscriber?.Dispose();
        }

        public void Stop()
        {
            _cashinSubscriber?.Stop();
            _cashoutSubscriber?.Stop();
        }
    }
}
