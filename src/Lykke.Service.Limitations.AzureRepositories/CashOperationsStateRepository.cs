using AzureStorage;
using Common.Log;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;

namespace Lykke.Service.Limitations.AzureRepositories
{
    public class CashOperationsStateRepository : ClientsCashOperationsRepositoryBase<CashOperation>, ICashOperationsRepository
    {
        public CashOperationsStateRepository(IBlobStorage blobStorage, ILog log)
            : base(blobStorage, log)
        {
        }
    }
}
