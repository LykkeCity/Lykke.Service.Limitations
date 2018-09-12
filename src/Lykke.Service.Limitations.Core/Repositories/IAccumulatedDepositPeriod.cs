using System;
using System.Collections.Generic;
using System.Text;

namespace Lykke.Service.Limitations.Core.Repositories
{
    public interface IAccumulatedAmountsPeriod
    {
        string ClientId { get; set; }
        string AssetId { get; set; }
        double Amount { get; set; }

    }
}
