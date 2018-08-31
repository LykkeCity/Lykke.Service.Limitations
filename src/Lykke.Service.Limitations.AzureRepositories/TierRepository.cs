using AzureStorage;
using Lykke.Common.Log;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.AzureRepositories
{
    public class TierRepository : ITierRepository
    {
        private readonly INoSQLTableStorage<TierEntity> _tableStorage;

        public TierRepository(
            INoSQLTableStorage<TierEntity> tableStorage
            )
        {
            _tableStorage = tableStorage;
        }

        public async Task<string> SaveTierAsync(ITier tier)
        {
            TierEntity e = new TierEntity();
            e.Id = String.IsNullOrWhiteSpace(tier.Id) ? Guid.NewGuid().ToString() : tier.Id;
            e.ShortName = tier.ShortName;
            e.LongName = tier.LongName;

            e.LimitTotalCashIn24Hours = tier.LimitTotalCashIn24Hours;
            e.LimitTotalCashIn30Days = tier.LimitTotalCashIn30Days;
            e.LimitTotalCashInAllTime = tier.LimitTotalCashInAllTime;
            e.LimitTotalCashOut24Hours = tier.LimitTotalCashOut24Hours;
            e.LimitTotalCashOut30Days = tier.LimitTotalCashOut30Days;
            e.LimitTotalCashOutAllTime = tier.LimitTotalCashOutAllTime;

            e.LimitCreditCardsCashIn24Hours= tier.LimitCreditCardsCashIn24Hours;
            e.LimitCreditCardsCashIn30Days= tier.LimitCreditCardsCashIn30Days;
            e.LimitCreditCardsCashInAllTime= tier.LimitCreditCardsCashInAllTime;
            e.LimitCreditCardsCashOut24Hours= tier.LimitCreditCardsCashOut24Hours;
            e.LimitCreditCardsCashOut30Days= tier.LimitCreditCardsCashOut30Days;
            e.LimitCreditCardsCashOutAllTime= tier.LimitCreditCardsCashOutAllTime;

            e.LimitSwiftCashIn24Hours = tier.LimitSwiftCashIn24Hours;
            e.LimitSwiftCashIn30Days = tier.LimitSwiftCashIn30Days;
            e.LimitSwiftCashInAllTime = tier.LimitSwiftCashInAllTime;
            e.LimitSwiftCashOut24Hours = tier.LimitSwiftCashOut24Hours;
            e.LimitSwiftCashOut30Days = tier.LimitSwiftCashOut30Days;
            e.LimitSwiftCashOutAllTime = tier.LimitSwiftCashOutAllTime;

            e.LimitCryptoCashIn24Hours = tier.LimitCryptoCashIn24Hours;
            e.LimitCryptoCashIn30Days = tier.LimitCryptoCashIn30Days;
            e.LimitCryptoCashInAllTime = tier.LimitCryptoCashInAllTime;
            e.LimitCryptoCashOut24Hours = tier.LimitCryptoCashOut24Hours;
            e.LimitCryptoCashOut30Days = tier.LimitCryptoCashOut30Days;
            e.LimitCryptoCashOutAllTime = tier.LimitCryptoCashOutAllTime;

            e.PartitionKey = e.Id;
            e.RowKey = e.Id;

            await _tableStorage.InsertOrReplaceAsync(e);

            return e.Id;
        }

        public async Task<IEnumerable<ITier>> LoadTiersAsync()
        {
            return await _tableStorage.GetDataAsync();
        }

        public async Task<ITier> LoadTierAsync(string id)
        {
            return await _tableStorage.GetDataAsync(id, id);
        }

        public async Task DeleteTierAsync(string id)
        {
            await _tableStorage.MergeAsync(id, id, entity =>
            {
                return entity;
            });
        }

    }
}
