using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Limitations.Settings
{
    [UsedImplicitly]
    public class RabbitMqSettings
    {
        [AmqpCheck]
        public string RabbitConnectionString { get; set; }
    }
}
