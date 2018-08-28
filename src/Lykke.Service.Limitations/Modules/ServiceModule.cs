﻿using Autofac;
using AzureStorage.Blob;
using AzureStorage.Tables;
using Lykke.Common.Cache;
using Lykke.Common.Log;
using Lykke.HttpClientGenerator;
using Lykke.Service.Assets.Client;
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
        private readonly IReloadingManager<AppSettings> _appSettings;
        
        public ServiceModule(IReloadingManager<AppSettings> appSettings)
        {
            _appSettings = appSettings;            
        }

        protected override void Load(ContainerBuilder builder)
        {
            var settings = _appSettings.CurrentValue;

            builder.Register(context => ConnectionMultiplexer.Connect(settings.LimitationsSettings.RedisConfiguration))
                .As<IConnectionMultiplexer>()
                .SingleInstance();
            
            builder.RegisterRateCalculatorClient(settings.RateCalculatorServiceClient.ServiceUrl);
            builder.RegisterAssetsClient(AssetServiceSettings.Create(new Uri(settings.AssetsServiceClient.ServiceUrl), TimeSpan.MaxValue));
            builder.RegisterClient<ILimitOperationsApi>(settings.LimitationsSettings.LimitOperationsJobUrl);

            RegisterRepositories(builder);

            RegisterServices(builder);
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


            builder.Register(ctx => AzureTableStorage<AccumulatedDepositPeriodEntity>.Create(_appSettings.ConnectionString(x => x.LimitationsSettings.DepositAccumulationConnectionString), "Setup", ctx.Resolve<ILogFactory>())).SingleInstance();
            builder.RegisterType<AccumulatedDepositRepository>().As<IAccumulatedDepositRepository>().SingleInstance();

            builder.Register(ctx => AzureTableStorage<TierEntity>.Create(_appSettings.ConnectionString(s => s.LimitationsSettings.DepositAccumulationConnectionString), "Tiers", ctx.Resolve<ILogFactory>())).SingleInstance();
            builder.RegisterType<TierRepository>().As<ITierRepository>().SingleInstance();

            builder.Register(ctx => AzureTableStorage<ClientTierEntity>.Create(_appSettings.ConnectionString(s => s.LimitationsSettings.DepositAccumulationConnectionString), "ClientTiers", ctx.Resolve<ILogFactory>())).SingleInstance();
            builder.RegisterType<ClientTierRepository>().As<IClientTierRepository>().SingleInstance();

            builder.Register(ctx => AzureTableStorage<ClientTierLogEntity>.Create(_appSettings.ConnectionString(s => s.LimitationsSettings.DepositAccumulationConnectionString), "ClientTierLogs", ctx.Resolve<ILogFactory>())).SingleInstance();
            builder.RegisterType<ClientTierLogRepository>().As<IClientTierLogRepository>().SingleInstance();


        }

        private void RegisterServices(ContainerBuilder builder)
        {
            var settings = _appSettings.CurrentValue.LimitationsSettings;

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
                .WithParameter("attemptRetainInMinutes", settings.AttemptRetainInMinutes);

            builder.RegisterType<AccumulatedDepositAggregator>()
                .As<IAccumulatedDepositAggregator>()
                .SingleInstance();


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
    }
}
