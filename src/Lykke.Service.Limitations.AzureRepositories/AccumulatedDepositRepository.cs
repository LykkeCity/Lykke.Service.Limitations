using AzureStorage;
using Lykke.Service.Limitations.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.AzureRepositories
{
    public class AccumulatedDepositRepository : IAccumulatedDepositRepository
    {
        private readonly INoSQLTableStorage<AccumulatedDepositPeriodEntity> _tableStorage;

        public AccumulatedDepositRepository(INoSQLTableStorage<AccumulatedDepositPeriodEntity> tableStorage)
        {
            _tableStorage = tableStorage;
        }


        public async Task AggregateTotalAsync(string clientId, string assetId, double amount)
        {
            IAccumulatedDepositPeriod existingPeriod = await _tableStorage.GetDataAsync(clientId, GenerateRowKey(assetId));
            if (existingPeriod == null)
            {
                AccumulatedDepositPeriodEntity entity = new AccumulatedDepositPeriodEntity();
                entity.PartitionKey = clientId;
                entity.RowKey = GenerateRowKey(assetId);

                entity.ClientId = clientId;
                entity.AssetId = assetId;
                entity.Amount = amount;

                await _tableStorage.InsertAsync(entity);
            }
            else
            {
                await _tableStorage.MergeAsync(clientId, GenerateRowKey(assetId), rowData =>
                {
                    rowData.Amount = Math.Round(amount, 15);
                    return rowData;
                });
            }
        }

        private string GenerateRowKey(string assetId)
        {
            return String.Format($"AllTime-{assetId}");
        }

    }
}
