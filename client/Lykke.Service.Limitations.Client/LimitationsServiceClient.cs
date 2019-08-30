using Lykke.HttpClientGenerator;
using Lykke.Service.Limitations.Client.Api;

namespace Lykke.Service.Limitations.Client
{
    public class LimitationsServiceClient : ILimitationsServiceClient
    {
        public ILimitationsApi Limitations { get; set; }
        public ISwiftLimitationsApi SwiftLimitations { get; set; }

        public LimitationsServiceClient(IHttpClientGenerator httpClientGenerator)
        {
            Limitations = httpClientGenerator.Generate<ILimitationsApi>();
            SwiftLimitations = httpClientGenerator.Generate<ISwiftLimitationsApi>();
        }
    }
}
