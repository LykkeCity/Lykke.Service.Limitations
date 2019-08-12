using JetBrains.Annotations;
using Lykke.Sdk.Settings;

namespace Lykke.Service.Limitations.Settings
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class AppSettings : BaseAppSettings
    {
        public LimitationsSettings LimitationsSettings { get; set; }
        public RateCalculatorServiceClient RateCalculatorServiceClient { get; set; }
        public AssetServiceClient AssetsServiceClient { get; set; }
        public RabbitMqSettings SagasRabbitMq { get; set; }
    }
}
