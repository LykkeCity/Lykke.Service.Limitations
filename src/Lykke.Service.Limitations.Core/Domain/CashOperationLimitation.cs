using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Limitations.Core.Domain
{
    public class CashOperationLimitation
    {
        public LimitationPeriod Period { get; set; }

        public LimitationType LimitationType { get; set; }

        [Optional]
        public string ClientId { get; set; }

        public string Asset { get; set; }

        public double Limit { get; set; }
    }
}
