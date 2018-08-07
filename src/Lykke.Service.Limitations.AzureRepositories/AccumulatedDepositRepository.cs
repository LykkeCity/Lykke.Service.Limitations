using AzureStorage;
using Lykke.Service.Limitations.Core.Domain;
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


        public async Task AggregateTotalAsync(string clientId, string assetId, double amount, CurrencyOperationType operationType)
        {
            IAccumulatedDepositPeriod existingRecord = await _tableStorage.GetDataAsync(clientId, GenerateRowKey(assetId, operationType));
            if (existingRecord == null)
            {
                AccumulatedDepositPeriodEntity entity = new AccumulatedDepositPeriodEntity();
                entity.PartitionKey = clientId;
                entity.RowKey = GenerateRowKey(assetId, operationType);

                entity.ClientId = clientId;
                entity.AssetId = assetId;
                entity.Amount = amount;

                await _tableStorage.InsertAsync(entity);
            }
            else
            {
                await _tableStorage.MergeAsync(clientId, GenerateRowKey(assetId, operationType), rowData =>
                {
                    rowData.Amount = Math.Round(rowData.Amount + amount, 15);
                    return rowData;
                });
            }
        }

        private string GenerateRowKey(string assetId, CurrencyOperationType operationType)
        {
            switch (operationType)
            {
                case CurrencyOperationType.CardCashIn:
                    return String.Format($"AllTime-Cards");
                case CurrencyOperationType.SwiftTransfer:
                    return String.Format($"AllTime-Swift");
            }
            throw new ArgumentException("Invalid input value", nameof(operationType));
        }

        public async Task<IEnumerable<IAccumulatedDepositPeriod>> GetAccumulatedDepositsAsync(string clientId)
        {
            return await _tableStorage.GetDataAsync(clientId);
        }

    }
}
