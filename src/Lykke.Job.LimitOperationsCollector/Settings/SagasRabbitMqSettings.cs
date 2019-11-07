using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.LimitOperationsCollector.Settings
{
    [UsedImplicitly]
    public class SagasRabbitMqSettings
    {
        [AmqpCheck]
        public string RabbitConnectionString { get; set; }
    }
}
