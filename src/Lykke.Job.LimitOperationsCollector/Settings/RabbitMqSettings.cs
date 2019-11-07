using JetBrains.Annotations;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.LimitOperationsCollector.Settings
{
    [UsedImplicitly]
    public class RabbitMqSettings
    {
        [AmqpCheck]
        public string ConnectionString { get; set; }

        public string CashOperationsExchangeName { get; set; }

        public string CashTransfersExchangeName { get; set; }
    }
}
