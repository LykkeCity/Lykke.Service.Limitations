using Lykke.Service.Limitations.Core.Domain;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Limitations.AzureRepositories
{
    public class TierEntity : TableEntity, ITier
    {
        public string Id { get; set; }

        public string ShortName { get; set; }
        public string LongName { get; set; }

        public long LimitTotalCashInAllTime { get; set; }
        public long LimitTotalCashIn30Days { get; set; }
        public long LimitTotalCashIn24Hours { get; set; }

        public long LimitTotalCashOutAllTime { get; set; }
        public long LimitTotalCashOut30Days { get; set; }
        public long LimitTotalCashOut24Hours { get; set; }

        public long LimitCreditCardsCashInAllTime { get; set; }
        public long LimitCreditCardsCashIn30Days { get; set; }
        public long LimitCreditCardsCashIn24Hours { get; set; }

        public long LimitCreditCardsCashOutAllTime { get; set; }
        public long LimitCreditCardsCashOut30Days { get; set; }
        public long LimitCreditCardsCashOut24Hours { get; set; }

        public long LimitSwiftCashInAllTime { get; set; }
        public long LimitSwiftCashIn30Days { get; set; }
        public long LimitSwiftCashIn24Hours { get; set; }

        public long LimitSwiftCashOutAllTime { get; set; }
        public long LimitSwiftCashOut30Days { get; set; }
        public long LimitSwiftCashOut24Hours { get; set; }

        public long LimitCryptoCashInAllTime { get; set; }
        public long LimitCryptoCashIn30Days { get; set; }
        public long LimitCryptoCashIn24Hours { get; set; }

        public long LimitCryptoCashOutAllTime { get; set; }
        public long LimitCryptoCashOut30Days { get; set; }
        public long LimitCryptoCashOut24Hours { get; set; }

        [IgnoreProperty]
        public bool IsDefault { get; set; }

    }
}
