using Lykke.Service.Limitations.Core.Domain;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Core.Repositories
{
    public interface IPaymentTransactionsRepository
    {
        Task<IPaymentTransaction> GetByIdForClientAsync(string id, string clientId);
    }
}
