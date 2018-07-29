using System;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Lykke.Service.Limitations.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;

namespace Lykke.Service.Limitations.AzureRepositories
{
    public class ApiCallHistoryRecord : TableEntity
    {
        public static string GeneratePartitionKey(string method, string clientId)
        {
            return $"{method}_{clientId}";
        }

        public static ApiCallHistoryRecord Create(string method, string clientId)
        {
            return new ApiCallHistoryRecord
            {
                PartitionKey = GeneratePartitionKey(method, clientId),
                DateTime = DateTime.UtcNow
            };
        }

        public DateTime DateTime { get; set; }
    }

    public class CallTimeLimitsRepository : ICallTimeLimitsRepository
    {
        private readonly INoSQLTableStorage<ApiCallHistoryRecord> _tableStorage;

        public CallTimeLimitsRepository(INoSQLTableStorage<ApiCallHistoryRecord> tableStorage)
        {
            _tableStorage = tableStorage;
        }

        public Task InsertRecordAsync(string method, string clientId)
        {
            var entity = ApiCallHistoryRecord.Create(method, clientId);
            return _tableStorage.InsertAndGenerateRowKeyAsDateTimeAsync(entity, entity.DateTime);
        }

        public async Task<DateTime[]> GetCallHistoryAsync(string method, string clientId, TimeSpan period)
        {
            var timeNow = DateTime.UtcNow;

            var result =
                await
                    _tableStorage.WhereAsync(ApiCallHistoryRecord.GeneratePartitionKey(method, clientId),
                        timeNow - period, timeNow, ToIntervalOption.IncludeTo, includeTime: true);

            return result.Select(x => x.DateTime).ToArray();
        }

        public async Task<int> GetCallsCount(string method, string clientId)
        {
            var all = await _tableStorage.GetDataAsync(ApiCallHistoryRecord.GeneratePartitionKey(method, clientId));
            return all.Count();
        }

        public async Task ClearCallsHistory(string method, string clientId)
        {
            var all = await _tableStorage.GetDataAsync(ApiCallHistoryRecord.GeneratePartitionKey(method, clientId));
            await _tableStorage.DeleteAsync(all);
        }
    }
}
