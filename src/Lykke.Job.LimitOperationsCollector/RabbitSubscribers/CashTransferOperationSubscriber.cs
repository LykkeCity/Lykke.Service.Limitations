using System;
using System.Threading.Tasks;
using Autofac;
using Common.Log;
using Lykke.Common.Log;
using Lykke.MatchingEngine.Connector.Models.Events;
using Lykke.MatchingEngine.Connector.Models.Events.Common;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Services;
using Lykke.Service.Limitations.Core.Repositories;

namespace Lykke.Job.LimitOperationsCollector.RabbitSubscribers
{
    public class CashTransferOperationSubscriber : IStartable, IDisposable
    {
        private readonly IPaymentTransactionsRepository _paymentTransactionsRepository;
        private readonly ICashOperationsCollector _cashOperationsCollector;
        private readonly ICashTransfersCollector _cashTransfersCollector;
        private readonly ILogFactory _logFactory;
        private readonly ILog _log;
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private RabbitMqSubscriber<CashTransferEvent> _subscriber;

        public CashTransferOperationSubscriber(
            IPaymentTransactionsRepository paymentTransactionsRepository,
            ICashOperationsCollector cashOperationsCollector,
            ICashTransfersCollector cashTransfersCollector,
            ILogFactory logFactory,
            string connectionString,
            string exchangeName)
        {
            _paymentTransactionsRepository = paymentTransactionsRepository;
            _cashOperationsCollector = cashOperationsCollector;
            _cashTransfersCollector = cashTransfersCollector;
            _logFactory = logFactory;
            _log = logFactory.CreateLog(this);
            _connectionString = connectionString;
            _exchangeName = exchangeName;
        }

        public void Start()
        {
            var settings = RabbitMqSubscriptionSettings
                .ForSubscriber(_connectionString, _exchangeName, "limitoperationscollector-transfers")
                .MakeDurable()
                .UseRoutingKey(((int)MessageType.CashTransfer).ToString());

            _subscriber = new RabbitMqSubscriber<CashTransferEvent>(_logFactory,
                    settings,
                    new ResilientErrorHandlingStrategy(_logFactory, settings,
                        retryTimeout: TimeSpan.FromSeconds(10),
                        next: new DeadQueueErrorHandlingStrategy(_logFactory, settings)))
                .SetMessageDeserializer(new ProtobufMessageDeserializer<CashTransferEvent>())
                .Subscribe(ProcessMessageAsync)
                .CreateDefaultBinding()
                .Start();
        }

        private async Task ProcessMessageAsync(CashTransferEvent item)
        {
            var id = item.Header.MessageId ?? item.Header.RequestId;
            try
            {
                IPaymentTransaction paymentTransaction = null;

                for (int i = 0; i < 3; i++)
                {
                    paymentTransaction = await _paymentTransactionsRepository.GetByIdForClientAsync(id, item.CashTransfer.ToWalletId);
                    if (paymentTransaction != null)
                        break;
                    await Task.Delay(2000 * (i + 1));
                }

                double volume = double.Parse(item.CashTransfer.Volume);

                if (item.CashTransfer.Fees != null)
                {
                    foreach (var fee in item.CashTransfer.Fees)
                    {
                        if (string.IsNullOrWhiteSpace(fee.Transfer?.Volume))
                            continue;

                        volume -= double.Parse(fee.Transfer.Volume);
                    }}

                if (paymentTransaction == null || paymentTransaction.PaymentSystem != CashInPaymentSystem.Swift)
                {
                    var cashOp = new CashOperation
                    {
                        Id = id,
                        ClientId = item.CashTransfer.ToWalletId,
                        Asset = item.CashTransfer.AssetId,
                        Volume = volume,
                        DateTime = item.Header.Timestamp,
                    };

                    await _cashOperationsCollector.AddDataItemAsync(cashOp);
                }
                else
                {
                    var transfer = new CashTransferOperation
                    {
                        Id = id,
                        FromClientId = item.CashTransfer.FromWalletId,
                        ToClientId = item.CashTransfer.ToWalletId,
                        Asset = item.CashTransfer.AssetId,
                        Volume = volume,
                        DateTime = item.Header.Timestamp,
                    };
                    await _cashTransfersCollector.AddDataItemAsync(transfer);
                }
            }
            catch (Exception exc)
            {
                _log.Error(exc, context: item);
            }
        }

        public void Dispose()
        {
            _subscriber?.Stop();
        }
    }
}
