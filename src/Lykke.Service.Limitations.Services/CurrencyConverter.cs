using Common.Log;
using Lykke.Service.Limitations.Core.Services;
using Lykke.Service.RateCalculator.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Common.Log;

namespace Lykke.Service.Limitations.Services
{
    public class CurrencyConverter : ICurrencyConverter
    {
        private const string ConversionBaseAsset = "USD";
        private const string InternalBitcoinAsset = "BTC";
        private const string InternalEthereumAsset = "ETH";

        private readonly HashSet<string> _convertibleCurrencies;

        private readonly IRateCalculatorClient _rateCalculatorClient;
        private readonly ILog _log;

        public string DefaultAsset => ConversionBaseAsset;
        public string BitcoinAsset => InternalBitcoinAsset;
        public string EthereumAsset => InternalEthereumAsset;


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

        public async Task<double> GetRateToUsd(Dictionary<string, double> cachedRates, string asset, double? rateToUsd)
        {
            if (rateToUsd != null && rateToUsd.HasValue)
            {
                return rateToUsd.Value;
            }

            if (asset != DefaultAsset)
            {
                double rate = 1;
                if (!cachedRates.TryGetValue(asset, out rate))
                {
                    var (convertedAsset, convertedRate) = await ConvertAsync(asset, DefaultAsset, 1, forceConvesion: true);

                    if (convertedRate == 0) // cross conversion try via BTC
                    {
                        (convertedAsset, convertedRate) = await ConvertAsync(asset, BitcoinAsset, 1, forceConvesion: true);
                        if (convertedRate != 0)
                        {
                            double btcRate = await GetRateToUsd(cachedRates, BitcoinAsset, null);
                            convertedRate = convertedRate * btcRate;
                        }
                    }

                    if (convertedRate == 0) // cross conversion try via ETH
                    {
                        (convertedAsset, convertedRate) = await ConvertAsync(asset, EthereumAsset, 1, forceConvesion: true);
                        if (convertedRate != 0)
                        {
                            double ethRate = await GetRateToUsd(cachedRates, EthereumAsset, null);
                            convertedRate = convertedRate * ethRate;
                        }
                    }

                    cachedRates[asset] = convertedRate;
                    rate = convertedRate;
                }
                return rate;
            }
            else
            {
                return 1;
            }
        }

    }
}
