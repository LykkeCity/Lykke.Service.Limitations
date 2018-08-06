using JetBrains.Annotations;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.SettingsReader.Attributes;
using System.Collections.Generic;

namespace Lykke.Service.Limitations.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AppSettings
    {
        public LimitationsSettings LimitationsSettings { get; set; }

        public SlackNotificationsSettings SlackNotifications { get; set; }

        public MonitoringServiceClientSettings MonitoringServiceClient { get; set; }

        public RateCalculatorServiceClient RateCalculatorServiceClient { get; set; }
    }

    public class SlackNotificationsSettings
    {
        public AzureQueuePublicationSettings AzureQueue { get; set; }
    }

    public class AzureQueuePublicationSettings
    {
        public string ConnectionString { get; set; }

        public string QueueName { get; set; }
    }

    public class MonitoringServiceClientSettings
    {
        [HttpCheck("api/isalive", false)]
        public string MonitoringServiceUrl { get; set; }
    }

    public class RateCalculatorServiceClient
    {
        [HttpCheck("api/isalive")]
        public string ServiceUrl { get; set; }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class LimitationsSettings
    {
        public AzureTableSettings Log { get; set; }

        public string RedisConfiguration { get; set; }

        public string RedisInstanceName { get; set; }

        [AzureBlobCheck]
        public string BlobStorageConnectionString { get; set; }

        [AzureTableCheck]
        public string LimitationSettingsConnectionString { get; set; }

        [AzureTableCheck]
        public string DepositAccumulationConnectionString { get; set; }

        [HttpCheck("api/isalive", false)]
        public string LimitOperationsJobUrl { get; set; }

        public int AttemptRetainInMinutes { get; set; }

        public List<CashOperationLimitation> Limits { get; set; }

        public List<string> ConvertibleAssets { get; set; }
    }

    public class AzureTableSettings
    {
        [AzureTableCheck]
        public string ConnectionString { get; set; }

        public string TableName { get; set; }
    }
}
