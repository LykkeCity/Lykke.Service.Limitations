using Autofac;
using Common;
using Common.Log;
using AzureStorage.Blob;
using AzureStorage.Tables;
using Lykke.SettingsReader;
using Lykke.Service.RateCalculator.Client;
using Lykke.Service.Limitations.AzureRepositories;
using Lykke.Service.Limitations.Core.Services;
using Lykke.Service.Limitations.Core.Repositories;
using Lykke.Service.Limitations.Services;
using Lykke.Job.LimitOperationsCollector.Settings;
using Lykke.Job.LimitOperationsCollector.RabbitSubscribers;
using StackExchange.Redis;

namespace Lykke.Job.LimitOperationsCollector.Modules
{
    public class JobModule : Module
    {
        private readonly AppSettings _appSettings;
        private readonly IReloadingManager<LimitOperationsCollectorSettings> _settingsManager;
        private readonly ILog _log;

        public JobModule(
            AppSettings appSettings,
            IReloadingManager<LimitOperationsCollectorSettings> settingsManager,
            ILog log)
        {
            _appSettings = appSettings;
            _settingsManager = settingsManager;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.Register(context => ConnectionMultiplexer.Connect(_appSettings.LimitOperationsCollectorJob.RedisConfiguration))
                .As<IConnectionMultiplexer>()
                .SingleInstance();

            var rateCalculatorClient = new RateCalculatorClient(_appSettings.RateCalculatorServiceClient.ServiceUrl, _log);
            builder.RegisterInstance(rateCalculatorClient).As<IRateCalculatorClient>().SingleInstance();

            ReagisterRepositories(builder);

            ReagisterServices(builder);

            RegisterRabbitMqSubscribers(builder);
        }

        private void ReagisterRepositories(ContainerBuilder builder)
        {
            var blobStorage = AzureBlobStorage.Create(_settingsManager.ConnectionString(s => s.BlobStorageConnectionString));

            builder.RegisterType<CashOperationsStateRepository>()
                .As<ICashOperationsRepository>()
                .WithParameter(TypedParameter.From(blobStorage))
                .SingleInstance();

            builder.RegisterType<CashTransfersStateRepository>()
                .As<ICashTransfersRepository>()
                .WithParameter(TypedParameter.From(blobStorage))
                .SingleInstance();

            var paymentsStorage = AzureTableStorage<PaymentTransactionEntity>.Create(
                _settingsManager.ConnectionString(s => s.PaymentTransactionsConnectionString),
                "PaymentTransactions",
                _log);

            builder.RegisterType<PaymentTransactionsRepository>()
                .As<IPaymentTransactionsRepository>()
                .WithParameter(TypedParameter.From(paymentsStorage))
                .SingleInstance();

            var accumulatedDepostStorage = AzureTableStorage<AccumulatedDepositPeriodEntity>.Create(
                _settingsManager.ConnectionString(s => s.BlobStorageConnectionString),
                "AccumulatedDeposits",
                _log);

            builder.RegisterType<AccumulatedDepositRepository>()
                .As<IAccumulatedDepositRepository>()
                .WithParameter(TypedParameter.From(accumulatedDepostStorage))
                .SingleInstance();

        }

        private void ReagisterServices(ContainerBuilder builder)
        {
            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>()
                .SingleInstance();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>()
                .SingleInstance();

            builder.RegisterType<CurrencyConverter>()
                .As<ICurrencyConverter>()
                .WithParameter("convertibleCurrencies", _appSettings.LimitOperationsCollectorJob.ConvertibleAssets)
                .SingleInstance();

            builder.RegisterType<AntiFraudCollector>()
                .As<IAntiFraudCollector>()
                .SingleInstance()
                .WithParameter("redisInstanceName", _appSettings.LimitOperationsCollectorJob.RedisInstanceName);

            builder.RegisterType<CashOperationsCollector>()
                .As<ICashOperationsCollector>()
                .SingleInstance()
                .WithParameter("redisInstanceName", _appSettings.LimitOperationsCollectorJob.RedisInstanceName);

            builder.RegisterType<CashTransfersCollector>()
                .As<ICashTransfersCollector>()
                .SingleInstance()
                .WithParameter("redisInstanceName", _appSettings.LimitOperationsCollectorJob.RedisInstanceName);

            builder.RegisterType<AccumulatedDepositAggregator>()
                .As<IAccumulatedDepositAggregator>()
                .SingleInstance();

        }

        private void RegisterRabbitMqSubscribers(ContainerBuilder builder)
        {
            builder.RegisterType<CashOperationSubscriber>()
                .As<IStopable>()
                .SingleInstance()
                .AutoActivate()
                .WithParameter("connectionString", _appSettings.LimitOperationsCollectorJob.Rabbit.ConnectionString)
                .WithParameter("exchangeName", _appSettings.LimitOperationsCollectorJob.Rabbit.CashOperationsExchangeName);

            builder.RegisterType<CashTransferOperationSubscriber>()
                .As<IStopable>()
                .SingleInstance()
                .AutoActivate()
                .WithParameter("connectionString", _appSettings.LimitOperationsCollectorJob.Rabbit.ConnectionString)
                .WithParameter("exchangeName", _appSettings.LimitOperationsCollectorJob.Rabbit.CashTransfersExchangeName);
        }
    }
}
