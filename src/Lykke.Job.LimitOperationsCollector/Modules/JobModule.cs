using Autofac;
using Common;
using Common.Log;
using AzureStorage.Blob;
using AzureStorage.Tables;
using Lykke.Common.Log;
using Lykke.SettingsReader;
using Lykke.Service.RateCalculator.Client;
using Lykke.Service.Limitations.AzureRepositories;
using Lykke.Service.Limitations.Core.Services;
using Lykke.Service.Limitations.Core.Repositories;
using Lykke.Service.Limitations.Services;
using Lykke.Job.LimitOperationsCollector.Settings;
using Lykke.Job.LimitOperationsCollector.RabbitSubscribers;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace Lykke.Job.LimitOperationsCollector.Modules
{
    public class JobModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;
        
        public JobModule(
            IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(context => ConnectionMultiplexer.Connect(_settings.CurrentValue.LimitOperationsCollectorJob.RedisConfiguration))
                .As<IConnectionMultiplexer>()
                .SingleInstance();
            
            builder.RegisterRateCalculatorClient(_settings.CurrentValue.RateCalculatorServiceClient.ServiceUrl);

            ReagisterRepositories(builder);

            ReagisterServices(builder);

            RegisterRabbitMqSubscribers(builder);
        }

        private void ReagisterRepositories(ContainerBuilder builder)
        {
            builder.RegisterInstance(AzureBlobStorage.Create(_settings.ConnectionString(s => s.LimitOperationsCollectorJob.BlobStorageConnectionString)));

            builder.RegisterType<CashOperationsStateRepository>()
                .As<ICashOperationsRepository>()
                .SingleInstance();            

            builder.RegisterType<CashTransfersStateRepository>()
                .As<ICashTransfersRepository>()                
                .SingleInstance();
            
            builder.Register(ctx => AzureTableStorage<PaymentTransactionEntity>.Create(
                _settings.ConnectionString(s => s.LimitOperationsCollectorJob.PaymentTransactionsConnectionString),
                "PaymentTransactions",
                ctx.Resolve<ILogFactory>())).SingleInstance();

            builder.RegisterType<PaymentTransactionsRepository>()
                .As<IPaymentTransactionsRepository>()                
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
                .WithParameter("convertibleCurrencies", _settings.CurrentValue.LimitOperationsCollectorJob.ConvertibleAssets)
                .SingleInstance();

            builder.RegisterType<AntiFraudCollector>()
                .As<IAntiFraudCollector>()
                .SingleInstance()
                .WithParameter("redisInstanceName", _settings.CurrentValue.LimitOperationsCollectorJob.RedisInstanceName);

            builder.RegisterType<CashOperationsCollector>()
                .As<ICashOperationsCollector>()
                .SingleInstance()
                .WithParameter("redisInstanceName", _settings.CurrentValue.LimitOperationsCollectorJob.RedisInstanceName);

            builder.RegisterType<CashTransfersCollector>()
                .As<ICashTransfersCollector>()
                .SingleInstance()
                .WithParameter("redisInstanceName", _settings.CurrentValue.LimitOperationsCollectorJob.RedisInstanceName);
        }

        private void RegisterRabbitMqSubscribers(ContainerBuilder builder)
        {
            builder.RegisterType<CashOperationSubscriber>()
                .As<IStopable>()
                .As<IStartable>()
                .SingleInstance()
                .AutoActivate()
                .WithParameter("connectionString", _settings.CurrentValue.LimitOperationsCollectorJob.Rabbit.ConnectionString)
                .WithParameter("exchangeName", _settings.CurrentValue.LimitOperationsCollectorJob.Rabbit.CashOperationsExchangeName);

            builder.RegisterType<CashTransferOperationSubscriber>()
                .As<IStopable>()
                .As<IStartable>()
                .SingleInstance()
                .AutoActivate()
                .WithParameter("connectionString", _settings.CurrentValue.LimitOperationsCollectorJob.Rabbit.ConnectionString)
                .WithParameter("exchangeName", _settings.CurrentValue.LimitOperationsCollectorJob.Rabbit.CashTransfersExchangeName);
        }
    }
}
