namespace Lykke.Service.Limitations.Core.Domain
{
    public class Asset
    {
        public string Id { get; set; }
        public int Accuracy { get; set; }
        public double? LowVolumeAmount { get; set; }
        public double CashoutMinimalAmount { get; set; }
    }
}
