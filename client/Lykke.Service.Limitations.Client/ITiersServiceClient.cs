using Lykke.Service.Limitations.Client.AutorestClient.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Client
{
    public interface ITiersServiceClient
    {
        Task SetTierToClient(ClientTier clientTier);

        Task SaveTier(Tier tier);

        Task<IEnumerable<Tier>> LoadTiers();

        Task<Tier> LoadTier(string id);

    }
}
