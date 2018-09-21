using System.Collections.Generic;
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

        Task<double> GetRateToUsd(Dictionary<string, double> cachedRates, string asset, double? rateToUsd);

        bool IsNotConvertible(string asset);

        string DefaultAsset { get; }

    }
}
