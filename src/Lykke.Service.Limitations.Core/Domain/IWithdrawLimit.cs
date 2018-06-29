namespace Lykke.Service.Limitations.Core.Domain
{
    public interface IWithdrawLimit
    {
        string AssetId { get; }
        double LimitAmount { get; }
    }
}
