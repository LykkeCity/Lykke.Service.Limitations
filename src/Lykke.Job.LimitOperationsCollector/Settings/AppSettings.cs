using JetBrains.Annotations;
using Lykke.Sdk.Settings;

namespace Lykke.Job.LimitOperationsCollector.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AppSettings : BaseAppSettings
    {
        public LimitOperationsCollectorSettings LimitOperationsCollectorJob { get; set; }

        public RateCalculatorServiceClient RateCalculatorServiceClient { get; set; }

        public SagasRabbitMqSettings SagasRabbitMq { get; set; }
    }
}
