using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Limitations.Client
{
    public class LimitationsServiceClientSettings
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }
    }
}
