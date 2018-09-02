using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using AzureStorage;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;

namespace Lykke.Service.Limitations.AzureRepositories
{
    public class SwiftTransferLimitationsRepository : ISwiftTransferLimitationsRepository
    {
        private static string GetPartitionKey() => "SwiftTransferLimitationEntity";

        private static string GetRowKey(string asset) => asset;

        private static string GetRowKey(SwiftTransferLimitation limitation) => limitation.Asset;

        private readonly INoSQLTableStorage<SwiftTransferLimitationEntity> _tableStorage;
        private readonly ILog _log;

        public SwiftTransferLimitationsRepository(INoSQLTableStorage<SwiftTransferLimitationEntity> tableStorage, ILogFactory logFactory)
        {
            _tableStorage = tableStorage;
            _log = logFactory.CreateLog(this);
        }

        public async Task<IReadOnlyCollection<SwiftTransferLimitation>> GetAllAsync()
        {
            var rows = await _tableStorage.GetDataAsync(GetPartitionKey());

            return rows.Select(x => x.ToModel()).ToList();
        }

        public async Task<SwiftTransferLimitation> GetAsync(string asset)
        {
            var row = await _tableStorage.GetDataAsync(GetPartitionKey(), GetRowKey(asset));

            return row?.ToModel();
        }

        public async Task DeleteIfExistAsync(string asset)
        {
            await _tableStorage.DeleteIfExistAsync(GetPartitionKey(), GetRowKey(asset));
        }

        public async Task SaveRangeAsync(IEnumerable<SwiftTransferLimitation> limitations)
        {
            foreach (var limitation in limitations)
            {
                try
                {
                    await _tableStorage.InsertOrModifyAsync(
                        GetPartitionKey(),
                        GetRowKey(limitation),
                        () => SwiftTransferLimitationEntity.Create(GetPartitionKey(), limitation),
                        existing => existing.UpdateFrom(limitation)
                    );
                }
                catch (Exception ex)
                {
                    _log.Error(ex);
                }
            }
        }
    }
}
