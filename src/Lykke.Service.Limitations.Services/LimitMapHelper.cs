using Lykke.Service.Limitations.Core.Domain;
using System;
using System.Collections.Generic;

namespace Lykke.Service.Limitations.Services
{
    internal static class LimitMapHelper
    {
        internal static List<CurrencyOperationType> MapLimitationType(LimitationType limitType)
        {
            var result = new List<CurrencyOperationType>();
            switch (limitType)
            {
                case LimitationType.CardCashIn:
                    result.Add(CurrencyOperationType.CardCashIn);
                    break;
                case LimitationType.CryptoCashOut:
                    result.Add(CurrencyOperationType.CryptoCashOut);
                    break;
                case LimitationType.SwiftCashIn:
                    result.Add(CurrencyOperationType.SwiftTransfer);
                    break;
                case LimitationType.SwiftCashOut:
                    result.Add(CurrencyOperationType.SwiftTransferOut);
                    break;
                default:
                    throw new NotSupportedException($"Limitation type {limitType} is not supported!");
            }

            return result;
        }

        internal static List<LimitationType> MapOperationType(CurrencyOperationType currencyOperationType)
        {
            var result = new List<LimitationType>();
            switch (currencyOperationType)
            {
                case CurrencyOperationType.CardCashIn:
                    result.Add(LimitationType.CardCashIn);
                    break;
                case CurrencyOperationType.SwiftTransfer:
                    result.Add(LimitationType.SwiftCashIn);
                    break;
                case CurrencyOperationType.CryptoCashOut:
                    result.Add(LimitationType.CryptoCashOut);
                    break;
                case CurrencyOperationType.CardCashOut:
                case CurrencyOperationType.CryptoCashIn:
                    break;
                case CurrencyOperationType.SwiftTransferOut:
                    result.Add(LimitationType.SwiftCashOut);
                    break;
                default:
                    throw new NotSupportedException($"Currency operation type {currencyOperationType} is not supported!");
            }

            return result;
        }
    }
}

