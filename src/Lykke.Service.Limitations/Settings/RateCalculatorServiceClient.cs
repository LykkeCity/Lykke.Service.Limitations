using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Limitations.Settings
{
    [UsedImplicitly]
    public class RateCalculatorServiceClient
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }
    }
}
