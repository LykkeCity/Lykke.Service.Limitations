using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Limitations.Core.Repositories
{
    public interface IAccumulatedDepositPeriod
    {
        DateTime StartDateTime { get; set; }

        string ClientId { get; set; }
        string AssetId { get; set; }
        double Amount { get; set; }

    }
}
