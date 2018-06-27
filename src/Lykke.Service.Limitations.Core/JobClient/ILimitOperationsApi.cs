using JetBrains.Annotations;
using Lykke.Service.Limitations.Core.Domain;
using Refit;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Core.JobClient
{
    /// <summary>
    /// LimitOperationsCollector job API.
    /// </summary>
    [PublicAPI]
    public interface ILimitOperationsApi
    {
        /// <summary>
        /// Deletes client dialog
        /// </summary>
        /// <param name="clientId">Client id.</param>
        /// <param name="assetId">Asset id.</param>
        /// <param name="amount">Amount.</param>
        /// <param name="ttlInMinutes">Attempt time to live in minutes.</param>
        /// <param name="currencyOperationType">Currency operation type.</param>
        [Post("/api/Operations/operationattempt")]
        Task AddOperationAttemptAsync(
            string clientId,
            string assetId,
            double amount,
            int ttlInMinutes,
            CurrencyOperationType currencyOperationType);

        /// <summary>
        /// Removes client operation from limitations processing.
        /// </summary>
        /// <param name="clientId">Client id.</param>
        /// <param name="operationId">Operation id.</param>
        [Delete("/api/Operations/removeoperation")]
        Task RemoveOperationAsync(string clientId, string operationId);

        /// <summary>
        /// Caches client data from persistent storage, if needed.
        /// </summary>
        /// <param name="clientId">Client id.</param>
        [Post("/api/Operations/cacheclientdata")]
        Task CacheClientDataAsync(string clientId);
    }
}
