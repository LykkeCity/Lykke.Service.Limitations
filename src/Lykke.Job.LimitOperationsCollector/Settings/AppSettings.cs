using System.Collections.Generic;
using Lykke.Sdk.Settings;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.LimitOperationsCollector.Settings
{
    public class AppSettings : BaseAppSettings
    {
        public LimitOperationsCollectorSettings LimitOperationsCollectorJob { get; set; }

        public RateCalculatorServiceClient RateCalculatorServiceClient { get; set; }

        public SagasRabbitMqSettings SagasRabbitMq { get; set; }
    }

    public class RateCalculatorServiceClient
    {
        [HttpCheck("api/isalive")]
        public string ServiceUrl { get; set; }
    }

    public class LimitOperationsCollectorSettings
    {
        [AzureTableCheck]
        public string LogsConnString { get; set; }

        public string RedisConfiguration { get; set; }

        public string RedisInstanceName { get; set; }

        public List<string> ConvertibleAssets { get; set; }

        [AzureBlobCheck]
        public string BlobStorageConnectionString { get; set; }

        [AzureTableCheck]
        public string PaymentTransactionsConnectionString { get; set; }

        public RabbitMqSettings Rabbit { get; set; }
    }

    public class SagasRabbitMqSettings
    {
        public string RabbitConnectionString { get; set; }
    }

    public class RabbitMqSettings
    {
        [AmqpCheck]
        public string ConnectionString { get; set; }

        public string CashOperationsExchangeName { get; set; }

        public string CashTransfersExchangeName { get; set; }
    }
}
