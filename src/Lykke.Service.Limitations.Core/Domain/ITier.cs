using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Limitations.Core.Domain
{
    public interface ITier
    {
        string Id { get; set; }

        string ShortName { get; set; }
        string LongName { get; set; }

        long LimitTotalCashInAllTime { get; set; }
        long LimitTotalCashIn30Days { get; set; }
        long LimitTotalCashIn24Hours { get; set; }

        long LimitTotalCashOutAllTime { get; set; }
        long LimitTotalCashOut30Days { get; set; }
        long LimitTotalCashOut24Hours { get; set; }

        long LimitCreditCardsCashInAllTime { get; set; }
        long LimitCreditCardsCashIn30Days { get; set; }
        long LimitCreditCardsCashIn24Hours { get; set; }

        long LimitCreditCardsCashOutAllTime { get; set; }
        long LimitCreditCardsCashOut30Days { get; set; }
        long LimitCreditCardsCashOut24Hours { get; set; }

        long LimitSwiftCashInAllTime { get; set; }
        long LimitSwiftCashIn30Days { get; set; }
        long LimitSwiftCashIn24Hours { get; set; }

        long LimitSwiftCashOutAllTime { get; set; }
        long LimitSwiftCashOut30Days { get; set; }
        long LimitSwiftCashOut24Hours { get; set; }

        long LimitCryptoCashInAllTime { get; set; }
        long LimitCryptoCashIn30Days { get; set; }
        long LimitCryptoCashIn24Hours { get; set; }

        long LimitCryptoCashOutAllTime { get; set; }
        long LimitCryptoCashOut30Days { get; set; }
        long LimitCryptoCashOut24Hours { get; set; }

        bool IsDefault { get; set; }

    }
}
