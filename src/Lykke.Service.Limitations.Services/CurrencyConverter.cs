using Common.Log;
using Lykke.Service.Limitations.Core.Services;
using Lykke.Service.RateCalculator.Client;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Common.Log;

namespace Lykke.Service.Limitations.Services
{
    public class CurrencyConverter : ICurrencyConverter
    {
        private const string ConversionBaseAsset = "USD";

        private readonly HashSet<string> _convertibleCurrencies;

        private readonly IRateCalculatorClient _rateCalculatorClient;
        private readonly ILog _log;

        public string DefaultAsset => ConversionBaseAsset;

        public CurrencyConverter(
            List<string> convertibleCurrencies,
            IRateCalculatorClient rateCalculatorClient,
            ILogFactory logFactory)
        {
            _convertibleCurrencies = new HashSet<string>(convertibleCurrencies);
            _rateCalculatorClient = rateCalculatorClient;
            _log = logFactory.CreateLog(this);
        }

        public async Task<(string assetTo, double convertedAmount)> ConvertAsync(
            string assetFrom,
            string assetTo,
            double amount,
            bool forceConvesion = false)
        {
            if (assetFrom == assetTo || IsNotConvertible(assetFrom) && !forceConvesion)
                return (assetFrom, amount);

            double convertedAmount = await _rateCalculatorClient.GetAmountInBaseAsync(assetFrom, amount, assetTo);

            if (amount != 0 && convertedAmount == 0)
                _log.Warning(
                    nameof(ConvertAsync),
                    $"Conversion from {amount} {assetFrom} to {assetTo} resulted in 0.");

            return (assetTo, convertedAmount);
        }

        public bool IsNotConvertible(string asset)
        {
            return !_convertibleCurrencies.Contains(asset);
        }
    }
}
