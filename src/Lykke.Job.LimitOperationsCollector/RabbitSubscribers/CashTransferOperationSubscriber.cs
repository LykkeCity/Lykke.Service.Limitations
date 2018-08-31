using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Common;
using Common.Log;
using Lykke.Common.Log;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Services;
using Lykke.Service.Limitations.Core.Repositories;
using CashTransferOperation = Lykke.MatchingEngine.Connector.Models.RabbitMq.CashTransferOperation;
using Lykke.Service.Operations.Client;

namespace Lykke.Job.LimitOperationsCollector.RabbitSubscribers
{
    public class CashTransferOperationSubscriber : IStartable, IStopable
    {
        private readonly IPaymentTransactionsRepository _paymentTransactionsRepository;
        private readonly ICashOperationsCollector _cashOperationsCollector;
        private readonly ICashTransfersCollector _cashTransfersCollector;
        private readonly IOperationsClient _operationsClient;
        private readonly ILog _log;
        private readonly string _connectionString;
        private readonly string _exchangeName;
        private RabbitMqSubscriber<CashTransferOperation> _subscriber;
        private RabbitMqSubscriber<CashTransferOperation> _oldSubscriber;

        public CashTransferOperationSubscriber(
            IPaymentTransactionsRepository paymentTransactionsRepository,
            ICashOperationsCollector cashOperationsCollector,
            ICashTransfersCollector cashTransfersCollector,
            IOperationsClient operationsClient,
            IStartupManager startupManager,
            ILogFactory log,
            string connectionString,
            string exchangeName)
        {
            _paymentTransactionsRepository = paymentTransactionsRepository;
            _cashOperationsCollector = cashOperationsCollector;
            _cashTransfersCollector = cashTransfersCollector;
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

            _subscriber = new RabbitMqSubscriber<CashTransferOperation>(settings,
                    new ResilientErrorHandlingStrategy(_log, settings,
                        retryTimeout: TimeSpan.FromSeconds(10),
                        next: new DeadQueueErrorHandlingStrategy(_log, settings)))
                .SetMessageDeserializer(new JsonMessageDeserializer<CashTransferOperation>())
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
            _oldSubscriber = new RabbitMqSubscriber<CashTransferOperation>(subscriberSettings, errorStrategy)
                .SetMessageDeserializer(new JsonMessageDeserializer<CashTransferOperation>())
                .Subscribe(ProcessMessageAsync)
                .SetLogger(_log)
                .SetConsole(new LogToConsole())
                .Start();
        }

        private async Task ProcessMessageAsync(CashTransferOperation item)
        {
            try
            {
                var op = await _operationsClient.Get(Guid.Parse(item.Id));
                if (op != null)
                {
                    _log.WriteInfo(nameof(CashTransferOperationSubscriber), nameof(ProcessMessageAsync), op.ToJson());

                    if (op.Type == Service.Operations.Contracts.OperationType.CashoutSwift)
                    {
                        var cashOp = new CashOperation
                        {
                            Id = item.Id,
                            ClientId = item.ToClientId,
                            Asset = item.Asset,
                            Volume = item.Volume,
                            DateTime = item.DateTime,
                            OperationType = CurrencyOperationType.SwiftTransferOut
                        };
                        await _cashOperationsCollector.AddDataItemAsync(cashOp, false);
                        return;
                    }
                }

                IPaymentTransaction paymentTransaction = null;
                for (int i = 0; i < 3; i++)
                {
                    paymentTransaction = await _paymentTransactionsRepository.GetByIdForClientAsync(item.Id, item.ToClientId);
                    if (paymentTransaction != null)
                        break;
                    else
                        await Task.Delay(2000 * (i + 1));
                }

                if (item.Fees != null)
                {
                    double feeSum = item.Fees.Sum(i => i.Transfer?.Volume ?? 0);
                    item.Volume -= feeSum;
                }

                if (paymentTransaction == null || paymentTransaction.PaymentSystem != CashInPaymentSystem.Swift)
                {
                    _log.WriteInfo(nameof(CashTransferOperationSubscriber), nameof(CashOperation), item.ToJson());

                    var cashOp = new CashOperation
                    {
                        Id = item.Id,
                        ClientId = item.ToClientId,
                        Asset = item.Asset,
                        Volume = item.Volume,
                        DateTime = item.DateTime,
                    };
                    await _cashOperationsCollector.AddDataItemAsync(cashOp);
                }
                else
                {
                    _log.WriteInfo(nameof(CashTransferOperationSubscriber), nameof(CashTransferOperation), item.ToJson());

                    var transfer = new Service.Limitations.Core.Domain.CashTransferOperation
                    {
                        Id = item.Id,
                        FromClientId = item.FromClientId,
                        ToClientId = item.ToClientId,
                        Asset = item.Asset,
                        Volume = item.Volume,
                        DateTime = item.DateTime,
                    };
                    await _cashTransfersCollector.AddDataItemAsync(transfer);
                }
            }
            catch (Exception exc)
            {
                _log.WriteError(nameof(ProcessMessageAsync), item, exc);
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
