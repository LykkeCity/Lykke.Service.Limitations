using System;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Core.Services
{
    public interface ICurrencyConverter
    {
        Task<(string assetTo, double convertedAmount)> ConvertAsync(
            string assetFrom,
            string assetTo,
            double amount,
            bool forceConvesion = false);

        bool IsNotConvertible(string asset);

        string DefaultAsset { get; }
    }
}
