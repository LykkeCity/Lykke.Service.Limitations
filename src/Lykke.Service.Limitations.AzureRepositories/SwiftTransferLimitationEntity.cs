using Lykke.Service.Limitations.Core.Domain;
using Microsoft.WindowsAzure.Storage.Table;
using System.Globalization;

namespace Lykke.Service.Limitations.AzureRepositories
{
    public class SwiftTransferLimitationEntity : TableEntity
    {
        public string Asset { get; set; }

        public string MinimalWithdraw { get; set; }

        public static SwiftTransferLimitationEntity Create(string partition, SwiftTransferLimitation limitation)
        {
            return new SwiftTransferLimitationEntity
            {
                PartitionKey = partition,
                RowKey = limitation.Asset,
                Asset = limitation.Asset,
                MinimalWithdraw = limitation.MinimalWithdraw.ToString(CultureInfo.InvariantCulture)
            };
        }

        public bool UpdateFrom(SwiftTransferLimitation limitation)
        {
            MinimalWithdraw = limitation.MinimalWithdraw.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        public SwiftTransferLimitation ToModel()
        {
            return new SwiftTransferLimitation
            {
                Asset = Asset,
                MinimalWithdraw = decimal.Parse(MinimalWithdraw, CultureInfo.InvariantCulture)
            };
        }
    }
}
