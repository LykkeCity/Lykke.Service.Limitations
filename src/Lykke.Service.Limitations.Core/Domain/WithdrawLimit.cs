namespace Lykke.Service.Limitations.Core.Domain
{
    public class WithdrawLimit : IWithdrawLimit
    {
        public string AssetId { get; set; }

        public double LimitAmount { get; set; }
    }
}
