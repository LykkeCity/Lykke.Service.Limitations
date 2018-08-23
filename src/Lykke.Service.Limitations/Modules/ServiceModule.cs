using Autofac;
using AzureStorage.Blob;
using AzureStorage.Tables;
using Common.Log;
using Lykke.Common.Log;
using Lykke.HttpClientGenerator;
using Lykke.Service.Limitations.AzureRepositories;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.JobClient;
using Lykke.Service.Limitations.Core.Repositories;
using Lykke.Service.Limitations.Core.Services;
using Lykke.Service.Limitations.Services;
using Lykke.Service.Limitations.Settings;
using Lykke.Service.RateCalculator.Client;
using Lykke.SettingsReader;
using StackExchange.Redis;
using System;
using System.Linq;

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

            builder.Register(ctx => AzureTableStorage<AccumulatedDepositPeriodEntity>.Create(_settingsManager.ConnectionString(s => s.DepositAccumulationConnectionString), "AccumulatedDeposits", _log)).SingleInstance();
            builder.RegisterType<AccumulatedDepositRepository>().As<IAccumulatedDepositRepository>().SingleInstance();

            builder.Register(ctx => AzureTableStorage<TierEntity>.Create(_settingsManager.ConnectionString(s => s.DepositAccumulationConnectionString), "Tiers", _log)).SingleInstance();
            builder.RegisterType<TierRepository>().As<ITierRepository>().SingleInstance();

            builder.Register(ctx => AzureTableStorage<ClientTierEntity>.Create(_settingsManager.ConnectionString(s => s.DepositAccumulationConnectionString), "ClientTiers", _log)).SingleInstance();
            builder.RegisterType<ClientTierRepository>().As<IClientTierRepository>().SingleInstance();

            builder.Register(ctx => AzureTableStorage<ClientTierLogEntity>.Create(_settingsManager.ConnectionString(s => s.DepositAccumulationConnectionString), "ClientTierLogs", _log)).SingleInstance();
            builder.RegisterType<ClientTierLogRepository>().As<IClientTierLogRepository>().SingleInstance();


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

            // limits from setting for fiat currency for an individual client will be ignored
            // only tier limits should be used for such purposes
            Func<CashOperationLimitation, bool> isIndividualFiatLimit = limit => !string.IsNullOrWhiteSpace(limit.ClientId) && settings.ConvertibleAssets.Contains(limit.Asset);
            var filteredLimits = settings.Limits.Where(limit => !isIndividualFiatLimit(limit)).ToList();
            builder.RegisterType<LimitationChecker>()
                .As<ILimitationCheck>()
                .SingleInstance()
                .WithParameter("limits", filteredLimits)
                .WithParameter("convertibleCurrencies", settings.ConvertibleAssets)
                .WithParameter("attemptRetainInMinutes", settings.AttemptRetainInMinutes)
                ;

            builder.RegisterType<AccumulatedDepositAggregator>()
                .As<IAccumulatedDepositAggregator>()
                .SingleInstance();

        }
    }
}
