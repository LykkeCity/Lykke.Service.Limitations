using Lykke.Service.Limitations.Core.Domain;

namespace Lykke.Service.Limitations.Services
{
    internal static class CashOperationLimitationExtensions
    {
        internal static bool IsValid(this CashOperationLimitation limit)
        {
            return !string.IsNullOrWhiteSpace(limit.Asset)
                && limit.Limit > 0;
        }
    }
}
