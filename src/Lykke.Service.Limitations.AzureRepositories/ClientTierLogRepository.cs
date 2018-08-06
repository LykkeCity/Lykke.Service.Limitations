
using AzureStorage;
using Common;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.AzureRepositories
{
    public class ClientTierLogRepository : IClientTierLogRepository
    {
        private readonly INoSQLTableStorage<ClientTierLogEntity> _tableStorage;

        public ClientTierLogRepository(INoSQLTableStorage<ClientTierLogEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public async Task WriteLogAsync(string clientId, string oldTierId, string newTierId, string changer)
        {
            var e = new ClientTierLogEntity();
            e.PartitionKey = clientId;
            e.RowKey = (long.MaxValue - DateTime.UtcNow.Ticks).ToString();

            e.DataOld = oldTierId == null ? null : new JsonData { TierId = oldTierId }.ToJson();
            e.DataNew = newTierId == null ? null : new JsonData { TierId = newTierId }.ToJson();
            e.ChangeDate = DateTime.UtcNow;
            e.Changer = changer;

            await _tableStorage.InsertOrReplaceAsync(e);
        }

        public async Task<IEnumerable<IClientTierLogRecord>> GetLogAsync(string clientId)
        {
            return await _tableStorage.GetDataAsync(clientId);
        }

        private class JsonData
        {
            public string TierId { get; set; }
        }
    }
}
