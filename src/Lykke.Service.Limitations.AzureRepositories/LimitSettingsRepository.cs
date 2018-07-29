using System;
using System.Threading.Tasks;
using AzureStorage;
using Common;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Limitations.AzureRepositories
{
    // TODO: move to LimitSettingsEntity with only limitation properties
    public class AppGlobalSettingsEntity : TableEntity
    {
        public static string GeneratePartitionKey()
        {
            return "Setup";
        }

        public static string GenerateRowKey()
        {
            return "AppSettings";
        }
        
        public int LowCashOutTimeoutMins { get; set; }
        public int LowCashOutLimit { get; set; }        
    }

    public class LimitSettingsRepository : ILimitSettingsRepository
    {

        private readonly INoSQLTableStorage<AppGlobalSettingsEntity> _tableStorage;

        public LimitSettingsRepository(INoSQLTableStorage<AppGlobalSettingsEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }
        
        public async Task<LimitSettings> GetAsync()
        {
            var partitionKey = AppGlobalSettingsEntity.GeneratePartitionKey();
            var rowKey = AppGlobalSettingsEntity.GenerateRowKey();
            var globalSettings = await _tableStorage.GetDataAsync(partitionKey, rowKey);

            return new LimitSettings
            {
                LowCashOutLimit = globalSettings.LowCashOutLimit,
                LowCashOutTimeoutMins = globalSettings.LowCashOutTimeoutMins
            };
        }
    }
}
