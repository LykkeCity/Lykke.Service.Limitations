using System.Collections.Generic;
using JetBrains.Annotations;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Limitations.Settings
{
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

        [HttpCheck("api/isalive")]
        public string LimitOperationsJobUrl { get; set; }

        public int AttemptRetainInMinutes { get; set; }

        public List<CashOperationLimitation> Limits { get; set; }

        public List<string> ConvertibleAssets { get; set; }
    }
}
