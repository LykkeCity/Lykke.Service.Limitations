using Autofac;
using AutoMapper;
using AzureStorage.Blob;
using AzureStorage.Tables;
using JetBrains.Annotations;
using Lykke.Common.Cache;
using Lykke.Common.Log;
using Lykke.HttpClientGenerator;
using Lykke.Sdk;
using Lykke.Service.Assets.Client;
using Lykke.Service.Limitations.AzureRepositories;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.JobClient;
using Lykke.Service.Limitations.Core.Repositories;
using Lykke.Service.Limitations.Core.Services;
using Lykke.Service.Limitations.Profiles;
using Lykke.Service.Limitations.Services;
using Lykke.Service.Limitations.Settings;
using Lykke.Service.RateCalculator.Client;
using Lykke.SettingsReader;
using StackExchange.Redis;

namespace Lykke.Service.Limitations.Modules
{
    [UsedImplicitly]
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<AppSettings> _appSettings;

        public ServiceModule(IReloadingManager<AppSettings> appSettings)
        {
            _appSettings = appSettings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            var settings = _appSettings.CurrentValue;

            builder.RegisterInstance(ConnectionMultiplexer.Connect(settings.LimitationsSettings.RedisConfiguration))
                .As<IConnectionMultiplexer>()
                .SingleInstance();

            builder.RegisterRateCalculatorClient(settings.RateCalculatorServiceClient.ServiceUrl);
            builder.RegisterAssetsClient(settings.AssetsServiceClient.ServiceUrl);
            builder.RegisterClient<ILimitOperationsApi>(settings.LimitationsSettings.LimitOperationsJobUrl);

            RegisterRepositories(builder);

            RegisterServices(builder);

            RegisterAutomapper(builder);
        }

        private void RegisterRepositories(ContainerBuilder builder)
        {
            builder.Register(ctx => AzureBlobStorage.Create(_appSettings.ConnectionString(s => s.LimitationsSettings.BlobStorageConnectionString))).SingleInstance();
            builder.Register(ctx => AzureTableStorage<SwiftTransferLimitationEntity>.Create(_appSettings.ConnectionString(s => s.LimitationsSettings.LimitationSettingsConnectionString), "SwiftTransferLimitations", ctx.Resolve<ILogFactory>())).SingleInstance();
            builder.Register(ctx => AzureTableStorage<ApiCallHistoryRecord>.Create(_appSettings.ConnectionString(x => x.LimitationsSettings.Log.ConnectionString), "ApiSuccessfulCalls", ctx.Resolve<ILogFactory>())).SingleInstance();
            builder.Register(ctx => AzureTableStorage<AppGlobalSettingsEntity>.Create(_appSettings.ConnectionString(x => x.LimitationsSettings.GlobalSettingsConnString), "Setup", ctx.Resolve<ILogFactory>())).SingleInstance();

            builder.RegisterType<CashOperationsStateRepository>().As<ICashOperationsRepository>().SingleInstance();
            builder.RegisterType<CashTransfersStateRepository>().As<ICashTransfersRepository>().SingleInstance();
            builder.RegisterType<SwiftTransferLimitationsRepository>().As<ISwiftTransferLimitationsRepository>().SingleInstance();
            builder.RegisterType<CallTimeLimitsRepository>().As<ICallTimeLimitsRepository>().SingleInstance();
            builder.RegisterType<LimitSettingsRepository>().As<ILimitSettingsRepository>().SingleInstance();
        }

        private void RegisterServices(ContainerBuilder builder)
        {
            var settings = _appSettings.CurrentValue.LimitationsSettings;

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>()
                .SingleInstance();

            builder.RegisterType<CurrencyConverter>()
                .As<ICurrencyConverter>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(settings.ConvertibleAssets))
                .WithParameter(TypedParameter.From(settings.BaseAsset));

            builder.RegisterType<AntiFraudCollector>()
                .As<IAntiFraudCollector>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(settings.RedisInstanceName));

            builder.RegisterType<CashOperationsCollector>()
                .As<ICashOperationsCollector>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(settings.RedisInstanceName));

            builder.RegisterType<CashTransfersCollector>()
                .As<ICashTransfersCollector>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(settings.RedisInstanceName));

            builder.RegisterType<LimitationChecker>()
                .As<ILimitationCheck>()
                .SingleInstance()
                .WithParameter(TypedParameter.From(settings.Limits))
                .WithParameter(TypedParameter.From(settings.AttemptRetainInMinutes));

            builder.Register(ctx =>
            {
                var assetService = ctx.Resolve<IAssetsService>();
                var assetsCache = new OnDemandDataCache<Asset>();
                var assets = assetService.AssetGetAll();

                foreach (var asset in assets)
                {
                    assetsCache.Set(asset.Id, new Asset
                    {
                        Id = asset.Id,
                        LowVolumeAmount = asset.LowVolumeAmount,
                        Accuracy = asset.Accuracy,
                        CashoutMinimalAmount = asset.CashoutMinimalAmount
                    });
                }

                return assetsCache;
            }).SingleInstance();
        }

        private void RegisterAutomapper(ContainerBuilder builder)
        {
            builder.Register(c =>
            {
                var mapperConfiguration = new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile(new ServiceProfile());
                });

                mapperConfiguration.AssertConfigurationIsValid();

                return mapperConfiguration.CreateMapper();
            }).As<IMapper>().SingleInstance();
        }
    }
}
