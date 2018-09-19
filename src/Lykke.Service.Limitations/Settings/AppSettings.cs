using System;
using JetBrains.Annotations;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.SettingsReader.Attributes;
using System.Collections.Generic;
using Lykke.Sdk.Settings;

namespace Lykke.Service.Limitations.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AppSettings : BaseAppSettings
    {
        public LimitationsSettings LimitationsSettings { get; set; }
        
        public RateCalculatorServiceClient RateCalculatorServiceClient { get; set; }

        public AssetServiceClient AssetsServiceClient { get; set; }

        public RabbitMqSagasSettings SagasRabbitMq { get; set; }
    }

    public class AssetServiceClient
    {
        public string ServiceUrl { get; set; }        
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
        public string GlobalSettingsConnString { get; set; }

        [AzureTableCheck]
        public string TiersConnectionString { get; set; }

        [HttpCheck("api/isalive", false)]
        public string LimitOperationsJobUrl { get; set; }

        public int AttemptRetainInMinutes { get; set; }

        public List<string> ConvertibleAssets { get; set; }
    }


    public class AzureTableSettings
    {
        [AzureTableCheck]
        public string ConnectionString { get; set; }        
    }

    public class RabbitMqSagasSettings
    {
        [AmqpCheck]
        public string RabbitConnectionString { get; set; }

    }

}
