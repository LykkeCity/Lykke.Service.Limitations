using AzureStorage;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using Lykke.Service.Limitations.Core.Services;
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

        public AccumulatedDepositRepository(
            INoSQLTableStorage<AccumulatedDepositPeriodEntity> tableStorage
            )
        {
            _tableStorage = tableStorage;
        }


        public async Task AggregateTotalAsync(string clientId, string assetId, double amount, CurrencyOperationType operationType)
        {
            IAccumulatedDepositPeriod existingRecord = await _tableStorage.GetDataAsync(clientId, GenerateRowKey(operationType));
            if (existingRecord == null)
            {
                AccumulatedDepositPeriodEntity entity = new AccumulatedDepositPeriodEntity();
                entity.PartitionKey = clientId;
                entity.RowKey = GenerateRowKey(operationType);

                entity.ClientId = clientId;
                entity.AssetId = assetId;
                entity.Amount = amount;

                await _tableStorage.InsertAsync(entity);
            }
            else
            {
                await _tableStorage.MergeAsync(clientId, GenerateRowKey(operationType), rowData =>
                {
                    rowData.Amount = Math.Round(rowData.Amount + amount, 15);
                    return rowData;
                });
            }
        }

        private string GenerateRowKey(CurrencyOperationType operationType)
        {
            switch (operationType)
            {
                case CurrencyOperationType.CardCashIn: return String.Format($"AllTime-Cards-CashIn");
                case CurrencyOperationType.SwiftTransfer: return String.Format($"AllTime-Swift-CashIn");
                case CurrencyOperationType.SwiftTransferOut: return String.Format($"AllTime-Swift-CashOut");
                case CurrencyOperationType.CryptoCashOut: return String.Format($"AllTime-Crypto-CashOut");
            }
            throw new ArgumentException("Invalid input value", nameof(operationType));
        }

        public async Task<double> GetAccumulatedAmountAsync(string clientId, CurrencyOperationType operationType)
        {
            switch (operationType)
            {
                case CurrencyOperationType.CardCashIn:
                case CurrencyOperationType.SwiftTransfer:
                case CurrencyOperationType.SwiftTransferOut:
                case CurrencyOperationType.CryptoCashOut:
                    var depositRecord = await _tableStorage.GetDataAsync(clientId, GenerateRowKey(operationType));
                    return depositRecord == null ? 0 : depositRecord.Amount;
            }
            throw new ArgumentException("Invalid input value", nameof(operationType));
        }

    }
}
