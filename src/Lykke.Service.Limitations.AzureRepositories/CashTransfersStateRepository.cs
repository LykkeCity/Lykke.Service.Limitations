using AzureStorage;
using Common.Log;
using Lykke.Service.Limitations.Core.Domain;
using Lykke.Service.Limitations.Core.Repositories;

namespace Lykke.Service.Limitations.AzureRepositories
{
    public class CashTransfersStateRepository : ClientsCashOperationsRepositoryBase<CashTransferOperation>, ICashTransfersRepository
    {
        public CashTransfersStateRepository(IBlobStorage blobStorage, ILog log)
            : base(blobStorage, log)
        {
        }
    }
}
