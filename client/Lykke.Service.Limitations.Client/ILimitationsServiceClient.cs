using JetBrains.Annotations;
using Lykke.Service.Limitations.Client.Api;

namespace Lykke.Service.Limitations.Client
{
    [PublicAPI]
    public interface ILimitationsServiceClient
    {
        /// <summary>
        /// Api for limitations
        /// </summary>
        ILimitationsApi Limitations { get; }

        ISwiftLimitationsApi SwiftLimitations { get; }
    }
}
