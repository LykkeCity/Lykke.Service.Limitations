using Lykke.Service.Limitations.Core.Repositories;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Limitations.AzureRepositories
{
    public class AccumulatedAmountsPeriodEntity : TableEntity, IAccumulatedAmountsPeriod
    {
        public string ClientId { get; set; }
        public string AssetId { get; set; }
        public double Amount { get; set; }
    }

}
