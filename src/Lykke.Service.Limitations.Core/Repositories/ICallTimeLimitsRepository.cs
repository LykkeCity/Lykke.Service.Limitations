using System;
using System.Linq;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Core.Repositories
{
    public interface ICallTimeLimitsRepository
    {
        Task InsertRecordAsync(string method, string clientId);
        Task<DateTime[]> GetCallHistoryAsync(string method, string clientId, TimeSpan period);
        Task<int> GetCallsCount(string method, string clientId);
        Task ClearCallsHistory(string method, string clietnId);
    }

    public static class ApiCallHistoryExt
    {
        public static bool IsCallEnabled(this DateTime[] history, TimeSpan period, int callLimit)
        {
            return history.Length < callLimit || DateTime.UtcNow - history.Last() > period;
        }
    }
}
