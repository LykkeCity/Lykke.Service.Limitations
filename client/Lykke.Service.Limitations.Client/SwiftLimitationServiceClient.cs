using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Lykke.Service.Limitations.Client.AutorestClient;
using Lykke.Service.Limitations.Client.AutorestClient.Models;

namespace Lykke.Service.Limitations.Client
{
    public class SwiftLimitationServiceClient : ISwiftLimitationServiceClient, IDisposable
    {
        private readonly LykkelimitationsService _service;

        public SwiftLimitationServiceClient(string serviceUrl)
        {
            if (string.IsNullOrWhiteSpace(serviceUrl))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(serviceUrl));

            _service = new LykkelimitationsService(new Uri(serviceUrl), new HttpClient());
        }

        public void Dispose()
        {
            _service.Dispose();
        }

        public async Task<IReadOnlyCollection<SwiftTransferLimitation>> GetAllAsync()
        {
            var result = await _service.ApiSwiftLimitationsGetAsync();

            return result.ToArray();
        }

        public Task<SwiftTransferLimitation> GetAsync(string asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            return _service.ApiSwiftLimitationsByAssetGetAsync(asset);
        }

        public Task SaveAsync(SwiftTransferLimitation limitation)
        {
            return SaveRangeAsync(new[] { limitation });
        }

        public Task SaveRangeAsync(IEnumerable<SwiftTransferLimitation> limitations)
        {
            if (limitations == null)
            {
                throw new ArgumentNullException(nameof(limitations));
            }

            var request = limitations.ToArray();

            if (request.Contains(null))
            {
                throw new ArgumentException("Limitaion cant be null", nameof(limitations));
            }

            if (request.Any(x => string.IsNullOrWhiteSpace(x.Asset)))
            {
                throw new ArgumentException("Limitaion asset cant be null or white space", nameof(limitations));
            }

            return request.Length == 0
                ? Task.CompletedTask
                : _service.ApiSwiftLimitationsPostWithHttpMessagesAsync(request);
        }

        public Task DeleteIfExistAsync(string asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            return _service.ApiSwiftLimitationsByAssetDeleteWithHttpMessagesAsync(asset);
        }
    }
}
