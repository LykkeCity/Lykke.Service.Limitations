
using AzureStorage;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.AzureRepositories
{
    public class ClientTierRepository : IClientTierRepository
    {
        private readonly string _defaultTierKey = "Default";
        private readonly INoSQLTableStorage<ClientTierEntity> _tableStorage;

        public ClientTierRepository(INoSQLTableStorage<ClientTierEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task SetClientTierAsync(string clientId, string tierId)
        {
            var e = new ClientTierEntity();

            e.PartitionKey = clientId;
            e.RowKey = tierId;

            await _tableStorage.InsertOrReplaceAsync(e);
        }

        public async Task<string> GetClientTierIdAsync(string clientId)
        {
            var resultTier = (await _tableStorage.GetDataAsync(clientId, clientId));
            if (resultTier == null) // no tier for a client - try to load a default tier
            {
                resultTier = (await _tableStorage.GetDataAsync(_defaultTierKey, _defaultTierKey));
            }
            return resultTier == null ? null : resultTier.TierId;
        }

        public async Task SetDefaultTierAsync(string tierId)
        {
            var e = new ClientTierEntity();

            e.PartitionKey = _defaultTierKey;
            e.RowKey = _defaultTierKey;
            e.TierId = tierId;

            await _tableStorage.InsertOrReplaceAsync(e);
        }

        public async Task<string> GetDefaultTierIdAsync()
        {
            var e = await _tableStorage.GetDataAsync(_defaultTierKey, _defaultTierKey);
            return e != null ? e.TierId : null;
        }


    }
}
