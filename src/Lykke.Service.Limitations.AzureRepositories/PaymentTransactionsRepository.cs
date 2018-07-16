using AzureStorage;
using Common.Log;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.AzureRepositories
{
    public class PaymentTransactionsRepository : IPaymentTransactionsRepository
    {
        private readonly INoSQLTableStorage<PaymentTransactionEntity> _tableStorage;

        public PaymentTransactionsRepository(INoSQLTableStorage<PaymentTransactionEntity> tableStorage, ILog log)
        {
            _tableStorage = tableStorage;
        }

        public async Task<IPaymentTransaction> GetByIdForClientAsync(string id, string clientId)
        {
            return await _tableStorage.GetDataAsync(clientId, id);
        }
    }
}
