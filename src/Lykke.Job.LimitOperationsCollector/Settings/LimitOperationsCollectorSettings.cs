using System.Collections.Generic;
using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.LimitOperationsCollector.Settings
{
    [UsedImplicitly]
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
}
