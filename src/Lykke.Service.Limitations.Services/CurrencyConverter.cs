using Common.Log;
using Lykke.Service.Limitations.Core.Services;
using Lykke.Service.RateCalculator.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lykke.Service.Limitations.Services
{
    public class CurrencyConverter : ICurrencyConverter
    {
        private const string _conversionBaseAsset = "USD";

        private HashSet<string> _convertibleCurrencies;

        private readonly IRateCalculatorClient _rateCalculatorClient;
        private readonly ILog _log;

        public string DefaultAsset { get { return _conversionBaseAsset; } }

        public CurrencyConverter(
            List<string> convertibleCurrencies,
            IRateCalculatorClient rateCalculatorClient,
            ILog log)
        {
            _convertibleCurrencies = new HashSet<string>(convertibleCurrencies);
            _rateCalculatorClient = rateCalculatorClient;
            _log = log;
        }

        public async Task<Tuple<string, double>> ConvertAsync(
            string assetFrom,
            string assetTo,
            double amount,
            bool forceConvesion = false)
        {
            if (assetFrom == assetTo || IsNotConvertible(assetFrom) && !forceConvesion)
                return new Tuple<string, double>(assetFrom, amount);

            double convertedAmount = await _rateCalculatorClient.GetAmountInBaseAsync(assetFrom, amount, assetTo);

            if (amount != 0 && convertedAmount == 0)
                _log.WriteWarning(
                    nameof(CurrencyConverter),
                    nameof(ConvertAsync),
                    $"Conversion from {amount} {assetFrom} to {assetTo} resulted in 0.");

            return new Tuple<string, double>(assetTo, convertedAmount);
        }

        public bool IsNotConvertible(string asset)
        {
            return !_convertibleCurrencies.Contains(asset);
        }
    }
}
