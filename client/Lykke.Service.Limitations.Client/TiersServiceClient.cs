using Lykke.Service.Limitations.Client.AutorestClient;
using Lykke.Service.Limitations.Client.AutorestClient.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Client
{
    public class TiersServiceClient : ITiersServiceClient, IDisposable
    {
        private readonly LykkelimitationsService _service;

        public TiersServiceClient(string serviceUrl)
        {
            if (string.IsNullOrWhiteSpace(serviceUrl))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(serviceUrl));

            _service = new LykkelimitationsService(new Uri(serviceUrl), new HttpClient());
        }

        public void Dispose()
        {
            _service.Dispose();
        }

        public async Task SetTierToClient(ClientTier clientTier)
        {
            await _service.ApiTiersSetTierToClientPostAsync(clientTier);
        }

        public async Task SaveTier(Tier tier)
        {
            await _service.ApiTiersSaveTierPostAsync(tier);
        }

        public async Task<IEnumerable<Tier>> LoadTiers()
        {
            return await _service.ApiTiersLoadTiersPostAsync();
        }

        public async Task<Tier> LoadTier(string id)
        {
            return await _service.ApiTiersLoadTierPostAsync(id);
        }

    }
}
