using Autofac;
using AzureStorage.Blob;
using AzureStorage.Tables;
using Common.Log;
using Lykke.HttpClientGenerator;
using Lykke.Service.Limitations.AzureRepositories;
using Lykke.Service.Limitations.Core.JobClient;
using Lykke.Service.Limitations.Core.Repositories;
using Lykke.Service.Limitations.Core.Services;
using Lykke.Service.Limitations.Services;
using Lykke.Service.Limitations.Settings;
using Lykke.Service.RateCalculator.Client;
using Lykke.SettingsReader;
using StackExchange.Redis;

namespace Lykke.Service.Limitations.Modules
{
    public class ServiceModule : Module
    {
        private readonly AppSettings _appSettings;
        private readonly IReloadingManager<LimitationsSettings> _settingsManager;
        private readonly ILog _log;

        public ServiceModule(
            AppSettings appSettings,
            IReloadingManager<LimitationsSettings> settingsManager,
            ILog log)
        {
            _appSettings = appSettings;
            _settingsManager = settingsManager;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log).As<ILog>().SingleInstance();

            var settings = _appSettings.LimitationsSettings;

            builder.Register(context => ConnectionMultiplexer.Connect(settings.RedisConfiguration))
                .As<IConnectionMultiplexer>()
                .SingleInstance();

            var rateCalculatorClient = new RateCalculatorClient(_appSettings.RateCalculatorServiceClient.ServiceUrl, _log);
            builder.RegisterInstance(rateCalculatorClient).As<IRateCalculatorClient>().SingleInstance();

            builder.RegisterClient<ILimitOperationsApi>(settings.LimitOperationsJobUrl);

            RegisterRepositories(builder);

            RegisterServices(builder);
        }

        private void RegisterRepositories(ContainerBuilder builder)
        {
            var blobStorage = AzureBlobStorage.Create(_settingsManager.ConnectionString(s => s.BlobStorageConnectionString));

            var cashOperationsStateRepository = new CashOperationsStateRepository(blobStorage, _log);
            builder.RegisterInstance(cashOperationsStateRepository)
                .As<ICashOperationsRepository>()
                .SingleInstance();

            var cashTransfersStateRepository = new CashTransfersStateRepository(blobStorage, _log);
            builder.RegisterInstance(cashTransfersStateRepository)
                .As<ICashTransfersRepository>()
                .SingleInstance();

            var swiftTransferLimitationsStorage = AzureTableStorage<SwiftTransferLimitationEntity>.Create(
                _settingsManager.ConnectionString(s => s.LimitationSettingsConnectionString),
                "SwiftTransferLimitations",
                _log);
            var swiftTransferLimitationsRepository = new SwiftTransferLimitationsRepository(swiftTransferLimitationsStorage, _log);
            builder
                .RegisterInstance(swiftTransferLimitationsRepository)
                .As<ISwiftTransferLimitationsRepository>()
                .SingleInstance();

            var accumulatedDepostStorage = AzureTableStorage<AccumulatedDepositPeriodEntity>.Create(
                _settingsManager.ConnectionString(s => s.DepositAccumulationConnectionString),
                "AccumulatedDeposits",
                _log);

            builder.RegisterType<AccumulatedDepositRepository>()
                .As<IAccumulatedDepositRepository>()
                .WithParameter(TypedParameter.From(accumulatedDepostStorage))
                .SingleInstance();

        }

        private void RegisterServices(ContainerBuilder builder)
        {
            var settings = _appSettings.LimitationsSettings;

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>()
                .SingleInstance();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>()
                .SingleInstance();

            builder.RegisterType<CurrencyConverter>()
                .As<ICurrencyConverter>()
                .SingleInstance()
                .WithParameter("convertibleCurrencies", settings.ConvertibleAssets);

            builder.RegisterType<AntiFraudCollector>()
                .As<IAntiFraudCollector>()
                .SingleInstance()
                .WithParameter("redisInstanceName", settings.RedisInstanceName);

            builder.RegisterType<CashOperationsCollector>()
                .As<ICashOperationsCollector>()
                .SingleInstance()
                .WithParameter("redisInstanceName", settings.RedisInstanceName);

            builder.RegisterType<CashTransfersCollector>()
                .As<ICashTransfersCollector>()
                .SingleInstance()
                .WithParameter("redisInstanceName", settings.RedisInstanceName);

            builder.RegisterType<LimitationChecker>()
                .As<ILimitationCheck>()
                .SingleInstance()
                .WithParameter("limits", settings.Limits)
                .WithParameter("attemptRetainInMinutes", settings.AttemptRetainInMinutes);

            builder.RegisterType<AccumulatedDepositAggregator>()
                .As<IAccumulatedDepositAggregator>()
                .SingleInstance();

        }
    }
}
